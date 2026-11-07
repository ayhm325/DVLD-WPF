import { lazy } from "react";
import { useNavigation } from "../../../context/useNavigation";

const LocalApplicationsPage = lazy(
  () => import("../../local-applications/LocalApplicationsPage"),
);
const TestAppointmentsPage = lazy(
  () => import("../../test-appointments/TestAppointmentsPage"),
);
const AppointmentDetailsPage = lazy(
  () => import("../../test-appointments/AppointmentDetailsPage"),
);
const ManageTestAppointmentsPage = lazy(
  () => import("../../test-appointments/ManageTestAppointmentsPage"),
);
const TakeExaminationPage = lazy(
  () => import("../../test-appointments/TakeExaminationPage"),
);
const ScheduleAppointmentPage = lazy(
  () => import("../../test-appointments/ScheduleAppointmentPage"),
);

export type AppointmentRoutesProps = {
  accessToken: string;
  activePage: string;
  userRole?: string | null;
};

export default function AppointmentRoutes({
  accessToken,
  activePage,
  userRole,
}: AppointmentRoutesProps) {
  const {
    navigate,
    selectedAppointmentId,
    appointmentDetailsSource,
    scheduleAppointmentContext,
    reopenModalTarget,
    clearReopenModalTarget,
    manageTestAppointmentsContext,
    takeExaminationContext,
    navigateToAppointmentDetails,
    navigateToScheduleAppointment,
    navigateToManageTestAppointments,
    navigateToTakeExamination,
    navigateToPersonLicenseHistory,
  } = useNavigation();

  if (activePage === "Local Applications") {
    return (
      <LocalApplicationsPage
        accessToken={accessToken}
        onShowLicenseHistory={navigateToPersonLicenseHistory}
        onShowAppointmentDetails={(id) =>
          navigateToAppointmentDetails(id, "Local Applications")
        }
        onScheduleAppointment={navigateToScheduleAppointment}
        onManageTestAppointments={navigateToManageTestAppointments}
        reopenModalTarget={reopenModalTarget}
        onClearReopenModalTarget={clearReopenModalTarget}
      />
    );
  }

  if (activePage === "Test Appointments") {
    return (
      <TestAppointmentsPage
        accessToken={accessToken}
        onNavigate={navigate}
        onShowAppointmentDetails={(id) =>
          navigateToAppointmentDetails(id, "Test Appointments")
        }
        onScheduleAppointment={navigateToScheduleAppointment}
        onTakeExamination={navigateToTakeExamination}
      />
    );
  }

  if (activePage === "Appointment Details") {
    return (
      <AppointmentDetailsPage
        accessToken={accessToken}
        appointmentId={selectedAppointmentId ?? null}
        onBack={() =>
          navigate(appointmentDetailsSource || "Test Appointments")
        }
        onNavigate={navigate}
        onTakeExamination={navigateToTakeExamination}
        userRole={userRole}
      />
    );
  }

  if (
    activePage === "Manage Test Appointments" ||
    activePage === "Written Test Appointments"
  ) {
    return (
      <ManageTestAppointmentsPage
        accessToken={accessToken}
        localAppId={manageTestAppointmentsContext?.localAppId ?? null}
        onBack={() =>
          navigate(
            manageTestAppointmentsContext?.returnPage || "Local Applications",
          )
        }
        onNavigate={navigate}
        onScheduleAppointment={navigateToScheduleAppointment}
        onShowAppointmentDetails={navigateToAppointmentDetails}
        onTakeExamination={navigateToTakeExamination}
      />
    );
  }

  if (activePage === "Take Examination") {
    return (
      <TakeExaminationPage
        accessToken={accessToken}
        appointmentId={takeExaminationContext?.appointmentId ?? null}
        onBack={() =>
          navigate(
            takeExaminationContext?.returnPage || "Manage Test Appointments",
          )
        }
        returnPage={
          takeExaminationContext?.returnPage || "Manage Test Appointments"
        }
      />
    );
  }

  if (activePage === "Schedule Appointment") {
    return (
      <ScheduleAppointmentPage
        accessToken={accessToken}
        initialLocalAppId={scheduleAppointmentContext?.localAppId ?? null}
        initialTestTypeId={scheduleAppointmentContext?.testTypeId ?? 1}
        returnPage={
          scheduleAppointmentContext?.returnPage ?? "Local Applications"
        }
        onBack={() =>
          navigate(
            scheduleAppointmentContext?.returnPage || "Local Applications",
          )
        }
        onNavigate={navigate}
        onShowAppointmentDetails={navigateToAppointmentDetails}
      />
    );
  }

  return null;
}
