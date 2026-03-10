using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductListDto>> GetAllAsync()
    {
        return await _db.Products
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductListDto(
                p.Id, p.Title, p.Description,
                p.Brand, p.Model, p.Sku,
                p.Photo1, p.Photo2, p.Photo3,
                p.CostPrice, p.RetailPrice, p.Stock,
                p.IsActive, p.CreatedAt, p.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<ProductListDto?> GetByIdAsync(int id)
    {
        return await _db.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductListDto(
                p.Id, p.Title, p.Description,
                p.Brand, p.Model, p.Sku,
                p.Photo1, p.Photo2, p.Photo3,
                p.CostPrice, p.RetailPrice, p.Stock,
                p.IsActive, p.CreatedAt, p.UpdatedAt
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ProductListDto?> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Title = request.Title,
            Description = request.Description,
            Brand = request.Brand,
            Model = request.Model,
            Sku = request.Sku,
            Photo1 = request.Photo1,
            Photo2 = request.Photo2,
            Photo3 = request.Photo3,
            CostPrice = request.CostPrice,
            RetailPrice = request.RetailPrice,
            Stock = request.Stock,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return new ProductListDto(
            product.Id, product.Title, product.Description,
            product.Brand, product.Model, product.Sku,
            product.Photo1, product.Photo2, product.Photo3,
            product.CostPrice, product.RetailPrice, product.Stock,
            product.IsActive, product.CreatedAt, product.UpdatedAt
        );
    }

    public async Task<ProductListDto?> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return null;

        if (request.Title is not null) product.Title = request.Title;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Brand is not null) product.Brand = request.Brand;
        if (request.Model is not null) product.Model = request.Model;
        if (request.Sku is not null) product.Sku = request.Sku == "" ? null : request.Sku;
        if (request.Photo1 is not null) product.Photo1 = request.Photo1 == "" ? null : request.Photo1;
        if (request.Photo2 is not null) product.Photo2 = request.Photo2 == "" ? null : request.Photo2;
        if (request.Photo3 is not null) product.Photo3 = request.Photo3 == "" ? null : request.Photo3;
        if (request.CostPrice.HasValue) product.CostPrice = request.CostPrice.Value;
        if (request.RetailPrice.HasValue) product.RetailPrice = request.RetailPrice.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ProductListDto(
            product.Id, product.Title, product.Description,
            product.Brand, product.Model, product.Sku,
            product.Photo1, product.Photo2, product.Photo3,
            product.CostPrice, product.RetailPrice, product.Stock,
            product.IsActive, product.CreatedAt, product.UpdatedAt
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return false;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }
}
