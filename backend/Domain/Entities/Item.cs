using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudCore.Domain.Entities;

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

    [JsonIgnore]
    public virtual ICollection<Item> InverseParent { get; set; } = new List<Item>();
    [JsonIgnore]
    public virtual Item? Parent { get; set; }
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
