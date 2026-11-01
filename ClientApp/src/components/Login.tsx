import { useState } from 'react'
import type { FormEvent } from 'react'
import { login as performLogin } from '../services/authService'
import { useAuth } from '../context/useAuth'
import {
  Layers,
  User as UserIcon,
  Lock as LockIcon,
  Eye as EyeIcon,
  EyeOff as EyeOffIcon,
  ArrowRight,
  ShieldCheck,
  AlertCircle,
  Info,
  Car,
  FileCheck2,
  Building2,
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
      {/* Decorative ambient background glows aligned with Dashboard PageHero */}
      <div className="dvld-ambient-glow dvld-ambient-glow-1" aria-hidden="true" />
      <div className="dvld-ambient-glow dvld-ambient-glow-2" aria-hidden="true" />
      <div className="dvld-ambient-glow dvld-ambient-glow-3" aria-hidden="true" />

      {/* Decorative flowing electric light stream SVG matching Dashboard Hero */}
      <svg
        className="dvld-login-stream-svg"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
        preserveAspectRatio="none"
        viewBox="0 0 1000 300"
      >
        <defs>
          <linearGradient id="login-stream-grad" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#3b82f6" stopOpacity="0" />
            <stop offset="35%" stopColor="#38bdf8" stopOpacity="0.45" />
            <stop offset="70%" stopColor="#60a5fa" stopOpacity="0.65" />
            <stop offset="100%" stopColor="#00f0ff" stopOpacity="0.1" />
          </linearGradient>
          <filter id="login-stream-glow" x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        <path
          d="M 0 180 Q 250 110 500 160 T 1000 110"
          fill="none"
          stroke="url(#login-stream-grad)"
          strokeWidth="2.5"
          filter="url(#login-stream-glow)"
        />
        <path
          d="M 100 220 Q 380 150 650 195 T 1000 155"
          fill="none"
          stroke="url(#login-stream-grad)"
          strokeWidth="1.5"
          opacity="0.35"
        />
      </svg>

      <div className="dvld-login-container">
        {/* Main 2-Column Split Card with Dashboard Dark Surface Aesthetic */}
        <div className="dvld-login-card">
          {/* =========================================================
              Left Column: DVLD Institutional Identity & Services
             ========================================================= */}
          <aside className="dvld-brand-column" aria-label="DVLD Information">
            <div className="dvld-brand-inner">
              {/* Brand Header replicating Sidebar.tsx brand squircle card */}
              <div className="dvld-brand-card">
                <div className="dvld-brand-icon" aria-hidden="true">
                  <Layers className="size-6 text-white stroke-[2.3]" />
                </div>
                <div className="dvld-brand-title-wrap">
                  <span className="dvld-brand-name">DVLD</span>
                  <span className="dvld-brand-tagline">Enterprise Portal</span>
                </div>
              </div>

              {/* Eyebrow with glowing cyan dot matching PageHero */}
              <div className="dvld-eyebrow-row">
                <span className="dvld-eyebrow-dot" aria-hidden="true" />
                <span className="dvld-eyebrow-text">THE HASHEMITE KINGDOM OF JORDAN</span>
              </div>

              {/* Institutional Headline & Mission */}
              <div className="dvld-mission-section">
                <h1 className="dvld-mission-heading">
                  Driving forward.{' '}
                  <span className="dvld-mission-highlight">Managing smarter.</span>
                </h1>
                <p className="dvld-mission-desc">
                  Centralized administrative portal for driver licensing, vehicle registration,
                  and regulatory compliance services across the Kingdom.
                </p>
              </div>

              {/* Institutional Service Highlights in Dark Frosted Surfaces */}
              <div className="dvld-services-list">
                <div className="dvld-service-card">
                  <div className="dvld-service-icon-box" aria-hidden="true">
                    <FileCheck2 size={18} />
                  </div>
                  <div className="dvld-service-content">
                    <h2 className="dvld-service-title">Driver Licensing Management</h2>
                    <p className="dvld-service-desc">
                      Automated testing, renewals, and international permit issuance.
                    </p>
                  </div>
                </div>

                <div className="dvld-service-card">
                  <div className="dvld-service-icon-box" aria-hidden="true">
                    <Car size={18} />
                  </div>
                  <div className="dvld-service-content">
                    <h2 className="dvld-service-title">Vehicle Records & Ownership</h2>
                    <p className="dvld-service-desc">
                      Secure registration, technical inspections, and ownership transfers.
                    </p>
                  </div>
                </div>

                <div className="dvld-service-card">
                  <div className="dvld-service-icon-box" aria-hidden="true">
                    <Building2 size={18} />
                  </div>
                  <div className="dvld-service-content">
                    <h2 className="dvld-service-title">Administrative Authority</h2>
                    <p className="dvld-service-desc">
                      Role-based governance under the Public Security Directorate.
                    </p>
                  </div>
                </div>
              </div>

              {/* Institutional Live Status Footer */}
              <div className="dvld-brand-status">
                <span className="dvld-status-dot" aria-hidden="true" />
                <span>Central Directory Connected &bull; Public Security Directorate</span>
              </div>
            </div>
          </aside>

          {/* =========================================================
              Right Column: Authentication Form Panel
             ========================================================= */}
          <main className="dvld-auth-column" aria-label="Sign In">
            <div className="dvld-auth-inner">
              {/* Form Header */}
              <header className="dvld-auth-header">
                <div className="dvld-auth-badge">
                  <span className="dvld-auth-badge-dot" aria-hidden="true" />
                  <span>ADMINISTRATIVE WORKSPACE</span>
                </div>
                <h2 className="dvld-auth-title">Welcome back</h2>
                <p className="dvld-auth-subtitle">
                  Please sign in with your credentials to access your administrative workspace.
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
              </form>

              {/* Security Footer Notice */}
              <div className="dvld-security-note">
                <ShieldCheck size={16} className="dvld-security-icon shrink-0" />
                <span>Protected by 256-bit encryption & token-based session security.</span>
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
