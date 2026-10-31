import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'

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
  const columnClasses = {
    2: 'grid-cols-1 sm:grid-cols-2',
    3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4',
  }[columns] || 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4'

  return (
    <div
      className={cn('stats-grid grid gap-4 sm:gap-5 mb-6', columnClasses, className)}
      style={style}
    >
      {children}
    </div>
  )
}
