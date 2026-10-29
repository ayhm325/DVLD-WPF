import type { CSSProperties, ReactNode } from 'react'

interface StatCardProps {
  label: string
  value: ReactNode
  description?: string
  icon: ReactNode
  iconStyle?: CSSProperties
  className?: string
  style?: CSSProperties
}

export default function StatCard({
  label,
  value,
  description,
  icon,
  iconStyle,
  className = '',
  style,
}: StatCardProps) {
  return (
    <article className={`stat-card ${className}`.trim()} style={style}>
      <div className="stat-card-top">
        <span>{label}</span>
        <span className="stat-icon" aria-hidden="true" style={iconStyle}>
          {icon}
        </span>
      </div>
      <strong>{value}</strong>
      {description && <p>{description}</p>}
    </article>
  )
}
