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
                i.Title, i.CategoryId, i.Price, i.CurrencyId,
                i.AvailableQuantity, i.SoldQuantity, i.Status,
                i.Condition, i.ListingTypeId, i.Thumbnail, i.Permalink,
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
            item.Title, item.CategoryId, item.Price, item.CurrencyId,
            item.AvailableQuantity, item.SoldQuantity, item.Status,
            item.Condition, item.ListingTypeId, item.Thumbnail, item.Permalink,
            item.Sku, item.UserProductId, item.FamilyId, item.FamilyName,
            item.DateCreated, item.LastUpdated);
    }

    public async Task<MeliItemSyncResult> SyncItemsAsync()
    {
        var accounts = await _accountService.GetAllAccountEntitiesAsync();
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

                var synced = await SyncItemsForAccountAsync(account, token);
                totalSynced += synced;
            }
            catch (Exception ex)
            {
                errors.Add($"Error en {account.Nickname}: {ex.Message}");
                totalErrors++;
            }
        }

        // Audit log for sync
        var syncInfo = $"Sincronizados {totalSynced} items, {totalErrors} errores";
        await _auditLog.LogAsync("Sync", "items", "SYNC", syncInfo);

        return new MeliItemSyncResult(totalSynced, totalErrors, errors);
    }

    private async Task<int> SyncItemsForAccountAsync(MeliAccount account, string token)
    {
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 1: Get all item IDs via search
        var allItemIds = new List<string>();
        int offset = 0;
        int limit = 100;
        bool hasMore = true;

        while (hasMore)
        {
            var url = $"https://api.mercadolibre.com/users/{account.MeliUserId}/items/search" +
                $"?offset={offset}&limit={limit}";

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"MeLi API error ({response.StatusCode}): {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            var results = doc.GetProperty("results");
            foreach (var id in results.EnumerateArray())
            {
                var itemId = id.GetString();
                if (!string.IsNullOrEmpty(itemId))
                    allItemIds.Add(itemId);
            }

            var paging = doc.GetProperty("paging");
            var total = paging.GetProperty("total").GetInt32();
            offset += limit;
            hasMore = offset < total;
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

        return synced;
    }

    private async Task<int> UpsertItemAsync(int accountId, JsonElement item)
    {
        var meliItemId = item.GetProperty("id").GetString() ?? "";
        var title = item.GetProperty("title").GetString() ?? "Sin titulo";
        var price = item.TryGetProperty("price", out var pr) && pr.ValueKind != JsonValueKind.Null
            ? pr.GetDecimal() : 0m;
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
        var sku = item.TryGetProperty("seller_custom_field", out var scf) && scf.ValueKind != JsonValueKind.Null
            ? scf.GetString() : null;
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

        var existing = await _db.MeliItems.FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);

        if (existing is not null)
        {
            existing.Title = title;
            existing.CategoryId = categoryId;
            existing.Price = price;
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
                DateCreated = dateCreated,
                LastUpdated = lastUpdated
            });
        }

        return 1;
    }
}
