using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmbraceInnerCritic.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
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
