import { lazy } from "react";
import { useNavigation } from "../../../context/useNavigation";

const UsersPage = lazy(() => import("../../users/UsersPage"));

export type UserRoutesProps = {
  accessToken: string;
};

export default function UserRoutes({ accessToken }: UserRoutesProps) {
  const { navigate } = useNavigation();

  return <UsersPage accessToken={accessToken} onNavigate={navigate} />;
}
