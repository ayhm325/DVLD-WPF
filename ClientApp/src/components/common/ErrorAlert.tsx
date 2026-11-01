import type { CSSProperties, ReactNode } from 'react'
import {
  AlertCircle,
  AlertTriangle,
  Info,
  CheckCircle2,
} from 'lucide-react'
import { cn } from '@/lib/utils'

export type AlertVariant = 'error' | 'warning' | 'info' | 'success'

export interface ErrorAlertProps {
  title?: string
  message?: string | null
  children?: ReactNode
  onRetry?: () => void
  retryLabel?: string
  onDismiss?: () => void
  dismissLabel?: string
  variant?: AlertVariant
  className?: string
  style?: CSSProperties
}

const variantStyles: Record<
  AlertVariant,
  {
    container: string
    iconColor: string
    titleColor: string
    textColor: string
    retryButton: string
    dismissButton: string
  }
> = {
  error: {
    container:
      'border-rose-500/40 bg-rose-950/70 text-rose-200 shadow-[0_0_15px_rgba(244,63,94,0.15)]',
    iconColor: 'text-rose-400',
    titleColor: 'text-rose-100',
    textColor: 'text-rose-300/90',
    retryButton:
      'bg-rose-900/50 hover:bg-rose-900/80 border-rose-500/50 text-rose-100 focus-visible:ring-rose-400',
    dismissButton: 'text-rose-300 hover:text-white hover:bg-rose-900/40 focus-visible:ring-rose-400',
  },
  warning: {
    container:
      'border-amber-500/40 bg-amber-950/70 text-amber-200 shadow-[0_0_15px_rgba(245,158,11,0.15)]',
    iconColor: 'text-amber-400',
    titleColor: 'text-amber-100',
    textColor: 'text-amber-300/90',
    retryButton:
      'bg-amber-900/50 hover:bg-amber-900/80 border-amber-500/50 text-amber-100 focus-visible:ring-amber-400',
    dismissButton: 'text-amber-300 hover:text-white hover:bg-amber-900/40 focus-visible:ring-amber-400',
  },
  info: {
    container:
      'border-sky-500/40 bg-sky-950/70 text-sky-200 shadow-[0_0_15px_rgba(14,165,233,0.15)]',
    iconColor: 'text-sky-400',
    titleColor: 'text-sky-100',
    textColor: 'text-sky-300/90',
    retryButton:
      'bg-sky-900/50 hover:bg-sky-900/80 border-sky-500/50 text-sky-100 focus-visible:ring-sky-400',
    dismissButton: 'text-sky-300 hover:text-white hover:bg-sky-900/40 focus-visible:ring-sky-400',
  },
  success: {
    container:
      'border-emerald-500/40 bg-emerald-950/70 text-emerald-200 shadow-[0_0_15px_rgba(16,185,129,0.15)]',
    iconColor: 'text-emerald-400',
    titleColor: 'text-emerald-100',
    textColor: 'text-emerald-300/90',
    retryButton:
      'bg-emerald-900/50 hover:bg-emerald-900/80 border-emerald-500/50 text-emerald-100 focus-visible:ring-emerald-400',
    dismissButton: 'text-emerald-300 hover:text-white hover:bg-emerald-900/40 focus-visible:ring-emerald-400',
  },
}

function getVariantIcon(variant: AlertVariant) {
  switch (variant) {
    case 'warning':
      return <AlertTriangle className="size-5 shrink-0 text-amber-400 mt-0.5 sm:mt-0" aria-hidden="true" />
    case 'info':
      return <Info className="size-5 shrink-0 text-sky-400 mt-0.5 sm:mt-0" aria-hidden="true" />
    case 'success':
      return <CheckCircle2 className="size-5 shrink-0 text-emerald-400 mt-0.5 sm:mt-0" aria-hidden="true" />
    case 'error':
    default:
      return <AlertCircle className="size-5 shrink-0 text-rose-400 mt-0.5 sm:mt-0" aria-hidden="true" />
  }
}

export default function ErrorAlert({
  title,
  message,
  children,
  onRetry,
  retryLabel = 'Retry',
  onDismiss,
  dismissLabel = 'Dismiss',
  variant = 'error',
  className = '',
  style,
}: ErrorAlertProps) {
  if (!message && !children) return null

  const styles = variantStyles[variant] || variantStyles.error

  return (
    <div
      role="alert"
      className={cn(
        'rounded-xl border p-4 text-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3 transition-colors',
        styles.container,
        className,
      )}
      style={style}
    >
      <div className="flex items-start sm:items-center gap-3 min-w-0 flex-1">
        {getVariantIcon(variant)}
        <div className="min-w-0 flex-1 break-words">
          {title && (
            <p className={cn('font-semibold text-sm', styles.titleColor)}>
              {title}
            </p>
          )}
          {message && (
            <span
              className={cn(
                title ? 'text-xs block mt-0.5' : 'font-semibold',
                title ? styles.textColor : styles.titleColor,
              )}
            >
              {message}
            </span>
          )}
          {children}
        </div>
      </div>

      <div className="flex items-center gap-2 shrink-0 self-start sm:self-auto">
        {onRetry && (
          <button
            type="button"
            className={cn(
              'inline-flex items-center justify-center rounded-lg border px-3 py-1.5 text-xs font-semibold shadow-xs transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-offset-[#080f1e]',
              styles.retryButton,
            )}
            onClick={onRetry}
          >
            {retryLabel}
          </button>
        )}
        {onDismiss && (
          <button
            type="button"
            className={cn(
              'inline-flex items-center justify-center rounded-lg px-2.5 py-1 text-xs font-medium transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-offset-[#080f1e]',
              styles.dismissButton,
            )}
            onClick={onDismiss}
          >
            {dismissLabel}
          </button>
        )}
      </div>
    </div>
  )
}
