import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface StatsGridProps {
  columns?: 1 | 2 | 3 | 4 | 5
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
  return (
    <div
      className={cn('stats-grid grid min-w-0 gap-4 sm:gap-5 mb-6', className)}
      data-columns={columns}
      style={style}
    >
      {children}
    </div>
  )
}
