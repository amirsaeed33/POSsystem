using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <summary>
    /// Baseline Code First sync. Schema for recent features was already applied by
    /// the earlier SQL migrations (restaurant, deals, car service, etc.).
    /// This migration only updates SmartPosDbContextModelSnapshot so future
    /// <c>dotnet ef migrations add</c> / <c>database update</c> work as Code First.
    /// </summary>
    public partial class Sync_CodeFirst_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: database already matches the model via prior SQL migrations.
            // Snapshot/Designer carry the Code First model state for subsequent migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op baseline.
        }
    }
}
