import { useState } from 'react'
import type { FormEvent } from 'react'
import { login as performLogin } from '../services/authService'
import { useAuth } from '../context/useAuth'
import SecurityEmblem from './login/SecurityEmblem'
import HolographicLicense from './login/HolographicLicense'
import LoginBranding from './login/LoginBranding'
import Login3DScene from './login/scene/Login3DScene'
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
    <div className="login-page">
      {/* ---------- 3D Physically-Based Cinematic Environment (with WebGL fallback) ---------- */}
      <Login3DScene />

      {/* ---------- Main Showcase + Glass Authentication Card ---------- */}
      <main className="login-content-wrapper">
        {/* Left Column: Branding, Holographic Scene & Slogan */}
        <section className="login-left-column" aria-label="DVLD Operations">
          <LoginBranding />

          {/* Holographic Projection in the Sky above the HQ artwork */}
          <div className="login-scene-midsection">
            <HolographicLicense />
          </div>
        </section>

        {/* Right Column: Glassmorphism Authentication Card */}
        <section className="login-card-container" aria-label="Authentication">
          <div className="login-glass-card">
            {/* Top Security Crest Emblem */}
            <SecurityEmblem />

            {/* Header */}
            <header className="login-card-header">
              <h2 className="login-card-title">
                Welcome <span>back</span>
              </h2>
              <p className="login-card-subtitle">
                Enter your credentials to access the DVLD administrative workspace.
              </p>
            </header>

            {/* Logout or session notification banner */}
            {authMessage && (
              <div className="login-info-banner" role="status">
                <Info className="size-4 shrink-0 text-cyan-300" />
                <span>{authMessage}</span>
              </div>
            )}

            {/* Error banner */}
            {error && (
              <div className="login-error-banner" role="alert">
                <AlertCircle className="size-4 shrink-0 text-red-300" />
                <span>{error}</span>
              </div>
            )}

            {/* Login form */}
            <form className="login-form" onSubmit={handleSubmit} noValidate>
              {/* Username field */}
              <div className="login-field">
                <label htmlFor="login-username">Username</label>
                <div className="login-input-shell">
                  <span className="login-input-icon" aria-hidden="true">
                    <UserIcon size={18} />
                  </span>
                  <input
                    id="login-username"
                    name="username"
                    type="text"
                    autoComplete="username"
                    placeholder="Enter your username"
                    value={userName}
                    onChange={(event) => {
                      setUserName(event.target.value)
                      if (error) setError('')
                    }}
                    disabled={loading}
                    required
                    autoFocus
                    className="login-input"
                  />
                </div>
              </div>

              {/* Password field */}
              <div className="login-field">
                <label htmlFor="login-password">Password</label>
                <div className="login-input-shell">
                  <span className="login-input-icon" aria-hidden="true">
                    <LockIcon size={18} />
                  </span>
                  <input
                    id="login-password"
                    name="password"
                    type={showPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    placeholder="Enter your password"
                    value={password}
                    onChange={(event) => {
                      setPassword(event.target.value)
                      if (error) setError('')
                    }}
                    disabled={loading}
                    required
                    className="login-input"
                  />
                  <button
                    className="login-password-toggle"
                    type="button"
                    onClick={() => setShowPassword((prev) => !prev)}
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                    aria-pressed={showPassword}
                    disabled={loading}
                  >
                    {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                  </button>
                </div>
              </div>

              {/* Remember me and Forgot password row */}
              <div className="login-options-row">
                <label className="login-remember-label">
                  <input
                    type="checkbox"
                    checked={rememberMe}
                    onChange={(event) => setRememberMe(event.target.checked)}
                    className="login-checkbox"
                    disabled={loading}
                  />
                  <span>Remember me</span>
                </label>

                <button
                  type="button"
                  className="login-forgot-link"
                  onClick={() => {
                    setError('Password reset requires administrative authorization. Contact DVLD System Admin.')
                  }}
                >
                  Forgot password?
                </button>
              </div>

              {/* Submit button */}
              <button
                className="login-submit-btn"
                type="submit"
                disabled={loading || !userName.trim() || !password}
              >
                {loading ? (
                  <>
                    <span className="login-spinner" aria-hidden="true" />
                    <span>Signing you in...</span>
                  </>
                ) : (
                  <>
                    <span>Sign in</span>
                    <ArrowRight className="size-4 transition-transform group-hover:translate-x-1" />
                  </>
                )}
              </button>

              {/* Alternative Authentication Options */}
              <div className="login-divider">
                <span className="login-divider-line" />
                <span className="login-divider-text">OR CONTINUE WITH</span>
                <span className="login-divider-line" />
              </div>

              <div className="login-social-grid">
                <button
                  type="button"
                  className="login-social-btn"
                  title="Enterprise Microsoft Entra ID Authentication"
                  onClick={() =>
                    setError(
                      'Microsoft SSO is reserved for enterprise active directory accounts.',
                    )
                  }
                >
                  <svg className="size-3.5" viewBox="0 0 21 21" fill="none" aria-hidden="true">
                    <rect x="1" y="1" width="9" height="9" fill="#f25022" />
                    <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
                    <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
                    <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
                  </svg>
                  <span>Microsoft</span>
                </button>

                <button
                  type="button"
                  className="login-social-btn"
                  title="Single Sign-On (SAML 2.0 / OIDC)"
                  onClick={() =>
                    setError(
                      'Single Sign-On is configured for government intranet portal access.',
                    )
                  }
                >
                  <ShieldCheck className="size-3.5 text-cyan-400" />
                  <span>SSO</span>
                </button>

                <button
                  type="button"
                  className="login-social-btn"
                  title="Scan Digital ID via DVLD Jordan Mobile App"
                  onClick={() =>
                    setError(
                      'QR Code sign-in requires the DVLD Jordan Mobile Authenticator app.',
                    )
                  }
                >
                  <QrCode className="size-3.5 text-cyan-400" />
                  <span>QR Code</span>
                </button>
              </div>
            </form>

            {/* Security note */}
            <div className="login-security-indicator">
              <ShieldCheck className="size-4" />
              <span>Protected by secure token-based authentication.</span>
            </div>
          </div>
        </section>
      </main>

      {/* ---------- Page Footer ---------- */}
      <footer className="login-footer">
        <p className="login-footer-copy">
          &copy; 2026 DVLD. Authorized Personnel Only.
        </p>
        <span className="login-footer-tagline">Driving Jordan Forward</span>
      </footer>
    </div>
  )
}
