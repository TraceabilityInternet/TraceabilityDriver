using Microsoft.EntityFrameworkCore;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;

namespace TraceabilityDriver.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EPCISEventDocument> EPCISEvents { get; set; }

        public DbSet<MasterDataDocument> MasterDataDocuments { get; set; }

        public DbSet<SyncHistoryItem> SyncHistory { get; set; }

        public DbSet<LogModel> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.EPCs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.ProductGTINs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.LocationGLNs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.PartyPGLNs).ToJson();

            modelBuilder.Entity<SyncHistoryItem>().OwnsOne(x => x.Memory, b =>
                {
                    b.WithOwner();
                    b.ToJson();
                }
            );
        }
    }
}
