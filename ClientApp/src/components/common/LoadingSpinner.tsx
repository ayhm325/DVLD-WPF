import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

export type LoadingSpinnerProps = {
  message?: string
  size?: 'sm' | 'md' | 'lg'
  layout?: 'row' | 'col'
  className?: string
}

export default function LoadingSpinner({
  message = 'Loading...',
  size = 'md',
  layout = 'row',
  className = '',
}: LoadingSpinnerProps) {
  const sizeMap = {
    sm: 'size-4',
    md: 'size-5 sm:size-6',
    lg: 'size-7 sm:size-8',
  }

  const textSizeMap = {
    sm: 'text-xs',
    md: 'text-xs sm:text-sm',
    lg: 'text-sm font-medium',
  }

  return (
    <div
      className={cn(
        'loading-container flex min-w-0 items-center justify-center p-6 text-muted-foreground transition-opacity',
        layout === 'col' ? 'flex-col gap-3 text-center' : 'flex-row gap-2.5',
        className,
      )}
      role="status"
      aria-live="polite"
      aria-atomic="true"
      aria-busy="true"
      aria-label={message || 'Loading'}
    >
      <Loader2
        className={cn(
          'shrink-0 animate-spin text-primary motion-reduce:animate-none',
          sizeMap[size],
        )}
        aria-hidden="true"
      />
      {message && (
        <span className={cn('min-w-0 break-words tracking-wide text-foreground', textSizeMap[size])}>
          {message}
        </span>
      )}
      {!message && <span className="sr-only">Loading</span>}
    </div>
  )
}
