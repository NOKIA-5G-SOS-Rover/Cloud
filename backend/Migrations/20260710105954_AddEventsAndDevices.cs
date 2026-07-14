using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsAndDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SOSAlerts");

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    RoverId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlertType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LocationX = table.Column<double>(type: "double", nullable: false),
                    LocationY = table.Column<double>(type: "double", nullable: false),
                    BoundingBoxWidth = table.Column<double>(type: "double", nullable: false),
                    BoundingBoxHeight = table.Column<double>(type: "double", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false),
                    MotorHaltRequested = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.CreateTable(
                name: "SOSAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlertType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BoundingBoxHeight = table.Column<double>(type: "double", nullable: false),
                    BoundingBoxWidth = table.Column<double>(type: "double", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LocationX = table.Column<double>(type: "double", nullable: false),
                    LocationY = table.Column<double>(type: "double", nullable: false),
                    MotorHaltRequested = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RoverId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOSAlerts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
