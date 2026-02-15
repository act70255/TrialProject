using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "directories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreationOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_directories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_directories_directories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.Id);
                    table.UniqueConstraint("AK_files_Id_FileType", x => new { x.Id, x.FileType });
                    table.CheckConstraint("CK_files_extension", "Extension = lower(Extension) AND Extension LIKE '.%'");
                    table.CheckConstraint("CK_files_file_type", "FileType IN (1, 2, 3)");
                    table.CheckConstraint("CK_files_size_bytes", "SizeBytes >= 0");
                    table.ForeignKey(
                        name: "FK_files_directories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalTable: "directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_metadata",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Encoding = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_metadata", x => x.FileId);
                    table.CheckConstraint("CK_file_metadata_type_fields", "((FileType = 1 AND PageCount IS NOT NULL AND Width IS NULL AND Height IS NULL AND Encoding IS NULL) OR (FileType = 2 AND PageCount IS NULL AND Width IS NOT NULL AND Height IS NOT NULL AND Encoding IS NULL) OR (FileType = 3 AND PageCount IS NULL AND Width IS NULL AND Height IS NULL AND Encoding IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_file_metadata_files_FileId_FileType",
                        columns: x => new { x.FileId, x.FileType },
                        principalTable: "files",
                        principalColumns: new[] { "Id", "FileType" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_directories_ParentId_CreationOrder",
                table: "directories",
                columns: new[] { "ParentId", "CreationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_directories_ParentId_Name",
                table: "directories",
                columns: new[] { "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_metadata_FileId_FileType",
                table: "file_metadata",
                columns: new[] { "FileId", "FileType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_DirectoryId_CreationOrder",
                table: "files",
                columns: new[] { "DirectoryId", "CreationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_files_DirectoryId_Name",
                table: "files",
                columns: new[] { "DirectoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_Extension",
                table: "files",
                column: "Extension");

            migrationBuilder.CreateIndex(
                name: "IX_files_FileType",
                table: "files",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_files_RelativePath",
                table: "files",
                column: "RelativePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_metadata");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "directories");
        }
    }
}
