import { useCallback, useState } from "react";
import "./App.css";

import Login from "./components/auth/Login";
import LoadingSpinner from "./components/common/LoadingSpinner";
import AppShell from "./components/layout/AppShell";

import { useAuth } from "./context/useAuth";
import { useNavigation } from "./context/useNavigation";

export default function App() {
  const { accessToken, user, isAuthenticated, isLoading, logout } = useAuth();
  const { navigate } = useNavigation();

  // Feature coordination keys intentionally maintained during bootstrap
  const [internationalRefreshKey, setInternationalRefreshKey] = useState(0);
  const [statisticsRefreshKey, setStatisticsRefreshKey] = useState(0);

  const handleInternationalIssued = useCallback(() => {
    setInternationalRefreshKey((current) => current + 1);
    setStatisticsRefreshKey((current) => current + 1);
  }, []);

  const handleDashboardNavigate = useCallback(() => {
    setStatisticsRefreshKey((current) => current + 1);
  }, []);

  const handleStatisticsRefresh = useCallback(() => {
    setStatisticsRefreshKey((current) => current + 1);
  }, []);

  const handleLogout = useCallback(() => {
    navigate("Dashboard", undefined, { replace: true });
    logout("You have successfully signed out of DVLD.");
  }, [logout, navigate]);

  if (isLoading) {
    return (
      <div className="login-page">
        <LoadingSpinner message="Checking DVLD session..." size="lg" />
      </div>
    );
  }

  if (!isAuthenticated || accessToken === null) {
    return <Login />;
  }

  return (
    <AppShell
      accessToken={accessToken}
      user={user}
      onLogout={handleLogout}
      statisticsRefreshKey={statisticsRefreshKey}
      onDashboardNavigate={handleDashboardNavigate}
      onStatisticsRefresh={handleStatisticsRefresh}
      internationalRefreshKey={internationalRefreshKey}
      onInternationalIssued={handleInternationalIssued}
    />
  );
}
