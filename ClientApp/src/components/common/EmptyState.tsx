import type { CSSProperties, ReactNode } from 'react'

interface EmptyStateProps {
  message?: string
  description?: string
  icon?: ReactNode
  children?: ReactNode
  className?: string
  style?: CSSProperties
}

export default function EmptyState({
  message,
  description,
  icon,
  children,
  className = '',
  style,
}: EmptyStateProps) {
  return (
    <div className={`empty-state ${className}`.trim()} style={style}>
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
        {icon}
        {message && <span>{message}</span>}
        {description && <p style={{ margin: 0, fontSize: 13, color: 'var(--dvld-faint)' }}>{description}</p>}
        {children}
      </div>
    </div>
  )
}
