import { useId } from 'react'
import type { ReactNode } from 'react'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import './PageHero.css'
import {
  PAGE_HERO_CONFIG,
  type PageHeroConfigItem,
  type FloatingIconItem,
  type FloatingIconColor,
  type FloatingIconPosition,
} from './pageHeroConfig'

export interface PageHeroProps {
  /** Known page identifier to retrieve preset neon configuration */
  pageId?: string
  /** Uppercase eyebrow label */
  eyebrow?: string
  /** Main title (or prefix when highlight is used) */
  title?: string
  /** Highlighted title segment rendered with glowing cyan/blue gradient */
  highlight?: string
  /** Descriptive explanation text */
  description?: string
  /** Custom Lucide icon (backward compatible) */
  icon?: ReactNode
  /** Custom badge text (backward compatible) */
  badgeText?: string
  /** Additional container classes */
  className?: string
  /** Action buttons, pills, or controls placed beside/below description */
  actions?: ReactNode
  /** Children elements */
  children?: ReactNode
  /** Custom central illustration node (overrides the default futuristic holographic card) */
  illustration?: ReactNode
  /** Custom illustration code tag (e.g. 'CITIZEN-ID') */
  illustrationTag?: string
  /** Custom central illustration icon */
  illustrationIcon?: ReactNode
  /** Custom set of floating icon containers */
  floatingIcons?: FloatingIconItem[]
  /** Custom visual accent variant (default 'blue') */
  variant?: 'blue' | 'emerald' | 'purple' | 'amber' | 'cyan' | 'rose'
}

function getFloatingIconClasses(
  color: FloatingIconColor = 'blue',
  position: FloatingIconPosition = 'left',
) {
  const colorStyles: Record<FloatingIconColor, string> = {
    blue: 'border-blue-400/50 bg-[#091834]/90 text-blue-400 shadow-[0_0_22px_rgba(59,130,246,0.45)]',
    emerald: 'border-emerald-400/50 bg-[#061d19]/90 text-emerald-400 shadow-[0_0_20px_rgba(16,185,129,0.45)]',
    purple: 'border-purple-400/50 bg-[#170e2c]/90 text-purple-400 shadow-[0_0_20px_rgba(168,85,247,0.45)]',
    amber: 'border-amber-400/50 bg-[#241505]/90 text-amber-400 shadow-[0_0_20px_rgba(245,158,11,0.45)]',
    cyan: 'border-cyan-400/50 bg-[#051d24]/90 text-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.45)]',
    rose: 'border-rose-400/50 bg-[#240815]/90 text-rose-400 shadow-[0_0_20px_rgba(244,63,94,0.45)]',
  }

  const positionStyles: Record<FloatingIconPosition, { wrapper: string; container: string }> = {
    left: {
      wrapper: 'absolute left-2 top-4 animate-[float-slow_5s_ease-in-out_infinite] z-20',
      container: 'flex size-14 items-center justify-center rounded-2xl border backdrop-blur-md',
    },
    'top-right': {
      wrapper: 'absolute right-4 -top-2 animate-[float-reverse_5.5s_ease-in-out_infinite] z-20',
      container: 'flex size-12 items-center justify-center rounded-2xl border backdrop-blur-md',
    },
    'bottom-right': {
      wrapper: 'absolute right-1 -bottom-2 animate-[float-slow_6.2s_ease-in-out_infinite_0.8s] z-20',
      container: 'flex size-12 items-center justify-center rounded-2xl border backdrop-blur-md',
    },
  }

  return {
    wrapper: positionStyles[position]?.wrapper ?? positionStyles.left.wrapper,
    container: cn(
      positionStyles[position]?.container ?? positionStyles.left.container,
      colorStyles[color] ?? colorStyles.blue,
    ),
  }
}

