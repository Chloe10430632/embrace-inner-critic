using EmbraceInnerCritic.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbraceInnerCritic.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261001000000_BackfillUserCriticName")]
public partial class BackfillUserCriticName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "AspNetUsers" AS users
            SET "CriticName" = latest."CriticName"
            FROM (
                SELECT DISTINCT ON ("UserId") "UserId", "CriticName"
                FROM "DiaryEntries"
                ORDER BY "UserId", "CreatedAt" DESC, "Id" DESC
            ) AS latest
            WHERE users."Id" = latest."UserId";
            """);

        migrationBuilder.Sql(
            "UPDATE \"AspNetUsers\" SET \"CriticName\" = '山姆' WHERE \"CriticName\" IS NULL OR btrim(\"CriticName\") = ''; ");

        migrationBuilder.AlterColumn<string>(
            name: "CriticName",
            table: "AspNetUsers",
            type: "text",
            nullable: false,
            defaultValue: "山姆",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "CriticName",
            table: "AspNetUsers",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");
        // Backfilled user names may have been edited after migration; do not erase them.
    }
}
