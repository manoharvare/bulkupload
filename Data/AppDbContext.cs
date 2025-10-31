using Microsoft.EntityFrameworkCore;
using OilAndGasImport.Models;

namespace OilAndGasImport.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ResourceSpread> ResourceSpreads => Set<ResourceSpread>();
    public DbSet<CraftSpread> CraftSpreads => Set<CraftSpread>();
}
