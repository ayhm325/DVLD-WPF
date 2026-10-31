import * as React from 'react'
import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface SpinnerProps extends React.HTMLAttributes<HTMLDivElement> {
  size?: 'sm' | 'default' | 'lg'
  label?: string
}

export function Spinner({
  size = 'default',
  label,
  className,
  ...props
}: SpinnerProps) {
  const sizeMap = {
    sm: 'size-4',
    default: 'size-5',
    lg: 'size-8',
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn('inline-flex items-center gap-2 text-muted-foreground', className)}
      {...props}
    >
      <Loader2 className={cn('animate-spin text-primary', sizeMap[size])} />
      {label && <span className="text-xs font-medium">{label}</span>}
      <span className="sr-only">{label || 'Loading...'}</span>
    </div>
  )
}
