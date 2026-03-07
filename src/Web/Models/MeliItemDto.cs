namespace Web.Models;

public class MeliItemDto
{
    public int Id { get; set; }
    public string MeliItemId { get; set; } = string.Empty;
    public int MeliAccountId { get; set; }
    public string AccountNickname { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public decimal Price { get; set; }
    public string CurrencyId { get; set; } = "ARS";
    public int AvailableQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public string? ListingTypeId { get; set; }
    public string? Thumbnail { get; set; }
    public string? Permalink { get; set; }
    public string? Sku { get; set; }
    public string? UserProductId { get; set; }
    public string? FamilyId { get; set; }
    public string? FamilyName { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class MeliItemsResponse
{
    public List<MeliItemDto> Items { get; set; } = new();
    public int Total { get; set; }
}

public class MeliItemSyncResult
{
    public int TotalSynced { get; set; }
    public int TotalErrors { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class UpdateMeliItemRequest
{
    public string? Title { get; set; }
    public decimal? Price { get; set; }
    public int? AvailableQuantity { get; set; }
    public string? Status { get; set; }
}
