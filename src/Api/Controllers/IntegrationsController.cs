using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IntegrationService _service;
    private readonly IHttpClientFactory _httpFactory;

    public IntegrationsController(IntegrationService service, IHttpClientFactory httpFactory)
    {
        _service = service;
        _httpFactory = httpFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var integrations = await _service.GetAllAsync();
        return Ok(integrations);
    }

    [HttpGet("{provider}")]
    public async Task<IActionResult> GetByProvider(string provider)
    {
        var integration = await _service.GetByProviderAsync(provider);
        if (integration is null) return NotFound();
        return Ok(integration);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveIntegrationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _service.SaveAsync(request);
        return Ok(result);
    }

    [HttpGet("openai/models")]
    public async Task<IActionResult> GetOpenAiModels()
    {
        var integration = await _service.GetByProviderAsync("openai");
        if (integration is null || !integration.HasSecret)
            return BadRequest(new { error = "No hay API Key de OpenAI configurada" });

        // Get the actual secret from DB (not exposed via DTO)
        var secret = await _service.GetSecretAsync("openai");
        if (string.IsNullOrEmpty(secret))
            return BadRequest(new { error = "No hay API Key de OpenAI configurada" });

        try
        {
            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
            var response = await http.GetAsync("https://api.openai.com/v1/models");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return BadRequest(new { error = $"Error de OpenAI ({response.StatusCode}): API Key invalida o sin permisos" });
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var models = new List<object>();

            foreach (var model in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var id = model.GetProperty("id").GetString() ?? "";
                // Filter only chat/completion models
                if (id.StartsWith("gpt-") || id.StartsWith("o1") || id.StartsWith("o3") || id.StartsWith("o4") || id.StartsWith("chatgpt"))
                {
                    models.Add(new { id });
                }
            }

            models = models.OrderBy(m => ((dynamic)m).id).ToList<object>();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Error al conectar con OpenAI: " + ex.Message });
        }
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> Delete(string provider)
    {
        var deleted = await _service.DeleteAsync(provider);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
