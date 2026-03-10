using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeliController : ControllerBase
{
    private readonly MeliAccountService _service;
    private readonly MeliOrderService _orderService;
    private readonly MeliItemService _itemService;
    private readonly AiService _aiService;

    public MeliController(MeliAccountService service, MeliOrderService orderService, MeliItemService itemService, AiService aiService)
    {
        _service = service;
        _orderService = orderService;
        _itemService = itemService;
        _aiService = aiService;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _service.GetAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl()
    {
        try
        {
            var url = _service.GetAuthUrl();
            return Ok(new MeliAuthUrlResponse(url));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("callback")]
    public async Task<IActionResult> HandleCallback([FromBody] MeliCallbackRequest request)
    {
        try
        {
            var account = await _service.HandleCallbackAsync(request.Code);
            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpGet("accounts/{id}/stats")]
    public async Task<IActionResult> GetAccountStats(int id)
    {
        var stats = await _service.GetAccountStatsAsync(id);
        if (stats is null) return NotFound();
        return Ok(stats);
    }

    [HttpDelete("accounts/{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var deleted = await _service.DeleteAccountAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? accountId)
    {
        try
        {
            var dateFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var dateTo = to ?? DateTime.UtcNow;
            var result = await _orderService.GetOrdersAsync(dateFrom, dateTo, accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders/detail/{meliOrderId}")]
    public async Task<IActionResult> GetOrderDetail(long meliOrderId)
    {
        try
        {
            var result = await _orderService.GetOrderDetailAsync(meliOrderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders/pack-detail/{packId}")]
    public async Task<IActionResult> GetPackDetail(long packId)
    {
        try
        {
            var result = await _orderService.GetPackDetailAsync(packId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("orders/sync")]
    public async Task<IActionResult> SyncOrders(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var dateFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var dateTo = to ?? DateTime.UtcNow;
            var result = await _orderService.SyncOrdersAsync(dateFrom, dateTo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] int? accountId,
        [FromQuery] string? status)
    {
        try
        {
            var result = await _itemService.GetItemsAsync(accountId, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("items/sync")]
    public async Task<IActionResult> SyncItems([FromQuery] string? status = null, [FromQuery] int? accountId = null)
    {
        try
        {
            var result = await _itemService.SyncItemsAsync(status, accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("items/{meliItemId}/promotions")]
    public async Task<IActionResult> GetItemPromotions(string meliItemId)
    {
        try
        {
            var result = await _itemService.GetItemPromotionsAsync(meliItemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("items/{meliItemId}")]
    public async Task<IActionResult> UpdateItem(string meliItemId, [FromBody] UpdateMeliItemRequest request)
    {
        try
        {
            var result = await _itemService.UpdateItemAsync(meliItemId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("items/{meliItemId}/costs")]
    public async Task<IActionResult> GetItemCosts(string meliItemId)
    {
        try
        {
            var result = await _itemService.GetListingCostsAsync(meliItemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("items/{meliItemId}/details")]
    public async Task<IActionResult> GetItemDetails(string meliItemId)
    {
        var details = await _itemService.GetItemDetailsAsync(meliItemId);
        if (details is null) return NotFound();
        return Ok(details);
    }

        [HttpPut("items/{meliItemId}/link")]
    public async Task<IActionResult> LinkItemToProduct(string meliItemId, [FromBody] LinkItemToProductRequest request)
    {
        try
        {
            var result = await _itemService.LinkToProductAsync(meliItemId, request.ProductId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("items/{meliItemId}/link")]
    public async Task<IActionResult> UnlinkItemProduct(string meliItemId)
    {
        try
        {
            var result = await _itemService.UnlinkProductAsync(meliItemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("items/bulk-delete")]
    public async Task<IActionResult> BulkDeleteItems([FromBody] BulkDeleteRequest request)
    {
        if (request.Ids == null || !request.Ids.Any())
            return BadRequest(new { error = "No se proporcionaron IDs" });

        var deleted = await _itemService.DeleteItemsAsync(request.Ids);
        return Ok(new { deleted });
    }


    // --- Publish endpoints ---

    [HttpPost("publish/predict-category")]
    public async Task<IActionResult> PredictCategory([FromBody] PredictCategoryRequest request, [FromQuery] int accountId)
    {
        try
        {
            var result = await _itemService.PredictCategoryAsync(request.Title, accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("publish/category-attributes/{categoryId}")]
    public async Task<IActionResult> GetCategoryAttributes(string categoryId)
    {
        try
        {
            var result = await _itemService.GetCategoryAttributesAsync(categoryId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("publish/suggest-attributes")]
    public async Task<IActionResult> SuggestAttributes([FromBody] SuggestAttributesRequest request)
    {
        try
        {
            var result = await _aiService.SuggestAttributesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("items/bulk-create-products")]
    public async Task<IActionResult> BulkCreateProducts([FromBody] BulkCreateProductsRequest request)
    {
        if (request.Ids == null || !request.Ids.Any())
            return BadRequest(new { error = "No se proporcionaron IDs" });
        try
        {
            var result = await _itemService.BulkCreateProductsAsync(request.Ids);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

        [HttpPost("publish")]
    public async Task<IActionResult> PublishItem([FromBody] PublishItemRequest request)
    {
        try
        {
            var result = await _itemService.PublishItemAsync(request);
            if (!result.Success)
                return BadRequest(new { error = result.Error });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class BulkDeleteRequest
{
    public List<int> Ids { get; set; } = new();
}

