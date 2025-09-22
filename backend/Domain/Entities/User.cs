using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudCore.Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    [JsonIgnore]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
