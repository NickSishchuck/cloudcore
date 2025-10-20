using System;
using System.Collections.Generic;


namespace CloudCore.Domain.Entities;

public partial class Teamspace
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int AdminUserId { get; set; }

    public long StorageLimitMb { get; set; }

    public int MemberLimit { get; set; }

    public long? StorageUsedMb { get; set; }

    public int? MemberCount { get; set; }

    public virtual User AdminUser { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<TeamspaceMember> TeamspaceMembers { get; set; } = new List<TeamspaceMember>();
}