using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class User : AuditableEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string? Phone { get; set; }
    public string? ProfileImage { get; set; }
    public bool IsActive { get; set; } = true;
    public Student? Student { get; set; }
    public Trainer? Trainer { get; set; }
}
