namespace Api.DTOs;

public record MeliAccountDto(
    int Id,
    long MeliUserId,
    string Nickname,
    string? Email,
    bool TokenValid,
    DateTime CreatedAt
);

public record MeliAuthUrlResponse(string AuthUrl);

public record MeliCallbackRequest(string Code);

public record MeliOrderDto(
    int Id,
    long MeliOrderId,
    int MeliAccountId,
    string AccountNickname,
    string Status,
    DateTime DateCreated,
    DateTime? DateClosed,
    decimal TotalAmount,
    string CurrencyId,
    long BuyerId,
    string BuyerNickname,
    string ItemId,
    string ItemTitle,
    int Quantity,
    decimal UnitPrice,
    long? ShippingId,
    long? PackId
);

public record MeliOrdersResponse(List<MeliOrderDto> Orders, int Total);

public record MeliOrderSyncResult(int TotalSynced, int TotalErrors, List<string> Errors);

// --- Order Detail DTOs (fetched from MeLi API in real-time) ---

public class MeliOrderDetailResponse
{
    public List<MeliOrderDetailDto> Orders { get; set; } = new();
}

public class MeliOrderDetailDto
{
    public long MeliOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime? DateClosed { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyId { get; set; } = "ARS";
    public long? PackId { get; set; }

    // Buyer
    public long BuyerId { get; set; }
    public string BuyerNickname { get; set; } = string.Empty;
    public string? BuyerFirstName { get; set; }
    public string? BuyerLastName { get; set; }

    // Items
    public List<MeliOrderItemDetail> Items { get; set; } = new();

    // Payments
    public List<MeliPaymentDetail> Payments { get; set; } = new();

    // Totals
    public decimal? ShippingCost { get; set; }
    public decimal? TotalSaleFee { get; set; }
    public decimal? TaxesAmount { get; set; }
}

public class MeliOrderItemDetail
{
    public string ItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? SaleFee { get; set; }
}

public class MeliPaymentDetail
{
    public long PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentType { get; set; }
    public string? PaymentMethodId { get; set; }
    public decimal TransactionAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? TaxesAmount { get; set; }
    public DateTime? DateApproved { get; set; }
    public int? Installments { get; set; }
}

// --- MeliItem DTOs ---

public record MeliItemDto(
    int Id,
    string MeliItemId,
    int MeliAccountId,
    string AccountNickname,
    string Title,
    string? CategoryId,
    decimal Price,
    string CurrencyId,
    int AvailableQuantity,
    int SoldQuantity,
    string Status,
    string? Condition,
    string? ListingTypeId,
    string? Thumbnail,
    string? Permalink,
    string? Sku,
    string? UserProductId,
    string? FamilyId,
    string? FamilyName,
    DateTime? DateCreated,
    DateTime? LastUpdated
);

public record MeliItemsResponse(List<MeliItemDto> Items, int Total);

public record MeliItemSyncResult(int TotalSynced, int TotalErrors, List<string> Errors);
