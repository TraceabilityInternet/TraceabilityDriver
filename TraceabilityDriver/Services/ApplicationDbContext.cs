using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using TraceabilityDriver.Models.MongoDB;
using TraceabilityDriver.Models.Sql;
using TraceabilityDriver.Models.Traceback;

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

        public DbSet<LogModelSql> Logs { get; set; }

        public DbSet<TracebackRecord> Tracebacks { get; set; }

        public DbSet<TracebackItem> TracebackItems { get; set; }

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

            // Configure log model
            var logLevelConverter = new ValueConverter<LogLevel, string>(
                x => x.ToString(),
                x => Enum.Parse<LogLevel>(x)
            );
            modelBuilder.Entity<LogModelSql>(entity =>
            {
                entity.ToTable("Logs"); // Match the table Serilog writes to
                entity.Property(x => x.Level).HasConversion(logLevelConverter);
                entity.Property(x => x.Level).HasColumnType("nvarchar(50)");
            });

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

            // String lists on the traceback record are stored as JSON, following the SyncHistoryItem.Memory pattern.
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, _jsonOptions) ?? new());

            modelBuilder.Entity<TracebackRecord>(entity =>
            {
                entity.ToTable("Tracebacks");
                entity.Property(x => x.Id).HasMaxLength(50);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(x => x.RequestedEpcs).HasConversion(stringListConverter);
                entity.Property(x => x.Errors).HasConversion(stringListConverter);
                entity.HasIndex(x => x.StartTime).HasDatabaseName("IX_Tracebacks_StartTime");
            });

            modelBuilder.Entity<TracebackItem>(entity =>
            {
                entity.ToTable("TracebackItems");
                entity.Property(x => x.Id).HasMaxLength(50);
                entity.Property(x => x.TracebackId).HasMaxLength(50);
                entity.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(50);
                entity.Property(x => x.ItemId).HasMaxLength(450);
                entity.HasIndex(x => new { x.TracebackId, x.ItemType, x.ItemId }).IsUnique(true).HasDatabaseName("IX_TracebackItems_Traceback_Type_Item");
                entity.HasIndex(x => x.TracebackId).HasDatabaseName("IX_TracebackItems_TracebackId");
            });
        }
    }
}
