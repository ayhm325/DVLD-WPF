import { describe, expect, it, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { useState } from 'react'
import ChangePasswordPage from '../../src/components/profile/ChangePasswordPage'
import Login from '../../src/components/auth/Login'
import App from '../../src/App'
import { NavigationProvider } from '../../src/context/NavigationContext'
import { AuthContext, type AuthContextValue } from '../../src/context/authContextInstance'
import * as authService from '../../src/services/authService'
import * as useAuthModule from '../../src/context/useAuth'

describe('ChangePasswordPage', () => {
  const failureMessage =
    'Your password was changed successfully and all existing sessions were revoked, but establishing a new session failed. Please log in with your new password.'

  it('clears all three password inputs, does not show false password error, and calls logout with exact message when replacement session fails', async () => {
    const mockLogin = vi.fn()
    const mockLogout = vi.fn()
    const mockNavigate = vi.fn()

    vi.spyOn(useAuthModule, 'useAuth').mockReturnValue({
      accessToken: 'test-token',
      user: null,
      isAuthenticated: true,
      isLoading: false,
      authMessage: null,
      login: mockLogin,
      logout: mockLogout,
      logoutAll: vi.fn(),
      clearAuthMessage: vi.fn(),
      updateUser: vi.fn(),
    })

    vi.spyOn(authService, 'changePassword').mockRejectedValue(new Error(failureMessage))

    render(<ChangePasswordPage accessToken="test-token" onNavigate={mockNavigate} />)

    const currentPwdInput = screen.getByPlaceholderText(/enter current password/i) as HTMLInputElement
    const newPwdInput = screen.getByPlaceholderText(/enter new strong password/i) as HTMLInputElement
    const confirmPwdInput = screen.getByPlaceholderText(/re-type new password/i) as HTMLInputElement

    fireEvent.change(currentPwdInput, { target: { value: 'OldPassword123!' } })
    fireEvent.change(newPwdInput, { target: { value: 'NewPassword123!' } })
    fireEvent.change(confirmPwdInput, { target: { value: 'NewPassword123!' } })

    expect(currentPwdInput.value).toBe('OldPassword123!')
    expect(newPwdInput.value).toBe('NewPassword123!')
    expect(confirmPwdInput.value).toBe('NewPassword123!')

    const submitButton = screen.getByRole('button', { name: /update account password/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockLogout).toHaveBeenCalledWith(failureMessage)
    })

    // Verify all three password inputs are cleared
    expect(currentPwdInput.value).toBe('')
    expect(newPwdInput.value).toBe('')
    expect(confirmPwdInput.value).toBe('')

    // Verify old password incorrect message is NOT displayed
    expect(screen.queryByText(/current password is incorrect/i)).toBeNull()
  })

  it('displays validation error when current password is incorrect, does not call logout, and preserves inputs', async () => {
    const mockLogin = vi.fn()
    const mockLogout = vi.fn()
    const mockNavigate = vi.fn()

    vi.spyOn(useAuthModule, 'useAuth').mockReturnValue({
      accessToken: 'test-token',
      user: null,
      isAuthenticated: true,
      isLoading: false,
      authMessage: null,
      login: mockLogin,
      logout: mockLogout,
      logoutAll: vi.fn(),
      clearAuthMessage: vi.fn(),
      updateUser: vi.fn(),
    })

    vi.spyOn(authService, 'changePassword').mockRejectedValue(
      new Error('Current password is incorrect.'),
    )

    render(<ChangePasswordPage accessToken="test-token" onNavigate={mockNavigate} />)

    const currentPwdInput = screen.getByPlaceholderText(/enter current password/i) as HTMLInputElement
    const newPwdInput = screen.getByPlaceholderText(/enter new strong password/i) as HTMLInputElement
    const confirmPwdInput = screen.getByPlaceholderText(/re-type new password/i) as HTMLInputElement

    fireEvent.change(currentPwdInput, { target: { value: 'WrongPassword123!' } })
    fireEvent.change(newPwdInput, { target: { value: 'NewPassword123!' } })
    fireEvent.change(confirmPwdInput, { target: { value: 'NewPassword123!' } })

    const submitButton = screen.getByRole('button', { name: /update account password/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(
        screen.getByText(/current password is incorrect\. please verify your existing password and try again\./i),
      ).toBeDefined()
    })

    // Incorrect password must not trigger logout
    expect(mockLogout).not.toHaveBeenCalled()

    // Inputs should remain intact so the user can correct the typo
    expect(currentPwdInput.value).toBe('WrongPassword123!')
    expect(newPwdInput.value).toBe('NewPassword123!')
    expect(confirmPwdInput.value).toBe('NewPassword123!')
  })

  it('transitions to Login and displays the notification banner when unauthenticated with session failure message', () => {
    vi.spyOn(useAuthModule, 'useAuth').mockReturnValue({
      accessToken: null,
      user: null,
      isAuthenticated: false,
      isLoading: false,
      authMessage: failureMessage,
      login: vi.fn(),
      logout: vi.fn(),
      logoutAll: vi.fn(),
      clearAuthMessage: vi.fn(),
      updateUser: vi.fn(),
    })

    render(
      <NavigationProvider>
        <App />
      </NavigationProvider>,
    )

    // The unauthenticated state must transition to Login
    expect(screen.getByRole('heading', { name: /welcome back/i })).toBeDefined()

    // The notification banner must be displayed on Login
    const statusBanner = screen.getByRole('status')
    expect(statusBanner).toBeDefined()
    expect(statusBanner).toHaveTextContent(failureMessage)
  })

  it('verifies interactive state transition from ChangePasswordPage to Login with alert banner', async () => {
    vi.restoreAllMocks()

    vi.spyOn(authService, 'changePassword').mockRejectedValue(new Error(failureMessage))

    // Interactive harness maintaining real auth state transition
    function TestAuthHarness() {
      const [isAuthenticated, setIsAuthenticated] = useState(true)
      const [accessToken, setAccessToken] = useState<string | null>('valid-token')
      const [authMessage, setAuthMessage] = useState<string | null>(null)

      const authContextValue: AuthContextValue = {
        accessToken,
        user: null,
        isAuthenticated,
        isLoading: false,
        authMessage,
        login: vi.fn(),
        logout: (msg?: string) => {
          setIsAuthenticated(false)
          setAccessToken(null)
          setAuthMessage(msg ?? null)
        },
        logoutAll: vi.fn(),
        clearAuthMessage: () => setAuthMessage(null),
        updateUser: vi.fn(),
      }

      return (
        <AuthContext.Provider value={authContextValue}>
          {isAuthenticated ? (
            <ChangePasswordPage accessToken="valid-token" onNavigate={vi.fn()} />
          ) : (
            <Login />
          )}
        </AuthContext.Provider>
      )
    }

    render(
      <NavigationProvider>
        <TestAuthHarness />
      </NavigationProvider>,
    )

    // Initially on ChangePasswordPage
    expect(screen.getByPlaceholderText(/enter current password/i)).toBeDefined()

    fireEvent.change(screen.getByPlaceholderText(/enter current password/i), {
      target: { value: 'OldPassword123!' },
    })
    fireEvent.change(screen.getByPlaceholderText(/enter new strong password/i), {
      target: { value: 'NewPassword123!' },
    })
    fireEvent.change(screen.getByPlaceholderText(/re-type new password/i), {
      target: { value: 'NewPassword123!' },
    })

    fireEvent.click(screen.getByRole('button', { name: /update account password/i }))

    // State transition occurs: ChangePasswordPage unmounts, Login mounts
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /welcome back/i })).toBeDefined()
    })

    // The notification is displayed on Login
    const statusBanner = screen.getByRole('status')
    expect(statusBanner).toBeDefined()
    expect(statusBanner).toHaveTextContent(failureMessage)
  })
})
