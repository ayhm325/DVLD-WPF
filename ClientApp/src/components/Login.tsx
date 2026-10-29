import { useState } from 'react'
import type { FormEvent } from 'react'
import { login as performLogin } from '../services/authService'
import { useAuth } from '../context/useAuth'
import LoginScene from './LoginScene'
import {
  CheckIcon,
  InfoIcon,
  AlertCircleIcon,
  UserIcon,
  LockIcon,
  EyeIcon,
  EyeOffIcon,
  ShieldCheckIcon,
} from './common/Icons'
import './Login.css'

type LoginProps = {
  onLoginSuccess?: (accessToken: string) => void
}

function BrandMark() {
  return (
    <span className="login-brand-mark" aria-hidden="true">
      <span>D</span>
    </span>
  )
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const { login: setAuthSession, authMessage, clearAuthMessage } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (loading) return

    setError('')
    clearAuthMessage()
    setLoading(true)

    try {
      const result = await performLogin({ userName: userName.trim(), password })
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
    <main className="login-page">
      <div className="login-layout">
        {/* ---------- Brand / automotive showcase ---------- */}
        <section className="login-showcase" aria-label="DVLD overview">
          <header className="login-brand">
            <BrandMark />
            <div className="login-brand-copy">
              <strong>DVLD</strong>
              <span>Driving &amp; Vehicle Licensing Department</span>
            </div>
          </header>

          <div className="login-hero">
            <span className="login-overline">
              <span className="login-overline-dot" aria-hidden="true" />
              Licensing operations platform
            </span>
            <h1>
              Driving forward.
              <span>Managing smarter.</span>
            </h1>
            <p>
              One secure workspace for people, drivers, licenses,
              and licensing applications.
            </p>
          </div>

          <LoginScene />

          <footer className="login-showcase-footer">
            <ul className="login-feature-list">
              <li>
                <CheckIcon size={13} strokeWidth={2.5} /> Role-based access
              </li>
              <li>
                <CheckIcon size={13} strokeWidth={2.5} /> Audit logging
              </li>
              <li>
                <CheckIcon size={13} strokeWidth={2.5} /> Encrypted transport
              </li>
            </ul>
          </footer>
        </section>

        {/* ---------- Sign-in form ---------- */}
        <section className="login-form-pane" aria-labelledby="login-title">
          <div className="login-form-container">
            <div className="login-mobile-brand">
              <BrandMark />
              <div className="login-brand-copy">
                <strong>DVLD</strong>
                <span>Management System</span>
              </div>
            </div>

            <header className="login-form-header">
              <span className="login-kicker">Sign in</span>
              <h2 id="login-title">Welcome back</h2>
              <p>Enter your credentials to access the DVLD administrative workspace.</p>
            </header>

            {authMessage && (
              <div className="login-info-banner" role="status">
                <InfoIcon size={18} />
                <span>{authMessage}</span>
              </div>
            )}

            <form className="login-form" onSubmit={handleSubmit} noValidate>
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
                    onChange={(event) => setUserName(event.target.value)}
                    disabled={loading}
                    required
                    autoFocus
                    aria-invalid={error ? true : undefined}
                  />
                </div>
              </div>

              <div className="login-field">
                <label htmlFor="login-password">Password</label>
                <div className="login-input-shell has-toggle">
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
                    onChange={(event) => setPassword(event.target.value)}
                    disabled={loading}
                    required
                    aria-invalid={error ? true : undefined}
                  />

                  <button
                    className="login-password-toggle"
                    type="button"
                    onClick={() => setShowPassword((value) => !value)}
                    aria-label={
                      showPassword ? 'Hide password' : 'Show password'
                    }
                    aria-pressed={showPassword}
                    disabled={loading}
                  >
                    {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                  </button>
                </div>
              </div>

              {error && (
                <div className="login-error login-form-error" role="alert">
                  <AlertCircleIcon size={18} />
                  <span>{error}</span>
                </div>
              )}

              <button
                className="login-submit"
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
                    <svg
                      className="login-submit-arrow"
                      viewBox="0 0 24 24"
                      fill="none"
                      aria-hidden="true"
                    >
                      <path d="M5 12h14M13 6l6 6-6 6" />
                    </svg>
                  </>
                )}
              </button>
            </form>

            <p className="login-security-note">
              <ShieldCheckIcon size={16} />
              <span>Protected by secure token-based authentication.</span>
            </p>

            <footer className="login-form-footer">
              <span>Authorized personnel only</span>
              <span>© {new Date().getFullYear()} DVLD</span>
            </footer>
          </div>
        </section>
      </div>
    </main>
  )
}
