using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record ProductListDto(
    int Id,
    string Title,
    string? Description,
    string? Brand,
    string? Model,
    string? Sku,
    string? Photo1,
    string? Photo2,
    string? Photo3,
    decimal CostPrice,
    decimal RetailPrice,
    int Stock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateProductRequest(
    [Required][MaxLength(200)] string Title,
    string? Description,
    [MaxLength(100)] string? Brand,
    [MaxLength(100)] string? Model,
    [MaxLength(100)] string? Sku,
    string? Photo1,
    string? Photo2,
    string? Photo3,
    decimal CostPrice,
    decimal RetailPrice,
    int Stock
);

public record UpdateProductRequest(
    [MaxLength(200)] string? Title,
    string? Description,
    [MaxLength(100)] string? Brand,
    [MaxLength(100)] string? Model,
    [MaxLength(100)] string? Sku,
    string? Photo1,
    string? Photo2,
    string? Photo3,
    decimal? CostPrice,
    decimal? RetailPrice,
    int? Stock,
    bool? IsActive
);
