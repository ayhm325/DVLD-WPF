import type { ReactNode } from 'react'
import './StatusBadge.css'

export type StatusVariant =
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'neutral'

export type StatusBadgeProps = {
  /**
   * Explicit semantic status variant:
   * - 'success': positive/healthy states (e.g. Active, Completed, Passed)
   * - 'warning': attention/in-progress states (e.g. New, Pending)
   * - 'danger': negative/terminated states (e.g. Inactive, Cancelled, Failed)
   * - 'info': informational states
   * - 'neutral': default fallback for unknown/unclassified states
   */
  variant?: StatusVariant
  /** Status text or element to display. Preferred over children/status. */
  label?: ReactNode
  /** Child content to display inside badge */
  children?: ReactNode
  /** Raw status value (alternate to label/children) */
  status?: ReactNode
  /** Whether to render the status indicator dot. Defaults to true. */
  showDot?: boolean
  /** Additional custom class names */
  className?: string
  /** Accessible tooltip / title attribute */
  title?: string
}

const VALID_VARIANTS: ReadonlySet<string> = new Set([
  'success',
  'warning',
  'danger',
  'info',
  'neutral',
])

/**
 * Standardized semantic status badge component for state and lifecycle indicators.
 * Requires an explicit semantic variant or defaults safely to 'neutral'.
 * No ambiguous automatic string or boolean inference is performed.
 */
export default function StatusBadge({
  variant = 'neutral',
  label,
  children,
  status,
  showDot = true,
  className = '',
  title,
}: StatusBadgeProps) {
  const resolvedVariant: StatusVariant = VALID_VARIANTS.has(variant)
    ? variant
    : 'neutral'

  const displayContent = label ?? children ?? status ?? null

  const classes = [
    'status-badge',
    `status-badge-${resolvedVariant}`,
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <span className={classes} title={title}>
      {showDot && <span className="status-badge-dot" aria-hidden="true" />}
      <span>{displayContent}</span>
    </span>
  )
}
