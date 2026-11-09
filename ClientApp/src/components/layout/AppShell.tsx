import { useCallback, useState, useEffect, useRef } from "react";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";
import Footer from "./Footer";
import ConfirmModal from "../common/ConfirmModal";
import CommandPalette from "./CommandPalette";
import AppRouter from "./AppRouter";

import { useNavigation } from "../../context/useNavigation";
import { PeopleProvider } from "../../context/PeopleContext";
import { useModal } from "../../hooks";
import type { AuthUser } from "../../types/Auth";

export type AppShellProps = {
  accessToken: string;
  user: AuthUser | null;
  onLogout: () => void;
  // Feature coordination keys passed to AppRouter
  statisticsRefreshKey: number;
  onDashboardNavigate?: () => void;
  onStatisticsRefresh?: () => void;
  internationalRefreshKey: number;
  onInternationalIssued: () => void;
};

export default function AppShell({
  accessToken,
  user,
  onLogout,
  statisticsRefreshKey,
  onDashboardNavigate,
  onStatisticsRefresh,
  internationalRefreshKey,
  onInternationalIssued,
}: AppShellProps) {
  const { activePage, navigate } = useNavigation();

  const mobileSidebar = useModal();
  const closeMobileSidebar = mobileSidebar.close;
  const mobileSidebarToggleRef = useRef<HTMLButtonElement>(null);
  const logoutModal = useModal();
  const commandPalette = useModal();
  const { open: openCommandPalette } = commandPalette;

  // Keep the page behind the mobile drawer still while it is open and allow
  // keyboard users to dismiss it with Escape.
  useEffect(() => {
    const mediaQuery = window.matchMedia("(max-width: 1023px)");
    const previousOverflow = document.body.style.overflow;

    const syncMobileDrawer = () => {
      document.body.style.overflow =
        mediaQuery.matches && mobileSidebar.isOpen ? "hidden" : previousOverflow;
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && mediaQuery.matches) {
        closeMobileSidebar();
      }
    };

    syncMobileDrawer();
    mediaQuery.addEventListener("change", syncMobileDrawer);
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      mediaQuery.removeEventListener("change", syncMobileDrawer);
      window.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [mobileSidebar.isOpen, closeMobileSidebar]);

  // Listen for Ctrl+K / Cmd+K global shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Avoid triggering when composing text with an IME
      if (e.isComposing || e.keyCode === 229) return;

      const isModifier = e.ctrlKey || e.metaKey;
      const isKKey = e.key.toLowerCase() === "k" || e.code === "KeyK";

      if (isModifier && !e.altKey && !e.shiftKey && isKKey) {
        // Do not activate while typing in inputs, textareas, or contenteditable elements
        const target = e.target as HTMLElement | null;
        const isEditable =
          target &&
          (target.tagName === "INPUT" ||
            target.tagName === "TEXTAREA" ||
            target.isContentEditable);
        if (isEditable) return;

        e.preventDefault();
        e.stopPropagation();
        openCommandPalette();
      }
    };

    window.addEventListener("keydown", handleKeyDown, true);
    return () => window.removeEventListener("keydown", handleKeyDown, true);
  }, [openCommandPalette]);

  // International license sub-view presentation state (controls Topbar title and router sub-view)
  const [showIssueInternational, setShowIssueInternational] = useState(false);

  const handleInternationalIssuedInternal = useCallback(() => {
    setShowIssueInternational(false);
    onInternationalIssued();
  }, [onInternationalIssued]);

  const handleNavigate = useCallback(
    (page: string) => {
      navigate(page);
      setShowIssueInternational(false);
      mobileSidebar.close();

      if (page === "Dashboard") {
        onDashboardNavigate?.();
      }
    },
    [mobileSidebar, navigate, onDashboardNavigate],
  );

  const handleConfirmLogout = useCallback(() => {
    logoutModal.close();
    mobileSidebar.close();
    onLogout();
  }, [logoutModal, mobileSidebar, onLogout]);

  return (
    <div className="app-shell">
      <Sidebar
        activePage={activePage}
        onNavigate={handleNavigate}
        isOpen={mobileSidebar.isOpen}
        onCloseMobile={mobileSidebar.close}
        mobileToggleRef={mobileSidebarToggleRef}
        onRequestLogout={logoutModal.open}
      />

      <main className="main-content">
        <Topbar
          activePage={
            activePage === "International Licenses" && showIssueInternational
              ? "Issue International License"
              : activePage
          }
          onRequestLogout={logoutModal.open}
          onOpenCommandPalette={commandPalette.open}
          onToggleSidebar={mobileSidebar.open}
          isSidebarOpen={mobileSidebar.isOpen}
          sidebarToggleRef={mobileSidebarToggleRef}
        />

        <PeopleProvider>
          <AppRouter
            accessToken={accessToken}
            userRole={user?.role}
            showIssueInternational={showIssueInternational}
            setShowIssueInternational={setShowIssueInternational}
            internationalRefreshKey={internationalRefreshKey}
            onInternationalIssued={handleInternationalIssuedInternal}
            statisticsRefreshKey={statisticsRefreshKey}
            onPersonAdded={onStatisticsRefresh}
          />
        </PeopleProvider>

        {/* Logout Confirmation Dialog */}
        <ConfirmModal
          isOpen={logoutModal.isOpen}
          title="Sign Out of DVLD"
          message="Are you sure you want to sign out? Your active session token will be cleared and you will be returned to the sign-in screen."
          confirmText="Sign Out"
          cancelText="Stay Signed In"
          confirmVariant="danger"
          onConfirm={handleConfirmLogout}
          onCancel={logoutModal.close}
        />

        {/* Global Dark Command Navigation Palette (Ctrl+K) */}
        <CommandPalette
          isOpen={commandPalette.isOpen}
          onClose={commandPalette.close}
          onNavigate={handleNavigate}
          userRole={user?.role}
        />

        <Footer />
      </main>
    </div>
  );
}
