import { useState } from 'react'
import { useAuth } from '../context/useAuth'

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
    icon: string,
    label: string = page,
  ) => (
    <button
      key={page}
      type="button"
      className={`nav-item ${activePage === page ? 'active' : ''}`}
      onClick={() => handleNavClick(page)}
    >
      <span className="nav-icon" aria-hidden="true">{icon}</span>
      <span className="nav-label">{label}</span>
      {activePage === page && <span className="nav-active-pill" aria-hidden="true" />}
    </button>
  )

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
          <div className="brand-icon">D</div>

          <div className="brand-text">
            <h2>DVLD</h2>
            <span>Management System</span>
          </div>

          {onCloseMobile && (
            <button
              type="button"
              className="sidebar-close-btn"
              onClick={onCloseMobile}
              aria-label="Close menu"
            >
              ✕
            </button>
          )}
        </div>

        {/* Navigation Section */}
        <p className="nav-heading">MAIN MENU</p>

        <nav className="sidebar-nav">
          {renderNavItem('Dashboard', '▦')}
          {renderNavItem('People', '♙')}
          {renderNavItem('Drivers', '♧')}
          {renderNavItem('Licenses', '▤')}
          {renderNavItem('International Licenses', '🌐')}
          {renderNavItem('Local Applications', '▣')}

          {/* Settings Section */}
          <button
            type="button"
            className={`nav-item settings-toggle ${
              isSettingsActive ? 'active' : ''
            }`}
            aria-expanded={isSettingsOpen}
            onClick={() => setIsSettingsOpen((previous) => !previous)}
          >
            <span className="nav-icon" aria-hidden="true">⚙</span>
            <span className="settings-label">Settings</span>
            <span className="settings-chevron" aria-hidden="true">
              {isSettingsOpen ? '⌄' : '›'}
            </span>
          </button>

          {isSettingsOpen && (
            <div className="settings-submenu">
              {renderNavItem('Application Types', '◦')}
              {renderNavItem('License Classes', '◦')}
              {renderNavItem('Test Types', '◦')}
              {renderNavItem('Countries', '◦')}
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
              title="Sign Out"
              aria-label="Sign Out"
            >
              ⇦
            </button>
          </div>
        </div>
      </aside>
    </>
  )
}
