using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbraceInnerCritic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStrengthEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StrengthEntryCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StrengthEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Event = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrengthEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrengthEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StrengthEntries_UserId_CreatedAt_Id",
                table: "StrengthEntries",
                columns: new[] { "UserId", "CreatedAt", "Id" });

            // ASP.NET Identity owns authorization; Supabase client roles cannot access these records.
            migrationBuilder.Sql("""
                ALTER TABLE "StrengthEntries" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "StrengthEntries" FROM PUBLIC;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                        REVOKE ALL ON TABLE "StrengthEntries" FROM anon;
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                        REVOKE ALL ON TABLE "StrengthEntries" FROM authenticated;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrengthEntries");

            migrationBuilder.DropColumn(
                name: "StrengthEntryCount",
                table: "AspNetUsers");
        }
    }
}
