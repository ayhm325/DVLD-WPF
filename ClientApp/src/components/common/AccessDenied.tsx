import { AlertCircle as AlertCircleIcon, LayoutDashboard as DashboardIcon } from 'lucide-react'

export interface AccessDeniedProps {
  pageTitle?: string
  message?: string
  userRole?: string
  onBackToDashboard?: () => void
}

export default function AccessDenied({
  pageTitle,
  message = 'You do not have permission to access this page.',
  userRole,
  onBackToDashboard,
}: AccessDeniedProps) {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '420px',
        padding: '3rem 1.5rem',
        margin: '2rem auto',
        maxWidth: '560px',
        backgroundColor: 'var(--dvld-surface, #ffffff)',
        border: '1px solid var(--dvld-border, #e2e8f0)',
        borderRadius: '16px',
        boxShadow: 'var(--dvld-shadow-sm, 0 4px 6px -1px rgba(0, 0, 0, 0.05))',
        textAlign: 'center',
      }}
      role="alert"
      aria-live="assertive"
    >
      <div
        style={{
          width: '64px',
          height: '64px',
          borderRadius: '50%',
          backgroundColor: '#fef2f2',
          border: '1px solid #fee2e2',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1.5rem',
          color: '#ef4444',
        }}
      >
        <AlertCircleIcon size={32} strokeWidth={2.2} />
      </div>

      <h2
        style={{
          fontSize: '1.5rem',
          fontWeight: 700,
          color: 'var(--dvld-text, #0f172a)',
          margin: '0 0 0.5rem 0',
          letterSpacing: '-0.02em',
        }}
      >
        Access Denied
      </h2>

      <p
        style={{
          fontSize: '1rem',
          color: 'var(--dvld-text-muted, #64748b)',
          margin: '0 0 1rem 0',
          lineHeight: 1.5,
          maxWidth: '440px',
        }}
      >
        {message}
      </p>

      {(pageTitle || userRole) && (
        <div
          style={{
            display: 'inline-flex',
            gap: '0.75rem',
            alignItems: 'center',
            padding: '0.4rem 0.85rem',
            backgroundColor: 'var(--dvld-surface-soft, #f8fafc)',
            border: '1px solid var(--dvld-border, #e2e8f0)',
            borderRadius: '9999px',
            fontSize: '0.8125rem',
            color: 'var(--dvld-text-muted, #64748b)',
            marginBottom: '1.75rem',
          }}
        >
          {pageTitle && <span>Resource: <strong>{pageTitle}</strong></span>}
          {pageTitle && userRole && <span>•</span>}
          {userRole && <span>Active Role: <strong>{userRole}</strong></span>}
        </div>
      )}

      {onBackToDashboard && (
        <button
          type="button"
          onClick={onBackToDashboard}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.625rem 1.25rem',
            backgroundColor: 'var(--dvld-primary, #2563eb)',
            color: '#ffffff',
            border: 'none',
            borderRadius: '8px',
            fontWeight: 600,
            fontSize: '0.875rem',
            cursor: 'pointer',
            transition: 'background-color 0.15s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#1d4ed8'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'var(--dvld-primary, #2563eb)'
          }}
        >
          <DashboardIcon size={16} />
          <span>Return to Dashboard</span>
        </button>
      )}
    </div>
  )
}
