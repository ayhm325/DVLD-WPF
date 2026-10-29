import { useState } from 'react'
import type { FormEvent } from 'react'
import { login as performLogin } from '../services/authService'
import { useAuth } from '../context/useAuth'
import './Login.css'

type LoginProps = {
  onLoginSuccess?: (accessToken: string) => void
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
      <div className="login-background-orb login-orb-one" aria-hidden="true" />
      <div className="login-background-orb login-orb-two" aria-hidden="true" />

      <div className="login-layout">
        <section className="login-showcase">
          <div className="login-showcase-content">
            <div className="login-brand-lockup">
              <div className="login-brand-mark" aria-hidden="true">
                <span>D</span>
                <i />
              </div>

              <div className="login-brand-copy">
                <strong>DVLD</strong>
                <span>MANAGEMENT SYSTEM</span>
              </div>
            </div>

            <div className="login-hero">
              <span className="login-overline">
                <span className="login-overline-dot" />
                YOUR OPERATIONS, SIMPLIFIED
              </span>

              <h1>
                Driving forward.
                <br />
                <span>Managing smarter.</span>
              </h1>

              <p>
                A centralized workspace for driving licenses,
                applications, people, and licensing operations.
              </p>
            </div>

            <div className="login-visual" aria-hidden="true">
              <div className="login-visual-grid" />

              <div className="login-orbit login-orbit-outer" />
              <div className="login-orbit login-orbit-inner" />

              <div className="login-license-card">
                <div className="login-license-top">
                  <div className="login-license-symbol">D</div>
                  <div>
                    <span>JORDAN</span>
                    <strong>DRIVING LICENSE</strong>
                  </div>
                  <span className="login-license-chip">DVLD</span>
                </div>

                <div className="login-license-body">
                  <div className="login-license-avatar">
                    <svg viewBox="0 0 48 48" fill="none">
                      <circle cx="24" cy="17" r="9" fill="currentColor" />
                      <path
                        d="M7 43c1-10 7-15 17-15s16 5 17 15"
                        fill="currentColor"
                      />
                    </svg>
                  </div>

                  <div className="login-license-fields">
                    <div className="login-license-row">
                      <span className="line-sm" />
                      <span className="line-lg" />
                    </div>
                    <div className="login-license-row">
                      <span className="line-md" />
                      <span className="line-sm" />
                    </div>
                    <div className="login-license-row">
                      <span className="line-lg" />
                    </div>
                  </div>
                </div>

                <div className="login-license-footer">
                  <span>VALID • OFFICIAL DOCUMENT</span>
                  <div className="login-license-barcode">
                    <i /><i /><i /><i /><i /><i /><i /><i /><i /><i /><i />
                  </div>
                </div>
              </div>

              <div className="login-stat-pill pill-one">
                <span className="pill-dot green" />
                <div>
                  <strong>All Systems Online</strong>
                  <span>API v1 Connected</span>
                </div>
              </div>

              <div className="login-stat-pill pill-two">
                <span className="pill-dot amber" />
                <div>
                  <strong>Secure Auth</strong>
                  <span>JWT Protected</span>
                </div>
              </div>
            </div>

            <footer className="login-showcase-footer">
              <div className="feature-chip">
                <span>✓</span> Role-based Access
              </div>
              <div className="feature-chip">
                <span>✓</span> Audit Logging
              </div>
              <div className="feature-chip">
                <span>✓</span> Encrypted Transport
              </div>
            </footer>
          </div>
        </section>

        <section className="login-form-pane">
          <div className="login-form-container">
            <header className="login-form-header">
              <span className="login-welcome-tag">PORTAL ACCESS</span>
              <h2>Welcome back</h2>
              <p>Sign in to access your administrative workspace.</p>
            </header>

            {authMessage && (
              <div className="login-info-banner" role="status">
                <span className="login-info-icon">ℹ</span>
                <span>{authMessage}</span>
              </div>
            )}

            <form className="login-form" onSubmit={handleSubmit} noValidate>
              <div className="login-field">
                <label htmlFor="login-username">Username</label>
                <div className="login-input-shell">
                  <span className="login-input-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none">
                      <circle cx="12" cy="8" r="4" />
                      <path d="M4 20c0-4 4-6 8-6s8 2 8 6" />
                    </svg>
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
                  />
                </div>
              </div>

              <div className="login-field">
                <div className="login-field-row">
                  <label htmlFor="login-password">Password</label>
                </div>
                <div className="login-input-shell">
                  <span className="login-input-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none">
                      <rect x="5" y="11" width="14" height="10" rx="2" />
                      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
                    </svg>
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
                    {showPassword ? 'HIDE' : 'SHOW'}
                  </button>
                </div>
              </div>

              {error && (
                <div className="login-error" role="alert">
                  <span className="login-error-icon">!</span>
                  <span>{error}</span>
                </div>
              )}

              <button
                className="login-submit"
                type="submit"
                disabled={loading || !userName.trim() || !password}
              >
                <span>{loading ? 'Signing you in...' : 'Sign in'}</span>

                {!loading && (
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M5 12h14M13 6l6 6-6 6" />
                  </svg>
                )}

                {loading && <span className="login-spinner" />}
              </button>

              <div className="login-security-note">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="5" y="10" width="14" height="11" rx="2" />
                  <path d="M8 10V7a4 4 0 0 1 8 0v3" />
                </svg>
                <span>Your session is protected by secure authentication.</span>
              </div>
            </form>

            <div className="login-divider">
              <span />
              <i>DVLD</i>
              <span />
            </div>

            <footer className="login-form-footer">
              <span>AUTHORIZED PERSONNEL ONLY</span>
              <span>© {new Date().getFullYear()} DVLD</span>
            </footer>
          </div>
        </section>
      </div>
    </main>
  )
}
