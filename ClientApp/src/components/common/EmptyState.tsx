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
        'empty-state flex min-w-0 flex-col items-center justify-center p-6 text-center text-muted-foreground sm:p-8',
        className,
      )}
      style={style}
      role="status"
      aria-live="polite"
      aria-atomic="true"
    >
      {icon && <div className="mb-3 text-muted-foreground [&_svg]:size-10" aria-hidden="true">{icon}</div>}
      {message && <strong className="max-w-full break-words text-sm font-semibold text-foreground">{message}</strong>}
      {description && <p className="mt-1 w-full max-w-sm break-words text-xs leading-relaxed text-muted-foreground">{description}</p>}
      {children}
    </div>
  )
}
