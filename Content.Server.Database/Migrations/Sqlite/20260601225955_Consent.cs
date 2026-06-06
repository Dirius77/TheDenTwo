using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Consent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "global_consent_info",
                columns: table => new
                {
                    global_consent_info_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    consent_id = table.Column<string>(type: "TEXT", nullable: false),
                    consent_value = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_consent_info", x => x.global_consent_info_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_global_consent_info_global_consent_info_id_user_id",
                table: "global_consent_info",
                columns: new[] { "global_consent_info_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_consent_info");
        }
    }
}
