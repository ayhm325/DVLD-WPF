import { createContext } from 'react'
import type { AuthUser, LoginResponse } from '../types/Auth'

export type AuthContextValue = {
  accessToken: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  authMessage: string | null
  login: (response: LoginResponse) => void
  logout: (reason?: string) => void
  clearAuthMessage: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
