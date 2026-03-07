using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<MeliAccount> MeliAccounts => Set<MeliAccount>();
    public DbSet<MeliOrder> MeliOrders => Set<MeliOrder>();
    public DbSet<MeliItem> MeliItems => Set<MeliItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.RoleNav)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.HasIndex(i => i.Provider).IsUnique();
        });

        modelBuilder.Entity<MeliAccount>(entity =>
        {
            entity.HasIndex(a => a.MeliUserId).IsUnique();
        });

        modelBuilder.Entity<MeliOrder>(entity =>
        {
            entity.HasIndex(o => new { o.MeliOrderId, o.ItemId }).IsUnique();
            entity.HasIndex(o => o.MeliAccountId);
            entity.HasIndex(o => o.DateCreated);
            entity.HasIndex(o => o.PackId);
            entity.HasOne(o => o.MeliAccount)
                  .WithMany()
                  .HasForeignKey(o => o.MeliAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MeliItem>(entity =>
        {
            entity.HasIndex(i => i.MeliItemId).IsUnique();
            entity.HasIndex(i => i.MeliAccountId);
            entity.HasIndex(i => i.Status);
            entity.HasIndex(i => i.UserProductId);
            entity.HasIndex(i => i.FamilyId);
            entity.HasOne(i => i.MeliAccount)
                  .WithMany()
                  .HasForeignKey(i => i.MeliAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
