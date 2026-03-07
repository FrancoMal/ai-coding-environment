using System.Net.Http.Headers;
using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MeliAccountService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public MeliAccountService(AppDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    public async Task<List<MeliAccountDto>> GetAccountsAsync()
    {
        return await _db.MeliAccounts
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new MeliAccountDto(
                a.Id, a.MeliUserId, a.Nickname, a.Email,
                a.TokenExpiresAt > DateTime.UtcNow,
                a.CreatedAt))
            .ToListAsync();
    }

    public string GetAuthUrl()
    {
        var integration = _db.Integrations.FirstOrDefault(i => i.Provider == "mercadolibre");
        if (integration is null || string.IsNullOrEmpty(integration.AppId))
            throw new InvalidOperationException("MercadoLibre integration not configured");

        var redirectUrl = integration.RedirectUrl ?? "";
        return $"https://auth.mercadolibre.com.ar/authorization?response_type=code&client_id={integration.AppId}&redirect_uri={Uri.EscapeDataString(redirectUrl)}";
    }

    public async Task<MeliAccountDto> HandleCallbackAsync(string code)
    {
        var integration = await _db.Integrations
            .FirstOrDefaultAsync(i => i.Provider == "mercadolibre");

        if (integration is null || string.IsNullOrEmpty(integration.AppId) || string.IsNullOrEmpty(integration.AppSecret))
            throw new InvalidOperationException("MercadoLibre integration not configured");

        // Exchange code for token
        var http = _httpFactory.CreateClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = integration.AppId,
            ["client_secret"] = integration.AppSecret,
            ["code"] = code,
            ["redirect_uri"] = integration.RedirectUrl ?? ""
        };

        var tokenResponse = await http.PostAsync(
            "https://api.mercadolibre.com/oauth/token",
            new FormUrlEncodedContent(tokenRequest));

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
            throw new Exception($"Error al obtener token de MercadoLibre: {tokenJson}");

        var tokenData = JsonDocument.Parse(tokenJson).RootElement;
        var accessToken = tokenData.GetProperty("access_token").GetString()!;
        var refreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = tokenData.GetProperty("expires_in").GetInt32();

        // Get user info
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await http.GetAsync("https://api.mercadolibre.com/users/me");
        var userJson = await userResponse.Content.ReadAsStringAsync();

        if (!userResponse.IsSuccessStatusCode)
            throw new Exception($"Error al obtener datos del usuario: {userJson}");

        var userData = JsonDocument.Parse(userJson).RootElement;
        var meliUserId = userData.GetProperty("id").GetInt64();
        var nickname = userData.GetProperty("nickname").GetString() ?? "Sin nombre";
        var email = userData.TryGetProperty("email", out var em) ? em.GetString() : null;

        // Check if account already exists
        var existing = await _db.MeliAccounts.FirstOrDefaultAsync(a => a.MeliUserId == meliUserId);
        if (existing is not null)
        {
            existing.AccessToken = accessToken;
            existing.RefreshToken = refreshToken;
            existing.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            existing.Nickname = nickname;
            existing.Email = email;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new MeliAccount
            {
                MeliUserId = meliUserId,
                Nickname = nickname,
                Email = email,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
            };
            _db.MeliAccounts.Add(existing);
        }

        await _db.SaveChangesAsync();

        return new MeliAccountDto(
            existing.Id, existing.MeliUserId, existing.Nickname, existing.Email,
            existing.TokenExpiresAt > DateTime.UtcNow, existing.CreatedAt);
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        var account = await _db.MeliAccounts.FindAsync(id);
        if (account is null) return false;

        _db.MeliAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MeliAccount>> GetAllAccountEntitiesAsync()
    {
        return await _db.MeliAccounts.ToListAsync();
    }

    public async Task<string?> GetValidTokenAsync(MeliAccount account)
    {
        if (account.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return account.AccessToken;

        var refreshed = await RefreshTokenAsync(account);
        return refreshed ? account.AccessToken : null;
    }

    public async Task<bool> RefreshTokenAsync(MeliAccount account)
    {
        var integration = await _db.Integrations
            .FirstOrDefaultAsync(i => i.Provider == "mercadolibre");

        if (integration is null || string.IsNullOrEmpty(integration.AppId)
            || string.IsNullOrEmpty(integration.AppSecret))
            return false;

        if (string.IsNullOrEmpty(account.RefreshToken))
            return false;

        var http = _httpFactory.CreateClient();
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = integration.AppId,
            ["client_secret"] = integration.AppSecret,
            ["refresh_token"] = account.RefreshToken
        };

        var response = await http.PostAsync(
            "https://api.mercadolibre.com/oauth/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonDocument.Parse(json).RootElement;

        account.AccessToken = tokenData.GetProperty("access_token").GetString()!;
        if (tokenData.TryGetProperty("refresh_token", out var rt))
            account.RefreshToken = rt.GetString();
        account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(
            tokenData.GetProperty("expires_in").GetInt32());
        account.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
