using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotesSolution.Core.Models;

namespace NotesSolution.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets for your domain models
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<Tag> Tags => Set<Tag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Many-to-many relationship configuration
            modelBuilder.Entity<Note>()
                .HasMany(n => n.Tags)
                .WithMany(t => t.Notes);

            // PostgreSQL constraints
            modelBuilder.Entity<Note>(entity =>
            {
                entity.Property(n => n.Name).HasMaxLength(100);
                entity.Property(n => n.Description).HasMaxLength(2000);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.Property(t => t.Name).HasMaxLength(50);
                entity.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
            });
        }
    }
}