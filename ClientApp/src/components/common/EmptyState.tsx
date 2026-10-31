import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'

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
    <div
      className={cn(
        'empty-state flex flex-col items-center justify-center p-8 sm:p-12 text-center text-slate-500',
        className,
      )}
      style={style}
    >
      {icon && <div className="mb-3 text-slate-400 [&_svg]:size-10" aria-hidden="true">{icon}</div>}
      {message && <strong className="text-sm font-semibold text-slate-700">{message}</strong>}
      {description && <p className="text-xs text-slate-500 mt-1 max-w-sm leading-relaxed">{description}</p>}
      {children}
    </div>
  )
}
