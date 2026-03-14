using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Web.Models;

namespace Web.Services;

public class AuthService
{
    private const string TokenKey = "tm_token";
    private const string UserKey = "tm_user";
    private const string ExpiryKey = "tm_expires";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Error al iniciar sesion";
            throw new Exception(message ?? "Error al iniciar sesion");
        }

        var data = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new Exception("Respuesta invalida del servidor");

        await SaveSessionAsync(data);
        return data;
    }

    public async Task LogoutAsync()
    {
        await ClearSessionAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    public async Task<UserInfo?> GetUserAsync()
    {
        var raw = await _js.InvokeAsync<string?>("localStorage.getItem", UserKey);
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            return JsonSerializer.Deserialize<UserInfo>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;

        var expiresAt = await _js.InvokeAsync<string?>("localStorage.getItem", ExpiryKey);
        if (string.IsNullOrEmpty(expiresAt)) return true;

        return DateTime.TryParse(expiresAt, out var expiry) && expiry > DateTime.UtcNow;
    }

    private async Task SaveSessionAsync(AuthResponse data)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, data.Token);
        var userJson = JsonSerializer.Serialize(new UserInfo { Username = data.Username, Role = data.Role, Permissions = data.Permissions ?? new() });
        await _js.InvokeVoidAsync("localStorage.setItem", UserKey, userJson);
        await _js.InvokeVoidAsync("localStorage.setItem", ExpiryKey, data.ExpiresAt.ToString("O"));
    }

    private async Task ClearSessionAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", UserKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", ExpiryKey);
    }
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
