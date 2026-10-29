import {
  useCallback,
  useEffect,
  useState,
} from 'react'
import type { ReactNode } from 'react'
import type { AuthSession, AuthUser, LoginResponse } from '../types/Auth'
import { setUnauthorizedHandler } from '../services/apiClient'
import { AuthContext, type AuthContextValue } from './authContextInstance'
import {
  loadStoredSession,
  persistSession,
} from '../utils/sessionStorage'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
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

  // Honest session initialization: starts isLoading true, validates storage asynchronously,
  // and transitions isLoading to false safely without leaving user stuck
  useEffect(() => {
    let active = true

    void Promise.resolve().then(() => {
      if (!active) return

      try {
        const restored = loadStoredSession()
        if (active) {
          setSession(restored)
        }
      } catch {
        if (active) {
          setSession(null)
        }
      } finally {
        if (active) {
          setIsLoading(false)
        }
      }
    })

    return () => {
      active = false
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

  // Active session timeout: auto-logout when expiresAtUtc is reached
  useEffect(() => {
    if (!session || !session.expiresAtUtc) return

    const expiresAt = new Date(session.expiresAtUtc).getTime()
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      const immediateTimer = window.setTimeout(() => {
        logout('Your session has expired. Please log in again.')
      }, 0)
      return () => {
        window.clearTimeout(immediateTimer)
      }
    }

    const msUntilExpiry = expiresAt - Date.now()
    // Set timer for remaining duration (capped to max 32-bit int)
    const safeTimeout = Math.min(msUntilExpiry, 2147483647)
    const timerId = window.setTimeout(() => {
      logout('Your session has expired. Please log in again.')
    }, safeTimeout)

    return () => {
      window.clearTimeout(timerId)
    }
  }, [session, logout])

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
