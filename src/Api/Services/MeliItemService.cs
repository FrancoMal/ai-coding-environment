using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MeliItemService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly MeliAccountService _accountService;
    private readonly AuditLogService _auditLog;

    public MeliItemService(AppDbContext db, IHttpClientFactory httpFactory, MeliAccountService accountService, AuditLogService auditLog)
    {
        _db = db;
        _httpFactory = httpFactory;
        _accountService = accountService;
        _auditLog = auditLog;
    }

    public async Task<MeliItemsResponse> GetItemsAsync(int? meliAccountId = null, string? status = null)
    {
        var query = _db.MeliItems
            .Include(i => i.MeliAccount)
            .AsQueryable();

        if (meliAccountId.HasValue)
            query = query.Where(i => i.MeliAccountId == meliAccountId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.Title)
            .Select(i => new MeliItemDto(
                i.Id, i.MeliItemId, i.MeliAccountId,
                i.MeliAccount != null ? i.MeliAccount.Nickname : "Desconocida",
                i.Title, i.CategoryId, i.CategoryPath, i.Price, i.OriginalPrice, i.CurrencyId,
                i.AvailableQuantity, i.SoldQuantity, i.Status,
                i.Condition, i.ListingTypeId, i.InstallmentTag, i.FreeShipping, i.Thumbnail, i.Permalink,
                i.Sku, i.UserProductId, i.FamilyId, i.FamilyName,
                i.DateCreated, i.LastUpdated))
            .ToListAsync();

        return new MeliItemsResponse(items, total);
    }

    public async Task<MeliItemDto> UpdateItemAsync(string meliItemId, UpdateMeliItemRequest request)
    {
        var item = await _db.MeliItems
            .Include(i => i.MeliAccount)
            .FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);

        if (item is null)
            throw new Exception($"Item {meliItemId} no encontrado");

        if (item.MeliAccount is null)
            throw new Exception($"Cuenta asociada no encontrada para {meliItemId}");

        var token = await _accountService.GetValidTokenAsync(item.MeliAccount);
        if (token is null)
            throw new Exception("Token expirado. Reconecta la cuenta de MercadoLibre.");

        // Capture old values for audit
        var oldTitle = item.Title;
        var oldPrice = item.Price;
        var oldStock = item.AvailableQuantity;
        var oldStatus = item.Status;

        // Build payload with only changed fields
        var payload = new Dictionary<string, object>();
        if (request.Title is not null) payload["title"] = request.Title;
        if (request.Price.HasValue) payload["price"] = request.Price.Value;
        if (request.AvailableQuantity.HasValue) payload["available_quantity"] = request.AvailableQuantity.Value;
        if (request.Status is not null) payload["status"] = request.Status;

        if (payload.Count > 0)
        {
            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PutAsync($"https://api.mercadolibre.com/items/{meliItemId}", content);

            // Si MeLi devuelve 401/403, intentar refrescar el token y reintentar una vez
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var newToken = await _accountService.GetValidTokenAsync(item.MeliAccount, forceRefresh: true);
                if (newToken is not null)
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await http.PutAsync($"https://api.mercadolibre.com/items/{meliItemId}", content);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de MercadoLibre ({response.StatusCode}): {errorBody}");
            }

            // Update local DB only after MeLi API success
            if (request.Title is not null) item.Title = request.Title;
            if (request.Price.HasValue) item.Price = request.Price.Value;
            if (request.AvailableQuantity.HasValue) item.AvailableQuantity = request.AvailableQuantity.Value;
            if (request.Status is not null) item.Status = request.Status;
            item.UpdatedAt = DateTime.UtcNow;
            item.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Audit log
            var changes = new Dictionary<string, object>();
            if (request.Title is not null && request.Title != oldTitle)
                changes["Titulo"] = new { old = oldTitle, @new = request.Title };
            if (request.Price.HasValue && request.Price.Value != oldPrice)
                changes["Precio"] = new { old = oldPrice, @new = request.Price.Value };
            if (request.AvailableQuantity.HasValue && request.AvailableQuantity.Value != oldStock)
                changes["Stock"] = new { old = oldStock, @new = request.AvailableQuantity.Value };
            if (request.Status is not null && request.Status != oldStatus)
                changes["Estado"] = new { old = oldStatus, @new = request.Status };

            if (changes.Count > 0)
            {
                var changesJson = JsonSerializer.Serialize(changes);
                await _auditLog.LogAsync("MeliItem", meliItemId, "UPDATE", changesJson);
            }
        }

        var nickname = item.MeliAccount?.Nickname ?? "Desconocida";
        return new MeliItemDto(
            item.Id, item.MeliItemId, item.MeliAccountId, nickname,
            item.Title, item.CategoryId, item.CategoryPath, item.Price, item.OriginalPrice, item.CurrencyId,
            item.AvailableQuantity, item.SoldQuantity, item.Status,
            item.Condition, item.ListingTypeId, item.InstallmentTag, item.FreeShipping, item.Thumbnail, item.Permalink,
            item.Sku, item.UserProductId, item.FamilyId, item.FamilyName,
            item.DateCreated, item.LastUpdated);
    }

    public async Task<List<ItemPromotionDto>> GetItemPromotionsAsync(string meliItemId)
    {
        var item = await _db.MeliItems
            .Include(i => i.MeliAccount)
            .FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);

        if (item is null)
            throw new Exception($"Item {meliItemId} no encontrado");
        if (item.MeliAccount is null)
            throw new Exception($"Cuenta asociada no encontrada para {meliItemId}");

        var token = await _accountService.GetValidTokenAsync(item.MeliAccount);
        if (token is null)
            throw new Exception("Token expirado. Reconecta la cuenta de MercadoLibre.");

        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = item.MeliAccount.MeliUserId.ToString();

        // Try URL patterns - the one without /marketplace/ prefix works
        var urlsToTry = new[]
        {
            $"https://api.mercadolibre.com/seller-promotions/items/{meliItemId}?app_version=v2",
            $"https://api.mercadolibre.com/marketplace/seller-promotions/items/{meliItemId}?user_id={userId}&app_version=v2",
        };

        string? promoJson = null;

        foreach (var promoUrl in urlsToTry)
        {
            using var promoRequest = new HttpRequestMessage(HttpMethod.Get, promoUrl);
            promoRequest.Headers.Add("version", "v2");
            promoRequest.Headers.Add("x-caller-id", userId);
            var promoResponse = await http.SendAsync(promoRequest);

            if (promoResponse.IsSuccessStatusCode)
            {
                promoJson = await promoResponse.Content.ReadAsStringAsync();
                break;
            }

            // On 401/403 refresh token once and retry same URL
            if (promoResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                promoResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var newToken = await _accountService.GetValidTokenAsync(item.MeliAccount, forceRefresh: true);
                if (newToken is not null)
                {
                    token = newToken;
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, promoUrl);
                    retryRequest.Headers.Add("version", "v2");
                    retryRequest.Headers.Add("x-caller-id", userId);
                    var retryResponse = await http.SendAsync(retryRequest);

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        promoJson = await retryResponse.Content.ReadAsStringAsync();
                        break;
                    }
                }
            }
        }

        if (promoJson is null)
            return new List<ItemPromotionDto>();

        var promoDoc = JsonDocument.Parse(promoJson).RootElement;

        var result = new List<ItemPromotionDto>();

        if (promoDoc.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var promo in promoDoc.EnumerateArray())
        {
            var promoId = promo.TryGetProperty("id", out var pid) ? pid.GetString() ?? "" : "";
            var promoType = promo.TryGetProperty("type", out var pt) ? pt.GetString() ?? "" : "";
            var promoStatus = promo.TryGetProperty("status", out var ps) ? ps.GetString() ?? "" : "";
            var promoName = promo.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "";

            DateTime? startDate = promo.TryGetProperty("start_date", out var sd) && sd.ValueKind != JsonValueKind.Null
                ? sd.GetDateTime() : null;
            DateTime? finishDate = promo.TryGetProperty("finish_date", out var fd) && fd.ValueKind != JsonValueKind.Null
                ? fd.GetDateTime() : null;

            // Parse percentage breakdown and prices directly from the first response
            decimal? meliPct = promo.TryGetProperty("meli_percentage", out var mp) && mp.ValueKind == JsonValueKind.Number
                ? mp.GetDecimal() : null;
            decimal? sellerPct = promo.TryGetProperty("seller_percentage", out var sp) && sp.ValueKind == JsonValueKind.Number
                ? sp.GetDecimal() : null;
            decimal? promoPrice = promo.TryGetProperty("price", out var pp) && pp.ValueKind == JsonValueKind.Number
                ? pp.GetDecimal() : null;
            decimal? originalPrice = promo.TryGetProperty("original_price", out var opp) && opp.ValueKind == JsonValueKind.Number
                ? opp.GetDecimal() : null;

            var dto = new ItemPromotionDto
            {
                PromotionId = promoId,
                Type = promoType,
                Status = promoStatus,
                Name = promoName,
                StartDate = startDate,
                FinishDate = finishDate,
                MeliPercentage = meliPct,
                SellerPercentage = sellerPct,
                PromotionPrice = promoPrice,
                OriginalPrice = originalPrice
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<MeliItemSyncResult> SyncItemsAsync(string? statusFilter = null, int? accountId = null)
    {
        var accounts = await _accountService.GetAllAccountEntitiesAsync();

        // Filter by account if specified
        if (accountId.HasValue)
            accounts = accounts.Where(a => a.Id == accountId.Value).ToList();

        int totalSynced = 0;
        int totalErrors = 0;
        var errors = new List<string>();

        foreach (var account in accounts)
        {
            try
            {
                var token = await _accountService.GetValidTokenAsync(account);
                if (token is null)
                {
                    errors.Add($"Token expirado para {account.Nickname}");
                    totalErrors++;
                    continue;
                }

                var synced = await SyncItemsForAccountAsync(account, token, statusFilter);
                totalSynced += synced;
            }
            catch (Exception ex)
            {
                errors.Add($"Error en {account.Nickname}: {ex.Message}");
                totalErrors++;
            }
        }

        // Audit log for sync - include full error details as JSON
        var filterDesc = statusFilter is not null ? $" (filtro: {statusFilter})" : "";
        var auditData = new
        {
            resumen = $"Sincronizados {totalSynced} items, {totalErrors} errores{filterDesc}",
            totalSincronizados = totalSynced,
            totalErrores = totalErrors,
            filtro = statusFilter,
            errores = errors
        };
        var syncJson = System.Text.Json.JsonSerializer.Serialize(auditData);
        await _auditLog.LogAsync("Sync", "items", "SYNC", syncJson);

        return new MeliItemSyncResult(totalSynced, totalErrors, errors);
    }

    private async Task<int> SyncItemsForAccountAsync(MeliAccount account, string token, string? statusFilter = null)
    {
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 1: Get all item IDs via search using scan mode (supports >1000 items)
        var allItemIds = new List<string>();
        int limit = 100;
        string? scrollId = null;
        bool isFirstRequest = true;

        while (true)
        {
            // Use search_type=scan for pagination (MeLi requirement for >1000 results)
            var url = $"https://api.mercadolibre.com/users/{account.MeliUserId}/items/search" +
                $"?search_type=scan&limit={limit}";

            // Add scroll_id for subsequent pages
            if (!isFirstRequest && !string.IsNullOrEmpty(scrollId))
                url += $"&scroll_id={scrollId}";

            // Add status filter if specified (active, paused, closed)
            if (!string.IsNullOrEmpty(statusFilter))
                url += $"&status={statusFilter}";

            var response = await http.GetAsync(url);

            // Si devuelve 401/403, refrescar token y reintentar
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var newToken = await _accountService.GetValidTokenAsync(account, forceRefresh: true);
                if (newToken is not null)
                {
                    token = newToken;
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    response = await http.GetAsync(url);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"MeLi API error ({response.StatusCode}): {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            var results = doc.GetProperty("results");
            int count = 0;
            foreach (var id in results.EnumerateArray())
            {
                var itemId = id.GetString();
                if (!string.IsNullOrEmpty(itemId))
                    allItemIds.Add(itemId);
                count++;
            }

            // Get scroll_id for next page - null or missing means we're done
            scrollId = doc.TryGetProperty("scroll_id", out var sid) && sid.ValueKind != JsonValueKind.Null
                ? sid.GetString() : null;

            isFirstRequest = false;

            // Stop if no results returned or no scroll_id for next page
            if (count == 0 || string.IsNullOrEmpty(scrollId))
                break;
        }

        // Step 2: Batch fetch item details (20 at a time)
        int synced = 0;
        for (int i = 0; i < allItemIds.Count; i += 20)
        {
            var batch = allItemIds.Skip(i).Take(20).ToList();
            var idsParam = string.Join(",", batch);

            var response = await http.GetAsync($"https://api.mercadolibre.com/items?ids={idsParam}");
            if (!response.IsSuccessStatusCode)
                continue;

            var json = await response.Content.ReadAsStringAsync();
            var items = JsonDocument.Parse(json).RootElement;

            foreach (var itemResult in items.EnumerateArray())
            {
                var code = itemResult.GetProperty("code").GetInt32();
                if (code != 200) continue;

                var body = itemResult.GetProperty("body");
                synced += await UpsertItemAsync(account.Id, body);
            }

            await _db.SaveChangesAsync();
        }

        // Step 3: Sync promotional prices for active items
        // The items API does not include promotional discounts, so we use /items/{id}/sale_price
        var activeItems = await _db.MeliItems
            .Where(i => i.MeliAccountId == account.Id && i.Status == "active")
            .Select(i => new { i.Id, i.MeliItemId })
            .ToListAsync();

        foreach (var ai in activeItems)
        {
            try
            {
                var spResponse = await http.GetAsync(
                    $"https://api.mercadolibre.com/items/{ai.MeliItemId}/sale_price?app_version=v2");

                if (!spResponse.IsSuccessStatusCode)
                {
                    // No sale_price means no active promotion - clear OriginalPrice if set
                    var itemToClean = await _db.MeliItems.FindAsync(ai.Id);
                    if (itemToClean is not null && itemToClean.OriginalPrice.HasValue)
                    {
                        itemToClean.OriginalPrice = null;
                    }
                    continue;
                }

                var spJson = await spResponse.Content.ReadAsStringAsync();
                var spDoc = JsonDocument.Parse(spJson).RootElement;

                var promoAmount = spDoc.TryGetProperty("amount", out var amt) && amt.ValueKind == JsonValueKind.Number
                    ? amt.GetDecimal() : (decimal?)null;
                var regularAmount = spDoc.TryGetProperty("regular_amount", out var reg) && reg.ValueKind == JsonValueKind.Number
                    ? reg.GetDecimal() : (decimal?)null;

                if (promoAmount.HasValue && promoAmount.Value > 0 && regularAmount.HasValue && regularAmount.Value > promoAmount.Value)
                {
                    var itemToUpdate = await _db.MeliItems.FindAsync(ai.Id);
                    if (itemToUpdate is not null)
                    {
                        itemToUpdate.Price = promoAmount.Value;
                        itemToUpdate.OriginalPrice = regularAmount.Value;
                    }
                }
            }
            catch
            {
                // Skip individual item errors
            }
        }

        await _db.SaveChangesAsync();

        // Step 4: Sync category paths for items missing CategoryPath
        var itemsMissingCatPath = await _db.MeliItems
            .Where(i => i.MeliAccountId == account.Id && i.CategoryId != null && i.CategoryPath == null)
            .Select(i => new { i.Id, i.CategoryId })
            .ToListAsync();

        var categoryCache = new Dictionary<string, string>();
        foreach (var ci in itemsMissingCatPath)
        {
            if (string.IsNullOrEmpty(ci.CategoryId)) continue;
            try
            {
                if (!categoryCache.TryGetValue(ci.CategoryId, out var path))
                {
                    var catResponse = await http.GetAsync(
                        $"https://api.mercadolibre.com/categories/{ci.CategoryId}");
                    if (catResponse.IsSuccessStatusCode)
                    {
                        var catJson = await catResponse.Content.ReadAsStringAsync();
                        var catDoc = JsonDocument.Parse(catJson).RootElement;
                        if (catDoc.TryGetProperty("path_from_root", out var pathArr) && pathArr.ValueKind == JsonValueKind.Array)
                        {
                            var names = new List<string>();
                            foreach (var node in pathArr.EnumerateArray())
                            {
                                if (node.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null)
                                    names.Add(n.GetString() ?? "");
                            }
                            path = string.Join(" > ", names);
                        }
                        else
                        {
                            path = ci.CategoryId;
                        }
                    }
                    else
                    {
                        path = ci.CategoryId;
                    }
                    categoryCache[ci.CategoryId] = path;
                }

                var itemToUpdate = await _db.MeliItems.FindAsync(ci.Id);
                if (itemToUpdate is not null)
                    itemToUpdate.CategoryPath = path;
            }
            catch { }
        }

        await _db.SaveChangesAsync();

        return synced;
    }

    private async Task<int> UpsertItemAsync(int accountId, JsonElement item)
    {
        var meliItemId = item.GetProperty("id").GetString() ?? "";
        var title = item.GetProperty("title").GetString() ?? "Sin titulo";
        var price = item.TryGetProperty("price", out var pr) && pr.ValueKind != JsonValueKind.Null
            ? pr.GetDecimal() : 0m;
        var originalPrice = item.TryGetProperty("original_price", out var op) && op.ValueKind != JsonValueKind.Null
            ? op.GetDecimal() : (decimal?)null;
        var currencyId = item.TryGetProperty("currency_id", out var cur) && cur.ValueKind != JsonValueKind.Null
            ? cur.GetString() ?? "ARS" : "ARS";
        var availableQty = item.TryGetProperty("available_quantity", out var aq) && aq.ValueKind != JsonValueKind.Null
            ? aq.GetInt32() : 0;
        var soldQty = item.TryGetProperty("sold_quantity", out var sq) && sq.ValueKind != JsonValueKind.Null
            ? sq.GetInt32() : 0;
        var status = item.GetProperty("status").GetString() ?? "unknown";
        var condition = item.TryGetProperty("condition", out var cond) && cond.ValueKind != JsonValueKind.Null
            ? cond.GetString() : null;
        var listingTypeId = item.TryGetProperty("listing_type_id", out var lt) && lt.ValueKind != JsonValueKind.Null
            ? lt.GetString() : null;
        var categoryId = item.TryGetProperty("category_id", out var cat) && cat.ValueKind != JsonValueKind.Null
            ? cat.GetString() : null;
        var permalink = item.TryGetProperty("permalink", out var pl) && pl.ValueKind != JsonValueKind.Null
            ? pl.GetString() : null;
        // SKU: prioritize SELLER_SKU attribute, fallback to seller_custom_field
        string? sku = null;
        if (item.TryGetProperty("attributes", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
        {
            foreach (var attr in attrs.EnumerateArray())
            {
                var attrId = attr.TryGetProperty("id", out var aid) ? aid.GetString() : null;
                if (attrId == "SELLER_SKU")
                {
                    sku = attr.TryGetProperty("value_name", out var vn) && vn.ValueKind != JsonValueKind.Null
                        ? vn.GetString() : null;
                    break;
                }
            }
        }
        // Fallback to seller_custom_field if SELLER_SKU attribute not found
        if (string.IsNullOrEmpty(sku))
        {
            sku = item.TryGetProperty("seller_custom_field", out var scf) && scf.ValueKind != JsonValueKind.Null
                ? scf.GetString() : null;
        }
        var dateCreated = item.TryGetProperty("date_created", out var dc) && dc.ValueKind != JsonValueKind.Null
            ? dc.GetDateTime() : (DateTime?)null;
        var lastUpdated = item.TryGetProperty("last_updated", out var lu) && lu.ValueKind != JsonValueKind.Null
            ? lu.GetDateTime() : (DateTime?)null;

        // Thumbnail - convert http to https
        var thumbnail = item.TryGetProperty("thumbnail", out var th) && th.ValueKind != JsonValueKind.Null
            ? th.GetString() : null;
        if (thumbnail != null && thumbnail.StartsWith("http://"))
            thumbnail = "https://" + thumbnail[7..];

        // User product grouping
        var userProductId = item.TryGetProperty("user_product_id", out var upid) && upid.ValueKind != JsonValueKind.Null
            ? upid.GetString() : null;

        // Family grouping - try multiple locations
        string? familyId = null;
        string? familyName = null;

        if (item.TryGetProperty("family", out var family) && family.ValueKind != JsonValueKind.Null)
        {
            if (family.TryGetProperty("id", out var fid) && fid.ValueKind != JsonValueKind.Null)
                familyId = fid.GetString();
            if (family.TryGetProperty("name", out var fname) && fname.ValueKind != JsonValueKind.Null)
                familyName = fname.GetString();
        }

        // Fallback: check family_id at root level
        if (familyId is null && item.TryGetProperty("family_id", out var fid2) && fid2.ValueKind != JsonValueKind.Null)
        {
            familyId = fid2.ValueKind == JsonValueKind.Number
                ? fid2.GetInt64().ToString()
                : fid2.GetString();
        }
        if (familyName is null && item.TryGetProperty("family_name", out var fname2) && fname2.ValueKind != JsonValueKind.Null)
            familyName = fname2.GetString();

        // Installment tag from item tags
        string? installmentTag = null;
        if (item.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            var installmentTags = new[] { "12x_campaign", "9x_campaign", "3x_campaign", "pcj-co-funded" };
            foreach (var tag in tags.EnumerateArray())
            {
                var tagStr = tag.GetString();
                if (tagStr is not null && installmentTags.Contains(tagStr))
                {
                    installmentTag = tagStr;
                    break;
                }
            }
        }

        // Free shipping
        var freeShipping = false;
        if (item.TryGetProperty("shipping", out var shipping) && shipping.ValueKind == JsonValueKind.Object)
        {
            freeShipping = shipping.TryGetProperty("free_shipping", out var fs) && fs.ValueKind == JsonValueKind.True;
        }

        var existing = await _db.MeliItems.FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);

        if (existing is not null)
        {
            existing.Title = title;
            existing.CategoryId = categoryId;
            existing.Price = price;
            existing.OriginalPrice = originalPrice;
            existing.CurrencyId = currencyId;
            existing.AvailableQuantity = availableQty;
            existing.SoldQuantity = soldQty;
            existing.Status = status;
            existing.Condition = condition;
            existing.ListingTypeId = listingTypeId;
            existing.Thumbnail = thumbnail;
            existing.Permalink = permalink;
            existing.Sku = sku;
            existing.UserProductId = userProductId;
            existing.FamilyId = familyId;
            existing.FamilyName = familyName;
            existing.InstallmentTag = installmentTag;
            existing.FreeShipping = freeShipping;
            existing.LastUpdated = lastUpdated;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.MeliItems.Add(new MeliItem
            {
                MeliItemId = meliItemId,
                MeliAccountId = accountId,
                Title = title,
                CategoryId = categoryId,
                Price = price,
                OriginalPrice = originalPrice,
                CurrencyId = currencyId,
                AvailableQuantity = availableQty,
                SoldQuantity = soldQty,
                Status = status,
                Condition = condition,
                ListingTypeId = listingTypeId,
                Thumbnail = thumbnail,
                Permalink = permalink,
                Sku = sku,
                UserProductId = userProductId,
                FamilyId = familyId,
                FamilyName = familyName,
                InstallmentTag = installmentTag,
                FreeShipping = freeShipping,
                DateCreated = dateCreated,
                LastUpdated = lastUpdated
            });
        }

        return 1;
    }

    public async Task<ListingCostDto> GetListingCostsAsync(string meliItemId)
    {
        var item = await _db.MeliItems
            .Include(i => i.MeliAccount)
            .FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);

        if (item is null)
            throw new Exception("Item no encontrado en la base de datos");

        if (item.MeliAccount is null)
            throw new Exception("Cuenta de MeLi no encontrada");

        var token = await _accountService.GetValidTokenAsync(item.MeliAccount);
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var price = item.OriginalPrice.HasValue && item.OriginalPrice.Value > 0
            ? item.OriginalPrice.Value
            : item.Price;

        // Build query params
        var queryParams = $"price={price.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrEmpty(item.CategoryId))
            queryParams += $"&category_id={item.CategoryId}";
        if (!string.IsNullOrEmpty(item.ListingTypeId))
            queryParams += $"&listing_type_id={item.ListingTypeId}";

        var url = $"https://api.mercadolibre.com/sites/MLA/listing_prices?{queryParams}";
        var resp = await http.GetAsync(url);

        var result = new ListingCostDto
        {
            Price = price,
            CurrencyId = item.CurrencyId ?? "ARS",
            ListingTypeId = item.ListingTypeId
        };

        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // The response may be an array (one entry per listing type) or a single object
            JsonElement root;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Find the matching listing type
                root = doc.RootElement.EnumerateArray()
                    .FirstOrDefault(e => e.TryGetProperty("listing_type_id", out var lt)
                        && lt.GetString() == item.ListingTypeId);
                if (root.ValueKind == JsonValueKind.Undefined)
                    root = doc.RootElement[0]; // fallback to first
            }
            else
            {
                root = doc.RootElement;
            }

            result.SaleFeeAmount = root.TryGetProperty("sale_fee_amount", out var sfa) && sfa.ValueKind == JsonValueKind.Number
                ? sfa.GetDecimal() : 0;
            result.ListingFeeAmount = root.TryGetProperty("listing_fee_amount", out var lfa) && lfa.ValueKind == JsonValueKind.Number
                ? lfa.GetDecimal() : 0;
            result.ListingTypeName = root.TryGetProperty("listing_type_name", out var ltn)
                ? ltn.GetString() : null;

            if (root.TryGetProperty("sale_fee_details", out var sfd))
            {
                result.FixedFee = sfd.TryGetProperty("fixed_fee", out var ff) && ff.ValueKind == JsonValueKind.Number
                    ? ff.GetDecimal() : 0;
                result.PercentageFee = sfd.TryGetProperty("percentage_fee", out var pf) && pf.ValueKind == JsonValueKind.Number
                    ? pf.GetDecimal() : 0;
                result.FinancingFee = sfd.TryGetProperty("financing_add_on_fee", out var faf) && faf.ValueKind == JsonValueKind.Number
                    ? faf.GetDecimal() : 0;
            }
        }

        // Step 2: Get shipping costs (only if item offers free shipping - seller pays)
        if (item.FreeShipping)
        {
            try
            {
                var userId = item.MeliAccount.MeliUserId;
                var shippingUrl = $"https://api.mercadolibre.com/users/{userId}/shipping_options/free?item_id={meliItemId}&item_price={price.ToString(System.Globalization.CultureInfo.InvariantCulture)}&free_shipping=true&listing_type_id={item.ListingTypeId}";
                var shippingResp = await http.GetAsync(shippingUrl);
                if (shippingResp.IsSuccessStatusCode)
                {
                    var shippingJson = await shippingResp.Content.ReadAsStringAsync();
                    using var shippingDoc = JsonDocument.Parse(shippingJson);
                    if (shippingDoc.RootElement.TryGetProperty("coverage", out var coverage)
                        && coverage.TryGetProperty("all_country", out var allCountry)
                        && allCountry.TryGetProperty("list_cost", out var listCost)
                        && listCost.ValueKind == JsonValueKind.Number)
                    {
                        result.ShippingCost = listCost.GetDecimal();
                    }
                }
            }
            catch { /* Shipping cost is optional */ }
        }


        // Net amount = price - sale_fee - listing_fee - taxes
        // NetAmount without taxes (taxes calculated in frontend with user percentage)
        result.NetAmount = Math.Round(price - result.SaleFeeAmount - result.ListingFeeAmount - result.ShippingCost, 2);

        return result;
    }
}
