import { useState, type ReactNode } from 'react'
import { useAuth } from '../context/useAuth'
import {
  DashboardIcon,
  UsersIcon,
  DriversIcon,
  LicenseIcon,
  GlobeIcon,
  ApplicationsIcon,
  SettingsIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  CloseIcon,
  LogOutIcon,
  TestTypeIcon,
  AwardIcon,
  FlagIcon,
} from './common/Icons'

type SidebarProps = {
  activePage: string
  onNavigate: (page: string) => void
  isOpen?: boolean
  onCloseMobile?: () => void
  onRequestLogout: () => void
}

export default function Sidebar({
  activePage,
  onNavigate,
  isOpen = false,
  onCloseMobile,
  onRequestLogout,
}: SidebarProps) {
  const { user } = useAuth()
  const settingsPages = [
    'Application Types',
    'License Classes',
    'Test Types',
    'Countries',
  ]

  const [isSettingsOpen, setIsSettingsOpen] = useState(
    settingsPages.includes(activePage),
  )

  const isSettingsActive = settingsPages.includes(activePage)

  const handleNavClick = (page: string) => {
    onNavigate(page)
    if (onCloseMobile) {
      onCloseMobile()
    }
  }

  const renderNavItem = (
    page: string,
    icon: ReactNode,
    label: string = page,
    isSubItem = false,
  ) => {
    const isActive = activePage === page
    return (
      <button
        key={page}
        type="button"
        className={`nav-item ${isActive ? 'active' : ''} ${isSubItem ? 'nav-sub-item' : ''}`}
        onClick={() => handleNavClick(page)}
      >
        <span className="nav-icon" aria-hidden="true">
          {icon}
        </span>
        <span className="nav-label">{label}</span>
        {isActive && <span className="nav-active-pill" aria-hidden="true" />}
      </button>
    )
  }

  const initial = user?.fullName
    ? user.fullName.charAt(0).toUpperCase()
    : user?.userName
      ? user.userName.charAt(0).toUpperCase()
      : 'A'

  return (
    <>
      {isOpen && (
        <div
          className="sidebar-backdrop"
          onClick={onCloseMobile}
          aria-hidden="true"
        />
      )}

      <aside className={`sidebar ${isOpen ? 'sidebar-open' : ''}`}>
        {/* Brand header */}
        <div className="brand">
          <div className="brand-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
          </div>

          <div className="brand-text">
            <h2>DVLD</h2>
            <span>Enterprise Portal</span>
          </div>

          {onCloseMobile && (
            <button
              type="button"
              className="sidebar-close-btn"
              onClick={onCloseMobile}
              aria-label="Close menu"
            >
              <CloseIcon size={16} />
            </button>
          )}
        </div>

        {/* Navigation Section */}
        <div className="nav-group-title">MAIN NAVIGATION</div>

        <nav className="sidebar-nav">
          {renderNavItem('Dashboard', <DashboardIcon size={18} />)}
          {renderNavItem('People', <UsersIcon size={18} />)}
          {renderNavItem('Drivers', <DriversIcon size={18} />)}
          {renderNavItem('Licenses', <LicenseIcon size={18} />)}
          {renderNavItem('International Licenses', <GlobeIcon size={18} />)}
          {renderNavItem('Local Applications', <ApplicationsIcon size={18} />)}

          {/* Settings Section */}
          <button
            type="button"
            className={`nav-item settings-toggle ${isSettingsActive ? 'active' : ''}`}
            aria-expanded={isSettingsOpen}
            onClick={() => setIsSettingsOpen((previous) => !previous)}
          >
            <span className="nav-icon" aria-hidden="true">
              <SettingsIcon size={18} />
            </span>
            <span className="settings-label">Settings</span>
            <span className="settings-chevron" aria-hidden="true">
              {isSettingsOpen ? <ChevronDownIcon size={14} /> : <ChevronRightIcon size={14} />}
            </span>
          </button>

          {isSettingsOpen && (
            <div className="settings-submenu">
              {renderNavItem('Application Types', <ApplicationsIcon size={15} />, 'Application Types', true)}
              {renderNavItem('License Classes', <AwardIcon size={15} />, 'License Classes', true)}
              {renderNavItem('Test Types', <TestTypeIcon size={15} />, 'Test Types', true)}
              {renderNavItem('Countries', <FlagIcon size={15} />, 'Countries', true)}
            </div>
          )}
        </nav>

        {/* User Card & Logout Footer */}
        <div className="sidebar-footer">
          <div className="user-profile-card">
            <div className="user-avatar">{initial}</div>

            <div className="user-info">
              <strong title={user?.fullName || 'Administrator'}>
                {user?.fullName || 'Administrator'}
              </strong>
              <span>{user?.role || 'System Admin'}</span>
            </div>

            <button
              type="button"
              className="sidebar-logout-btn"
              onClick={onRequestLogout}
              title="Sign Out of Session"
              aria-label="Sign Out"
            >
              <LogOutIcon size={16} />
            </button>
          </div>
        </div>
      </aside>
    </>
  )
}
