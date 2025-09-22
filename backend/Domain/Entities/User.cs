using System;
using System.Collections.Generic;
using CloudCore.Domain.Entities;

namespace CloudCore.Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? SubscriptionPlan { get; set; }

    public string? SubscriptionStatus { get; set; }

    public DateTime? SubscriptionExpiresAt { get; set; }

    public long? PersonalStorageUsedMb { get; set; }

    public int? TeamspacesOwned { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<TeamspaceMember> TeamspaceMemberInvitedByNavigations { get; set; } = new List<TeamspaceMember>();

    public virtual ICollection<TeamspaceMember> TeamspaceMemberUsers { get; set; } = new List<TeamspaceMember>();

    public virtual ICollection<Teamspace> Teamspaces { get; set; } = new List<Teamspace>();
}
