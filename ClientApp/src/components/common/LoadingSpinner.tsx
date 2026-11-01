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
    md: 'size-6',
    lg: 'size-8',
  }

  const textSizeMap = {
    sm: 'text-xs',
    md: 'text-xs sm:text-sm',
    lg: 'text-sm font-medium',
  }

  return (
    <div
      className={cn(
        'loading-container flex items-center justify-center p-6 text-slate-300 transition-opacity',
        layout === 'col' ? 'flex-col gap-3 text-center' : 'flex-row gap-2.5',
        className,
      )}
      role="status"
      aria-live="polite"
    >
      <Loader2
        className={cn(
          'animate-spin text-cyan-400 drop-shadow-[0_0_10px_rgba(0,229,255,0.7)] shrink-0',
          sizeMap[size],
        )}
        aria-hidden="true"
      />
      {message && (
        <span className={cn('text-slate-300 tracking-wide', textSizeMap[size])}>
          {message}
        </span>
      )}
    </div>
  )
}
