import { useState, useRef, useEffect } from 'react'
import { useAuth } from '../context/useAuth'

type TopbarProps = {
  activePage: string
  onToggleSidebar?: () => void
  onRequestLogout: () => void
}

export default function Topbar({
  activePage,
  onToggleSidebar,
  onRequestLogout,
}: TopbarProps) {
  const { user } = useAuth()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isMenuOpen])

  const initial = user?.fullName
    ? user.fullName.charAt(0).toUpperCase()
    : user?.userName
      ? user.userName.charAt(0).toUpperCase()
      : 'A'

  return (
    <header className="topbar">
      <div className="topbar-left">
        {onToggleSidebar && (
          <button
            type="button"
            className="mobile-sidebar-toggle"
            onClick={onToggleSidebar}
            aria-label="Toggle navigation menu"
          >
            ☰
          </button>
        )}
        <div>
          <p className="breadcrumb">DVLD / Management</p>
          <h1>{activePage}</h1>
        </div>
      </div>

      <div className="topbar-actions">
        <span className="system-status">
          <span className="status-dot" />
          System Online
        </span>

        {/* User profile dropdown */}
        <div className="user-menu-container" ref={menuRef}>
          <button
            type="button"
            className="topbar-user-btn"
            onClick={() => setIsMenuOpen((prev) => !prev)}
            aria-expanded={isMenuOpen}
            aria-haspopup="menu"
            title="User Profile &amp; Settings"
          >
            <div className="topbar-avatar">{initial}</div>
            <div className="topbar-user-meta">
              <span className="topbar-user-name">
                {user?.fullName || user?.userName || 'Administrator'}
              </span>
              <span className="topbar-user-role">
                {user?.role || 'Staff'}
              </span>
            </div>
            <span className="topbar-chevron">{isMenuOpen ? '▴' : '▾'}</span>
          </button>

          {isMenuOpen && (
            <div className="user-menu-dropdown" role="menu">
              <div className="user-menu-header">
                <div className="user-menu-avatar">{initial}</div>
                <div className="user-menu-details">
                  <strong>{user?.fullName || 'Administrator'}</strong>
                  <span>@{user?.userName || 'admin'}</span>
                  <span className="role-pill">{user?.role || 'Staff'}</span>
                </div>
              </div>

              <div className="user-menu-divider" />

              <div className="user-menu-section">
                <button
                  type="button"
                  className="user-menu-item logout-menu-item"
                  role="menuitem"
                  onClick={() => {
                    setIsMenuOpen(false)
                    onRequestLogout()
                  }}
                >
                  <span className="user-menu-icon" aria-hidden="true">
                    ⇦
                  </span>
                  <span>Sign Out</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
