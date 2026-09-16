using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmbraceInnerCritic.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
