import { useState, useRef, useEffect } from 'react'
import { useAuth } from '../context/useAuth'
import {
  MenuIcon,
  ChevronDownIcon,
  LogOutIcon,
  UserIcon,
} from './common/Icons'

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
            <MenuIcon size={20} />
          </button>
        )}
        <h1>{activePage}</h1>
      </div>

      <div className="topbar-actions">
        <div className="system-status" title="DVLD Core API Services are operational">
          <span className="status-dot" aria-hidden="true" />
          <span>System Online</span>
        </div>

        {/* User profile dropdown */}
        <div className="user-menu-container" ref={menuRef}>
          <button
            type="button"
            className={`topbar-user-btn ${isMenuOpen ? 'open' : ''}`}
            onClick={() => setIsMenuOpen((prev) => !prev)}
            aria-expanded={isMenuOpen}
            aria-haspopup="menu"
            title="User Profile &amp; Session"
          >
            <div className="topbar-avatar">{initial}</div>
            <div className="topbar-user-meta">
              <span className="topbar-user-name">
                {user?.fullName || user?.userName || 'User'}
              </span>
              <span className="topbar-user-role">
                {user?.role || 'User'}
              </span>
            </div>
            <span className="topbar-chevron" aria-hidden="true">
              <ChevronDownIcon size={14} className={isMenuOpen ? 'rotate-180' : ''} />
            </span>
          </button>

          {isMenuOpen && (
            <div className="user-menu-dropdown" role="menu">
              <div className="user-menu-header">
                <div className="user-menu-avatar">{initial}</div>
                <div className="user-menu-details">
                  <strong>{user?.fullName || user?.userName || 'User'}</strong>
                  {user?.userName && <span>@{user.userName}</span>}
                  <div className="role-pill-badge">{user?.role || 'User'}</div>
                </div>
              </div>

              <div className="user-menu-divider" />

              <div className="user-menu-section">
                {user?.userId && (
                  <div className="user-menu-info-row">
                    <UserIcon size={14} className="text-muted" />
                    <span>User ID: #{user.userId}</span>
                  </div>
                )}

                <div className="user-menu-divider" />

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
                    <LogOutIcon size={16} />
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
