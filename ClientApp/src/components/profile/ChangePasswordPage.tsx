import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  LockKeyhole,
  KeyRound,
  Eye,
  EyeOff,
  CheckCircle2,
  ShieldCheck,
  AlertCircle,
  Loader2,
  RotateCcw,
  Check,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  Input,
  Label,
} from '../ui'
import { changePassword } from '../../services/authService'
import { useAuth } from '../../context/useAuth'
import { cn } from '@/lib/utils'

interface ChangePasswordPageProps {
  accessToken: string
  onNavigate: (page: string) => void
}

interface ValidationErrors {
  currentPassword?: string
  newPassword?: string
  confirmPassword?: string
}

export default function ChangePasswordPage({
  accessToken,
  onNavigate,
}: ChangePasswordPageProps) {
  const { login, logout } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  const [showCurrentPassword, setShowCurrentPassword] = useState(false)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({})

  // Interactive password criteria evaluation
  const hasMinLength = newPassword.length >= 8
  const hasValidLength = newPassword.length <= 100
  const isDifferentFromCurrent =
    currentPassword.length > 0 && newPassword.length > 0 && currentPassword !== newPassword
  const passwordsMatch = newPassword.length > 0 && newPassword === confirmPassword

  function validateForm(): boolean {
    const errors: ValidationErrors = {}

    if (!currentPassword) {
      errors.currentPassword = 'Current password is required.'
    }

    if (!newPassword) {
      errors.newPassword = 'New password is required.'
    } else if (newPassword.length < 8) {
      errors.newPassword = 'New password must be at least 8 characters.'
    } else if (newPassword.length > 100) {
      errors.newPassword = 'New password cannot exceed 100 characters.'
    } else if (currentPassword && newPassword === currentPassword) {
      errors.newPassword = 'The new password must be different from your current password.'
    }

    if (!confirmPassword) {
      errors.confirmPassword = 'Confirming your new password is required.'
    } else if (newPassword && confirmPassword !== newPassword) {
      errors.confirmPassword = 'Password confirmation does not match the new password.'
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  function handleReset() {
    setCurrentPassword('')
    setNewPassword('')
    setConfirmPassword('')
    setValidationErrors({})
    setError(null)
    setSuccess(false)
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (submitting) return

    if (!validateForm()) {
      return
    }

    setSubmitting(true)
    setError(null)
    setSuccess(false)

    try {
      const refreshedSession = await changePassword(accessToken, {
        currentPassword,
        newPassword,
      })

      // The password change revoked every session. Apply the fresh access token
      // and rotated refresh cookie issued for this device.
      login(refreshedSession)

      // Sensitive form state cleared on success
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setValidationErrors({})
      setSuccess(true)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to update account password.'
      const isSessionEstablishmentFailure =
        msg.toLowerCase().includes('establishing a new session failed') ||
        msg.toLowerCase().includes('session establishment failed') ||
        (msg.toLowerCase().includes('password was changed successfully') &&
          msg.toLowerCase().includes('log in'))

      if (isSessionEstablishmentFailure) {
        // Clear sensitive input fields so old credentials cannot be re-submitted
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
        setValidationErrors({})
        // Log out locally to clear stale tokens and navigate to login screen with clear notification
        logout(msg)
        return
      }

      if (msg.toLowerCase().includes('current password is incorrect')) {
        setError('Current password is incorrect. Please verify your existing password and try again.')
      } else {
        setError(msg)
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* 1. PageHero — Visual Introduction Only (NO BUTTONS) */}
      <PageHero
        eyebrow="SECURITY & AUTHENTICATION"
        title="Change"
        highlight="Password"
        description="Update your DVLD enterprise credentials in accordance with national transport registry security policies."
      />

      {/* 2. Page Actions — Dedicated Action Area Below PageHero */}
      <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-between gap-3 pt-1">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onNavigate('My Profile')}
          className="h-10 px-4 gap-2 text-xs font-semibold text-slate-200 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-cyan-500/50 rounded-xl transition-all cursor-pointer"
        >
          <ArrowLeft className="size-4 text-cyan-400" />
          <span>Back to My Profile</span>
        </Button>

        <div className="flex items-center gap-2">
          <span className="size-2 rounded-full bg-emerald-400 shadow-[0_0_8px_#10b981]" aria-hidden="true" />
          <span className="text-xs font-semibold text-slate-300">
            Encrypted Session · Real-time Credential Verification
          </span>
        </div>
      </div>

      {/* Error Alert */}
      {error && <ErrorAlert title="Password Change Failure" message={error} />}

      {/* Success Banner */}
      {success && (
        <div className="p-6 rounded-2xl border border-emerald-500/40 bg-gradient-to-r from-emerald-950/60 to-slate-900/90 shadow-xl space-y-4">
          <div className="flex items-start gap-4">
            <div className="size-10 rounded-xl bg-emerald-500/20 border border-emerald-500/40 flex items-center justify-center shrink-0">
              <CheckCircle2 className="size-6 text-emerald-400" />
            </div>
            <div className="space-y-1">
              <h4 className="text-base font-bold text-white">Password Successfully Updated</h4>
              <p className="text-xs text-emerald-300/90 leading-relaxed">
                Your enterprise account password has been updated and securely hashed with BCrypt. Your current active session remains valid and secure.
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 pt-2 border-t border-emerald-500/20">
            <Button
              type="button"
              variant="default"
              size="sm"
              onClick={() => onNavigate('My Profile')}
              className="h-10 px-5 gap-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-500 rounded-xl shadow-lg cursor-pointer"
            >
              <span>Return to My Profile</span>
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleReset}
              className="h-10 px-4 gap-2 text-xs font-semibold text-slate-300 bg-[#0e1b33] border-[#263750] rounded-xl cursor-pointer"
            >
              <RotateCcw className="size-3.5" />
              <span>Change Another Password</span>
            </Button>
          </div>
        </div>
      )}

      {/* 3. Main Page Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Form Card (2 cols on lg) */}
        <Card className="lg:col-span-2 border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-xl overflow-hidden text-white">
          <CardHeader className="border-b border-[#1e3458]/70 px-6 py-5">
            <CardTitle className="text-base font-bold flex items-center gap-2.5 text-white">
              <KeyRound className="size-4.5 text-cyan-400" />
              <span>Account Password Modification</span>
            </CardTitle>
            <CardDescription className="text-xs text-slate-400">
              Please enter your current password to authenticate, followed by your new password.
            </CardDescription>
          </CardHeader>
          <CardContent className="p-6">
            <form onSubmit={handleSubmit} className="space-y-5">
              {/* Current Password Field */}
              <div className="space-y-2">
                <Label
                  htmlFor="currentPasswordInput"
                  className="text-xs font-semibold text-slate-300 flex items-center justify-between"
                >
                  <span>Current Password *</span>
                  <span className="text-[11px] text-slate-400 font-normal">Existing secret key</span>
                </Label>
                <div className="relative">
                  <LockKeyhole className="absolute left-3.5 top-1/2 -translate-y-1/2 size-4 text-slate-400 pointer-events-none" />
                  <Input
                    id="currentPasswordInput"
                    type={showCurrentPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    placeholder="Enter current password"
                    value={currentPassword}
                    onChange={(e) => {
                      setCurrentPassword(e.target.value)
                      if (validationErrors.currentPassword) {
                        setValidationErrors((prev) => ({ ...prev, currentPassword: undefined }))
                      }
                      if (error) setError(null)
                    }}
                    disabled={submitting}
                    className={cn(
                      'h-11 pl-10 pr-11 bg-[#081326] text-white text-sm rounded-xl transition-all',
                      validationErrors.currentPassword
                        ? 'border-rose-500 focus:border-rose-500'
                        : 'border-[#1b2f52] focus:border-cyan-500',
                    )}
                  />
                  <button
                    type="button"
                    onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                    aria-label={showCurrentPassword ? 'Hide current password' : 'Show current password'}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white transition-colors cursor-pointer"
                  >
                    {showCurrentPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
                {validationErrors.currentPassword && (
                  <p className="text-xs text-rose-400 mt-1 flex items-center gap-1.5">
                    <AlertCircle className="size-3.5 shrink-0" />
                    <span>{validationErrors.currentPassword}</span>
                  </p>
                )}
              </div>

              {/* New Password Field */}
              <div className="space-y-2">
                <Label
                  htmlFor="newPasswordInput"
                  className="text-xs font-semibold text-slate-300 flex items-center justify-between"
                >
                  <span>New Password *</span>
                  <span className="text-[11px] text-slate-400 font-normal">Minimum 8 characters</span>
                </Label>
                <div className="relative">
                  <KeyRound className="absolute left-3.5 top-1/2 -translate-y-1/2 size-4 text-slate-400 pointer-events-none" />
                  <Input
                    id="newPasswordInput"
                    type={showNewPassword ? 'text' : 'password'}
                    autoComplete="new-password"
                    placeholder="Enter new strong password"
                    value={newPassword}
                    onChange={(e) => {
                      setNewPassword(e.target.value)
                      if (validationErrors.newPassword) {
                        setValidationErrors((prev) => ({ ...prev, newPassword: undefined }))
                      }
                      if (error) setError(null)
                    }}
                    disabled={submitting}
                    className={cn(
                      'h-11 pl-10 pr-11 bg-[#081326] text-white text-sm rounded-xl transition-all',
                      validationErrors.newPassword
                        ? 'border-rose-500 focus:border-rose-500'
                        : 'border-[#1b2f52] focus:border-cyan-500',
                    )}
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    aria-label={showNewPassword ? 'Hide new password' : 'Show new password'}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white transition-colors cursor-pointer"
                  >
                    {showNewPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
                {validationErrors.newPassword && (
                  <p className="text-xs text-rose-400 mt-1 flex items-center gap-1.5">
                    <AlertCircle className="size-3.5 shrink-0" />
                    <span>{validationErrors.newPassword}</span>
                  </p>
                )}
              </div>

              {/* Confirm New Password Field */}
              <div className="space-y-2">
                <Label
                  htmlFor="confirmPasswordInput"
                  className="text-xs font-semibold text-slate-300 flex items-center justify-between"
                >
                  <span>Confirm New Password *</span>
                  <span className="text-[11px] text-slate-400 font-normal">Must match new password</span>
                </Label>
                <div className="relative">
                  <KeyRound className="absolute left-3.5 top-1/2 -translate-y-1/2 size-4 text-slate-400 pointer-events-none" />
                  <Input
                    id="confirmPasswordInput"
                    type={showConfirmPassword ? 'text' : 'password'}
                    autoComplete="new-password"
                    placeholder="Re-type new password"
                    value={confirmPassword}
                    onChange={(e) => {
                      setConfirmPassword(e.target.value)
                      if (validationErrors.confirmPassword) {
                        setValidationErrors((prev) => ({ ...prev, confirmPassword: undefined }))
                      }
                      if (error) setError(null)
                    }}
                    disabled={submitting}
                    className={cn(
                      'h-11 pl-10 pr-11 bg-[#081326] text-white text-sm rounded-xl transition-all',
                      validationErrors.confirmPassword
                        ? 'border-rose-500 focus:border-rose-500'
                        : 'border-[#1b2f52] focus:border-cyan-500',
                    )}
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    aria-label={showConfirmPassword ? 'Hide password confirmation' : 'Show password confirmation'}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white transition-colors cursor-pointer"
                  >
                    {showConfirmPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
                {validationErrors.confirmPassword && (
                  <p className="text-xs text-rose-400 mt-1 flex items-center gap-1.5">
                    <AlertCircle className="size-3.5 shrink-0" />
                    <span>{validationErrors.confirmPassword}</span>
                  </p>
                )}
              </div>

              {/* Action Buttons */}
              <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-3 border-t border-[#1b2f52]/60">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleReset}
                  disabled={submitting || (!currentPassword && !newPassword && !confirmPassword)}
                  className="h-10 px-5 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] text-slate-300 hover:text-white rounded-xl cursor-pointer"
                >
                  <RotateCcw className="size-4 text-slate-400" />
                  <span>Clear Inputs</span>
                </Button>

                <Button
                  type="submit"
                  variant="default"
                  size="sm"
                  disabled={submitting || !currentPassword || !newPassword || !confirmPassword}
                  className="h-10 px-6 gap-2 text-xs font-semibold text-white bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 rounded-xl shadow-lg shadow-blue-600/20 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
                >
                  {submitting ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      <span>Verifying & Hashing...</span>
                    </>
                  ) : (
                    <>
                      <LockKeyhole className="size-4" />
                      <span>Update Account Password</span>
                    </>
                  )}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        {/* Password Policy & Requirements Helper Card */}
        <div className="space-y-6">
          <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-xl overflow-hidden text-white">
            <CardHeader className="border-b border-[#1e3458]/70 px-6 py-5">
              <CardTitle className="text-base font-bold flex items-center gap-2.5 text-white">
                <ShieldCheck className="size-4.5 text-cyan-400" />
                <span>Security Policy Checklist</span>
              </CardTitle>
              <CardDescription className="text-xs text-slate-400">
                Official password complexity rules enforced by DVLD enterprise servers.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-6 space-y-3.5">
              <div className="flex items-start gap-3">
                <div
                  className={cn(
                    'size-5 rounded-full flex items-center justify-center shrink-0 mt-0.5 transition-colors',
                    hasMinLength
                      ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500 border border-slate-700',
                  )}
                >
                  <Check className="size-3 stroke-[3]" />
                </div>
                <div className="space-y-0.5">
                  <span className={cn('text-xs font-medium', hasMinLength ? 'text-white' : 'text-slate-400')}>
                    Minimum 8 characters
                  </span>
                  <p className="text-[11px] text-slate-400">
                    Provides resistance against dictionary attack vectors.
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div
                  className={cn(
                    'size-5 rounded-full flex items-center justify-center shrink-0 mt-0.5 transition-colors',
                    hasValidLength
                      ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500 border border-slate-700',
                  )}
                >
                  <Check className="size-3 stroke-[3]" />
                </div>
                <div className="space-y-0.5">
                  <span className={cn('text-xs font-medium', hasValidLength ? 'text-white' : 'text-slate-400')}>
                    Maximum 100 characters
                  </span>
                  <p className="text-[11px] text-slate-400">
                    Complies with backend BCrypt buffer constraints.
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div
                  className={cn(
                    'size-5 rounded-full flex items-center justify-center shrink-0 mt-0.5 transition-colors',
                    isDifferentFromCurrent
                      ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500 border border-slate-700',
                  )}
                >
                  <Check className="size-3 stroke-[3]" />
                </div>
                <div className="space-y-0.5">
                  <span className={cn('text-xs font-medium', isDifferentFromCurrent ? 'text-white' : 'text-slate-400')}>
                    Different from current password
                  </span>
                  <p className="text-[11px] text-slate-400">
                    Prevents accidental re-submission of unchanged secrets.
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div
                  className={cn(
                    'size-5 rounded-full flex items-center justify-center shrink-0 mt-0.5 transition-colors',
                    passwordsMatch
                      ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500 border border-slate-700',
                  )}
                >
                  <Check className="size-3 stroke-[3]" />
                </div>
                <div className="space-y-0.5">
                  <span className={cn('text-xs font-medium', passwordsMatch ? 'text-white' : 'text-slate-400')}>
                    Passwords match exactly
                  </span>
                  <p className="text-[11px] text-slate-400">
                    Confirms typo-free credential entry before submission.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Cryptographic Protection Notice */}
          <div className="p-4.5 rounded-2xl bg-[#09152b] border border-blue-500/20 text-xs text-slate-300 space-y-2">
            <div className="flex items-center gap-2 text-cyan-400 font-bold">
              <ShieldCheck className="size-4" />
              <span>Cryptographic Hashing</span>
            </div>
            <p className="text-[11.5px] leading-relaxed text-slate-300">
              Passwords are never transmitted or stored in plaintext. Passwords undergo salt generation and adaptive one-way hashing on the backend server with BCrypt before persistence.
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}
