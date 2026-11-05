import { lazy, Suspense } from "react";
import AccessDenied from "./common/AccessDenied";
import LoadingSpinner from "./common/LoadingSpinner";
import { isPagePermitted } from "../utils/permissions";
import type { Person } from "../types/Person";
import type { Driver } from "../types/Driver";

const DashboardPage = lazy(() => import("./DashboardPage"));
const PeoplePage = lazy(() => import("./PeoplePage"));
const AddPersonPage = lazy(() => import("./AddPersonPage"));
const EditPersonPage = lazy(() => import("./people/EditPersonPage"));
const PersonDetailsPage = lazy(() => import("./people/PersonDetailsPage"));
const DriversPage = lazy(() => import("./DriversPage"));
const LicensesPage = lazy(() => import("./LicensesPage"));
const LicenseHistoryPage = lazy(() => import("./licenses/LicenseHistoryPage"));
const InternationalLicensesPage = lazy(
  () => import("./international license/InternationalLicensesPage"),
);
const IssueInternationalLicensePage = lazy(
  () => import("./international license/IssueInternationalLicensePage"),
);
const LocalApplicationsPage = lazy(
  () => import("./local applications/LocalApplicationsPage"),
);
const TestAppointmentsPage = lazy(
  () => import("./test appointments/TestAppointmentsPage"),
);
const AppointmentDetailsPage = lazy(
  () => import("./test appointments/AppointmentDetailsPage"),
);
const ScheduleAppointmentPage = lazy(
  () => import("./test appointments/ScheduleAppointmentPage"),
);
const ManageTestAppointmentsPage = lazy(
  () => import("./test appointments/ManageTestAppointmentsPage"),
);
const TakeExaminationPage = lazy(
  () => import("./test appointments/TakeExaminationPage"),
);
const RenewLicensePage = lazy(() => import("./licenses/RenewLicensePage"));
const ReplaceLicensePage = lazy(() => import("./licenses/ReplaceLicensePage"));
const DetainLicensePage = lazy(() => import("./detained/DetainLicensePage"));
const DetainedLicensesPage = lazy(
  () => import("./detained/DetainedLicensesPage"),
);
const CountriesPage = lazy(() => import("./CountriesPage"));
const ApplicationTypesPage = lazy(() => import("./ApplicationTypesPage"));
const LicenseClassesPage = lazy(() => import("./LicenseClassesPage"));
const TestTypesPage = lazy(() => import("./TestTypesPage"));
const MyProfilePage = lazy(() => import("./profile/MyProfilePage"));
const ChangePasswordPage = lazy(() => import("./profile/ChangePasswordPage"));
const UsersPage = lazy(() => import("./users/UsersPage"));

export type AppRouterProps = {
  activePage: string;
  accessToken: string;
  userRole?: string | null;
  onNavigate: (page: string) => void;
  showIssueInternational: boolean;
  setShowIssueInternational: (show: boolean) => void;
  internationalRefreshKey: number;
  onInternationalIssued: () => void;
  statisticsRefreshKey: number;
  people: Person[];
  filteredPeople: Person[];
  searchName: string;
  onSearchChange: (value: string) => void;
  loadingPeople: boolean;
  peopleError: string;
  onRetryPeople: () => void;
  onAddPerson: () => void;
  onPersonAdded?: () => void;
  onShowDetails?: (person: Person) => void;
  onEditPerson?: (person: Person) => void;
  selectedPersonId?: number | null;
  setSelectedPersonId?: (id: number | null) => void;
  selectedDriver?: Driver | null;
  setSelectedDriver?: (driver: Driver | null) => void;
  onShowPersonInfo?: (personId: number) => void;
  onShowLicenseHistory?: (personId: number, driver?: Driver) => void;
  personDetailsSource?: string;
  licenseHistorySource?: string;
  selectedAppointmentId?: number | null;
  appointmentDetailsSource?: string;
  onShowAppointmentDetails?: (appointmentId: number, source?: string) => void;
  scheduleAppointmentContext?: {
    localAppId?: number | null;
    testTypeId?: 1 | 2 | 3;
    returnPage?: string;
  } | null;
  onScheduleAppointment?: (
    localAppId?: number | null,
    testTypeId?: 1 | 2 | 3,
    source?: string,
    reopenModalOnReturn?: boolean,
  ) => void;
  reopenModalTarget?: {
    localAppId: number;
    testTypeId: 1 | 2 | 3;
  } | null;
  onClearReopenModalTarget?: () => void;
  manageTestAppointmentsContext?: {
    localAppId: number;
    testTypeId?: 1 | 2 | 3;
    returnPage?: string;
  } | null;
  onManageTestAppointments?: (
    localAppId: number,
    sourceOrTestType?: 1 | 2 | 3 | string,
    source?: string,
  ) => void;
  takeExaminationContext?: {
    appointmentId: number;
    returnPage?: string;
  } | null;
  onTakeExamination?: (appointmentId: number, source?: string) => void;
};

