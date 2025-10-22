using System;
using System.Collections.Generic;

namespace CloudCore.Domain.Entities;

public partial class TeamspaceMember
{
    public int Id { get; set; }

    public int TeamspaceId { get; set; }

    public int UserId { get; set; }

    public string? PermissionLevel { get; set; }

    public int? InvitedBy { get; set; }

    public virtual User? InvitedByNavigation { get; set; }

    public virtual Teamspace Teamspace { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}