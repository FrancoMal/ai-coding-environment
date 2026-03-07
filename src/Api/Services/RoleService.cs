using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class RoleService
{
    private readonly AppDbContext _db;

    public RoleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        return await _db.Roles
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt,
                r.Users.Count
            ))
            .ToListAsync();
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        return await _db.Roles
            .Where(r => r.Id == id)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt,
                r.Users.Count
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<RoleDto?> CreateAsync(CreateRoleRequest request)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == request.Name))
            return null;

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return new RoleDto(role.Id, role.Name, role.Description, role.CreatedAt, 0);
    }

    public async Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return null;

        if (request.Name is not null && request.Name != role.Name)
        {
            if (await _db.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
                return null;
            role.Name = request.Name;
        }

        if (request.Description is not null) role.Description = request.Description;

        await _db.SaveChangesAsync();

        return new RoleDto(role.Id, role.Name, role.Description, role.CreatedAt, role.Users.Count);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return false;

        // No permitir borrar roles que tienen usuarios asignados
        if (role.Users.Any()) return false;

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return true;
    }
}
