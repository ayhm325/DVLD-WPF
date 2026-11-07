import { Suspense } from "react";
import AccessDenied from "../common/AccessDenied";
import LoadingSpinner from "../common/LoadingSpinner";
import { isPagePermitted } from "../../utils/permissions";
import { useNavigation } from "../../context/useNavigation";
import { resolveRouteGroup } from "./router/routeConfig";

import DashboardRoutes from "./router/DashboardRoutes";
import UserRoutes from "./router/UserRoutes";
import PeopleRoutes from "./router/PeopleRoutes";
import LicenseRoutes from "./router/LicenseRoutes";
import AppointmentRoutes from "./router/AppointmentRoutes";
import ReferenceDataRoutes from "./router/ReferenceDataRoutes";
import AccountRoutes from "./router/AccountRoutes";

export type AppRouterProps = {
  activePage?: string;
  accessToken: string;
  userRole?: string | null;
  onNavigate?: (page: string) => void;
  showIssueInternational: boolean;
  setShowIssueInternational: (show: boolean) => void;
  internationalRefreshKey: number;
  onInternationalIssued: () => void;
  statisticsRefreshKey: number;
  onPersonAdded?: () => void;
};

export default function AppRouter({
  activePage: propActivePage,
  accessToken,
  userRole,
  onNavigate: propOnNavigate,
  showIssueInternational,
  setShowIssueInternational,
  internationalRefreshKey,
  onInternationalIssued,
  statisticsRefreshKey,
  onPersonAdded,
}: AppRouterProps) {
  const nav = useNavigation();

  const activePage = propActivePage ?? nav.activePage;
  const onNavigate = propOnNavigate ?? nav.navigate;
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

  const routeGroup = resolveRouteGroup(activePage);

  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center min-h-100 w-full py-16">
          <LoadingSpinner size="lg" />
        </div>
      }
    >
      {routeGroup === "dashboard" && (
        <DashboardRoutes
          accessToken={accessToken}
          statisticsRefreshKey={statisticsRefreshKey}
        />
      )}

      {routeGroup === "users" && <UserRoutes accessToken={accessToken} />}

      {routeGroup === "people" && (
        <PeopleRoutes
          accessToken={accessToken}
          activePage={activePage}
          onPersonAdded={onPersonAdded}
        />
      )}

      {routeGroup === "licenses" && (
        <LicenseRoutes
          accessToken={accessToken}
          activePage={activePage}
          showIssueInternational={showIssueInternational}
          setShowIssueInternational={setShowIssueInternational}
          internationalRefreshKey={internationalRefreshKey}
          onInternationalIssued={onInternationalIssued}
        />
      )}

      {routeGroup === "appointments" && (
        <AppointmentRoutes
          accessToken={accessToken}
          activePage={activePage}
          userRole={userRole}
        />
      )}

      {routeGroup === "reference" && (
        <ReferenceDataRoutes
          accessToken={accessToken}
          activePage={activePage}
          userRole={userRole}
        />
      )}

      {routeGroup === "account" && (
        <AccountRoutes accessToken={accessToken} activePage={activePage} />
      )}
    </Suspense>
  );
}
