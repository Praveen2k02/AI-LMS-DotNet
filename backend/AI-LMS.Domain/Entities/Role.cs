using AI_LMS.Domain.Common;

namespace AI_LMS.Domain.Entities;

public sealed class Role : BaseEntity
{
    public required string Name { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
