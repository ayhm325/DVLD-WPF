import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

type LoadingSpinnerProps = {
  message?: string
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export default function LoadingSpinner({
  message = 'Loading...',
  size = 'md',
  className = '',
}: LoadingSpinnerProps) {
  const sizeMap = {
    sm: 'size-4',
    md: 'size-6',
    lg: 'size-8',
  }

  return (
    <div
      className={cn(
        'loading-container flex items-center justify-center gap-2.5 p-8 text-slate-500',
        className,
      )}
      role="status"
    >
      <Loader2 className={cn('animate-spin text-blue-600', sizeMap[size])} aria-hidden="true" />
      {message && <span className="text-xs font-semibold text-slate-600">{message}</span>}
    </div>
  )
}
