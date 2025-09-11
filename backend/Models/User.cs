using System;
using System.Collections.Generic;

namespace CloudCore.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
