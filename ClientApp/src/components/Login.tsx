
import { useState } from 'react'
import { login } from '../services/authService'

type LoginProps = {
  onLoginSuccess: (accessToken: string) => void
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setLoading(true)

    try {
      const result = await login({ userName, password })
      onLoginSuccess(result.accessToken)
    } catch {
      setError('اسم المستخدم أو كلمة المرور غير صحيحة.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-brand">D</div>

        <p className="eyebrow">DVLD MANAGEMENT SYSTEM</p>
        <h1>Welcome back</h1>
        <p className="login-description">
          Sign in to access your dashboard.
        </p>

        <label htmlFor="userName">Username</label>
        <input
          id="userName"
          type="text"
          autoComplete="username"
          value={userName}
          onChange={(event) => setUserName(event.target.value)}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />

        {error && <p className="login-error">{error}</p>}

        <button
          type="submit"
          className="primary-button"
          disabled={loading}
        >
          {loading ? 'Signing in...' : 'Sign in'}
        </button>
      </form>
    </main>
  )
}
