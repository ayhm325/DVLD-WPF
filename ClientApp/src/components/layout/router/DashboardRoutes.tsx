import { lazy } from "react";
import { useNavigation } from "../../../context/useNavigation";

const DashboardPage = lazy(() => import("../../dashboard/DashboardPage"));

export type DashboardRoutesProps = {
  accessToken: string;
  statisticsRefreshKey: number;
};

export default function DashboardRoutes({
  accessToken,
  statisticsRefreshKey,
}: DashboardRoutesProps) {
  const { navigateToAddPerson, navigateToPersonDetails, navigateToEditPerson } =
    useNavigation();

  return (
    <DashboardPage
      accessToken={accessToken}
      statisticsRefreshKey={statisticsRefreshKey}
      onAddPerson={navigateToAddPerson}
      onShowDetails={(person) =>
        navigateToPersonDetails(person.personId, "People")
      }
      onEditPerson={(person) => navigateToEditPerson(person.personId)}
    />
  );
}