/**
 * Global Premium Neon Hero Component.
 * Reproduces the Dashboard reference visual language across all pages in DVLD:
 * - Deep navy gradient background with ambient radial glow
 * - Illuminated electric blue border with corner bloom
 * - Layered flowing electric light stream SVG
 * - High-contrast typography with gradient title highlight
 * - Tailored central futuristic holographic card illustration
 * - Floating neon Lucide icon panels with subtle floating animations
 */
export default function PageHero({
  pageId,
  eyebrow,
  title,
  highlight,
  description,
  icon,
  badgeText,
  className = '',
  actions,
  children,
  illustration,
  illustrationTag,
  illustrationIcon,
  floatingIcons,
}: PageHeroProps) {
  const config: PageHeroConfigItem | undefined = pageId ? PAGE_HERO_CONFIG[pageId] : undefined
  const filterUniqueId = useId().replace(/:/g, '')

  const finalEyebrow = eyebrow ?? config?.eyebrow ?? 'SYSTEM SECTION'
  const finalTitle = title ?? config?.title ?? ''
  const finalHighlight = highlight ?? config?.highlight
  const finalDescription = description ?? config?.description ?? ''

  const finalIllustrationTag = illustrationTag ?? badgeText ?? config?.illustrationTag ?? 'DVLD-SYS'
  const finalIllustrationIcon = illustrationIcon ?? config?.illustrationIcon ?? icon
  const finalFloatingIcons = floatingIcons ?? config?.floatingIcons ?? []

  return (
    <header
      className={cn(
        'page-hero relative overflow-hidden rounded-2xl border border-blue-500/50 bg-gradient-to-r from-[#070f20] via-[#0c1c38] to-[#122b5c] p-6 sm:p-8 lg:p-9 shadow-[0_0_30px_rgba(37,99,235,0.22),inset_0_0_25px_rgba(59,130,246,0.08)] mb-6',
        className,
      )}
    >
      {/* Ambient background glows */}
      <div
        className="pointer-events-none absolute -right-20 -top-20 h-72 w-72 rounded-full bg-blue-500/15 blur-3xl"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute right-1/4 -bottom-20 h-64 w-64 rounded-full bg-cyan-500/15 blur-3xl"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute right-12 bottom-0 h-48 w-48 rounded-full bg-purple-500/15 blur-3xl"
        aria-hidden="true"
      />

      {/* Decorative flowing electric light stream behind content */}
      <svg
        className="pointer-events-none absolute inset-0 h-full w-full opacity-60 select-none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
        preserveAspectRatio="none"
        viewBox="0 0 1000 200"
      >
        <defs>
          <linearGradient id={`hero-stream-grad-${filterUniqueId}`} x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#3b82f6" stopOpacity="0" />
            <stop offset="35%" stopColor="#38bdf8" stopOpacity="0.4" />
            <stop offset="70%" stopColor="#60a5fa" stopOpacity="0.6" />
            <stop offset="100%" stopColor="#a855f7" stopOpacity="0.1" />
          </linearGradient>
          <filter id={`hero-stream-glow-${filterUniqueId}`} x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="4" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        <path
          d="M 0 130 Q 250 80 500 120 T 1000 80"
          fill="none"
          stroke={`url(#hero-stream-grad-${filterUniqueId})`}
          strokeWidth="2.5"
          filter={`url(#hero-stream-glow-${filterUniqueId})`}
        />
        <path
          d="M 100 160 Q 380 110 650 145 T 1000 115"
          fill="none"
          stroke={`url(#hero-stream-grad-${filterUniqueId})`}
          strokeWidth="1.5"
          opacity="0.4"
        />
      </svg>

      <div className="relative z-10 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8">
        {/* Left Side: High-Contrast Typography & Actions */}
        <div className="max-w-xl">
          <div className="flex items-center gap-2">
            <span
              className="inline-block size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#22d3ee]"
              aria-hidden="true"
            />
            <span className="text-[11px] font-extrabold uppercase tracking-[2.5px] text-cyan-400">
              {finalEyebrow}
            </span>
          </div>

          <h1 className="mt-3 text-3xl sm:text-4xl lg:text-[40px] font-black tracking-tight text-white leading-tight">
            {finalTitle}{' '}
            {finalHighlight && (
              <span className="bg-gradient-to-r from-blue-400 via-sky-300 to-cyan-300 bg-clip-text text-transparent drop-shadow-[0_0_25px_rgba(56,189,248,0.5)]">
                {finalHighlight}
              </span>
            )}
          </h1>

          {finalDescription && (
            <p className="mt-2.5 text-sm sm:text-base font-normal text-slate-300/90 leading-relaxed max-w-lg">
              {finalDescription}
            </p>
          )}

          {actions && <div className="mt-4 flex items-center gap-3">{actions}</div>}

          {children}
        </div>

        {/* Right Side: Futuristic Holographic Card Illustration & Floating Neon Icons */}
        <div
          className="relative hidden md:flex items-center justify-center shrink-0 w-full lg:w-[420px] h-[160px] pointer-events-none select-none"
          aria-hidden="true"
        >
          {illustration ? (
            illustration
          ) : (
            <>
              {/* Floating Lucide Icon Panels */}
              {finalFloatingIcons.map((item, index) => {
                const defaultPositions: FloatingIconPosition[] = ['left', 'top-right', 'bottom-right']
                const pos = item.position ?? defaultPositions[index % 3]
                const classes = getFloatingIconClasses(item.color, pos)

                return (
                  <div key={index} className={classes.wrapper}>
                    <div className={classes.container}>{item.icon}</div>
                  </div>
                )
              })}

              {/* Centerpiece: Futuristic Holographic Identification Card */}
              <div className="relative z-10 w-[210px] h-[135px] rounded-2xl border border-blue-400/50 bg-gradient-to-br from-[#0b1730]/95 via-[#0d2044]/95 to-[#09152b]/95 p-3.5 shadow-[0_12px_32px_rgba(0,0,0,0.6),0_0_30px_rgba(59,130,246,0.35)] backdrop-blur-lg animate-[float-main_6s_ease-in-out_infinite]">
                {/* Holographic Card Header with 3 glowing status dots */}
                <div className="flex items-center justify-between pb-2 border-b border-blue-500/20">
                  <div className="flex items-center gap-1.5">
                    <span className="size-1.5 rounded-full bg-cyan-400 shadow-[0_0_4px_#22d3ee]" />
                    <span className="size-1.5 rounded-full bg-blue-400/70" />
                    <span className="size-1.5 rounded-full bg-indigo-400/50" />
                  </div>
                  <span className="text-[9px] font-mono font-bold tracking-widest text-blue-300/80 uppercase">
                    {finalIllustrationTag}
                  </span>
                </div>

                {/* Holographic Card Body */}
                <div className="mt-2.5 flex items-center gap-3">
                  {/* Central Avatar / Domain Emblem */}
                  <div className="flex size-11 shrink-0 items-center justify-center rounded-full border border-blue-400/40 bg-blue-600/20 text-blue-300 shadow-[0_0_12px_rgba(59,130,246,0.3)] [&>svg]:size-6">
                    {finalIllustrationIcon}
                  </div>

                  {/* Data Placeholder Bars */}
                  <div className="flex-1 space-y-1.5">
                    <div className="h-2 w-20 rounded-full bg-gradient-to-r from-blue-400/60 to-cyan-400/40 shadow-[0_0_8px_rgba(56,189,248,0.3)]" />
                    <div className="h-1.5 w-14 rounded-full bg-slate-600/60" />
                    <div className="h-1.5 w-16 rounded-full bg-slate-600/40" />
                  </div>
                </div>

                {/* Glowing Verified Badge */}
                <div className="absolute right-3 bottom-3 flex size-6 items-center justify-center rounded-full border border-emerald-400/60 bg-emerald-500/20 text-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.55)]">
                  <Check className="size-3.5 stroke-[2.5]" />
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
