
import { useState } from 'react'
import type { FormEvent } from 'react'
import { login } from '../services/authService'
import './Login.css'

type LoginProps = {
  onLoginSuccess: (accessToken: string) => void
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (loading) return

    setError('')
    setLoading(true)

    try {
      const result = await login({ userName: userName.trim(), password })
      onLoginSuccess(result.accessToken)
    } catch {
      setError('اسم المستخدم أو كلمة المرور غير صحيحة.')
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

                  <div className="login-license-lines">
                    <span />
                    <span />
                    <span />
                  </div>
                </div>

                <div className="login-license-footer">
                  <span>LICENSE MANAGEMENT</span>
                  <div className="login-license-barcode">
                    {Array.from({ length: 22 }, (_, index) => (
                      <i key={index} />
                    ))}
                  </div>
                </div>
              </div>

              <div className="login-floating-badge">
                <span className="login-badge-icon">✓</span>
                <span>
                  <strong>One workspace</strong>
                  <small>Everything organized</small>
                </span>
              </div>

              <div className="login-visual-caption">
                <span>01 /</span>
                <span>LICENSING OPERATIONS</span>
                <span className="login-caption-line" />
              </div>
            </div>
          </div>

          <footer className="login-showcase-footer">
            <span>DVLD · DIGITAL LICENSING</span>
            <span>SECURE ADMINISTRATION</span>
          </footer>
        </section>

        <section className="login-form-panel">
          <div className="login-form-inner">
            <div className="login-mobile-brand">
              <div className="login-brand-mark" aria-hidden="true">
                <span>D</span>
                <i />
              </div>
              <div className="login-brand-copy">
                <strong>DVLD</strong>
                <span>MANAGEMENT SYSTEM</span>
              </div>
            </div>

            <div className="login-form-heading">
              <span className="login-form-kicker">WELCOME BACK</span>
              <h2>Sign in to your account</h2>
              <p>
                Enter your credentials to access the management dashboard.
              </p>
            </div>

            <form className="login-form" onSubmit={handleSubmit}>
              <div className="login-field">
                <label htmlFor="login-username">Username</label>

                <div className="login-input-wrap">
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <circle cx="12" cy="8" r="4" />
                    <path d="M4 21v-2a8 8 0 0 1 16 0v2" />
                  </svg>

                  <input
                    id="login-username"
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
                <div className="login-label-row">
                  <label htmlFor="login-password">Password</label>
                  <span>SECURE ACCESS</span>
                </div>

                <div className="login-input-wrap">
                  <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <rect x="4" y="10" width="16" height="11" rx="2" />
                    <path d="M8 10V7a4 4 0 0 1 8 0v3" />
                    <path d="M12 14v3" />
                  </svg>

                  <input
                    id="login-password"
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
