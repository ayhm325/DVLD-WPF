import { useState } from 'react'
import type { FormEvent } from 'react'
import { login as performLogin } from '../services/authService'
import { useAuth } from '../context/useAuth'
import {
  User as UserIcon,
  Lock as LockIcon,
  Eye as EyeIcon,
  EyeOff as EyeOffIcon,
  ArrowRight,
  ShieldCheck,
  AlertCircle,
  Info,
  QrCode,
  Building2,
  Car,
  FileCheck2,
  CheckCircle2,
} from 'lucide-react'
import './Login.css'

type LoginProps = {
  onLoginSuccess?: (accessToken: string) => void
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const { login: setAuthSession, authMessage, clearAuthMessage } = useAuth()
  const [userName, setUserName] = useState(
    () => localStorage.getItem('dvld_remembered_username') || '',
  )
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [rememberMe, setRememberMe] = useState(
    () => Boolean(localStorage.getItem('dvld_remembered_username')),
  )
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (loading) return

    setError('')
    clearAuthMessage()
    setLoading(true)

    try {
      const trimmedUser = userName.trim()
      const result = await performLogin({ userName: trimmedUser, password })

      if (rememberMe) {
        localStorage.setItem('dvld_remembered_username', trimmedUser)
      } else {
        localStorage.removeItem('dvld_remembered_username')
      }

      setAuthSession(result)
      if (onLoginSuccess) {
        onLoginSuccess(result.accessToken)
      }
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Invalid username or password. Please verify your credentials.',
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="dvld-login-root">
      <div className="dvld-login-container">
        {/* Main 2-Column Split Card */}
        <div className="dvld-login-card">
          {/* =========================================================
              Left Column: DVLD Institutional Branding & Information
             ========================================================= */}
          <aside className="dvld-brand-column" aria-label="DVLD Information">
            <div className="dvld-brand-inner">
              {/* Official Emblem & Authority Header */}
              <div className="dvld-emblem-header">
                <div className="dvld-emblem-badge" aria-hidden="true">
                  <ShieldCheck className="dvld-emblem-icon" size={28} />
                </div>
                <div>
                  <span className="dvld-kingdom-title">The Hashemite Kingdom of Jordan</span>
                  <h1 className="dvld-brand-heading">DVLD Portal</h1>
                  <span className="dvld-arabic-title" dir="rtl">
                    إدارة ترخيص السواقين والمركبات
                  </span>
                </div>
              </div>

              {/* Mission Statement */}
              <div className="dvld-mission-section">
                <div className="dvld-mission-tag">Driving Forward. Managing Smarter.</div>
                <p className="dvld-mission-desc">
                  The centralized administrative portal for driver licensing, vehicle registration,
                  and regulatory compliance services across the Kingdom.
                </p>
              </div>

              {/* Three Institutional Pillars */}
              <div className="dvld-pillars-list">
                <div className="dvld-pillar-item">
                  <div className="dvld-pillar-icon-box">
                    <FileCheck2 size={18} />
                  </div>
                  <div>
                    <h3 className="dvld-pillar-title">Driver Licensing Management</h3>
                    <p className="dvld-pillar-desc">
                      Automated testing, renewals, and international permit issuance.
                    </p>
                  </div>
                </div>

                <div className="dvld-pillar-item">
                  <div className="dvld-pillar-icon-box">
                    <Car size={18} />
                  </div>
                  <div>
                    <h3 className="dvld-pillar-title">Vehicle Records & Ownership</h3>
                    <p className="dvld-pillar-desc">
                      Secure registration, technical inspections, and ownership transfers.
                    </p>
                  </div>
                </div>

                <div className="dvld-pillar-item">
                  <div className="dvld-pillar-icon-box">
                    <Building2 size={18} />
                  </div>
                  <div>
                    <h3 className="dvld-pillar-title">Administrative Authority</h3>
                    <p className="dvld-pillar-desc">
                      Role-based governance under the Public Security Directorate.
                    </p>
                  </div>
                </div>
              </div>

              {/* Institutional Badge Footer */}
              <div className="dvld-brand-footer">
                <CheckCircle2 size={15} className="text-emerald-600 shrink-0" />
                <span>Government Certified Security Infrastructure</span>
              </div>
            </div>
          </aside>

          {/* =========================================================
              Right Column: Authentication Form Card
             ========================================================= */}
          <main className="dvld-auth-column" aria-label="Sign In">
            <div className="dvld-auth-inner">
              {/* Card Header */}
              <header className="dvld-auth-header">
                <h2 className="dvld-auth-title">Welcome back</h2>
                <p className="dvld-auth-subtitle">
                  Please enter your credentials to access your administrative workspace.
                </p>
              </header>

              {/* Session / Logout notification banner */}
              {authMessage && (
                <div className="dvld-alert dvld-alert-info" role="status">
                  <Info className="dvld-alert-icon shrink-0" size={18} />
                  <span>{authMessage}</span>
                </div>
              )}

              {/* Error notification banner */}
              {error && (
                <div className="dvld-alert dvld-alert-error" role="alert">
                  <AlertCircle className="dvld-alert-icon shrink-0" size={18} />
                  <span>{error}</span>
                </div>
              )}

              {/* Login Form */}
              <form className="dvld-auth-form" onSubmit={handleSubmit} noValidate>
                {/* Username Input */}
                <div className="dvld-form-group">
                  <label htmlFor="login-username" className="dvld-form-label">
                    Username
                  </label>
                  <div className="dvld-input-wrapper">
                    <span className="dvld-input-leading-icon" aria-hidden="true">
                      <UserIcon size={18} />
                    </span>
                    <input
                      id="login-username"
                      name="username"
                      type="text"
                      autoComplete="username"
                      placeholder="Enter your administrative username"
                      value={userName}
                      onChange={(event) => {
                        setUserName(event.target.value)
                        if (error) setError('')
                      }}
                      disabled={loading}
                      required
                      autoFocus
                      className="dvld-input"
                    />
                  </div>
                </div>

                {/* Password Input */}
                <div className="dvld-form-group">
                  <div className="dvld-password-label-row">
                    <label htmlFor="login-password" className="dvld-form-label">
                      Password
                    </label>
                  </div>
                  <div className="dvld-input-wrapper">
                    <span className="dvld-input-leading-icon" aria-hidden="true">
                      <LockIcon size={18} />
                    </span>
                    <input
                      id="login-password"
                      name="password"
                      type={showPassword ? 'text' : 'password'}
                      autoComplete="current-password"
                      placeholder="Enter your secure password"
                      value={password}
                      onChange={(event) => {
                        setPassword(event.target.value)
                        if (error) setError('')
                      }}
                      disabled={loading}
                      required
                      className="dvld-input dvld-input-with-trailing"
                    />
                    <button
                      type="button"
                      className="dvld-password-toggle"
                      onClick={() => setShowPassword((prev) => !prev)}
                      aria-label={showPassword ? 'Hide password' : 'Show password'}
                      aria-pressed={showPassword}
                      disabled={loading}
                    >
                      {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                    </button>
                  </div>
                </div>

                {/* Remember Me & Forgot Password Row */}
                <div className="dvld-options-row">
                  <label className="dvld-checkbox-label">
                    <input
                      type="checkbox"
                      checked={rememberMe}
                      onChange={(event) => setRememberMe(event.target.checked)}
                      className="dvld-checkbox"
                      disabled={loading}
                    />
                    <span>Remember me</span>
                  </label>

                  <button
                    type="button"
                    className="dvld-forgot-btn"
                    onClick={() => {
                      setError(
                        'Password reset is managed administratively. Please contact your DVLD Systems Administrator.',
                      )
                    }}
                  >
                    Forgot password?
                  </button>
                </div>

                {/* Primary Submit Button */}
                <button
                  type="submit"
                  className="dvld-submit-btn"
                  disabled={loading || !userName.trim() || !password}
                >
                  {loading ? (
                    <>
                      <span className="dvld-spinner" aria-hidden="true" />
                      <span>Signing you in...</span>
                    </>
                  ) : (
                    <>
                      <span>Sign in to Workspace</span>
                      <ArrowRight size={18} className="dvld-submit-arrow" />
                    </>
                  )}
                </button>

                {/* Auxiliary Login Divider */}
                <div className="dvld-divider" aria-hidden="true">
                  <span className="dvld-divider-line" />
                  <span className="dvld-divider-text">OR SIGN IN WITH</span>
                  <span className="dvld-divider-line" />
                </div>

                {/* Auxiliary Sign-in Buttons */}
                <div className="dvld-auxiliary-grid">
                  <button
                    type="button"
                    className="dvld-aux-btn"
                    title="Enterprise Microsoft Entra ID Authentication"
                    onClick={() =>
                      setError(
                        'Microsoft Entra ID is reserved for authorized ministry intranet networks.',
                      )
                    }
                  >
                    <svg className="size-4 shrink-0" viewBox="0 0 21 21" fill="none" aria-hidden="true">
                      <rect x="1" y="1" width="9" height="9" fill="#f25022" />
                      <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
                      <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
                      <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
                    </svg>
                    <span>Microsoft</span>
                  </button>

                  <button
                    type="button"
                    className="dvld-aux-btn"
                    title="Single Sign-On (SAML 2.0 / OIDC)"
                    onClick={() =>
                      setError(
                        'Single Sign-On is configured for government intranet directory services.',
                      )
                    }
                  >
                    <ShieldCheck size={16} className="text-blue-600 shrink-0" />
                    <span>Gov SSO</span>
                  </button>

                  <button
                    type="button"
                    className="dvld-aux-btn"
                    title="Scan Digital ID via Sanad Mobile App"
                    onClick={() =>
                      setError(
                        'Sanad QR authentication requires the Sanad Jordan Mobile Authenticator app.',
                      )
                    }
                  >
                    <QrCode size={16} className="text-blue-600 shrink-0" />
                    <span>Sanad QR</span>
                  </button>
                </div>
              </form>

              {/* Security Footer Notice */}
              <div className="dvld-security-note">
                <ShieldCheck size={15} className="text-emerald-600 shrink-0" />
                <span>Protected by 256-bit SSL encryption & session token security.</span>
              </div>
            </div>
          </main>
        </div>

        {/* Global Page Footer */}
        <footer className="dvld-page-footer">
          <p className="dvld-footer-copyright">
            &copy; 2026 Driver and Vehicle Licensing Department. All rights reserved.
          </p>
          <p className="dvld-footer-notice">
            Authorized Personnel Only &bull; Hashemite Kingdom of Jordan &bull; Public Security Directorate
          </p>
        </footer>
      </div>
    </div>
  )
}
