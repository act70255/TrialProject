using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTagSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                    table.CheckConstraint("CK_tags_color", "Color IN ('Red', 'Blue', 'Green')");
                    table.CheckConstraint("CK_tags_name", "Name IN ('Urgent', 'Work', 'Personal')");
                });

            migrationBuilder.CreateTable(
                name: "node_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_tags", x => x.Id);
                    table.CheckConstraint("CK_node_tags_single_target", "((DirectoryId IS NOT NULL AND FileId IS NULL) OR (DirectoryId IS NULL AND FileId IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_node_tags_directories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalTable: "directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_node_tags_files_FileId",
                        column: x => x.FileId,
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_node_tags_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tags",
                columns: new[] { "Id", "Color", "CreatedTime", "Name" },
                values: new object[,]
                {
                    { new Guid("17d22f5d-7fd0-4975-8f56-ce8ac2aa42e8"), "Green", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Personal" },
                    { new Guid("2c2c123c-45d6-4a89-8b9d-4d3b7fd72111"), "Red", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Urgent" },
                    { new Guid("74457bd5-0709-4afb-8a2f-7209f19766b6"), "Blue", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Work" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_node_tags_DirectoryId",
                table: "node_tags",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_node_tags_DirectoryId_TagId",
                table: "node_tags",
                columns: new[] { "DirectoryId", "TagId" },
                unique: true,
                filter: "DirectoryId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_node_tags_FileId",
                table: "node_tags",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_node_tags_FileId_TagId",
                table: "node_tags",
                columns: new[] { "FileId", "TagId" },
                unique: true,
                filter: "FileId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_node_tags_TagId",
                table: "node_tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name",
                table: "tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_tags");

            migrationBuilder.DropTable(
                name: "tags");
        }
    }
}