export default function AppRouter({
  activePage,
  accessToken,
  userRole,
  onNavigate,
  showIssueInternational,
  setShowIssueInternational,
  internationalRefreshKey,
  onInternationalIssued,
  statisticsRefreshKey,
  people,
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
  onPersonAdded,
  onShowDetails,
  onEditPerson,
  selectedPersonId,
  setSelectedPersonId,
  selectedDriver,
  onShowPersonInfo,
  onShowLicenseHistory,
  personDetailsSource,
  licenseHistorySource,
  selectedAppointmentId,
  appointmentDetailsSource,
  onShowAppointmentDetails,
  scheduleAppointmentContext,
  onScheduleAppointment,
  reopenModalTarget,
  onClearReopenModalTarget,
  manageTestAppointmentsContext,
  onManageTestAppointments,
  takeExaminationContext,
  onTakeExamination,
}: AppRouterProps) {
  const isCurrentPagePermitted = isPagePermitted(activePage, userRole);

  if (!isCurrentPagePermitted) {
    return (
      <section className="page-content">
        <AccessDenied
          pageTitle={activePage}
          userRole={userRole ?? undefined}
          onBackToDashboard={() => onNavigate("Dashboard")}
        />
      </section>
    );
  }

  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center min-h-100 w-full py-16">
          <LoadingSpinner size="lg" />
        </div>
      }
    >
      {/* Dashboard */}
      {activePage === "Dashboard" && (
        <DashboardPage
          accessToken={accessToken}
          statisticsRefreshKey={statisticsRefreshKey}
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={onRetryPeople}
          onAddPerson={onAddPerson}
          onShowDetails={onShowDetails}
          onEditPerson={onEditPerson}
        />
      )}

      {/* User Management (Admin only) */}
      {activePage === "User Management" && (
        <UsersPage accessToken={accessToken} onNavigate={onNavigate} />
      )}

      {/* People */}
      {activePage === "People" && (
        <PeoplePage
          accessToken={accessToken}
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={onRetryPeople}
          onAddPerson={onAddPerson}
          onShowDetails={onShowDetails}
          onEditPerson={onEditPerson}
        />
      )}

      {/* Add Person */}
      {activePage === "Add Person" && (
        <AddPersonPage
          accessToken={accessToken}
          onBack={() => onNavigate("People")}
          onPersonAdded={onPersonAdded ?? (() => onNavigate("People"))}
        />
      )}

      {/* Edit Person */}
      {activePage === "Edit Person" && (
        <EditPersonPage
          accessToken={accessToken}
          personId={selectedPersonId ?? null}
          onBack={() => onNavigate("People")}
          onPersonUpdated={onPersonAdded ?? (() => onNavigate("People"))}
        />
      )}

      {/* Person Details */}
      {activePage === "Person Details" && (
        <PersonDetailsPage
          accessToken={accessToken}
          personId={selectedPersonId ?? null}
          onBack={() => onNavigate(personDetailsSource || "People")}
          onEdit={(id) => {
            setSelectedPersonId?.(id);
            onNavigate("Edit Person");
          }}
        />
      )}

      {/* Drivers */}
      {activePage === "Drivers" && (
        <DriversPage
          accessToken={accessToken}
          onShowPersonInfo={onShowPersonInfo}
          onShowLicenseHistory={onShowLicenseHistory}
        />
      )}

      {/* Person License History */}
      {activePage === "Person License History" && (
        <LicenseHistoryPage
          accessToken={accessToken}
          personId={selectedPersonId}
          driver={selectedDriver}
          onBack={() => onNavigate(licenseHistorySource || "Drivers")}
        />
      )}

      {/* Licenses */}
      {activePage === "Licenses" && (
        <LicensesPage accessToken={accessToken} onNavigate={onNavigate} />
      )}

      {/* International Licenses */}
      {activePage === "International Licenses" &&
        (showIssueInternational ? (
          <IssueInternationalLicensePage
            accessToken={accessToken}
            onBack={() => setShowIssueInternational(false)}
            onIssued={onInternationalIssued}
          />
        ) : (
          <InternationalLicensesPage
            key={internationalRefreshKey}
            accessToken={accessToken}
            onIssueNew={() => setShowIssueInternational(true)}
          />
        ))}

      {/* Applications */}
      {activePage === "Local Applications" && (
        <LocalApplicationsPage
          accessToken={accessToken}
          onShowLicenseHistory={onShowLicenseHistory}
          onShowAppointmentDetails={
            onShowAppointmentDetails
              ? (id) => onShowAppointmentDetails(id, "Local Applications")
              : undefined
          }
          onScheduleAppointment={onScheduleAppointment}
          onManageTestAppointments={onManageTestAppointments}
          reopenModalTarget={reopenModalTarget}
          onClearReopenModalTarget={onClearReopenModalTarget}
        />
      )}

      {/* Test Appointments (Staff only) */}
      {activePage === "Test Appointments" && (
        <TestAppointmentsPage
          accessToken={accessToken}
          onNavigate={onNavigate}
          onShowAppointmentDetails={
            onShowAppointmentDetails
              ? (id) => onShowAppointmentDetails(id, "Test Appointments")
              : undefined
          }
          onScheduleAppointment={onScheduleAppointment}
          onTakeExamination={onTakeExamination}
        />
      )}

      {/* Appointment Details (Staff only) */}
      {activePage === "Appointment Details" && (
        <AppointmentDetailsPage
          accessToken={accessToken}
          appointmentId={selectedAppointmentId ?? null}
          onBack={() =>
            onNavigate(appointmentDetailsSource || "Test Appointments")
          }
          onNavigate={onNavigate}
          onTakeExamination={onTakeExamination}
          userRole={userRole}
        />
      )}

      {/* Manage Test Appointments (Staff only) */}
      {(activePage === "Manage Test Appointments" ||
        activePage === "Written Test Appointments") && (
        <ManageTestAppointmentsPage
          accessToken={accessToken}
          localAppId={manageTestAppointmentsContext?.localAppId ?? null}
          onBack={() =>
            onNavigate(
              manageTestAppointmentsContext?.returnPage || "Local Applications",
            )
          }
          onNavigate={onNavigate}
          onScheduleAppointment={onScheduleAppointment}
          onShowAppointmentDetails={onShowAppointmentDetails}
          onTakeExamination={onTakeExamination}
        />
      )}

      {/* Take Examination (Staff only) */}
      {activePage === "Take Examination" && (
        <TakeExaminationPage
          accessToken={accessToken}
          appointmentId={takeExaminationContext?.appointmentId ?? null}
          onBack={() =>
            onNavigate(
              takeExaminationContext?.returnPage || "Manage Test Appointments",
            )
          }
          returnPage={
            takeExaminationContext?.returnPage || "Manage Test Appointments"
          }
        />
      )}

      {/* Schedule Appointment (Staff only) */}
      {activePage === "Schedule Appointment" && (
        <ScheduleAppointmentPage
          accessToken={accessToken}
          initialLocalAppId={scheduleAppointmentContext?.localAppId ?? null}
          initialTestTypeId={scheduleAppointmentContext?.testTypeId ?? 1}
          returnPage={
            scheduleAppointmentContext?.returnPage ?? "Local Applications"
          }
          onBack={() =>
            onNavigate(
              scheduleAppointmentContext?.returnPage || "Local Applications",
            )
          }
          onNavigate={onNavigate}
          onShowAppointmentDetails={onShowAppointmentDetails}
        />
      )}

      {/* License Renewal (Staff only) */}
      {activePage === "Renew License" && (
        <RenewLicensePage
          accessToken={accessToken}
          onBack={() => onNavigate("Licenses")}
          onRenewed={() => onNavigate("Licenses")}
        />
      )}

      {/* License Replacement (Staff only) */}
      {activePage === "Replace License" && (
        <ReplaceLicensePage
          accessToken={accessToken}
          onBack={() => onNavigate("Licenses")}
          onReplaced={() => onNavigate("Licenses")}
        />
      )}

      {/* License Detention (Staff only) */}
      {activePage === "Detain License" && (
        <DetainLicensePage
          accessToken={accessToken}
          onBack={() => onNavigate("Detained Licenses")}
          onDetained={() => onNavigate("Detained Licenses")}
        />
      )}

      {/* Detained Licenses Registry (Staff only) */}
      {activePage === "Detained Licenses" && (
        <DetainedLicensesPage
          accessToken={accessToken}
          onNavigateToDetain={() => onNavigate("Detain License")}
        />
      )}

      {/* Settings pages */}
      {activePage === "Countries" && (
        <CountriesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === "Application Types" && (
        <ApplicationTypesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === "License Classes" && (
        <LicenseClassesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === "Test Types" && (
        <TestTypesPage accessToken={accessToken} userRole={userRole} />
      )}

      {/* Account Management (Both Admin and Staff) */}
      {activePage === "My Profile" && (
        <MyProfilePage accessToken={accessToken} onNavigate={onNavigate} />
      )}

      {activePage === "Change Password" && (
        <ChangePasswordPage accessToken={accessToken} onNavigate={onNavigate} />
      )}
    </Suspense>
  );
}
