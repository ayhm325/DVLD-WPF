using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddUniqueLocalApplicationApplicationId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "UX_LocalDrivingLicenseApplications_ApplicationID",
            table: "LocalDrivingLicenseApplications",
            column: "ApplicationID",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_LocalDrivingLicenseApplications_ApplicationID",
            table: "LocalDrivingLicenseApplications");
    }
}