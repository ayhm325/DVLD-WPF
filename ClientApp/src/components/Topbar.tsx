import { useAuth } from '../context/useAuth'
import {
  ChevronDown,
  ChevronRight,
  Home,
  LogOut,
  User as UserIcon,
  Search,
  ShieldCheck,
} from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

export type TopbarProps = {
  activePage: string
  onToggleSidebar?: () => void
  onRequestLogout: () => void
  onOpenCommandPalette?: () => void
  className?: string
}

function getBreadcrumbs(page: string): { parent?: string; current: string } {
  switch (page) {
    case 'Dashboard':
      return { current: 'Dashboard' }
    case 'People':
      return { parent: 'Directory', current: 'People' }
    case 'Drivers':
      return { parent: 'Directory', current: 'Drivers' }
    case 'Licenses':
      return { parent: 'Management', current: 'Licenses' }
    case 'Local Applications':
      return { parent: 'Applications', current: 'Local Applications' }
    case 'International Licenses':
      return { parent: 'Applications', current: 'International Licenses' }
    case 'Issue International License':
      return { parent: 'Applications', current: 'Issue International' }
    case 'Application Types':
      return { parent: 'Settings', current: 'Application Types' }
    case 'License Classes':
      return { parent: 'Settings', current: 'License Classes' }
    case 'Test Types':
      return { parent: 'Settings', current: 'Test Types' }
    case 'Countries':
      return { parent: 'Settings', current: 'Countries' }
    default:
      return { current: page }
  }
}

