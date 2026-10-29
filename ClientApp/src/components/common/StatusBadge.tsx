type StatusVariant =
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'neutral'

type StatusBadgeProps = {
  status: string | boolean
  label?: string
  variant?: StatusVariant
}

export default function StatusBadge({
  status,
  label,
  variant,
}: StatusBadgeProps) {
  let computedVariant: StatusVariant = variant ?? 'neutral'
  let displayLabel = label

  if (typeof status === 'boolean') {
    computedVariant = status ? 'success' : 'danger'
    displayLabel = displayLabel ?? (status ? 'Active' : 'Inactive')
  } else {
    const normalized = String(status).trim().toLowerCase()
    displayLabel = displayLabel ?? String(status)

    if (!variant) {
      if (['active', 'completed', 'passed', 'yes', 'approved'].includes(normalized)) {
        computedVariant = 'success'
      } else if (['new', 'pending', 'in progress'].includes(normalized)) {
        computedVariant = 'warning'
      } else if (['inactive', 'cancelled', 'failed', 'no', 'locked', 'detained'].includes(normalized)) {
        computedVariant = 'danger'
      } else if (['info', 'theory', 'written', 'practical'].includes(normalized)) {
        computedVariant = 'info'
      }
    }
  }

  return (
    <span className={`status-badge status-badge-${computedVariant}`}>
      <span className="status-badge-dot" />
      {displayLabel}
    </span>
  )
}
