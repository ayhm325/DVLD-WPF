import { lazy } from "react";

const CountriesPage = lazy(() => import("../../countries/CountriesPage"));
const ApplicationTypesPage = lazy(
  () => import("../../application-types/ApplicationTypesPage"),
);
const LicenseClassesPage = lazy(
  () => import("../../license-classes/LicenseClassesPage"),
);
const TestTypesPage = lazy(() => import("../../test-types/TestTypesPage"));

export type ReferenceDataRoutesProps = {
  accessToken: string;
  activePage: string;
  userRole?: string | null;
};

export default function ReferenceDataRoutes({
  accessToken,
  activePage,
  userRole,
}: ReferenceDataRoutesProps) {
  if (activePage === "Countries") {
    return <CountriesPage accessToken={accessToken} userRole={userRole} />;
  }

  if (activePage === "Application Types") {
    return (
      <ApplicationTypesPage accessToken={accessToken} userRole={userRole} />
    );
  }

  if (activePage === "License Classes") {
    return <LicenseClassesPage accessToken={accessToken} userRole={userRole} />;
  }

  if (activePage === "Test Types") {
    return <TestTypesPage accessToken={accessToken} userRole={userRole} />;
  }

  return null;
}
