using System;
using System.Collections.Generic;
using CloudCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace CloudCore.Data.Context;

public partial class CloudCoreDbContext : DbContext
{
    public CloudCoreDbContext()
    {
    }

    public CloudCoreDbContext(DbContextOptions<CloudCoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Teamspace> Teamspaces { get; set; }

    public virtual DbSet<TeamspaceMember> TeamspaceMembers { get; set; }

    public virtual DbSet<User> Users { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("items");

            entity.HasIndex(e => e.Name, "idx_name");

            entity.HasIndex(e => new { e.ParentId, e.UserId }, "idx_parent_user");

            entity.HasIndex(e => new { e.TeamspaceId, e.IsDeleted }, "idx_teamspace_items");

            entity.HasIndex(e => new { e.UserId, e.Type, e.IsDeleted }, "idx_user_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessLevel)
                .HasDefaultValueSql("'private'")
                .HasColumnType("enum('private','team_read','team_write')")
                .HasColumnName("access_level");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp")
                .HasColumnName("deleted_at");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_deleted");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.TeamspaceId).HasColumnName("teamspace_id");
            entity.Property(e => e.Type)
                .HasColumnType("enum('file','folder')")
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("items_ibfk_1");

            entity.HasOne(d => d.Teamspace).WithMany(p => p.Items)
                .HasForeignKey(d => d.TeamspaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("items_ibfk_3");

            entity.HasOne(d => d.User).WithMany(p => p.Items)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("items_ibfk_2");
        });

        modelBuilder.Entity<Teamspace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("teamspaces");

            entity.HasIndex(e => e.AdminUserId, "idx_admin_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.MemberCount)
                .HasDefaultValueSql("'1'")
                .HasColumnName("member_count");
            entity.Property(e => e.MemberLimit).HasColumnName("member_limit");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.StorageLimitMb).HasColumnName("storage_limit_mb");
            entity.Property(e => e.StorageUsedMb)
                .HasDefaultValueSql("'0'")
                .HasColumnName("storage_used_mb");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.Teamspaces)
                .HasForeignKey(d => d.AdminUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teamspaces_ibfk_1");
        });

        modelBuilder.Entity<TeamspaceMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("teamspace_members");

            entity.HasIndex(e => e.InvitedBy, "invited_by");

            entity.HasIndex(e => new { e.TeamspaceId, e.UserId }, "unique_membership").IsUnique();

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.PermissionLevel)
                .HasDefaultValueSql("'read'")
                .HasColumnType("enum('read','write','admin')")
                .HasColumnName("permission_level");
            entity.Property(e => e.TeamspaceId).HasColumnName("teamspace_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.TeamspaceMemberInvitedByNavigations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("teamspace_members_ibfk_3");

            entity.HasOne(d => d.Teamspace).WithMany(p => p.TeamspaceMembers)
                .HasForeignKey(d => d.TeamspaceId)
                .HasConstraintName("teamspace_members_ibfk_1");

            entity.HasOne(d => d.User).WithMany(p => p.TeamspaceMemberUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("teamspace_members_ibfk_2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IsEmailVerified)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_email_verified");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PersonalStorageUsedMb)
                .HasDefaultValueSql("'0'")
                .HasColumnName("personal_storage_used_mb");
            entity.Property(e => e.SubscriptionPlan)
                .HasDefaultValueSql("'free'")
                .HasColumnType("enum('free','premium','enterprise')")
                .HasColumnName("subscription_plan");
            entity.Property(e => e.TeamspacesOwned)
                .HasDefaultValueSql("'0'")
                .HasColumnName("teamspaces_owned");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}