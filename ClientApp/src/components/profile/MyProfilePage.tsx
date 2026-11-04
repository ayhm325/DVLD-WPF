import { useState, useCallback, type FormEvent } from 'react'
import {
  UserRound,
  ShieldCheck,
  Calendar,
  Globe,
  Phone,
  Mail,
  MapPin,
  LockKeyhole,
  Pencil,
  Save,
  X,
  Hash,
  IdCard,
  User,
  ShieldAlert,
  Loader2,
  CheckCircle2,
  RotateCcw,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
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
import { useAuth } from '../../context/useAuth'
import { useAsyncData } from '../../hooks'
import { getUserProfile } from '../../services/authService'
import { updatePerson } from '../../services/peopleService'
import type { UpdatePersonRequest } from '../../types/Person'
import { formatDate } from '../../utils/formatters'
import { cn } from '@/lib/utils'

interface MyProfilePageProps {
  accessToken: string
  onNavigate: (page: string) => void
}

interface ValidationErrors {
  phone?: string
  email?: string
  address?: string
}

const JORDAN_PHONE_REGEX = /^(077|078|079)\d{7}$/
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export default function MyProfilePage({ accessToken, onNavigate }: MyProfilePageProps) {
  const { user, updateUser } = useAuth()

  const [actionError, setActionError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  // Edit Mode state
  const [isEditing, setIsEditing] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [formValues, setFormValues] = useState({
    phone: '',
    email: '',
    address: '',
  })
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({})

  const fetcher = useCallback(() => getUserProfile(accessToken), [accessToken])

  const {
    data: profile,
    loading,
    error: loadError,
    refresh: refreshProfile,
  } = useAsyncData(fetcher, {
    dependencies: [accessToken],
    defaultErrorMessage: 'Failed to load user profile.',
    onSuccess: (data) => {
      setFormValues({
        phone: data.phone || '',
        email: data.email || '',
        address: data.address || '',
      })
    },
  })

  const error = actionError || loadError || null

  function handleStartEdit() {
    if (!profile) return
    setFormValues({
      phone: profile.phone || '',
      email: profile.email || '',
      address: profile.address || '',
    })
    setValidationErrors({})
    setActionError(null)
    setSuccessMessage(null)
    setIsEditing(true)
  }

  function handleCancelEdit() {
    if (!profile) return
    setFormValues({
      phone: profile.phone || '',
      email: profile.email || '',
      address: profile.address || '',
    })
    setValidationErrors({})
    setActionError(null)
    setIsEditing(false)
  }

  function validateForm(): boolean {
    const errors: ValidationErrors = {}
    const trimmedPhone = formValues.phone.trim()
    const trimmedEmail = formValues.email.trim()
    const trimmedAddress = formValues.address.trim()

    if (!trimmedPhone) {
      errors.phone = 'Phone number is required.'
    } else if (!JORDAN_PHONE_REGEX.test(trimmedPhone)) {
      errors.phone = 'Phone must start with 077, 078, or 079 and contain exactly 10 digits.'
    }

    if (trimmedEmail) {
      if (trimmedEmail.length > 100) {
        errors.email = 'Email cannot exceed 100 characters.'
      } else if (!EMAIL_REGEX.test(trimmedEmail)) {
        errors.email = 'Please enter a valid email address.'
      }
    }

    if (!trimmedAddress) {
      errors.address = 'Address is required.'
    } else if (trimmedAddress.length > 500) {
      errors.address = 'Address cannot exceed 500 characters.'
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSaveProfile(e: FormEvent) {
    e.preventDefault()
    if (!profile || submitting) return

    if (!validateForm()) {
      return
    }

    setSubmitting(true)
    setActionError(null)
    setSuccessMessage(null)

    try {
      const updateData: UpdatePersonRequest = {
        nationalNo: profile.nationalNo,
        firstName: profile.firstName,
        secondName: profile.secondName,
        thirdName: profile.thirdName,
        lastName: profile.lastName,
        dateOfBirth: profile.dateOfBirth,
        gender: profile.gender,
        address: formValues.address.trim(),
        phone: formValues.phone.trim(),
        email: formValues.email.trim() || null,
        nationalityCountryID: profile.nationalityCountryID,
        imagePath: profile.imagePath,
      }

      await updatePerson(accessToken, profile.personId, updateData)

      // Refresh profile data
      await refreshProfile()
      setIsEditing(false)
      setSuccessMessage('Profile contact details updated successfully.')

      // Update session if needed
      updateUser({
        fullName: profile.fullName,
      })
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Failed to update profile information.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* 1. PageHero — Visual Introduction Only (NO BUTTONS) */}
      <PageHero
        eyebrow="ACCOUNT MANAGEMENT"
        title="My"
        highlight="Profile"
        description="View your verified employee credentials, identity records, and update permitted personal contact details."
        variant="blue"
      />

      {/* 2. Page Actions — Dedicated Action Area Below PageHero */}
      <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-between gap-3 pt-1">
        <div className="flex items-center gap-2">
          <span className="size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#00e5ff]" aria-hidden="true" />
          <span className="text-xs font-semibold text-slate-300">
            Authenticated Account · {user?.role || 'Staff'} Access
          </span>
        </div>

        <div className="flex items-center gap-2.5">
          {!isEditing ? (
            <>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => onNavigate('Change Password')}
                className="h-10 px-4 gap-2 text-xs font-semibold text-slate-200 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-cyan-500/50 rounded-xl transition-all cursor-pointer"
              >
                <LockKeyhole className="size-4 text-cyan-400" />
                <span>Change Password</span>
              </Button>

              <Button
                type="button"
                variant="default"
                size="sm"
                onClick={handleStartEdit}
                disabled={loading || !profile}
                className="h-10 px-5 gap-2 text-xs font-semibold text-white bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 rounded-xl shadow-lg shadow-blue-600/20 transition-all cursor-pointer"
              >
                <Pencil className="size-4" />
                <span>Edit Contact Details</span>
              </Button>
            </>
          ) : (
            <>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleCancelEdit}
                disabled={submitting}
                className="h-10 px-4 gap-2 text-xs font-semibold text-slate-300 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] rounded-xl transition-all cursor-pointer"
              >
                <X className="size-4 text-slate-400" />
                <span>Cancel</span>
              </Button>

              <Button
                type="button"
                variant="default"
                size="sm"
                onClick={handleSaveProfile}
                disabled={submitting}
                className="h-10 px-5 gap-2 text-xs font-semibold text-white bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 rounded-xl shadow-lg shadow-emerald-600/20 transition-all cursor-pointer"
              >
                {submitting ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Saving Changes...</span>
                  </>
                ) : (
                  <>
                    <Save className="size-4" />
                    <span>Save Changes</span>
                  </>
                )}
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Notifications */}
      {error && <ErrorAlert title="Profile Notice" message={error} />}

      {successMessage && (
        <div className="flex items-start gap-3 p-4.5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Profile Updated</h4>
            <p className="text-xs text-emerald-300/90">{successMessage}</p>
          </div>
        </div>
      )}

      {/* Loading State */}
      {loading && !profile && (
        <div className="flex flex-col items-center justify-center p-16 rounded-2xl bg-[#0c1830] border border-[#1e3458] text-center space-y-4">
          <LoadingSpinner size="lg" />
          <p className="text-sm font-medium text-slate-300">Loading authenticated profile credentials...</p>
        </div>
      )}

      {/* Failed to Load State */}
      {!loading && !profile && error && (
        <Card className="border-[#1e3458] bg-[#0c1830] text-center p-12 space-y-4">
          <p className="text-sm text-slate-400">Unable to load employee profile data.</p>
          <Button
            type="button"
            variant="outline"
            onClick={() => void refreshProfile()}
            className="h-10 px-5 gap-2 bg-[#0e1b33] border-[#263750] text-white rounded-xl cursor-pointer"
          >
            <RotateCcw className="size-4 text-cyan-400" />
            <span>Retry Profile Load</span>
          </Button>
        </Card>
      )}

      {/* 3. Main Page Content */}
      {profile && (
        <div className="space-y-6">
          {/* Card 1: Official Employee Identity & Overview */}
          <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-xl overflow-hidden text-white">
            <CardHeader className="border-b border-[#1e3458]/70 px-6 py-5">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div className="space-y-1">
                  <CardTitle className="text-base font-bold flex items-center gap-2.5 text-white">
                    <UserRound className="size-4.5 text-cyan-400" />
                    <span>Employee Identity & Credentials</span>
                  </CardTitle>
                  <CardDescription className="text-xs text-slate-400">
                    Official identity registration recorded in the DVLD enterprise system.
                  </CardDescription>
                </div>
                <div className="flex items-center gap-2">
                  <StatusBadge status={user?.role || 'Staff'} />
                  <StatusBadge status={profile.isActive ? 'Active' : 'Inactive'} />
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6 space-y-6">
              {/* Photo & Primary Bio Row */}
              <div className="flex flex-col sm:flex-row items-center sm:items-start gap-6 p-5 sm:p-6 rounded-2xl bg-[#081326] border border-[#1b2f52]">
                <div className="shrink-0 flex flex-col items-center">
                  <PersonPhoto
                    personId={profile.personId}
                    photoKey={profile.imagePath}
                    fullName={profile.fullName}
                    size="xl"
                    accessToken={accessToken}
                    className="border-2 border-[#1b2f52] shadow-xl"
                  />
                  <span className="text-[11px] font-mono text-slate-400 mt-2">
                    Person #{profile.personId}
                  </span>
                </div>

                <div className="flex-1 space-y-3.5 text-center sm:text-left min-w-0">
                  <div>
                    <h3 className="text-xl sm:text-2xl font-black text-white tracking-tight leading-tight">
                      {profile.fullName}
                    </h3>
                    <p className="text-xs text-cyan-400 font-mono mt-1 flex items-center justify-center sm:justify-start gap-2">
                      <IdCard className="size-3.5 text-cyan-400/80" />
                      <span>National ID: <strong>{profile.nationalNo}</strong></span>
                    </p>
                  </div>

                  <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2 pt-1">
                    <span className="px-3 py-1 rounded-lg text-xs font-mono font-bold bg-[#0c1e3d] text-cyan-300 border border-cyan-500/30">
                      @{profile.userName}
                    </span>
                    <span className="px-3 py-1 rounded-lg text-xs font-semibold bg-[#0d2238] text-slate-300 border border-[#1e3458]">
                      Account #{profile.userId}
                    </span>
                    <span className="px-3 py-1 rounded-lg text-xs font-semibold bg-[#132338] text-indigo-300 border border-indigo-500/30 flex items-center gap-1.5">
                      <Globe className="size-3.5 text-indigo-400" />
                      <span>{profile.countryName || 'Jordan'}</span>
                    </span>
                  </div>
                </div>
              </div>

              {/* Verified Identity Attributes Grid (Read-Only) */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3.5">
                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                      National No
                    </span>
                    <Hash className="size-3.5 text-cyan-400/80" />
                  </div>
                  <strong className="text-sm font-extrabold text-white font-mono block">
                    {profile.nationalNo}
                  </strong>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                      Date of Birth
                    </span>
                    <Calendar className="size-3.5 text-slate-400/80" />
                  </div>
                  <strong className="text-sm font-semibold text-slate-200 block">
                    {formatDate(profile.dateOfBirth)}
                  </strong>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                      Gender
                    </span>
                    <User className="size-3.5 text-slate-400/80" />
                  </div>
                  <strong className="text-sm font-semibold text-slate-200 block">
                    {profile.gender === 0 ? 'Male' : 'Female'}
                  </strong>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                      Nationality
                    </span>
                    <Globe className="size-3.5 text-indigo-400/80" />
                  </div>
                  <strong className="text-sm font-semibold text-slate-200 block truncate" title={profile.countryName}>
                    {profile.countryName || 'Jordan'}
                  </strong>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Card 2: Contact & Residential Communication (Editable) */}
          <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-xl overflow-hidden text-white">
            <CardHeader className="border-b border-[#1e3458]/70 px-6 py-5">
              <div className="flex items-center justify-between gap-3">
                <div className="space-y-1">
                  <CardTitle className="text-base font-bold flex items-center gap-2.5 text-white">
                    <Phone className="size-4.5 text-emerald-400" />
                    <span>Personal Contact & Communication</span>
                  </CardTitle>
                  <CardDescription className="text-xs text-slate-400">
                    Communication channels used for official administrative notices and emergency contact.
                  </CardDescription>
                </div>
                {isEditing && (
                  <span className="text-xs font-semibold text-amber-400 bg-amber-500/10 border border-amber-500/30 px-3 py-1 rounded-lg">
                    Editing Mode
                  </span>
                )}
              </div>
            </CardHeader>
            <CardContent className="p-6">
              {!isEditing ? (
                /* Read-Only Display */
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-2">
                    <div className="flex items-center gap-2 text-xs font-semibold text-slate-400">
                      <Phone className="size-3.5 text-emerald-400" />
                      <span>Phone Number</span>
                    </div>
                    <p className="text-sm font-bold text-white font-mono">
                      {profile.phone || 'Not provided'}
                    </p>
                    <span className="text-[11px] text-slate-400 block">
                      Jordan Telecom Carrier
                    </span>
                  </div>

                  <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-2">
                    <div className="flex items-center gap-2 text-xs font-semibold text-slate-400">
                      <Mail className="size-3.5 text-cyan-400" />
                      <span>Email Address</span>
                    </div>
                    <p className="text-sm font-semibold text-slate-200 truncate" title={profile.email || ''}>
                      {profile.email || <span className="text-slate-400 italic">None provided</span>}
                    </p>
                    <span className="text-[11px] text-slate-400 block">
                      Administrative notification inbox
                    </span>
                  </div>

                  <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-2">
                    <div className="flex items-center gap-2 text-xs font-semibold text-slate-400">
                      <MapPin className="size-3.5 text-rose-400" />
                      <span>Residential Address</span>
                    </div>
                    <p className="text-sm font-semibold text-slate-200 line-clamp-2" title={profile.address}>
                      {profile.address || 'Not specified'}
                    </p>
                    <span className="text-[11px] text-slate-400 block">
                      Official registered domicile
                    </span>
                  </div>
                </div>
              ) : (
                /* Editable Form */
                <form onSubmit={handleSaveProfile} className="space-y-5">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {/* Phone */}
                    <div className="space-y-2">
                      <Label htmlFor="profilePhone" className="text-xs font-semibold text-slate-300 flex items-center justify-between">
                        <span>Phone Number *</span>
                        <span className="text-[11px] text-slate-400 font-normal">Jordan (077/078/079)</span>
                      </Label>
                      <div className="relative">
                        <Phone className="absolute left-3.5 top-1/2 -translate-y-1/2 size-4 text-slate-400 pointer-events-none" />
                        <Input
                          id="profilePhone"
                          type="tel"
                          placeholder="0791234567"
                          value={formValues.phone}
                          onChange={(e) => {
                            setFormValues((prev) => ({ ...prev, phone: e.target.value }))
                            if (validationErrors.phone) {
                              setValidationErrors((prev) => ({ ...prev, phone: undefined }))
                            }
                          }}
                          disabled={submitting}
                          className={cn(
                            'h-10.5 pl-10 pr-4 bg-[#081326] text-white font-mono text-sm rounded-xl transition-all',
                            validationErrors.phone
                              ? 'border-rose-500 focus:border-rose-500'
                              : 'border-[#1b2f52] focus:border-cyan-500',
                          )}
                        />
                      </div>
                      {validationErrors.phone && (
                        <p className="text-xs text-rose-400 mt-1">{validationErrors.phone}</p>
                      )}
                    </div>

                    {/* Email */}
                    <div className="space-y-2">
                      <Label htmlFor="profileEmail" className="text-xs font-semibold text-slate-300 flex items-center justify-between">
                        <span>Email Address</span>
                        <span className="text-[11px] text-slate-400 font-normal">Optional</span>
                      </Label>
                      <div className="relative">
                        <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 size-4 text-slate-400 pointer-events-none" />
                        <Input
                          id="profileEmail"
                          type="email"
                          placeholder="employee@dvld.gov.jo"
                          value={formValues.email}
                          onChange={(e) => {
                            setFormValues((prev) => ({ ...prev, email: e.target.value }))
                            if (validationErrors.email) {
                              setValidationErrors((prev) => ({ ...prev, email: undefined }))
                            }
                          }}
                          disabled={submitting}
                          className={cn(
                            'h-10.5 pl-10 pr-4 bg-[#081326] text-white text-sm rounded-xl transition-all',
                            validationErrors.email
                              ? 'border-rose-500 focus:border-rose-500'
                              : 'border-[#1b2f52] focus:border-cyan-500',
                          )}
                        />
                      </div>
                      {validationErrors.email && (
                        <p className="text-xs text-rose-400 mt-1">{validationErrors.email}</p>
                      )}
                    </div>
                  </div>

                  {/* Address */}
                  <div className="space-y-2">
                    <Label htmlFor="profileAddress" className="text-xs font-semibold text-slate-300 flex items-center justify-between">
                      <span>Residential Address *</span>
                      <span className="text-[11px] text-slate-400 font-normal">Max 500 chars</span>
                    </Label>
                    <div className="relative">
                      <MapPin className="absolute left-3.5 top-3 size-4 text-slate-400 pointer-events-none" />
                      <textarea
                        id="profileAddress"
                        rows={3}
                        placeholder="Street, Building No, City, Jordan"
                        value={formValues.address}
                        onChange={(e) => {
                          setFormValues((prev) => ({ ...prev, address: e.target.value }))
                          if (validationErrors.address) {
                            setValidationErrors((prev) => ({ ...prev, address: undefined }))
                          }
                        }}
                        disabled={submitting}
                        className={cn(
                          'w-full pl-10 pr-4 py-2.5 bg-[#081326] text-white text-sm rounded-xl border transition-all resize-none focus:outline-none focus:ring-1',
                          validationErrors.address
                            ? 'border-rose-500 focus:border-rose-500 focus:ring-rose-500/30'
                            : 'border-[#1b2f52] focus:border-cyan-500 focus:ring-cyan-500/30',
                        )}
                      />
                    </div>
                    {validationErrors.address && (
                      <p className="text-xs text-rose-400 mt-1">{validationErrors.address}</p>
                    )}
                  </div>
                </form>
              )}
            </CardContent>
          </Card>

          {/* Card 3: Security & Enterprise Governance (Read-Only) */}
          <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-xl overflow-hidden text-white">
            <CardHeader className="border-b border-[#1e3458]/70 px-6 py-5">
              <CardTitle className="text-base font-bold flex items-center gap-2.5 text-white">
                <ShieldCheck className="size-4.5 text-cyan-400" />
                <span>Enterprise Security & Account Privileges</span>
              </CardTitle>
              <CardDescription className="text-xs text-slate-400">
                System privileges and authentication identity bound to your corporate account.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-6 space-y-5">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    System User ID
                  </span>
                  <strong className="text-base font-bold text-white font-mono block">
                    #{profile.userId}
                  </strong>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Username
                  </span>
                  <strong className="text-base font-bold text-cyan-400 font-mono block">
                    @{profile.userName}
                  </strong>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Role Privileges
                  </span>
                  <div className="pt-0.5">
                    <StatusBadge status={user?.role || 'Staff'} />
                  </div>
                </div>

                <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Account Status
                  </span>
                  <div className="pt-0.5">
                    <StatusBadge status={profile.isActive ? 'Active' : 'Inactive'} />
                  </div>
                </div>
              </div>

              {/* Security Governance Notice */}
              <div className="flex items-start gap-3.5 p-4 rounded-xl bg-[#111c33] border border-blue-500/20 text-xs text-slate-300">
                <ShieldAlert className="size-5 text-cyan-400 shrink-0 mt-0.5" />
                <div className="space-y-0.5 leading-relaxed">
                  <strong className="text-white font-semibold block text-xs">
                    Security Governance & Authorization Policy:
                  </strong>
                  <span className="text-slate-300 text-xs">
                    User IDs, usernames, and enterprise role assignments are strictly managed by system administrators in compliance with national transport registry governance. To request permission changes, submit an administrative ticket to DVLD IT Support.
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </section>
  )
}
