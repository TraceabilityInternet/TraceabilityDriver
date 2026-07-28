using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using TraceabilityDriver.Models.DB;
using TraceabilityDriver.Models.DB.Sql;
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

            // Synced events are upserted by (event key, deployment version): the event id is a content
            // hash that changes while an event is still accumulating source rows across sync runs, so it
            // cannot be the upsert key. Traceback rows carry a null event key and null deployment version
            // and are excluded from the unique index by its filter.
            modelBuilder.Entity<EPCISEventSqlDocument>()
                .Property(e => e.DeploymentVersion)
                .HasMaxLength(100);
            modelBuilder.Entity<EPCISEventSqlDocument>()
                .Property(e => e.EventKey)
                .HasMaxLength(450);
            modelBuilder.Entity<EPCISEventSqlDocument>()
                .HasIndex(e => new { e.EventKey, e.DeploymentVersion })
                .IsUnique(true)
                .HasFilter("[EventKey] IS NOT NULL")
                .HasDatabaseName("IX_EPCISEvents_EventKey_DeploymentVersion");

            // The event id is indexed non-unique: the traceback skip check and query fetches look events
            // up by id, and the same content hash can legitimately appear on multiple rows (a traceback
            // copy of a synced event, or the same content synced under two deployment versions).
            modelBuilder.Entity<EPCISEventSqlDocument>()
                .HasIndex(e => e.EventId)
                .IsUnique(false)
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

            // The deployment version index lets queries narrow the search rows to the currently
            // configured deployment version efficiently (traceback rows carry a null version). The event
            // key index scopes the delete-then-insert rebuild when a synced event is stored again.
            modelBuilder.Entity<EventSearchSqlDocument>()
                .Property(e => e.DeploymentVersion)
                .HasMaxLength(100);
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.DeploymentVersion)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_DeploymentVersion");
            modelBuilder.Entity<EventSearchSqlDocument>()
                .Property(e => e.EventKey)
                .HasMaxLength(450);
            modelBuilder.Entity<EventSearchSqlDocument>()
                .HasIndex(e => e.EventKey)
                .IsUnique(false)
                .HasDatabaseName("IX_EventSearchDocuments_EventKey");

            // Master data is unique per (element id, deployment version). The element id needs an
            // explicit max length so it can participate in the index. Traceback rows carry a null
            // deployment version and are excluded by the null filter EF adds to the unique index;
            // their one-row-per-element-id rule is enforced by the skip-if-exists store logic instead.
            modelBuilder.Entity<MasterDataSqlDocument>()
                .Property(e => e.ElementId)
                .HasMaxLength(450);
            modelBuilder.Entity<MasterDataSqlDocument>()
                .Property(e => e.DeploymentVersion)
                .HasMaxLength(100);
            modelBuilder.Entity<MasterDataSqlDocument>()
                .HasIndex(e => new { e.ElementId, e.DeploymentVersion })
                .IsUnique(true)
                .HasDatabaseName("IX_MasterDataDocuments_ElementId_DeploymentVersion");

            modelBuilder.Entity<SyncHistoryItem>()
                .Property(e => e.Memory)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, _jsonOptions) ?? new());

            // The previous-sync lookup filters by deployment version and takes the latest by end time.
            modelBuilder.Entity<SyncHistoryItem>()
                .Property(e => e.DeploymentVersion)
                .HasMaxLength(100);
            modelBuilder.Entity<SyncHistoryItem>()
                .HasIndex(e => new { e.DeploymentVersion, e.EndTime })
                .IsUnique(false)
                .HasDatabaseName("IX_SyncHistory_DeploymentVersion_EndTime");

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
