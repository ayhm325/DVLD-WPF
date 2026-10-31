import { useEffect, useId, useState } from 'react'
import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'

export type StatCardVariant =
  | 'blue'
  | 'green'
  | 'emerald'
  | 'purple'
  | 'violet'
  | 'amber'
  | 'gold'
  | 'rose'
  | 'slate'
  | 'default'

type NormalizedVariant =
  | 'blue'
  | 'emerald'
  | 'violet'
  | 'amber'
  | 'rose'
  | 'slate'

export interface StatCardProps {
  /** Primary metric title */
  title?: string
  /** Backward-compatible alias for title */
  label?: string
  /** The numerical or formatted metric value */
  value: ReactNode
  /** Descriptive caption or context */
  description?: string
  /** Primary Lucide icon placed in the glowing container */
  icon?: ReactNode
  /** Custom CSS properties for the icon box */
  iconStyle?: CSSProperties
  /** Additional container classes */
  className?: string
  /** Inline container style overrides */
  style?: CSSProperties
  /** Color theme variant */
  variant?: StatCardVariant
  /** Optional bottom-right badge element or string */
  badge?: ReactNode
  /** Whether the metric is currently loading / updating */
  loading?: boolean
  /** Whether to render the decorative neon waveform SVG (defaults to true) */
  showWaveform?: boolean
  /** Optional click handler */
  onClick?: () => void
  /** Backward-compatible flag (always uses premium neon design) */
  neon?: boolean
}

function normalizeVariant(variant: StatCardVariant = 'blue'): NormalizedVariant {
  switch (variant) {
    case 'green':
    case 'emerald':
      return 'emerald'
    case 'purple':
    case 'violet':
      return 'violet'
    case 'amber':
    case 'gold':
      return 'amber'
    case 'rose':
      return 'rose'
    case 'slate':
      return 'slate'
    case 'blue':
    case 'default':
    default:
      return 'blue'
  }
}

interface VariantDefinition {
  variantClass: string
  background: string
  borderColor: string
  borderHoverColor: string
  shadow: string
  shadowHover: string
  iconBg: string
  iconBorder: string
  iconShadow: string
  iconShadowHover: string
  waveStroke: string
  waveHighlight: string
  waveGlow: string
  accentGradient: string
  badgeClass: string
}

