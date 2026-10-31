import type { CSSProperties, ReactNode } from 'react'
import { AlertCircle } from 'lucide-react'

interface ErrorAlertProps {
  message?: string | null
  children?: ReactNode
  onRetry?: () => void
  retryLabel?: string
  className?: string
  style?: CSSProperties
}

export default function ErrorAlert({
  message,
  children,
  onRetry,
  retryLabel = 'Retry',
  className = '',
  style,
}: ErrorAlertProps) {
  if (!message && !children) return null

  return (
    <div
      role="alert"
      className={`rounded-xl border border-red-200 bg-red-50/80 p-4 text-sm text-red-800 shadow-2xs flex flex-col sm:flex-row sm:items-center justify-between gap-3 ${className}`.trim()}
      style={style}
    >
      <div className="flex items-start sm:items-center gap-3">
        <AlertCircle className="size-5 shrink-0 text-red-600 mt-0.5 sm:mt-0" />
        <div>
          {message && <span className="font-semibold text-red-900">{message}</span>}
          {children}
        </div>
      </div>
      {onRetry && (
        <button
          type="button"
          className="inline-flex items-center justify-center rounded-lg bg-white border border-red-300 px-3 py-1.5 text-xs font-semibold text-red-700 shadow-2xs hover:bg-red-50 transition-colors cursor-pointer shrink-0 self-start sm:self-auto"
          onClick={onRetry}
        >
          {retryLabel}
        </button>
      )}
    </div>
  )
}
