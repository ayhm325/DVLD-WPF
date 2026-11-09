import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import type { ReactNode } from 'react'
import type { AuthSession, AuthUser, LoginResponse } from '../types/Auth'
import {
  logoutAllSessions,
  logoutSession,
  refreshSession,
  setUnauthorizedHandler,
} from '../services/apiClient'
import { setAccessToken } from '../utils/authTokenStore'
import { purgeLegacyTokenStorage } from '../utils/sessionStorage'
import { AuthContext, type AuthContextValue } from './authContextInstance'

type AuthChannelMessage = {
  type?: 'login' | 'logout'
  reason?: string
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [authMessage, setAuthMessage] = useState<string | null>(null)
  const channelRef = useRef<BroadcastChannel | null>(null)

  const applySession = useCallback((response: LoginResponse) => {
    const user: AuthUser = {
      userId: response.userId,
      userName: response.userName,
      personId: response.personId,
      fullName: response.fullName,
      role: response.role,
    }

    const nextSession: AuthSession = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user,
    }

    setAccessToken(nextSession.accessToken)
    setSession(nextSession)
    setAuthMessage(null)
  }, [])

  const clearLocalState = useCallback((message: string | null) => {
    setAccessToken(null)
    setSession(null)
    setAuthMessage(message)
  }, [])

  const login = useCallback(
    (response: LoginResponse) => {
      applySession(response)
      channelRef.current?.postMessage({ type: 'login' } satisfies AuthChannelMessage)
    },
    [applySession],
  )

  const logout = useCallback(
    (reason?: string) => {
      // Revoke the server session (best effort) and clear every local trace.
      void logoutSession()
      clearLocalState(reason ?? 'You have successfully signed out.')
      channelRef.current?.postMessage({
        type: 'logout',
        reason,
      } satisfies AuthChannelMessage)
    },
    [clearLocalState],
  )

  const logoutAll = useCallback(() => {
    void logoutAllSessions().finally(() => {
      clearLocalState('All sessions have been revoked. Please log in again.')
      channelRef.current?.postMessage({ type: 'logout' } satisfies AuthChannelMessage)
    })
  }, [clearLocalState])

  // Cross-tab coordination: one tab logging out clears every tab; a login in one
  // tab lets the others pick up the shared refresh cookie.
  useEffect(() => {
    if (typeof BroadcastChannel === 'undefined') {
      return
    }

    const channel = new BroadcastChannel('dvld-auth')

    channel.onmessage = (event: MessageEvent<AuthChannelMessage>) => {
      const message = event.data

      if (message?.type === 'logout') {
        clearLocalState(
          message.reason ?? 'You have been signed out in another tab.',
        )
      }

      if (message?.type === 'login') {
        void refreshSession().then((restored) => {
          if (restored) {
            applySession(restored)
          }
        })
      }
    }

    channelRef.current = channel

    return () => {
      channel.close()
      channelRef.current = null
    }
  }, [applySession, clearLocalState])

  // Session restoration: the access token is never persisted; the HttpOnly cookie
  // is exchanged for a fresh access token on every page load.
  useEffect(() => {
    let active = true

    purgeLegacyTokenStorage()

    void refreshSession()
      .then((restored) => {
        if (active && restored) {
          applySession(restored)
        }
      })
      .finally(() => {
        if (active) {
          setIsLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [applySession])

  useEffect(() => {
    setUnauthorizedHandler((message: string) => {
      clearLocalState(message || 'Your session has expired. Please log in again.')
      channelRef.current?.postMessage({ type: 'logout' } satisfies AuthChannelMessage)
    })

    return () => {
      setUnauthorizedHandler(null)
    }
  }, [clearLocalState])

  // Silent renewal shortly before the access token expires.
  useEffect(() => {
    if (!session) {
      return
    }

    const expiresAt = new Date(session.expiresAtUtc).getTime()

    if (!Number.isFinite(expiresAt)) {
      return
    }

    const delayMs = Math.max(
      15_000,
      Math.min(expiresAt - Date.now() - 60_000, 2_147_483_647),
    )

    const timerId = window.setTimeout(() => {
      void refreshSession().then((restored) => {
        if (restored) {
          applySession(restored)
        } else {
          clearLocalState('Your session has expired. Please log in again.')
        }
      })
    }, delayMs)

    return () => {
      window.clearTimeout(timerId)
    }
  }, [session, applySession, clearLocalState])

  const clearAuthMessage = useCallback(() => {
    setAuthMessage(null)
  }, [])

  const updateUser = useCallback((updatedUser: Partial<AuthUser>) => {
    setSession((previous) => {
      if (!previous) return null

      return {
        ...previous,
        user: {
          ...previous.user,
          ...updatedUser,
        },
      }
    })
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      accessToken: session?.accessToken ?? null,
      user: session?.user ?? null,
      isAuthenticated: session !== null && session.accessToken.length > 0,
      isLoading,
      authMessage,
      login,
      logout,
      logoutAll,
      clearAuthMessage,
      updateUser,
    }),
    [
      session,
      isLoading,
      authMessage,
      login,
      logout,
      logoutAll,
      clearAuthMessage,
      updateUser,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
