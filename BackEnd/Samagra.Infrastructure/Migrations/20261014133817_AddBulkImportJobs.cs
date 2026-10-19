using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Samagra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SucceededRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BulkImportRowErrors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportRowErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkImportRowErrors_BulkImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "BulkImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportJobs_CreatedBy_CreatedAtUtc",
                table: "BulkImportJobs",
                columns: new[] { "CreatedBy", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportRowErrors_JobId_RowNumber",
                table: "BulkImportRowErrors",
                columns: new[] { "JobId", "RowNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkImportRowErrors");

            migrationBuilder.DropTable(
                name: "BulkImportJobs");
        }
    }
}
