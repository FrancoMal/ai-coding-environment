using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record RoleDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int UserCount
);

public record CreateRoleRequest(
    [Required][MaxLength(50)] string Name,
    [MaxLength(255)] string? Description
);

public record UpdateRoleRequest(
    [MaxLength(50)] string? Name,
    [MaxLength(255)] string? Description
);
