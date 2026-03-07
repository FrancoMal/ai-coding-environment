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

    public IntegrationsController(IntegrationService service) => _service = service;

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

    [HttpDelete("{provider}")]
    public async Task<IActionResult> Delete(string provider)
    {
        var deleted = await _service.DeleteAsync(provider);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
