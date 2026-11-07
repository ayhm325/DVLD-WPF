import { lazy } from "react";
import { useNavigation } from "../../../context/useNavigation";

const MyProfilePage = lazy(() => import("../../profile/MyProfilePage"));
const ChangePasswordPage = lazy(
  () => import("../../profile/ChangePasswordPage"),
);

export type AccountRoutesProps = {
  accessToken: string;
  activePage: string;
};

export default function AccountRoutes({
  accessToken,
  activePage,
}: AccountRoutesProps) {
  const { navigate } = useNavigation();

  if (activePage === "My Profile") {
    return <MyProfilePage accessToken={accessToken} onNavigate={navigate} />;
  }

  if (activePage === "Change Password") {
    return (
      <ChangePasswordPage accessToken={accessToken} onNavigate={navigate} />
    );
  }

  return null;
}
