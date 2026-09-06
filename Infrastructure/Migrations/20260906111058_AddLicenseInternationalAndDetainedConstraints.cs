using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseInternationalAndDetainedConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TestAppointments_CreatedByUserID_AppointmentDate",
                table: "TestAppointments",
                columns: new[] { "CreatedByUserID", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TestAppointments_LocalDrivingLicenseApplicationID_AppointmentDate",
                table: "TestAppointments",
                columns: new[] { "LocalDrivingLicenseApplicationID", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TestAppointments_LocalDrivingLicenseApplicationID_TestTypeID",
                table: "TestAppointments",
                columns: new[] { "LocalDrivingLicenseApplicationID", "TestTypeID" });

            migrationBuilder.CreateIndex(
                name: "IX_InternationalLicenses_DriverID",
                table: "InternationalLicenses",
                column: "DriverID",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DetainedLicenses_LicenseID_IsReleased",
                table: "DetainedLicenses",
                columns: new[] { "LicenseID", "IsReleased" },
                unique: true,
                filter: "[IsReleased] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_DriverID_LicenseClass",
                table: "Licenses",
                columns: new[] { "DriverID", "LicenseClass" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestAppointments_CreatedByUserID_AppointmentDate",
                table: "TestAppointments");

            migrationBuilder.DropIndex(
                name: "IX_TestAppointments_LocalDrivingLicenseApplicationID_AppointmentDate",
                table: "TestAppointments");

            migrationBuilder.DropIndex(
                name: "IX_TestAppointments_LocalDrivingLicenseApplicationID_TestTypeID",
                table: "TestAppointments");

            migrationBuilder.DropIndex(
                name: "IX_InternationalLicenses_DriverID",
                table: "InternationalLicenses");

            migrationBuilder.DropIndex(
                name: "IX_DetainedLicenses_LicenseID_IsReleased",
                table: "DetainedLicenses");

            migrationBuilder.DropIndex(
                name: "IX_Licenses_DriverID_LicenseClass",
                table: "Licenses");
        }
    }
}
