using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stride.Metrics.ServerApp.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpToLocations",
                columns: table => new
                {
                    IpFrom = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpTo = table.Column<long>(type: "bigint", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpToLocations", x => x.IpFrom);
                });

            migrationBuilder.CreateTable(
                name: "MetricApps",
                columns: table => new
                {
                    AppId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricApps", x => x.AppId);
                });

            migrationBuilder.CreateTable(
                name: "MetricEventDefinitions",
                columns: table => new
                {
                    MetricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricEventDefinitions", x => x.MetricId);
                });

            migrationBuilder.CreateTable(
                name: "MetricInstalls",
                columns: table => new
                {
                    InstallId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricInstalls", x => x.InstallId);
                });

            migrationBuilder.CreateTable(
                name: "MetricMarkerGroups",
                columns: table => new
                {
                    MarkerGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricMarkerGroups", x => x.MarkerGroupId);
                });

            migrationBuilder.CreateTable(
                name: "MetricEvents",
                columns: table => new
                {
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppId = table.Column<int>(type: "int", nullable: false),
                    InstallId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    MetricId = table.Column<int>(type: "int", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MetricValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricEvents", x => new { x.Timestamp, x.AppId, x.InstallId, x.SessionId, x.MetricId });
                    table.ForeignKey(
                        name: "FK_MetricEvents_MetricApps_AppId",
                        column: x => x.AppId,
                        principalTable: "MetricApps",
                        principalColumn: "AppId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetricEvents_MetricEventDefinitions_MetricId",
                        column: x => x.MetricId,
                        principalTable: "MetricEventDefinitions",
                        principalColumn: "MetricId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetricEvents_MetricInstalls_InstallId",
                        column: x => x.InstallId,
                        principalTable: "MetricInstalls",
                        principalColumn: "InstallId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricMarkers",
                columns: table => new
                {
                    MarkerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarkerGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricMarkers", x => x.MarkerId);
                    table.ForeignKey(
                        name: "FK_MetricMarkers_MetricMarkerGroups_MarkerGroupId",
                        column: x => x.MarkerGroupId,
                        principalTable: "MetricMarkerGroups",
                        principalColumn: "MarkerGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpToLocations_IpFrom",
                table: "IpToLocations",
                column: "IpFrom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpToLocations_IpTo",
                table: "IpToLocations",
                column: "IpTo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricApps_AppGuid",
                table: "MetricApps",
                column: "AppGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricApps_AppName",
                table: "MetricApps",
                column: "AppName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricEventDefinitions_MetricGuid",
                table: "MetricEventDefinitions",
                column: "MetricGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricEventDefinitions_MetricName",
                table: "MetricEventDefinitions",
                column: "MetricName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricEvents_AppId",
                table: "MetricEvents",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricEvents_InstallId",
                table: "MetricEvents",
                column: "InstallId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricEvents_MetricId",
                table: "MetricEvents",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricInstalls_InstallGuid",
                table: "MetricInstalls",
                column: "InstallGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricMarkers_MarkerGroupId",
                table: "MetricMarkers",
                column: "MarkerGroupId");
            
            migrationBuilder.Sql(@"CREATE FUNCTION [dbo].[IPAddressToInteger] (@IP AS varchar(15))
                    RETURNS bigint
                    AS
                    BEGIN
                    RETURN (CONVERT(bigint, PARSENAME(@IP,1)) +
                    CONVERT(bigint, PARSENAME(@IP,2)) * 256 +
                    CONVERT(bigint, PARSENAME(@IP,3)) * 65536 +
                    CONVERT(bigint, PARSENAME(@IP,4)) * 16777216)
                    END");
                    
            migrationBuilder.Sql(@"CREATE FUNCTION [dbo].[IPAddressToCountry] (@IP AS varchar(15))
                    RETURNS nvarchar(64)
                    AS
                    BEGIN
                    RETURN (SELECT TOP 1 CountryName FROM IpToLocations WHERE dbo.IPAddressToInteger(@IP) BETWEEN IpToLocations.IpFrom AND IpToLocations.IpTo ORDER BY IpToLocations.IpFrom DESC)
                    END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpToLocations");

            migrationBuilder.DropTable(
                name: "MetricEvents");

            migrationBuilder.DropTable(
                name: "MetricMarkers");

            migrationBuilder.DropTable(
                name: "MetricApps");

            migrationBuilder.DropTable(
                name: "MetricEventDefinitions");

            migrationBuilder.DropTable(
                name: "MetricInstalls");

            migrationBuilder.DropTable(
                name: "MetricMarkerGroups");
        }
    }
}
