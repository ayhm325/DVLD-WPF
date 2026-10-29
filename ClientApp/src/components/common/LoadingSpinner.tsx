type LoadingSpinnerProps = {
  message?: string
  size?: 'sm' | 'md' | 'lg'
}

export default function LoadingSpinner({
  message = 'Loading...',
  size = 'md',
}: LoadingSpinnerProps) {
  return (
    <div className={`loading-container loading-${size}`} role="status">
      <div className="spinner" aria-hidden="true" />
      {message && <span className="loading-text">{message}</span>}
    </div>
  )
}
