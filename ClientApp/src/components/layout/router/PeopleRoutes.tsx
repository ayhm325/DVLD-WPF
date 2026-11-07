import { lazy, useCallback } from "react";
import { useNavigation } from "../../../context/useNavigation";
import { usePeopleData } from "../../../context/usePeopleData";

const PeoplePage = lazy(() => import("../../people/PeoplePage"));
const AddPersonPage = lazy(() => import("../../people/AddPersonPage"));
const EditPersonPage = lazy(() => import("../../people/EditPersonPage"));
const PersonDetailsPage = lazy(() => import("../../people/PersonDetailsPage"));
const DriversPage = lazy(() => import("../../drivers/DriversPage"));
const LicenseHistoryPage = lazy(
  () => import("../../licenses/LicenseHistoryPage"),
);

export type PeopleRoutesProps = {
  accessToken: string;
  activePage: string;
  onPersonAdded?: () => void;
};

export default function PeopleRoutes({
  accessToken,
  activePage,
  onPersonAdded,
}: PeopleRoutesProps) {
  const {
    navigate,
    selectedPersonId,
    selectedDriver,
    personDetailsSource,
    licenseHistorySource,
    setSelectedPersonId,
    navigateToAddPerson,
    navigateToPersonDetails,
    navigateToEditPerson,
    navigateToPersonLicenseHistory,
  } = useNavigation();

  const { refreshPeople } = usePeopleData();

  const handlePersonSaved = useCallback(async () => {
    try {
      await refreshPeople();
    } finally {
      onPersonAdded?.();
      navigate("People");
    }
  }, [navigate, refreshPeople, onPersonAdded]);

  if (activePage === "People") {
    return (
      <PeoplePage
        onAddPerson={navigateToAddPerson}
        onShowDetails={(person) =>
          navigateToPersonDetails(person.personId, "People")
        }
        onEditPerson={(person) => navigateToEditPerson(person.personId)}
      />
    );
  }

  if (activePage === "Add Person") {
    return (
      <AddPersonPage
        accessToken={accessToken}
        onBack={() => navigate("People")}
        onPersonAdded={handlePersonSaved}
      />
    );
  }

  if (activePage === "Edit Person") {
    return (
      <EditPersonPage
        accessToken={accessToken}
        personId={selectedPersonId ?? null}
        onBack={() => navigate("People")}
        onPersonUpdated={handlePersonSaved}
      />
    );
  }

  if (activePage === "Person Details") {
    return (
      <PersonDetailsPage
        accessToken={accessToken}
        personId={selectedPersonId ?? null}
        onBack={() => navigate(personDetailsSource || "People")}
        onEdit={(id) => {
          setSelectedPersonId(id);
          navigate("Edit Person");
        }}
      />
    );
  }

  if (activePage === "Drivers") {
    return (
      <DriversPage
        accessToken={accessToken}
        onShowPersonInfo={(id) => navigateToPersonDetails(id, "Drivers")}
        onShowLicenseHistory={navigateToPersonLicenseHistory}
      />
    );
  }

  if (activePage === "Person License History") {
    return (
      <LicenseHistoryPage
        accessToken={accessToken}
        personId={selectedPersonId}
        driver={selectedDriver}
        onBack={() => navigate(licenseHistorySource || "Drivers")}
      />
    );
  }

  return null;
}
