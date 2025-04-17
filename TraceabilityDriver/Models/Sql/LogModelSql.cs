namespace TraceabilityDriver.Models.Sql
{
    public class LogModelSql
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public string? MessageTemplate { get; set; }
        public LogLevel Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Exception { get; set; }
        public string? Properties { get; set; }
        public string? LogEvent { get; set; }

        // Additional columns
        public string? MachineName { get; set; }
        public int? ProcessId { get; set; }
        public int? ThreadId { get; set; }
    }
}
