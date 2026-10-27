
import { useState } from 'react'

type SidebarProps = {
  activePage: string
  onNavigate: (page: string) => void
}

export default function Sidebar({
  activePage,
  onNavigate,
}: SidebarProps) {
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

  const renderNavItem = (
    page: string,
    icon: string,
    label: string = page,
  ) => (
    <button
      key={page}
      type="button"
      className={`nav-item ${activePage === page ? 'active' : ''}`}
      onClick={() => onNavigate(page)}
    >
      <span className="nav-icon">{icon}</span>
      {label}
    </button>
  )

  return (
    <aside className="sidebar">
      {/* شعار النظام */}
      <div className="brand">
        <div className="brand-icon">D</div>

        <div>
          <h2>DVLD</h2>
          <span>Management System</span>
        </div>
      </div>

      {/* عنوان القائمة */}
      <p className="nav-heading">MAIN MENU</p>

      {/* روابط التنقل بين الصفحات */}
      <nav className="sidebar-nav">
        {renderNavItem('Dashboard', '▦')}
        {renderNavItem('People', '♙')}
        {renderNavItem('Drivers', '♧')}
        {renderNavItem('Licenses', '▤')}
        {renderNavItem('Applications', '▣')}

        {/* مجموعة الإعدادات */}
        <button
          type="button"
          className={`nav-item settings-toggle ${
            isSettingsActive ? 'active' : ''
          }`}
          aria-expanded={isSettingsOpen}
          onClick={() => setIsSettingsOpen((previous) => !previous)}
        >
          <span className="nav-icon">⚙</span>

          <span className="settings-label">Settings</span>

          <span className="settings-chevron">
            {isSettingsOpen ? '⌄' : '›'}
          </span>
        </button>

        {/* الصفحات الفرعية */}
        {isSettingsOpen && (
          <div className="settings-submenu">
            {renderNavItem('Application Types', '◦')}
            {renderNavItem('License Classes', '◦')}
            {renderNavItem('Test Types', '◦')}
            {renderNavItem('Countries', '◦')}
          </div>
        )}
      </nav>

      {/* بيانات المستخدم أسفل القائمة */}
      <div className="sidebar-footer">
        <div className="user-avatar">A</div>

        <div className="user-info">
          <strong>Administrator</strong>
          <span>System Admin</span>
        </div>
      </div>
    </aside>
  )
}
