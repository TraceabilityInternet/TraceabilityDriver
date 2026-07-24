using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TraceabilityDriver.Services
{
    /// <summary>
    /// Creates <see cref="ApplicationDbContext"/> instances for the EF Core design-time tools.
    /// </summary>
    /// <remarks>
    /// Program.cs builds the host manually through <see cref="Startup"/>, so `dotnet ef` cannot
    /// discover the application's service provider on its own. This factory gives the tooling a
    /// context wired for SQL Server; the connection string is a placeholder because design-time
    /// commands (migrations add/script) never open a connection.
    /// </remarks>
    public class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <summary>
        /// Creates a context configured for SQL Server using a placeholder connection string.
        /// </summary>
        /// <param name="args">Arguments passed by the design-time tools. Not used.</param>
        /// <returns>A context suitable for generating migrations.</returns>
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=TraceabilityDriverDesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
