import type { CSSProperties, ReactNode } from 'react'

interface StatsGridProps {
  columns?: 2 | 3 | 4 | number
  children: ReactNode
  className?: string
  style?: CSSProperties
}

export default function StatsGrid({
  columns = 4,
  children,
  className = '',
  style,
}: StatsGridProps) {
  const inlineStyle: CSSProperties = {
    ...(columns ? { gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` } : {}),
    ...style,
  }

  return (
    <div className={`stats-grid ${className}`.trim()} style={inlineStyle}>
      {children}
    </div>
  )
}
