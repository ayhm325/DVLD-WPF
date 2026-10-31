import { useState, type ReactNode } from 'react'
import { motion, AnimatePresence } from 'motion/react'
import { useAuth } from '../context/useAuth'
import { isPagePermitted } from '../utils/permissions'
import { cn } from '@/lib/utils'
import {
  LayoutDashboard,
  Users,
  Car,
  CreditCard,
  FileText,
  Globe,
  Settings,
  ChevronRight,
  ChevronDown,
  X,
  LogOut,
  Layers,
  Award,
  ClipboardCheck,
  Flag,
} from 'lucide-react'

type SidebarProps = {
  activePage: string
  onNavigate: (page: string) => void
  isOpen?: boolean
  onCloseMobile?: () => void
  onRequestLogout: () => void
}

interface NavItemDef {
  page: string
  icon: ReactNode
  label?: string
}

export default function Sidebar({
  activePage,
  onNavigate,
  isOpen = false,
  onCloseMobile,
  onRequestLogout,
}: SidebarProps) {
  const { user } = useAuth()

  const allMainNavItems: NavItemDef[] = [
    { page: 'Dashboard', icon: <LayoutDashboard className="size-4.5" /> },
    { page: 'People', icon: <Users className="size-4.5" /> },
    { page: 'Drivers', icon: <Car className="size-4.5" /> },
    { page: 'Licenses', icon: <CreditCard className="size-4.5" /> },
    { page: 'Local Applications', icon: <FileText className="size-4.5" /> },
    { page: 'International Licenses', icon: <Globe className="size-4.5" /> },
  ]

  const allSettingsPages: NavItemDef[] = [
    { page: 'Application Types', icon: <Layers className="size-4" />, label: 'Application Types' },
    { page: 'License Classes', icon: <Award className="size-4" />, label: 'License Classes' },
    { page: 'Test Types', icon: <ClipboardCheck className="size-4" />, label: 'Test Types' },
    { page: 'Countries', icon: <Flag className="size-4" />, label: 'Countries' },
  ]

  // Filter items strictly by role-based permissions
  const visibleMainNavItems = allMainNavItems.filter((item) =>
    isPagePermitted(item.page, user?.role),
  )

  const visibleSettingsPages = allSettingsPages.filter((item) =>
    isPagePermitted(item.page, user?.role),
  )

  const visibleSettingsPageNames = visibleSettingsPages.map((item) => item.page)

  const [isSettingsOpen, setIsSettingsOpen] = useState(
    visibleSettingsPageNames.includes(activePage),
  )

  const isSettingsActive = visibleSettingsPageNames.includes(activePage)

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
        className={cn(
          'nav-item group relative flex items-center gap-3 w-full rounded-xl transition-all duration-200 cursor-pointer text-left',
          isSubItem ? 'pl-8 py-2 text-[12.5px]' : 'py-2.5 px-3.5 text-[13.5px]',
          isActive
            ? 'active bg-gradient-to-r from-blue-600/35 via-blue-500/20 to-blue-600/10 border-2 border-[#0090ff] shadow-[0_0_24px_rgba(0,140,255,0.65),inset_0_0_16px_rgba(0,140,255,0.35)] text-white font-bold'
            : 'border border-transparent text-slate-200 hover:text-white hover:bg-[#0a1c3d]/80 hover:border-blue-400/50 hover:shadow-[0_0_16px_rgba(0,140,255,0.3)] font-medium',
        )}
        onClick={() => handleNavClick(page)}
      >
        <span
          className={cn(
            'nav-icon flex items-center justify-center shrink-0 transition-colors',
            isActive
              ? 'text-cyan-400 drop-shadow-[0_0_10px_rgba(0,229,255,0.9)]'
              : 'text-cyan-400/90 drop-shadow-[0_0_6px_rgba(0,229,255,0.5)] group-hover:text-cyan-300 group-hover:drop-shadow-[0_0_10px_rgba(0,229,255,0.9)]',
          )}
          aria-hidden="true"
        >
          {icon}
        </span>
        <span className="nav-label flex-1 truncate">{label}</span>
        {isActive && (
          <ChevronRight className="size-4.5 text-cyan-400 drop-shadow-[0_0_8px_rgba(0,229,255,0.9)] shrink-0 ml-auto" aria-hidden="true" />
        )}
      </button>
    )
  }

  const initial = user?.fullName
    ? user.fullName.charAt(0).toUpperCase()
    : user?.userName
      ? user.userName.charAt(0).toUpperCase()
      : 'U'

  return (
    <>
      {isOpen && (
        <div
          className="sidebar-backdrop"
          onClick={onCloseMobile}
          aria-hidden="true"
        />
      )}

      <aside className={cn('sidebar relative flex flex-col justify-between overflow-hidden', isOpen && 'sidebar-open')}>
        {/* Decorative futuristic glowing waves across the lower sidebar */}
        <div className="pointer-events-none absolute bottom-0 inset-x-0 h-[380px] w-full overflow-hidden select-none z-0">
          <svg
            className="h-full w-full opacity-90"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 260 380"
            preserveAspectRatio="none"
            aria-hidden="true"
          >
            <defs>
              <radialGradient id="sidebar-ambient-light" cx="65%" cy="50%" r="50%">
                <stop offset="0%" stopColor="#0090ff" stopOpacity="0.3" />
                <stop offset="60%" stopColor="#00f0ff" stopOpacity="0.12" />
                <stop offset="100%" stopColor="#0090ff" stopOpacity="0" />
              </radialGradient>
              <linearGradient id="sidebar-wave-grad-1" x1="0%" y1="100%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#0080ff" stopOpacity="0.8" />
                <stop offset="45%" stopColor="#00f0ff" stopOpacity="0.95" />
                <stop offset="100%" stopColor="#7c3aed" stopOpacity="0.3" />
              </linearGradient>
              <linearGradient id="sidebar-wave-grad-2" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stopColor="#00d0ff" stopOpacity="0.3" />
                <stop offset="50%" stopColor="#38bdf8" stopOpacity="0.8" />
                <stop offset="100%" stopColor="#1d4ed8" stopOpacity="0.7" />
              </linearGradient>
              <linearGradient id="sidebar-wave-grad-3" x1="0%" y1="50%" x2="100%" y2="50%">
                <stop offset="0%" stopColor="#00f0ff" stopOpacity="0.2" />
                <stop offset="60%" stopColor="#0088ff" stopOpacity="0.6" />
                <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.8" />
              </linearGradient>
              <filter id="sidebar-wave-glow-strong" x="-30%" y="-30%" width="160%" height="160%">
                <feGaussianBlur stdDeviation="4" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
              <filter id="sidebar-particle-glow" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur stdDeviation="3" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>

            {/* Ambient radiant light bloom */}
            <rect x="0" y="40" width="260" height="340" fill="url(#sidebar-ambient-light)" />

            {/* Wave Layer 1 - Upper intense cyan wave */}
            <path
              d="M -10 160 C 60 90, 130 200, 190 130 C 230 80, 250 110, 270 70"
              fill="none"
              stroke="url(#sidebar-wave-grad-1)"
              strokeWidth="2.6"
              filter="url(#sidebar-wave-glow-strong)"
            />

            {/* Wave Layer 2 - Secondary luminous cyan/blue wave */}
            <path
              d="M -10 220 C 70 140, 130 250, 200 170 C 240 120, 260 150, 270 110"
              fill="none"
              stroke="url(#sidebar-wave-grad-2)"
              strokeWidth="2.2"
              filter="url(#sidebar-wave-glow-strong)"
            />

            {/* Wave Layer 3 - Mid flowing wave */}
            <path
              d="M -10 270 C 75 190, 145 285, 205 215 C 240 175, 255 195, 270 155"
              fill="none"
              stroke="url(#sidebar-wave-grad-1)"
              strokeWidth="1.8"
              opacity="0.85"
            />

            {/* Wave Layer 4 - Deeper lower undulating wave */}
            <path
              d="M -10 320 C 85 240, 155 330, 215 260 C 245 220, 260 240, 270 200"
              fill="none"
              stroke="url(#sidebar-wave-grad-3)"
              strokeWidth="1.5"
              opacity="0.6"
            />

            {/* Glowing particle stars along the waves */}
            <circle cx="190" cy="130" r="3" fill="#00f0ff" filter="url(#sidebar-particle-glow)" />
            <circle cx="130" cy="200" r="2.5" fill="#38bdf8" filter="url(#sidebar-particle-glow)" />
            <circle cx="60" cy="90" r="2.2" fill="#67e8f9" filter="url(#sidebar-particle-glow)" />
            <circle cx="200" cy="170" r="2.5" fill="#00f0ff" filter="url(#sidebar-particle-glow)" />
            <circle cx="80" cy="200" r="2" fill="#60a5fa" filter="url(#sidebar-particle-glow)" />
            <circle cx="205" cy="215" r="2.5" fill="#38bdf8" filter="url(#sidebar-particle-glow)" />
            <circle cx="145" cy="285" r="1.8" fill="#00f0ff" filter="url(#sidebar-particle-glow)" />
            <circle cx="240" cy="120" r="2.2" fill="#93c5fd" filter="url(#sidebar-particle-glow)" />
            <circle cx="35" cy="135" r="1.8" fill="#38bdf8" filter="url(#sidebar-particle-glow)" />
            <circle cx="170" cy="100" r="1.5" fill="#a5f3fc" filter="url(#sidebar-particle-glow)" />
            <circle cx="225" cy="190" r="1.5" fill="#38bdf8" filter="url(#sidebar-particle-glow)" />
          </svg>
        </div>

        <div className="relative z-10 flex flex-col flex-1 min-h-0">
          {/* Brand header enclosed in a prominent rounded illuminated card container */}
          <div className="brand-card flex items-center gap-3.5 p-3.5 rounded-2xl border-2 border-[#0090ff] bg-[#07122a]/90 shadow-[0_0_24px_rgba(0,140,255,0.65),inset_0_0_16px_rgba(0,140,255,0.25)] backdrop-blur-md mb-4">
            <div className="brand-icon flex size-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-blue-700 text-white border-[1.5px] border-blue-300/70 shadow-[0_0_20px_rgba(0,140,255,0.8),inset_0_1px_2px_rgba(255,255,255,0.6)]">
              <Layers className="size-6 text-white stroke-[2.3]" />
            </div>

            <div className="brand-text flex flex-col min-w-0 flex-1">
              <h2 className="text-[22px] font-black tracking-tight text-white leading-none">DVLD</h2>
              <span className="text-[11.5px] font-medium text-blue-200/80 mt-1 tracking-normal">Enterprise Portal</span>
            </div>

            {onCloseMobile && (
              <button
                type="button"
                className="sidebar-close-btn md:hidden flex items-center justify-center size-8 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800/80 transition-colors cursor-pointer"
                onClick={onCloseMobile}
                aria-label="Close menu"
              >
                <X className="size-4" />
              </button>
            )}
          </div>

          {/* Navigation Section */}
          <div className="nav-group-title flex items-center gap-2 px-2 mt-1 mb-2.5">
            <span className="size-2 rounded-full bg-cyan-400 shadow-[0_0_10px_#00e5ff]" aria-hidden="true" />
            <span className="text-[10.5px] font-extrabold uppercase tracking-[2px] text-blue-200/70">
              MAIN NAVIGATION
            </span>
            <div className="h-[1px] flex-1 bg-gradient-to-r from-blue-500/50 via-blue-400/25 to-transparent ml-2" />
          </div>

          <nav className="sidebar-nav flex flex-col gap-1.5 px-1 overflow-y-auto flex-1 scrollbar-thin scrollbar-thumb-[#1e2d48] scrollbar-track-transparent">
            {visibleMainNavItems.map((item) =>
              renderNavItem(item.page, item.icon, item.label || item.page),
            )}

            {/* Settings Section */}
            {visibleSettingsPages.length > 0 && (
              <>
                <button
                  type="button"
                  className={cn(
                    'nav-item settings-toggle group relative flex items-center gap-3 w-full py-2.5 px-3.5 rounded-xl transition-all duration-200 cursor-pointer text-left',
                    isSettingsActive
                      ? 'active bg-gradient-to-r from-blue-600/35 via-blue-500/20 to-blue-600/10 border-2 border-[#0090ff] shadow-[0_0_24px_rgba(0,140,255,0.65),inset_0_0_16px_rgba(0,140,255,0.35)] text-white font-bold'
                      : 'border border-transparent text-slate-200 hover:text-white hover:bg-[#0a1c3d]/80 hover:border-blue-400/50 hover:shadow-[0_0_16px_rgba(0,140,255,0.3)] font-medium',
                  )}
                  aria-expanded={isSettingsOpen}
                  onClick={() => setIsSettingsOpen((previous) => !previous)}
                >
                  <span
                    className={cn(
                      'nav-icon flex items-center justify-center shrink-0 transition-colors',
                      isSettingsActive
                        ? 'text-cyan-400 drop-shadow-[0_0_10px_rgba(0,229,255,0.9)]'
                        : 'text-cyan-400/90 drop-shadow-[0_0_6px_rgba(0,229,255,0.5)] group-hover:text-cyan-300 group-hover:drop-shadow-[0_0_10px_rgba(0,229,255,0.9)]',
                    )}
                    aria-hidden="true"
                  >
                    <Settings className="size-4.5" />
                  </span>
                  <span className="settings-label flex-1 truncate">Settings</span>
                  <span
                    className={cn(
                      'settings-chevron shrink-0 ml-auto transition-transform',
                      isSettingsActive ? 'text-cyan-400 drop-shadow-[0_0_8px_rgba(0,229,255,0.9)]' : 'text-cyan-400/70 group-hover:text-cyan-300',
                    )}
                    aria-hidden="true"
                  >
                    {isSettingsOpen ? (
                      <ChevronDown className="size-3.5" />
                    ) : (
                      <ChevronRight className="size-3.5" />
                    )}
                  </span>
                </button>

                <AnimatePresence initial={false}>
                  {isSettingsOpen && (
                    <motion.div
                      className="settings-submenu overflow-hidden flex flex-col gap-1 pl-2 border-l border-blue-500/20 ml-5 my-1"
                      initial={{ opacity: 0, height: 0 }}
                      animate={{ opacity: 1, height: 'auto' }}
                      exit={{ opacity: 0, height: 0 }}
                      transition={{ duration: 0.18, ease: 'easeInOut' }}
                    >
                      {visibleSettingsPages.map((item) =>
                        renderNavItem(
                          item.page,
                          item.icon,
                          item.label || item.page,
                          true,
                        ),
                      )}
                    </motion.div>
                  )}
                </AnimatePresence>
              </>
            )}
          </nav>
        </div>

        {/* User Card & Logout Footer */}
        <div className="sidebar-footer relative z-10 mt-auto pt-2 px-1">
          <div className="user-profile-card flex items-center gap-3 p-3 rounded-2xl border-2 border-[#0090ff] bg-[#07122a]/95 shadow-[0_0_24px_rgba(0,140,255,0.55),inset_0_0_16px_rgba(0,140,255,0.2)] backdrop-blur-md">
            <div className="user-avatar flex size-9.5 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 border-[1.5px] border-blue-300/70 shadow-[0_0_14px_rgba(0,140,255,0.7)] text-white font-bold text-sm">
              {initial}
            </div>

            <div className="user-info flex flex-col min-w-0 flex-1 text-left">
              <strong
                className="text-xs font-bold text-white truncate leading-tight"
                title={user?.fullName || user?.userName || 'User'}
              >
                {user?.fullName || user?.userName || 'User'}
              </strong>
              <span className="text-[10px] font-extrabold uppercase tracking-wider text-cyan-400 mt-0.5 drop-shadow-[0_0_6px_rgba(0,229,255,0.6)]">
                {user?.role || 'Staff'}
              </span>
            </div>

            <button
              type="button"
              className="sidebar-logout-btn flex size-8.5 shrink-0 items-center justify-center rounded-xl text-cyan-400 hover:text-red-400 hover:bg-red-500/25 border-[1.5px] border-blue-500/50 hover:border-red-500/70 bg-[#0c224a]/85 shadow-[0_0_12px_rgba(0,140,255,0.25)] hover:shadow-[0_0_16px_rgba(239,68,68,0.4)] transition-all cursor-pointer"
              onClick={onRequestLogout}
              title="Sign Out of Session"
              aria-label="Sign Out"
            >
              <LogOut className="size-4" />
            </button>
          </div>
        </div>
      </aside>
    </>
  )
}