const variantConfig: Record<NormalizedVariant, VariantDefinition> = {
  blue: {
    variantClass: 'variant-blue',
    background:
      'radial-gradient(circle at 85% 45%, rgba(59, 130, 246, 0.35) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(37, 99, 235, 0.4) 0%, transparent 55%), linear-gradient(135deg, #071530 0%, #0d224d 50%, #071328 100%)',
    borderColor: 'rgba(59, 130, 246, 0.65)',
    borderHoverColor: 'rgba(96, 165, 250, 0.95)',
    shadow:
      '0 0 24px -2px rgba(59, 130, 246, 0.38), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.25), inset 0 0 20px rgba(59, 130, 246, 0.12)',
    shadowHover:
      '0 0 36px 0px rgba(59, 130, 246, 0.6), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.4), inset 0 0 28px rgba(59, 130, 246, 0.2)',
    iconBg: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
    iconBorder: '1.5px solid rgba(147, 197, 253, 0.6)',
    iconShadow: '0 0 20px rgba(59, 130, 246, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.45)',
    iconShadowHover: '0 0 26px rgba(59, 130, 246, 0.85), inset 0 1px 2px rgba(255, 255, 255, 0.6)',
    waveStroke: '#0284c7',
    waveHighlight: '#38bdf8',
    waveGlow: 'rgba(56, 189, 248, 0.55)',
    accentGradient: 'from-blue-500 via-cyan-400 to-indigo-500',
    badgeClass: 'bg-blue-950/90 text-blue-200 border-blue-500/40 shadow-[0_0_10px_rgba(59,130,246,0.25)]',
  },
  emerald: {
    variantClass: 'variant-emerald',
    background:
      'radial-gradient(circle at 85% 45%, rgba(16, 185, 129, 0.35) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(5, 150, 105, 0.4) 0%, transparent 55%), linear-gradient(135deg, #041c16 0%, #08382b 50%, #031812 100%)',
    borderColor: 'rgba(16, 185, 129, 0.65)',
    borderHoverColor: 'rgba(52, 211, 153, 0.95)',
    shadow:
      '0 0 24px -2px rgba(16, 185, 129, 0.38), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.25), inset 0 0 20px rgba(16, 185, 129, 0.12)',
    shadowHover:
      '0 0 36px 0px rgba(16, 185, 129, 0.6), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.4), inset 0 0 28px rgba(16, 185, 129, 0.2)',
    iconBg: 'linear-gradient(135deg, #10b981 0%, #047857 100%)',
    iconBorder: '1.5px solid rgba(110, 231, 183, 0.6)',
    iconShadow: '0 0 20px rgba(16, 185, 129, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.45)',
    iconShadowHover: '0 0 26px rgba(16, 185, 129, 0.85), inset 0 1px 2px rgba(255, 255, 255, 0.6)',
    waveStroke: '#059669',
    waveHighlight: '#34d399',
    waveGlow: 'rgba(52, 211, 153, 0.55)',
    accentGradient: 'from-emerald-500 via-teal-400 to-green-500',
    badgeClass: 'bg-emerald-950/90 text-emerald-200 border-emerald-500/40 shadow-[0_0_10px_rgba(16,185,129,0.25)]',
  },
  violet: {
    variantClass: 'variant-violet',
    background:
      'radial-gradient(circle at 85% 45%, rgba(168, 85, 247, 0.35) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(124, 58, 237, 0.4) 0%, transparent 55%), linear-gradient(135deg, #170b2e 0%, #2b1458 50%, #120724 100%)',
    borderColor: 'rgba(168, 85, 247, 0.65)',
    borderHoverColor: 'rgba(192, 132, 252, 0.95)',
    shadow:
      '0 0 24px -2px rgba(168, 85, 247, 0.38), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.25), inset 0 0 20px rgba(168, 85, 247, 0.12)',
    shadowHover:
      '0 0 36px 0px rgba(168, 85, 247, 0.6), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.4), inset 0 0 28px rgba(168, 85, 247, 0.2)',
    iconBg: 'linear-gradient(135deg, #a855f7 0%, #7c3aed 100%)',
    iconBorder: '1.5px solid rgba(216, 180, 254, 0.6)',
    iconShadow: '0 0 20px rgba(168, 85, 247, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.45)',
    iconShadowHover: '0 0 26px rgba(168, 85, 247, 0.85), inset 0 1px 2px rgba(255, 255, 255, 0.6)',
    waveStroke: '#7c3aed',
    waveHighlight: '#c084fc',
    waveGlow: 'rgba(192, 132, 252, 0.55)',
    accentGradient: 'from-purple-500 via-violet-400 to-indigo-500',
    badgeClass: 'bg-purple-950/90 text-purple-200 border-purple-500/40 shadow-[0_0_10px_rgba(168,85,247,0.25)]',
  },
  amber: {
    variantClass: 'variant-amber',
    background:
      'radial-gradient(circle at 85% 45%, rgba(245, 158, 11, 0.35) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(217, 119, 6, 0.4) 0%, transparent 55%), linear-gradient(135deg, #241403 0%, #3f2307 50%, #1c0e02 100%)',
    borderColor: 'rgba(245, 158, 11, 0.65)',
    borderHoverColor: 'rgba(251, 191, 36, 0.95)',
    shadow:
      '0 0 24px -2px rgba(245, 158, 11, 0.38), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.25), inset 0 0 20px rgba(245, 158, 11, 0.12)',
    shadowHover:
      '0 0 36px 0px rgba(245, 158, 11, 0.6), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.4), inset 0 0 28px rgba(245, 158, 11, 0.2)',
    iconBg: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
    iconBorder: '1.5px solid rgba(253, 230, 138, 0.6)',
    iconShadow: '0 0 20px rgba(245, 158, 11, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.45)',
    iconShadowHover: '0 0 26px rgba(245, 158, 11, 0.85), inset 0 1px 2px rgba(255, 255, 255, 0.6)',
    waveStroke: '#d97706',
    waveHighlight: '#fbbf24',
    waveGlow: 'rgba(251, 191, 36, 0.55)',
    accentGradient: 'from-amber-500 via-orange-400 to-yellow-500',
    badgeClass: 'bg-amber-950/90 text-amber-200 border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.25)]',
  },
  rose: {
    variantClass: 'variant-rose',
    background:
      'radial-gradient(circle at 85% 45%, rgba(244, 63, 94, 0.35) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(225, 29, 72, 0.4) 0%, transparent 55%), linear-gradient(135deg, #290818 0%, #460d29 50%, #1f0512 100%)',
    borderColor: 'rgba(244, 63, 94, 0.65)',
    borderHoverColor: 'rgba(251, 113, 133, 0.95)',
    shadow:
      '0 0 24px -2px rgba(244, 63, 94, 0.38), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.25), inset 0 0 20px rgba(244, 63, 94, 0.12)',
    shadowHover:
      '0 0 36px 0px rgba(244, 63, 94, 0.6), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.4), inset 0 0 28px rgba(244, 63, 94, 0.2)',
    iconBg: 'linear-gradient(135deg, #f43f5e 0%, #be123c 100%)',
    iconBorder: '1.5px solid rgba(254, 205, 211, 0.6)',
    iconShadow: '0 0 20px rgba(244, 63, 94, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.45)',
    iconShadowHover: '0 0 26px rgba(244, 63, 94, 0.85), inset 0 1px 2px rgba(255, 255, 255, 0.6)',
    waveStroke: '#e11d48',
    waveHighlight: '#fb7185',
    waveGlow: 'rgba(244, 63, 94, 0.55)',
    accentGradient: 'from-rose-500 via-pink-400 to-red-500',
    badgeClass: 'bg-rose-950/90 text-rose-200 border-rose-500/40 shadow-[0_0_10px_rgba(244,63,94,0.25)]',
  },
  slate: {
    variantClass: 'variant-slate',
    background:
      'radial-gradient(circle at 85% 45%, rgba(148, 163, 184, 0.28) 0%, transparent 60%), radial-gradient(circle at 15% 25%, rgba(100, 116, 139, 0.32) 0%, transparent 55%), linear-gradient(135deg, #0c1424 0%, #18253d 50%, #0a101d 100%)',
    borderColor: 'rgba(148, 163, 184, 0.55)',
    borderHoverColor: 'rgba(203, 213, 225, 0.85)',
    shadow:
      '0 0 24px -2px rgba(148, 163, 184, 0.28), 0 8px 30px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.2), inset 0 0 20px rgba(148, 163, 184, 0.1)',
    shadowHover:
      '0 0 36px 0px rgba(148, 163, 184, 0.45), 0 12px 36px rgba(0, 0, 0, 0.65), inset 0 1px 2px rgba(255, 255, 255, 0.3), inset 0 0 28px rgba(148, 163, 184, 0.15)',
    iconBg: 'linear-gradient(135deg, #64748b 0%, #334155 100%)',
    iconBorder: '1.5px solid rgba(203, 213, 225, 0.5)',
    iconShadow: '0 0 20px rgba(148, 163, 184, 0.5), inset 0 1px 2px rgba(255, 255, 255, 0.35)',
    iconShadowHover: '0 0 26px rgba(148, 163, 184, 0.7), inset 0 1px 2px rgba(255, 255, 255, 0.5)',
    waveStroke: '#475569',
    waveHighlight: '#94a3b8',
    waveGlow: 'rgba(148, 163, 184, 0.45)',
    accentGradient: 'from-slate-400 via-slate-300 to-slate-500',
    badgeClass: 'bg-slate-800 text-slate-200 border-slate-600 shadow-[0_0_10px_rgba(100,116,139,0.25)]',
  },
}

