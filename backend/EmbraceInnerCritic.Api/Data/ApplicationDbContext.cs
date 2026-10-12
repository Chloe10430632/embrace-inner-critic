using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmbraceInnerCritic.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<StrengthEntry> StrengthEntries => Set<StrengthEntry>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StrengthEntry>(entity =>
        {
            entity.Property(entry => entry.Event).HasMaxLength(2000);
            entity.Property(entry => entry.Notes).HasMaxLength(4000);
            entity.Property(entry => entry.Tags).HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<string[]>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                    (left, right) => left!.SequenceEqual(right!),
                    tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.GetHashCode())),
                    tags => tags.ToArray()));
            entity.HasIndex(entry => new { entry.UserId, entry.CreatedAt, entry.Id });
            entity.HasOne(entry => entry.User).WithMany().HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.CriticName).HasDefaultValue("山姆");
            entity.ToTable("AspNetUsers", table =>
                table.HasCheckConstraint(
                    "CK_AspNetUsers_DiaryEntryCount_NonNegative",
                    "\"DiaryEntryCount\" >= 0"));
        });

        builder.Entity<DiaryEntry>(entity =>
        {
            entity.Property(entry => entry.CriticName).HasMaxLength(64);
            entity.Property(entry => entry.AnswersJson).HasColumnType("jsonb");
            entity.HasIndex(entry => new { entry.UserId, entry.UpdatedAt });
            entity.HasOne(entry => entry.User)
                .WithMany(user => user.DiaryEntries)
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
