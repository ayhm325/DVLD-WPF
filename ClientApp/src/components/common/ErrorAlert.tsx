import type { CSSProperties, ReactNode } from 'react'

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
    <div className={`login-error ${className}`.trim()} style={style} role="alert">
      {message && <span>{message}</span>}
      {children}
      {onRetry && (
        <button type="button" className="primary-button" onClick={onRetry}>
          {retryLabel}
        </button>
      )}
    </div>
  )
}