export default function Topbar({
  activePage,
  onRequestLogout,
  onOpenCommandPalette,
  className = '',
}: TopbarProps) {
  const { user } = useAuth()
  const breadcrumbs = getBreadcrumbs(activePage)

  const initial = user?.fullName
    ? user.fullName.charAt(0).toUpperCase()
    : user?.userName
      ? user.userName.charAt(0).toUpperCase()
      : 'A'

  return (
    <header className={cn('topbar', className)}>
      {/* Layered flowing blue and cyan neon waves background */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden select-none z-0">
        <svg
          className="h-full w-full opacity-80"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 1200 90"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <defs>
            <linearGradient id="topbar-wave-grad-1" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#00d0ff" stopOpacity="0.1" />
              <stop offset="20%" stopColor="#00f0ff" stopOpacity="0.85" />
              <stop offset="50%" stopColor="#0080ff" stopOpacity="0.9" />
              <stop offset="85%" stopColor="#6366f1" stopOpacity="0.3" />
              <stop offset="100%" stopColor="#00d0ff" stopOpacity="0.05" />
            </linearGradient>
            <linearGradient id="topbar-wave-grad-2" x1="0%" y1="100%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#0080ff" stopOpacity="0.05" />
              <stop offset="35%" stopColor="#38bdf8" stopOpacity="0.75" />
              <stop offset="70%" stopColor="#00f0ff" stopOpacity="0.6" />
              <stop offset="100%" stopColor="#2563eb" stopOpacity="0.1" />
            </linearGradient>
            <filter id="topbar-wave-glow" x="-20%" y="-40%" width="140%" height="180%">
              <feGaussianBlur stdDeviation="3.5" result="blur" />
              <feMerge>
                <feMergeNode in="blur" />
                <feMergeNode in="blur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
            <filter id="topbar-particle-glow" x="-50%" y="-50%" width="200%" height="200%">
              <feGaussianBlur stdDeviation="2.5" result="blur" />
              <feMerge>
                <feMergeNode in="blur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
            <radialGradient id="topbar-ambient-bloom" cx="30%" cy="50%" r="45%">
              <stop offset="0%" stopColor="#0090ff" stopOpacity="0.35" />
              <stop offset="60%" stopColor="#00f0ff" stopOpacity="0.12" />
              <stop offset="100%" stopColor="#0080ff" stopOpacity="0" />
            </radialGradient>
          </defs>

          {/* Ambient radiant light bloom */}
          <rect x="0" y="0" width="1200" height="90" fill="url(#topbar-ambient-bloom)" />

          {/* Primary undulating luminous neon wave */}
          <path
            d="M 0 55 Q 200 20 400 45 T 800 35 T 1200 45"
            fill="none"
            stroke="url(#topbar-wave-grad-1)"
            strokeWidth="2.5"
            filter="url(#topbar-wave-glow)"
          />

          {/* Secondary delicate flowing cyan wave */}
          <path
            d="M 0 65 Q 250 80 500 40 T 950 55 T 1200 30"
            fill="none"
            stroke="url(#topbar-wave-grad-2)"
            strokeWidth="1.8"
            filter="url(#topbar-wave-glow)"
            opacity="0.8"
          />

          {/* Glowing particle stars along the waves */}
          <circle cx="220" cy="30" r="2.2" fill="#00f0ff" filter="url(#topbar-particle-glow)" />
          <circle cx="380" cy="46" r="1.8" fill="#38bdf8" filter="url(#topbar-particle-glow)" />
          <circle cx="520" cy="42" r="2.2" fill="#00f0ff" filter="url(#topbar-particle-glow)" />
          <circle cx="720" cy="36" r="1.5" fill="#67e8f9" filter="url(#topbar-particle-glow)" />
          <circle cx="910" cy="52" r="2" fill="#38bdf8" filter="url(#topbar-particle-glow)" />
          <circle cx="1060" cy="35" r="1.6" fill="#00f0ff" filter="url(#topbar-particle-glow)" />
        </svg>
      </div>

      {/* Left Identity & Dynamic Page Title with Breadcrumbs (NO SIDEBAR TOGGLE) */}
      <div className="topbar-identity relative z-10 flex items-center gap-3.5">
        <span
          className="topbar-beacon w-1.5 h-7 rounded-full bg-cyan-400 shadow-[0_0_12px_#00e5ff] shrink-0"
          aria-hidden="true"
        />

        <div className="flex flex-col min-w-0">
          <h1 className="topbar-title text-xl sm:text-2xl font-black text-white tracking-tight leading-none">
            {activePage}
          </h1>

          <nav
            aria-label="Breadcrumb"
            className="topbar-breadcrumb flex items-center gap-1.5 mt-1 text-[11px] font-medium text-slate-400"
          >
            <Home className="size-3 text-slate-400 shrink-0" aria-hidden="true" />
            <span>Home</span>
            {breadcrumbs.parent && (
              <>
                <ChevronRight className="size-3 text-slate-500 shrink-0" aria-hidden="true" />
                <span>{breadcrumbs.parent}</span>
              </>
            )}
            <ChevronRight className="size-3 text-slate-500 shrink-0" aria-hidden="true" />
            <span className="text-cyan-400 font-semibold">{breadcrumbs.current}</span>
          </nav>
        </div>
      </div>

      {/* Right Controls: Search, System Status, User Profile */}
      <div className="topbar-actions relative z-10 flex items-center gap-3 sm:gap-4">
        {onOpenCommandPalette && (
          <button
            type="button"
            onClick={onOpenCommandPalette}
            className="topbar-search-btn"
            title="Search & Jump to page (Ctrl + K)"
            aria-label="Global search shortcut Ctrl K"
          >
            <Search className="size-4 text-cyan-400 drop-shadow-[0_0_8px_rgba(0,229,255,0.8)] shrink-0" />
            <span className="hidden sm:inline">Search anything...</span>
            <span className="sm:hidden">Search</span>
            <kbd className="topbar-search-kbd">
              Ctrl + K
            </kbd>
          </button>
        )}

        <div
          className="system-status"
          title="DVLD Core API Services are operational"
        >
          <span className="relative flex items-center justify-center size-3">
            <span className="status-pulse-ring absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75 animate-ping" />
            <span className="status-pulse-dot relative inline-flex size-2.5 rounded-full bg-emerald-400 shadow-[0_0_8px_#10b981]" />
          </span>
          <span className="font-semibold text-xs text-white">System Online</span>
          <svg
            className="topbar-status-waveform size-5 sm:w-7 sm:h-3.5 text-emerald-400 drop-shadow-[0_0_6px_rgba(52,211,153,0.7)] ml-0.5"
            viewBox="0 0 28 14"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden="true"
          >
            <path
              d="M 1 7 L 6 7 L 9 2 L 13 12 L 16 4 L 19 9 L 21 7 L 27 7"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          <ChevronDown className="size-3 text-emerald-400/80 ml-0.5" aria-hidden="true" />
        </div>

        {/* User Profile Accessible Dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              className="topbar-user-btn"
              title="User Profile & Session"
              aria-label="User profile and session options"
            >
              <div className="topbar-avatar">{initial}</div>
              <div className="topbar-user-meta hidden sm:flex flex-col text-left">
                <span className="topbar-user-name">
                  {user?.fullName || user?.userName || 'User'}
                </span>
                <div className="flex items-center gap-1 mt-0.5">
                  <ShieldCheck className="size-3 text-cyan-400 shrink-0" aria-hidden="true" />
                  <span className="topbar-user-role">
                    {user?.role || 'Staff'}
                  </span>
                </div>
              </div>
              <ChevronDown className="topbar-chevron size-3.5 text-cyan-400 shrink-0 transition-transform duration-200" aria-hidden="true" />
            </button>
          </DropdownMenuTrigger>

          <DropdownMenuContent
            align="end"
            sideOffset={10}
            className="w-72 p-0 overflow-hidden bg-[#061226]/95 border-2 border-[#0090ff] shadow-[0_16px_40px_rgba(0,0,0,0.8),0_0_25px_rgba(0,140,255,0.4)] rounded-2xl z-50 backdrop-blur-xl ring-1 ring-white/10"
          >
            {/* Header: Dark navy to royal blue gradient with avatar, name, and role */}
            <div className="bg-gradient-to-br from-[#050e20] via-[#091a3c] to-[#0f2d64] p-4 text-white border-b border-blue-500/30">
              <div className="flex items-center gap-3">
                <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 text-white font-extrabold text-base shadow-[0_0_14px_rgba(0,140,255,0.8)] border-[1.5px] border-blue-300/80">
                  {initial}
                </div>
                <div className="flex flex-col min-w-0 flex-1">
                  <span className="text-[10px] uppercase font-bold tracking-wider text-cyan-400">
                    Active Session
                  </span>
                  <strong
                    className="truncate text-sm font-bold text-white leading-tight mt-0.5"
                    title={user?.fullName || user?.userName || 'User'}
                  >
                    {user?.fullName || user?.userName || 'User'}
                  </strong>
                  {user?.userName && (
                    <span className="text-xs text-slate-300 truncate font-mono">
                      @{user.userName}
                    </span>
                  )}
                </div>
              </div>
              <div className="mt-3 flex items-center justify-between">
                <Badge
                  variant="secondary"
                  className="bg-blue-500/20 text-blue-200 border-blue-400/40 text-[10px] font-bold px-2.5 py-0.5"
                >
                  {user?.role || 'Staff'}
                </Badge>
                <span className="text-[10px] text-emerald-400 flex items-center gap-1.5 font-medium">
                  <span className="size-1.5 rounded-full bg-emerald-400 shadow-[0_0_6px_#34d399]" />
                  Authorized
                </span>
              </div>
            </div>

            {/* Body: Elevated dark surface for account info */}
            <div className="p-3 bg-[#050d1a]/85 border-b border-blue-500/20 space-y-1">
              {user?.userId && (
                <div className="flex items-center justify-between px-2.5 py-2 rounded-xl bg-[#09162e] border border-blue-500/30 text-xs shadow-xs">
                  <span className="text-slate-400 flex items-center gap-2 font-medium">
                    <UserIcon className="size-3.5 text-blue-400" />
                    Account ID
                  </span>
                  <span className="font-mono font-bold text-white">
                    #{user.userId}
                  </span>
                </div>
              )}
            </div>

            {/* Actions: Distinct Sign Out */}
            <div className="p-2 bg-[#061226]">
              <DropdownMenuItem
                className="w-full text-red-400 focus:text-red-300 focus:bg-red-950/40 hover:bg-red-950/40 cursor-pointer font-semibold rounded-xl px-3 py-2.5 flex items-center gap-2 transition-colors"
                onClick={onRequestLogout}
              >
                <LogOut className="size-4 text-red-400" />
                <span>Sign Out of Session</span>
              </DropdownMenuItem>
            </div>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
