using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TraceabilityDriver.Models;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;

namespace TraceabilityDriver.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EventSearchSqlDocument> EventSearchDocuments { get; set; } = null!;

        public DbSet<EPCISEventSqlDocument> EPCISEvents { get; set; }

        public DbSet<MasterDataSqlDocument> MasterDataDocuments { get; set; }

        public DbSet<SyncHistoryItem> SyncHistory { get; set; }

        public DbSet<LogModel> Logs { get; set; }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.EPCs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.ProductGTINs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.LocationGLNs).ToJson();
            //modelBuilder.Entity<EPCISEventDocument>().Property(x => x.PartyPGLNs).ToJson();

            // Add index for EventId on EPCISEventSqlDocument
            modelBuilder.Entity<EPCISEventSqlDocument>()
                .HasIndex(e => e.EventId)
                .IsUnique(true)
                .HasDatabaseName("IX_EPCISEvents_EventId");

            // Add index to EventSearchSqlDocument
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.EventId)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_EventId");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.EventTime)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_EventTime");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.EPC)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_EPC");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.ProductGTIN)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_ProductGTIN");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.LocationGLN)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_LocationGLN");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.PartyPGLN)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_PartyPGLN");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.BizStep)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_BizStep");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.Action)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_Action");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.RecordTime)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_RecordTime");

            modelBuilder.Entity<SyncHistoryItem>()
                .Property(e => e.Memory)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, _jsonOptions) ?? new());
        }
    }
}
