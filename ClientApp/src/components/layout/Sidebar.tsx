import { useEffect, useRef, useState, type RefObject } from "react";
import {
  ChevronDown,
  ChevronLeft,
  Layers,
  LogOut,
  X,
} from "lucide-react";
import { useAuth } from "../../context/useAuth";
import { useNavigation } from "../../context/useNavigation";
import { getPathForPage } from "../../context/navigationContextInstance";
import {
  buildVisibleSections,
  findActiveGroupId,
  isLinkActive,
  type SidebarEntry,
  type SidebarGroupDef,
  type SidebarLinkDef,
} from "./sidebar/sidebarNavigation";

type SidebarProps = {
  activePage?: string;
  onNavigate?: (page: string) => void;
  isOpen?: boolean;
  onCloseMobile?: () => void;
  mobileToggleRef?: RefObject<HTMLButtonElement | null>;
  onRequestLogout: () => void;
};

export default function Sidebar({
  activePage: propActivePage,
  onNavigate: propOnNavigate,
  isOpen = false,
  onCloseMobile,
  mobileToggleRef,
  onRequestLogout,
}: SidebarProps) {
  const { user } = useAuth();
  const nav = useNavigation();
  const activePage = propActivePage ?? nav.activePage;
  const sections = buildVisibleSections(user?.role);
  const activeGroupId = findActiveGroupId(sections, activePage);
  const [collapsed, setCollapsed] = useState(false);
  const isDesktopCollapsed = collapsed && !isMobileViewport;
  const [openGroupId, setOpenGroupId] = useState<string | null>(activeGroupId);
  const [isMobileViewport, setIsMobileViewport] = useState(false);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const previousOpenRef = useRef(false);
  const initial = (user?.fullName || user?.userName || "U").charAt(0).toUpperCase();

  useEffect(() => {
    const mediaQuery = window.matchMedia("(max-width: 1023px)");
    const updateViewport = () => setIsMobileViewport(mediaQuery.matches);
    updateViewport();
    mediaQuery.addEventListener("change", updateViewport);
    return () => mediaQuery.removeEventListener("change", updateViewport);
  }, []);

  useEffect(() => {
    setOpenGroupId(activeGroupId);
  }, [activeGroupId]);

  useEffect(() => {
    if (isMobileViewport && isOpen) closeButtonRef.current?.focus();
    else if (isMobileViewport && previousOpenRef.current) mobileToggleRef?.current?.focus();
    previousOpenRef.current = isOpen;
  }, [isOpen, isMobileViewport, mobileToggleRef]);

  const handleNavClick = (page: string) => {
    if (propOnNavigate) propOnNavigate(page);
    else nav.navigate(page);
    onCloseMobile?.();
  };

  const renderLink = (item: SidebarLinkDef, nested = false) => {
    const active = isLinkActive(item, activePage);
    const label = item.label ?? item.page;
    return (
      <a
        key={item.page}
        className={`dvld-nav-link${active ? " is-active" : ""}${nested ? " is-nested" : ""}`}
        href={getPathForPage(item.page)}
        title={collapsed && !isMobileViewport ? label : undefined}
        aria-label={collapsed && !isMobileViewport ? label : undefined}
        aria-current={active ? "page" : undefined}
        onClick={(event) => {
          event.preventDefault();
          handleNavClick(item.page);
        }}
      >
        <item.icon className="dvld-nav-icon" aria-hidden="true" />
        <span className="dvld-nav-label">{label}</span>
      </a>
    );
  };

  const renderEntry = (entry: SidebarEntry) => {
    if (entry.kind === "link") return renderLink(entry);
    return renderGroup(entry);
  };

  const renderGroup = (group: SidebarGroupDef) => {
    const expanded = openGroupId === group.id && !isDesktopCollapsed;
    const groupActive = group.items.some((item) => isLinkActive(item, activePage));
    const submenuId = `sidebar-submenu-${group.id}`;
    const Icon = group.icon;
    return (
      <div className="dvld-nav-group" key={group.id}>
        <button
          type="button"
          className={`dvld-nav-group-button${groupActive ? " is-active" : ""}`}
          title={collapsed && !isMobileViewport ? group.label : undefined}
          aria-label={collapsed && !isMobileViewport ? group.label : undefined}
          aria-expanded={expanded}
          aria-controls={submenuId}
          onClick={() => {
            if (isDesktopCollapsed) setCollapsed(false);
            setOpenGroupId((current) => current === group.id && !isDesktopCollapsed ? null : group.id);
          }}
        >
          <Icon className="dvld-nav-icon" aria-hidden="true" />
          <span className="dvld-nav-label">{group.label}</span>
          <ChevronDown className={`dvld-nav-chevron${expanded ? " is-open" : ""}`} aria-hidden="true" />
        </button>
        <div
          className={`dvld-submenu${expanded ? " is-open" : ""}`}
          id={submenuId}
          aria-hidden={!expanded}
          inert={!expanded}
        >
          <div className="dvld-submenu-inner">
            {group.items.map((item) => renderLink(item, true))}
          </div>
        </div>
      </div>
    );
  };

  return (
    <>
      {isOpen && isMobileViewport && (
        <button type="button" className="sidebar-backdrop" onClick={onCloseMobile} aria-label="Close navigation menu" />
      )}
      <aside
        className={`sidebar dvld-sidebar${isDesktopCollapsed ? " is-collapsed" : ""}${isOpen ? " sidebar-open" : ""}`}
        id="dvld-primary-sidebar"
        aria-label="Primary navigation"
        aria-hidden={isMobileViewport && !isOpen}
        inert={isMobileViewport && !isOpen}
      >
        <div className="dvld-sidebar-header">
          <div className="dvld-brand">
            <span className="dvld-brand-mark"><Layers aria-hidden="true" /></span>
            <span className="dvld-brand-copy"><strong>DVLD</strong><small>Enterprise Portal</small></span>
            {onCloseMobile && <button type="button" ref={closeButtonRef} className="sidebar-close-btn" onClick={onCloseMobile} aria-label="Close navigation"><X aria-hidden="true" /></button>}
          </div>
          <button
            type="button"
            className="dvld-collapse-button"
            onClick={() => setCollapsed((value) => !value)}
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            aria-expanded={!collapsed}
            title={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          >
            <ChevronLeft className={collapsed ? "is-collapsed" : ""} aria-hidden="true" />
          </button>
        </div>

        <div className="sidebar-nav-container dvld-sidebar-scroll">
          <nav className="dvld-sidebar-nav" aria-label="DVLD pages">
            {sections.map((section) => (
              <section className="dvld-nav-section" key={section.id} aria-label={section.title}>
                {section.title && <h2 className="dvld-nav-section-title"><span>{section.title}</span></h2>}
                {section.entries.map(renderEntry)}
              </section>
            ))}
          </nav>
        </div>

        <div className="dvld-sidebar-footer">
          <button type="button" className="dvld-profile-link" onClick={() => handleNavClick("My Profile")} title={collapsed ? "My Profile" : undefined}>
            <span className="dvld-user-avatar" aria-hidden="true">{initial}</span>
            <span className="dvld-user-copy"><strong>{user?.fullName || user?.userName || "User"}</strong><small>{user?.role || "Staff"}</small></span>
          </button>
          <button type="button" className="dvld-logout-button" onClick={onRequestLogout} aria-label="Sign out" title="Sign out"><LogOut aria-hidden="true" /></button>
        </div>
      </aside>
    </>
  );
}
