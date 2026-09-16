using EmbraceInnerCritic.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbraceInnerCritic.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260916120000_AddDiaryEntryQuota")]
public partial class AddDiaryEntryQuota : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DiaryEntryCount",
            table: "AspNetUsers",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "AspNetUsers" AS users
            SET "DiaryEntryCount" = counts."EntryCount"
            FROM (
                SELECT "UserId", COUNT(*)::integer AS "EntryCount"
                FROM "DiaryEntries"
                GROUP BY "UserId"
            ) AS counts
            WHERE users."Id" = counts."UserId";
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_AspNetUsers_DiaryEntryCount_NonNegative",
            table: "AspNetUsers",
            sql: "\"DiaryEntryCount\" >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AspNetUsers_DiaryEntryCount_NonNegative",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "DiaryEntryCount",
            table: "AspNetUsers");
    }
}
