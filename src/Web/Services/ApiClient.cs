using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    private readonly NavigationManager _navigation;

    public ApiClient(HttpClient http, AuthService authService, NavigationManager navigation)
    {
        _http = http;
        _authService = authService;
        _navigation = navigation;
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        return await GetAsync<DashboardStats>("/api/dashboard/stats");
    }

    public async Task<UserDto?> GetMeAsync()
    {
        return await GetAsync<UserDto>("/api/auth/me");
    }

    // --- Users ---
    public async Task<List<UserManageDto>?> GetUsersAsync()
    {
        return await GetAsync<List<UserManageDto>>("/api/users");
    }

    public async Task<UserManageDto?> CreateUserAsync(CreateUserRequest request)
    {
        return await PostAsync<UserManageDto>("/api/users", request);
    }

    public async Task<UserManageDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        return await PutAsync<UserManageDto>($"/api/users/{id}", request);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        return await DeleteAsync($"/api/users/{id}");
    }

    // --- Roles ---
    public async Task<List<RoleDto>?> GetRolesAsync()
    {
        return await GetAsync<List<RoleDto>>("/api/roles");
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request)
    {
        return await PostAsync<RoleDto>("/api/roles", request);
    }

    public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        return await PutAsync<RoleDto>($"/api/roles/{id}", request);
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        return await DeleteAsync($"/api/roles/{id}");
    }

    // --- Profile ---
    public async Task<ProfileDto?> GetProfileAsync()
    {
        return await GetAsync<ProfileDto>("/api/auth/profile");
    }

    public async Task<ProfileDto?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        return await PutAsync<ProfileDto>("/api/auth/profile", request);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("/api/auth/password", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    // --- MeLi Item Details (pictures + description) ---
    public async Task<MeliItemDetailsDto?> GetMeliItemDetailsAsync(string meliItemId)
    {
        return await GetAsync<MeliItemDetailsDto>($"/api/meli/items/{meliItemId}/details");
    }

        // --- Item-Product Linking ---
    public async Task<MeliItemDto?> LinkItemToProductAsync(string meliItemId, int productId)
    {
        return await PutAsync<MeliItemDto>($"/api/meli/items/{meliItemId}/link", new { productId });
    }

    public async Task<MeliItemDto?> UnlinkItemProductAsync(string meliItemId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"/api/meli/items/{meliItemId}/link");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MeliItemDto>();
    }

    // --- Products ---
    public async Task<List<ProductDto>?> GetProductsAsync()
    {
        return await GetAsync<List<ProductDto>>("/api/products");
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        return await PostAsync<ProductDto>("/api/products", request);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        return await PutAsync<ProductDto>($"/api/products/{id}", request);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        return await DeleteAsync($"/api/products/{id}");
    }

    // --- Integrations ---
    public async Task<List<IntegrationDto>?> GetIntegrationsAsync()
    {
        return await GetAsync<List<IntegrationDto>>("/api/integrations");
    }

    public async Task<IntegrationDto?> GetIntegrationAsync(string provider)
    {
        return await GetAsync<IntegrationDto>($"/api/integrations/{provider}");
    }

    public async Task<IntegrationDto?> SaveIntegrationAsync(SaveIntegrationRequest request)
    {
        return await PostAsync<IntegrationDto>("/api/integrations", request);
    }

    public async Task<bool> DeleteIntegrationAsync(string provider)
    {
        return await DeleteAsync($"/api/integrations/{provider}");
    }

    // --- MercadoLibre Accounts ---
    public async Task<List<MeliAccountDto>?> GetMeliAccountsAsync()
    {
        return await GetAsync<List<MeliAccountDto>>("/api/meli/accounts");
    }

    public async Task<MeliAuthUrlResponse?> GetMeliAuthUrlAsync()
    {
        return await GetAsync<MeliAuthUrlResponse>("/api/meli/auth-url");
    }

    public async Task<MeliAccountDto?> MeliCallbackAsync(string code)
    {
        return await PostAsync<MeliAccountDto>("/api/meli/callback", new MeliCallbackRequest { Code = code });
    }


    public async Task<MeliAccountStatsDto?> GetMeliAccountStatsAsync(int id)
    {
        return await GetAsync<MeliAccountStatsDto>($"/api/meli/accounts/{id}/stats");
    }

    public async Task<bool> DeleteMeliAccountAsync(int id)
    {
        return await DeleteAsync($"/api/meli/accounts/{id}");
    }

    // --- MercadoLibre Orders ---
    public async Task<MeliOrdersResponse?> GetMeliOrdersAsync(DateTime from, DateTime to, int? accountId = null)
    {
        var url = $"/api/meli/orders?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}";
        if (accountId.HasValue)
            url += $"&accountId={accountId.Value}";
        return await GetAsync<MeliOrdersResponse>(url);
    }

    public async Task<MeliOrderSyncResult?> SyncMeliOrdersAsync(DateTime from, DateTime to)
    {
        var url = $"/api/meli/orders/sync?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}";
        return await PostAsync<MeliOrderSyncResult>(url, new { });
    }

    // --- MercadoLibre Items ---
    public async Task<MeliItemsResponse?> GetMeliItemsAsync(int? accountId = null, string? status = null)
    {
        var url = "/api/meli/items";
        var queryParams = new List<string>();
        if (accountId.HasValue)
            queryParams.Add($"accountId={accountId.Value}");
        if (!string.IsNullOrEmpty(status))
            queryParams.Add($"status={status}");
        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams);
        return await GetAsync<MeliItemsResponse>(url);
    }

    public async Task<MeliItemSyncResult?> SyncMeliItemsAsync(string? status = null, int? accountId = null)
    {
        var url = "/api/meli/items/sync";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
        if (accountId.HasValue) queryParams.Add($"accountId={accountId.Value}");
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
        return await PostAsync<MeliItemSyncResult>(url, new { });
    }

    public async Task<List<ItemPromotionDto>?> GetItemPromotionsAsync(string meliItemId)
    {
        return await GetAsync<List<ItemPromotionDto>>($"/api/meli/items/{meliItemId}/promotions");
    }

    public async Task<ListingCostDto?> GetItemCostsAsync(string meliItemId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ListingCostDto>($"api/meli/items/{meliItemId}/costs");
        }
        catch
        {
            return null;
        }
    }

    public async Task<MeliItemDto?> UpdateMeliItemAsync(string meliItemId, UpdateMeliItemRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"/api/meli/items/{meliItemId}", request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    throw new Exception(errorProp.GetString());
            }
            catch (System.Text.Json.JsonException) { }
            throw new Exception($"Error del servidor ({response.StatusCode})");
        }

        return await response.Content.ReadFromJsonAsync<MeliItemDto>();
    }

    public async Task<List<OpenAiModelDto>> GetOpenAiModelsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("/api/integrations/openai/models");

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return new();
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    throw new Exception(errorProp.GetString());
            }
            catch (System.Text.Json.JsonException) { }
            throw new Exception($"Error al obtener modelos ({response.StatusCode})");
        }

        return await response.Content.ReadFromJsonAsync<List<OpenAiModelDto>>() ?? new();
    }

    public async Task<List<ClaudeModelDto>> GetClaudeModelsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("/api/integrations/claude/models");

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return new();
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    throw new Exception(errorProp.GetString());
            }
            catch (System.Text.Json.JsonException) { }
            throw new Exception($"Error al obtener modelos ({response.StatusCode})");
        }

        return await response.Content.ReadFromJsonAsync<List<ClaudeModelDto>>() ?? new();
    }

    public async Task<int> DeleteMeliItemsBulkAsync(List<int> ids)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/meli/items/bulk-delete", new { ids });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return 0;
        }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error al eliminar publicaciones ({response.StatusCode})");

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return result.GetProperty("deleted").GetInt32();
    }

    public async Task<BulkCreateProductResult?> CreateProductFromItemAsync(int itemId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync("/api/meli/items/" + itemId + "/create-product", null);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            throw new Exception(errorText);
        }

        return await response.Content.ReadFromJsonAsync<BulkCreateProductResult>();
    }

        public async Task<BulkCreateProductResult?> BulkCreateProductsAsync(List<int> ids)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/meli/items/bulk-create-products", new { ids });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error al crear productos ({response.StatusCode}): {errorText}");
        }

        return await response.Content.ReadFromJsonAsync<BulkCreateProductResult>();
    }


        // --- Audit Logs ---
    public async Task<AuditLogListResponse?> GetAuditLogsAsync(DateTime from, DateTime to, string? entityType = null, int page = 1)
    {
        var url = $"/api/audit-logs?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}&page={page}";
        if (!string.IsNullOrEmpty(entityType))
            url += $"&entityType={entityType}";
        return await GetAsync<AuditLogListResponse>(url);
    }

    // --- MercadoLibre Order Detail ---
    public async Task<MeliOrderDetailResponse?> GetMeliOrderDetailAsync(long meliOrderId)
    {
        return await GetAsync<MeliOrderDetailResponse>($"/api/meli/orders/detail/{meliOrderId}");
    }

    public async Task<MeliOrderDetailResponse?> GetMeliPackDetailAsync(long packId)
    {
        return await GetAsync<MeliOrderDetailResponse>($"/api/meli/orders/pack-detail/{packId}");
    }

    // --- Scheduled Processes ---
    public async Task<List<ScheduledProcessDto>?> GetScheduledProcessesAsync()
    {
        return await GetAsync<List<ScheduledProcessDto>>("/api/scheduled-processes");
    }

    public async Task<ScheduledProcessDto?> UpdateProcessScheduleAsync(string code, UpdateScheduleRequest request)
    {
        return await PutAsync<ScheduledProcessDto>($"/api/scheduled-processes/{code}/schedule", request);
    }

    public async Task<RunProcessResponse?> RunProcessNowAsync(string code)
    {
        return await PostAsync<RunProcessResponse>($"/api/scheduled-processes/{code}/run", new { });
    }

    public async Task<ProcessLogListResponse?> GetProcessLogsAsync(string? code = null, int page = 1)
    {
        var url = code != null
            ? $"/api/scheduled-processes/{code}/logs?page={page}"
            : $"/api/scheduled-processes/logs?page={page}";
        return await GetAsync<ProcessLogListResponse>(url);
    }

    // --- MeLi Publish ---
    public async Task<List<CategoryPredictionDto>?> PredictCategoryAsync(string title, int accountId)
    {
        return await PostAsync<List<CategoryPredictionDto>>($"/api/meli/publish/predict-category?accountId={accountId}", new { title });
    }

    public async Task<List<CategoryAttributeDto>?> GetCategoryAttributesAsync(string categoryId)
    {
        return await GetAsync<List<CategoryAttributeDto>>($"/api/meli/publish/category-attributes/{categoryId}");
    }

    public async Task<List<SuggestedAttributeDto>?> SuggestAttributesAsync(object request)
    {
        return await PostAsync<List<SuggestedAttributeDto>>("/api/meli/publish/suggest-attributes", request);
    }

    public async Task<PublishItemResponse?> PublishItemAsync(PublishItemRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("/api/meli/publish", request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    return new PublishItemResponse { Error = errorProp.GetString() };
            }
            catch (System.Text.Json.JsonException) { }
            return new PublishItemResponse { Error = $"Error del servidor ({response.StatusCode})" };
        }
        return await response.Content.ReadFromJsonAsync<PublishItemResponse>();
    }

    // --- HTTP helpers ---
    private async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PostAsync<T>(string url, object data)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, data);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PutAsync<T>(string url, object data)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync(url, data);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<bool> DeleteAsync(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
