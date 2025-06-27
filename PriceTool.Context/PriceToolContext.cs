using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PriceTool.Context.Models;

namespace PriceTool.Context;

public class PriceToolContext : DbContext
{
    public PriceToolContext(DbContextOptions<PriceToolContext> options) : base(options)
    {
    }    
    
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientProductDiscount> ClientProductDiscounts { get; set; }
    public DbSet<Domain> Domains { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListEntry> DomainPriceListEntries { get; set; }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.DateCreated = DateTime.UtcNow;
            }

            entity.DateModified = DateTime.UtcNow;
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Client entity
        modelBuilder.Entity<Client>()
            .HasOne(c => c.PriceList)
            .WithMany()
            .HasForeignKey(c => c.PriceListId);

        // Configure ClientProductDiscount entity
        modelBuilder.Entity<ClientProductDiscount>()
            .HasOne(cpd => cpd.Client)
            .WithMany()
            .HasForeignKey(cpd => cpd.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientProductDiscount>()
            .HasOne(cpd => cpd.Product)
            .WithMany()
            .HasForeignKey(cpd => cpd.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Domain entity
        modelBuilder.Entity<Domain>()
            .HasOne(d => d.Client)
            .WithMany()
            .HasForeignKey(d => d.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceListEntry>()
            .HasOne(dple => dple.Product)
            .WithMany()
            .HasForeignKey(dple => dple.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceListEntry>()
            .HasOne(dple => dple.PriceList)
            .WithMany()
            .HasForeignKey(dple => dple.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
