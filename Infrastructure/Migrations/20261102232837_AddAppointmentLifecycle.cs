using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserID",
                table: "TestAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "TestAppointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoShowDate",
                table: "TestAppointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TestAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestAppointments_CancelledByUserID",
                table: "TestAppointments",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TestAppointments_Status",
                table: "TestAppointments",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_TestAppointments_Users_CancelledByUserID",
                table: "TestAppointments",
                column: "CancelledByUserID",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            // Backfill rules:
            // 1. Consistent Scheduled: Unlocked appointments without a test record become Scheduled (1).
            migrationBuilder.Sql(
                @"UPDATE TestAppointments
                  SET Status = 1
                  WHERE IsLocked = 0
                    AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

            // 2. Consistent Completed: Locked appointments with associated Test records become Completed (2).
            migrationBuilder.Sql(
                @"UPDATE TestAppointments
                  SET Status = 2
                  WHERE IsLocked = 1
                    AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

            // 3. Inconsistent Unlocked with Test: If an appointment has a Test record, it is Completed and must be locked.
            migrationBuilder.Sql(
                @"UPDATE TestAppointments
                  SET Status = 2,
                      IsLocked = 1
                  WHERE IsLocked = 0
                    AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);");

            // 4. Inconsistent Locked without Test (Temporal Safety Rule):
            //    An appointment becomes NoShow (3) ONLY when:
            //      CurrentJordanTime >= AppointmentDate + 40 minutes (i.e. slot has elapsed > 40 minutes ago)
            //      AND no Test record exists.
            migrationBuilder.Sql(
                @"UPDATE TestAppointments
                  SET Status = 3,
                      NoShowDate = DATEADD(minute, 40, AppointmentDate)
                  WHERE IsLocked = 1
                    AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID)
                    AND DATEADD(minute, 40, AppointmentDate) <= CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Jordan Standard Time' AS datetime2);");

            // 5. Inconsistent Locked without Test (Future or Not-Yet-Expired):
            //    If IsLocked = 1 and NO Test exists, but AppointmentDate + 40 minutes > CurrentJordanTime:
            //    - It cannot be NoShow (the 40-minute slot has not expired).
            //    - It cannot be Completed (no test was administered).
            //    - It cannot be Scheduled (being locked contradicts the definition of an open/scheduled slot).
            //    - Cancellation cannot be assumed without audit evidence.
            //    Therefore, this is an unresolved ambiguity. The migration does NOT guess:
            //    Status remains NULL (unclassified), preserving original record data for administrative review.

            // 6. Default constraint for subsequent new inserts:
            migrationBuilder.Sql(
                @"ALTER TABLE TestAppointments ADD CONSTRAINT DF_TestAppointments_Status DEFAULT 1 FOR Status;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestAppointments_Users_CancelledByUserID",
                table: "TestAppointments");

            migrationBuilder.DropIndex(
                name: "IX_TestAppointments_CancelledByUserID",
                table: "TestAppointments");

            migrationBuilder.DropIndex(
                name: "IX_TestAppointments_Status",
                table: "TestAppointments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserID",
                table: "TestAppointments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "TestAppointments");

            migrationBuilder.DropColumn(
                name: "NoShowDate",
                table: "TestAppointments");

            migrationBuilder.Sql(
                @"ALTER TABLE TestAppointments DROP CONSTRAINT IF EXISTS DF_TestAppointments_Status;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TestAppointments");
        }
    }
}
