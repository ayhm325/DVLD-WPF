import { lazy } from "react";
import { useNavigation } from "../../../context/useNavigation";

const LicensesPage = lazy(() => import("../../licenses/LicensesPage"));
const InternationalLicensesPage = lazy(
  () => import("../../international-licenses/InternationalLicensesPage"),
);
const IssueInternationalLicensePage = lazy(
  () => import("../../international-licenses/IssueInternationalLicensePage"),
);
const RenewLicensePage = lazy(() => import("../../licenses/RenewLicensePage"));
const ReplaceLicensePage = lazy(
  () => import("../../licenses/ReplaceLicensePage"),
);
const DetainLicensePage = lazy(() => import("../../detained/DetainLicensePage"));
const DetainedLicensesPage = lazy(
  () => import("../../detained/DetainedLicensesPage"),
);

export type LicenseRoutesProps = {
  accessToken: string;
  activePage: string;
  showIssueInternational: boolean;
  setShowIssueInternational: (show: boolean) => void;
  internationalRefreshKey: number;
  onInternationalIssued: () => void;
};

export default function LicenseRoutes({
  accessToken,
  activePage,
  showIssueInternational,
  setShowIssueInternational,
  internationalRefreshKey,
  onInternationalIssued,
}: LicenseRoutesProps) {
  const { navigate } = useNavigation();

  if (activePage === "Licenses") {
    return <LicensesPage accessToken={accessToken} onNavigate={navigate} />;
  }

  if (activePage === "International Licenses") {
    return showIssueInternational ? (
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
    );
  }

  if (activePage === "Renew License") {
    return (
      <RenewLicensePage
        accessToken={accessToken}
        onBack={() => navigate("Licenses")}
        onRenewed={() => navigate("Licenses")}
      />
    );
  }

  if (activePage === "Replace License") {
    return (
      <ReplaceLicensePage
        accessToken={accessToken}
        onBack={() => navigate("Licenses")}
        onReplaced={() => navigate("Licenses")}
      />
    );
  }

  if (activePage === "Detain License") {
    return (
      <DetainLicensePage
        accessToken={accessToken}
        onBack={() => navigate("Detained Licenses")}
        onDetained={() => navigate("Detained Licenses")}
      />
    );
  }

  if (activePage === "Detained Licenses") {
    return (
      <DetainedLicensesPage
        accessToken={accessToken}
        onNavigateToDetain={() => navigate("Detain License")}
      />
    );
  }

  return null;
}
