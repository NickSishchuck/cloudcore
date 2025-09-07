using System;
using System.Collections.Generic;

namespace CloudCore.Models;

public partial class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public int? ParentId { get; set; }

    public int UserId { get; set; }

    public string? FilePath { get; set; }

    public long? FileSize { get; set; }

    public string? MimeType { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Item> InverseParent { get; set; } = new List<Item>();

    public virtual Item? Parent { get; set; }

    public virtual User User { get; set; } = null!;
}