/**
 * Global Premium Neon Statistics Card.
 * Renders consistent deep-navy tinted backgrounds, illuminated 360-degree neon borders,
 * prominent rounded-square glowing icon containers, high-contrast metrics, and multi-peak neon waveforms.
 */
export default function StatCard({
  title,
  label,
  value,
  description,
  icon,
  iconStyle,
  className = '',
  style,
  variant = 'blue',
  badge,
  loading = false,
  showWaveform = true,
  onClick,
}: StatCardProps) {
  const [tilt, setTilt] = useState({ x: 0, y: 0 })
  const [isHovered, setIsHovered] = useState(false)
  const filterUniqueId = useId().replace(/:/g, '')
  const [reducedMotion, setReducedMotion] = useState(() => {
    if (typeof window === 'undefined') return false
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches
  })

  useEffect(() => {
    if (typeof window === 'undefined') return
    const mq = window.matchMedia('(prefers-reduced-motion: reduce)')
    const listener = (e: MediaQueryListEvent) => setReducedMotion(e.matches)
    mq.addEventListener('change', listener)
    return () => mq.removeEventListener('change', listener)
  }, [])

  const handleMouseMove = (e: React.MouseEvent<HTMLElement>) => {
    if (reducedMotion) return
    const rect = e.currentTarget.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top
    const centerX = rect.width / 2
    const centerY = rect.height / 2
    // Subtle, elegant 3D perspective tilt (-3.5 to +3.5 degrees)
    const rotateX = ((y - centerY) / centerY) * -3.5
    const rotateY = ((x - centerX) / centerX) * 3.5
    setTilt({ x: rotateX, y: rotateY })
  }

  const handleMouseEnter = () => {
    setIsHovered(true)
  }

  const handleMouseLeave = () => {
    setIsHovered(false)
    setTilt({ x: 0, y: 0 })
  }

  const dynamicTransform = reducedMotion
    ? undefined
    : isHovered
      ? `perspective(800px) rotateX(${tilt.x.toFixed(2)}deg) rotateY(${tilt.y.toFixed(2)}deg) translateY(-4px)`
      : 'perspective(800px) rotateX(0deg) rotateY(0deg) translateY(0px)'

  const normalized = normalizeVariant(variant)
  const config = variantConfig[normalized]
  const titleText = title ?? label ?? ''

  const cardStyle: CSSProperties = {
    background: config.background,
    borderColor: isHovered ? config.borderHoverColor : config.borderColor,
    boxShadow: isHovered ? config.shadowHover : config.shadow,
    transform: dynamicTransform,
    ...style,
  }

  const iconBoxStyle: CSSProperties = {
    background: config.iconBg,
    border: config.iconBorder,
    boxShadow: isHovered ? config.iconShadowHover : config.iconShadow,
    ...iconStyle,
  }

  return (
    <article
      className={cn(
        'stat-card group relative flex min-h-[136px] flex-col justify-between overflow-hidden rounded-2xl border p-5 transition-all duration-300',
        config.variantClass,
        onClick ? 'cursor-pointer' : 'cursor-default',
        className,
      )}
      style={cardStyle}
      onMouseMove={handleMouseMove}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onClick={onClick}
    >
      {/* Distinct Glowing Top Border Highlight */}
      <div
        className={cn(
          'absolute inset-x-0 top-0 h-[2.5px] bg-gradient-to-r opacity-95 group-hover:opacity-100 transition-all duration-300 shadow-sm',
          config.accentGradient,
        )}
        aria-hidden="true"
      />

      {/* Prominent Multi-Peak Neon Waveform Activity SVG on the right */}
      {showWaveform && (
        <svg
          className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 h-16 w-36 sm:w-44 opacity-80 group-hover:opacity-100 transition-opacity duration-300 select-none overflow-visible"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 200 60"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <defs>
            <filter id={`wave-glow-${filterUniqueId}`} x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="3.5" result="coloredBlur" />
              <feMerge>
                <feMergeNode in="coloredBlur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
          </defs>
          {/* Ambient blurred glow curve */}
          <path
            d="M 0 38 C 22 38, 32 50, 50 32 C 68 15, 82 44, 100 26 C 118 8, 132 38, 150 18 C 164 4, 178 30, 190 20 C 195 16, 198 22, 200 22"
            fill="none"
            stroke={config.waveStroke}
            strokeWidth="4"
            strokeLinecap="round"
            strokeLinejoin="round"
            opacity="0.55"
            filter={`url(#wave-glow-${filterUniqueId})`}
          />
          {/* Sharp bright neon curve */}
          <path
            d="M 0 38 C 22 38, 32 50, 50 32 C 68 15, 82 44, 100 26 C 118 8, 132 38, 150 18 C 164 4, 178 30, 190 20 C 195 16, 198 22, 200 22"
            fill="none"
            stroke={config.waveHighlight}
            strokeWidth="2.4"
            strokeLinecap="round"
            strokeLinejoin="round"
            opacity="0.95"
          />
        </svg>
      )}

      {/* Main Content Layout: Illuminated Icon Box on Left, Metrics in Middle */}
      <div className="relative z-10 flex items-start gap-4">
        {icon && (
          <div
            className="stat-icon flex size-[52px] min-w-[52px] shrink-0 items-center justify-center rounded-2xl text-white transition-transform duration-300 group-hover:scale-105 [&>svg]:size-6 [&>svg]:stroke-[2.2] [&>svg]:text-white"
            style={iconBoxStyle}
            aria-hidden="true"
          >
            {icon}
          </div>
        )}

        <div className="min-w-0 flex-1">
          <span className="block text-[12px] font-bold uppercase tracking-wider text-slate-300 group-hover:text-white transition-colors">
            {titleText}
          </span>

          <div className="mt-1">
            {loading ? (
              <div className="h-9 w-24 rounded-lg bg-slate-800/80 animate-pulse border border-slate-700/60" />
            ) : (
              <strong className="block text-3xl sm:text-4xl font-black text-white tracking-tight leading-tight">
                {value}
              </strong>
            )}
          </div>

          {description && (
            <p className="mt-1 text-xs font-medium text-slate-300/90 truncate group-hover:text-slate-200 transition-colors">
              {description}
            </p>
          )}
        </div>
      </div>

      {/* Bottom Right Badge (if provided) */}
      {badge && (
        <div className="relative z-10 self-end mt-2">
          {typeof badge === 'string' ? (
            <span
              className={cn(
                'inline-flex items-center text-[11px] font-bold px-2.5 py-0.5 rounded-full border',
                config.badgeClass,
              )}
            >
              {badge}
            </span>
          ) : (
            badge
          )}
        </div>
      )}
    </article>
  )
}

export { StatCard, StatCard as MetricCard }
