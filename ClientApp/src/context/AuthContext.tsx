import {
  useCallback,
  useEffect,
  useState,
} from 'react'
import type { ReactNode } from 'react'
import type { AuthSession, AuthUser, LoginResponse } from '../types/Auth'
import { setUnauthorizedHandler } from '../services/apiClient'
import { AuthContext, type AuthContextValue } from './authContextInstance'

const AUTH_STORAGE_KEY = 'dvld_auth_session'

function loadStoredSession(): AuthSession | null {
  try {
    const raw = sessionStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) return null

    const session = JSON.parse(raw) as AuthSession
    if (!session || !session.accessToken || !session.expiresAtUtc) {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
      return null
    }

    const expiresAt = new Date(session.expiresAtUtc).getTime()
    if (Number.isFinite(expiresAt) && expiresAt <= Date.now()) {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
      return null
    }

    return session
  } catch {
    try {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
    } catch {
      // Ignore storage access errors
    }
    return null
  }
}

function persistSession(session: AuthSession | null): void {
  try {
    if (session) {
      sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
    } else {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
    }
  } catch {
    // Ignore storage quota or access errors in restricted environments
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => loadStoredSession())
  const [isLoading] = useState(false)
  const [authMessage, setAuthMessage] = useState<string | null>(null)

  const logout = useCallback((reason?: string) => {
    persistSession(null)
    setSession(null)
    if (reason) {
      setAuthMessage(reason)
    } else {
      setAuthMessage('You have successfully signed out.')
    }
  }, [])

  // Hook into API client 401 unauthorized responses
  useEffect(() => {
    setUnauthorizedHandler((message: string) => {
      logout(message || 'Your session has expired. Please log in again.')
    })

    return () => {
      setUnauthorizedHandler(null)
    }
  }, [logout])

  const login = useCallback((response: LoginResponse) => {
    const user: AuthUser = {
      userId: response.userId,
      userName: response.userName,
      personId: response.personId,
      fullName: response.fullName,
      role: response.role,
    }

    const newSession: AuthSession = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user,
    }

    persistSession(newSession)
    setSession(newSession)
    setAuthMessage(null)
  }, [])

  const clearAuthMessage = useCallback(() => {
    setAuthMessage(null)
  }, [])

  const value: AuthContextValue = {
    accessToken: session?.accessToken ?? null,
    user: session?.user ?? null,
    isAuthenticated: session !== null && session.accessToken.length > 0,
    isLoading,
    authMessage,
    login,
    logout,
    clearAuthMessage,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
