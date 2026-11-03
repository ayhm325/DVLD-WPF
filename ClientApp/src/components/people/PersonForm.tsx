import { useState, useMemo } from 'react'
import type { FormEvent, ChangeEvent } from 'react'
import {
  UserCheck,
  Save,
  Upload,
  Trash2,
  Phone as PhoneIcon,
  MapPin,
  Mail,
  ShieldCheck,
} from 'lucide-react'
import type { Country } from '../../types/Country'
import { sanitizePersonName, validatePersonName } from '../../utils/nameValidation'
import ErrorAlert from '../common/ErrorAlert'
import PersonPhoto from '../common/PersonPhoto'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '../ui'

export type PersonFormMode = 'create' | 'edit'

export interface PersonFormInitialValues {
  nationalNo?: string
  firstName?: string
  secondName?: string
  thirdName?: string | null
  lastName?: string
  dateOfBirth?: string
  gender?: number | string
  nationalityCountryID?: number | string
  phone?: string
  email?: string | null
  address?: string
  imagePath?: string | null
}

export interface PersonFormSubmitValues {
  nationalNo: string
  firstName: string
  secondName: string
  thirdName: string | null
  lastName: string
  dateOfBirth: string
  gender: number
  nationalityCountryID: number
  phone: string
  email: string | null
  address: string
  photoFile: File | null
  isPhotoRemoved: boolean
}

export interface PersonFormProps {
  mode: PersonFormMode
  initialValues?: PersonFormInitialValues
  countries: Country[]
  loadingCountries?: boolean
  personId?: number | null
  accessToken?: string
  saving: boolean
  error?: string | null
  onClearError?: () => void
  onSubmit: (values: PersonFormSubmitValues) => void | Promise<void>
  onCancel: () => void
  submitLabel?: string
  cancelLabel?: string
}

/**
 * Calculates the maximum selectable Date of Birth, which is exactly 18 years
 * before the local current date. This also serves as the default form value.
 * Safely handles leap years (e.g. Feb 29 rolls back to Feb 28 on non-leap years)
 * and avoids UTC timezone conversion shifts.
 */
function calculateEighteenYearsAgo(currentDate: Date = new Date()): string {
  const targetYear = currentDate.getFullYear() - 18
  const targetMonth = currentDate.getMonth()
  const targetDay = currentDate.getDate()

  const cutoffDate = new Date(targetYear, targetMonth, targetDay)
  if (cutoffDate.getMonth() !== targetMonth) {
    cutoffDate.setDate(0)
  }

  const y = cutoffDate.getFullYear()
  const m = String(cutoffDate.getMonth() + 1).padStart(2, '0')
  const d = String(cutoffDate.getDate()).padStart(2, '0')

  return `${y}-${m}-${d}`
}

function calculateMinDateOfBirth(currentDate: Date = new Date()): string {
  const minYear = currentDate.getFullYear() - 120
  const m = String(currentDate.getMonth() + 1).padStart(2, '0')
  const d = String(currentDate.getDate()).padStart(2, '0')
  return `${minYear}-${m}-${d}`
}

export default function PersonForm({
  mode,
  initialValues,
  countries,
  loadingCountries = false,
  personId,
  accessToken,
  saving,
  error,
  onClearError,
  onSubmit,
  onCancel,
  submitLabel,
  cancelLabel,
}: PersonFormProps) {
  const maxDateOfBirth = useMemo(() => calculateEighteenYearsAgo(), [])
  const minDateOfBirth = useMemo(() => calculateMinDateOfBirth(), [])

  // Local validation error message & active error field
  const [validationError, setValidationError] = useState('')
  const [errorField, setErrorField] = useState<string | null>(null)

  // Photo management state
  const hasStoredPhoto = Boolean(initialValues?.imagePath)
  const storedPhotoKey = initialValues?.imagePath ?? null
  const [selectedPhotoFile, setSelectedPhotoFile] = useState<File | null>(null)
  const [localPhotoPreview, setLocalPhotoPreview] = useState<string | null>(null)
  const [isPhotoRemoved, setIsPhotoRemoved] = useState(false)

  // Default country resolution
  const defaultCountryId = useMemo(() => {
    if (initialValues?.nationalityCountryID) {
      return String(initialValues.nationalityCountryID)
    }
    if (countries.length > 0) {
      const jordan = countries.find((c) => c.countryName.toLowerCase() === 'jordan')
      return String(jordan ? jordan.countryId : countries[0].countryId)
    }
    return ''
  }, [initialValues?.nationalityCountryID, countries])

  // Form field state
  const [nationalNo, setNationalNo] = useState(initialValues?.nationalNo ?? '')
  const [firstName, setFirstName] = useState(initialValues?.firstName ?? '')
  const [secondName, setSecondName] = useState(initialValues?.secondName ?? '')
  const [thirdName, setThirdName] = useState(initialValues?.thirdName ?? '')
  const [lastName, setLastName] = useState(initialValues?.lastName ?? '')
  const [dateOfBirth, setDateOfBirth] = useState(
    initialValues?.dateOfBirth ? initialValues.dateOfBirth.split('T')[0] : maxDateOfBirth,
  )
  const [gender, setGender] = useState(String(initialValues?.gender ?? 0))
  const [nationalityCountryID, setNationalityCountryID] = useState(defaultCountryId)
  const [phone, setPhone] = useState(initialValues?.phone ?? '')
  const [address, setAddress] = useState(initialValues?.address ?? '')
  const [email, setEmail] = useState(initialValues?.email ?? '')

  // Effective nationality country ID derived if state is empty but countries became available
  const effectiveNationalityCountryID = nationalityCountryID || defaultCountryId

  function setFieldError(field: string, message: string) {
    setErrorField(field)
    setValidationError(message)
  }

  function clearFieldError(field?: string) {
    if (!field || errorField === field) {
      setErrorField(null)
      setValidationError('')
      onClearError?.()
    }
  }

  // Validate the selected image and generate preview
  function handlePhotoChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      setFieldError('image', 'Please select a valid image file (JPEG, PNG, WebP).')
      event.target.value = ''
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setFieldError('image', 'Image size must not exceed 5 MB.')
      event.target.value = ''
      return
    }

    clearFieldError('image')
    setSelectedPhotoFile(file)
    setIsPhotoRemoved(false)

    const reader = new FileReader()
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        setLocalPhotoPreview(reader.result)
      }
    }
    reader.onerror = () => {
      setFieldError('image', 'Failed to preview the selected image.')
      setLocalPhotoPreview(null)
    }
    reader.readAsDataURL(file)
  }

  // Remove the active photo (local preview or stored photo)
  function handleRemovePhoto() {
    setSelectedPhotoFile(null)
    setLocalPhotoPreview(null)
    setIsPhotoRemoved(true)
    clearFieldError('image')

    const input = document.getElementById('person-photo-input') as HTMLInputElement | null
    if (input) {
      input.value = ''
    }
  }

  // Field-level validation and form submission
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return

    setValidationError('')
    setErrorField(null)

    const trimmedNationalNo = nationalNo.trim()
    const trimmedFirstName = firstName.trim()
    const trimmedSecondName = secondName.trim()
    const trimmedThirdName = thirdName.trim()
    const trimmedLastName = lastName.trim()
    const trimmedPhone = phone.trim()
    const trimmedAddress = address.trim()
    const trimmedEmail = email.trim()

    // 1. National Number validation: exactly 10 numeric digits
    if (!trimmedNationalNo) {
      setFieldError('nationalNo', 'National Number is required.')
      return
    }
    if (trimmedNationalNo.length !== 10 || !/^\d{10}$/.test(trimmedNationalNo)) {
      setFieldError('nationalNo', 'National Number must contain exactly 10 numeric digits.')
      return
    }

    // 2. Names validation (First, Second, Last Name required; Third Name optional)
    const firstNameValidation = validatePersonName(firstName, 'First Name', true)
    if (!firstNameValidation.valid) {
      setFieldError('firstName', firstNameValidation.error!)
      return
    }

    const secondNameValidation = validatePersonName(secondName, 'Second Name', true)
    if (!secondNameValidation.valid) {
      setFieldError('secondName', secondNameValidation.error!)
      return
    }

    const thirdNameValidation = validatePersonName(thirdName, 'Third Name', false)
    if (!thirdNameValidation.valid) {
      setFieldError('thirdName', thirdNameValidation.error!)
      return
    }

    const lastNameValidation = validatePersonName(lastName, 'Last Name', true)
    if (!lastNameValidation.valid) {
      setFieldError('lastName', lastNameValidation.error!)
      return
    }

    // 3. Date of Birth validation: minimum 18 years, not in future
    if (!dateOfBirth) {
      setFieldError('dateOfBirth', 'Please select a valid Date of Birth.')
      return
    }

    const birthDate = new Date(`${dateOfBirth}T00:00:00`)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    if (birthDate > today) {
      setFieldError('dateOfBirth', 'Date of Birth cannot be in the future.')
      return
    }

    let age = today.getFullYear() - birthDate.getFullYear()
    const monthDifference = today.getMonth() - birthDate.getMonth()
    if (
      monthDifference < 0 ||
      (monthDifference === 0 && today.getDate() < birthDate.getDate())
    ) {
      age--
    }

    if (!Number.isFinite(birthDate.getTime()) || dateOfBirth > maxDateOfBirth || age < 18) {
      setFieldError('dateOfBirth', 'The citizen must be at least 18 years old to be registered.')
      return
    }

    // 4. Gender validation: 0 (Male) or 1 (Female)
    if (gender !== '0' && gender !== '1') {
      setFieldError('gender', 'Please select a valid gender.')
      return
    }

    // 5. Nationality validation
    const targetCountryId = effectiveNationalityCountryID
    const countryIdNum = Number(targetCountryId)
    if (!targetCountryId || !Number.isInteger(countryIdNum) || countryIdNum <= 0) {
      setFieldError('nationalityCountryID', 'Please select a valid nationality country.')
      return
    }

    // 6. Phone validation: Jordanian mobile (077/078/079 + 7 digits)
    if (!trimmedPhone) {
      setFieldError('phone', 'Phone number is required.')
      return
    }
    if (!/^(077|078|079)\d{7}$/.test(trimmedPhone)) {
      setFieldError(
        'phone',
        'Phone number must be a 10-digit Jordanian mobile number starting with 077, 078, or 079.',
      )
      return
    }

    // 7. Email validation (Optional, but if provided must match standard format)
    if (trimmedEmail) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(trimmedEmail)) {
        setFieldError('email', 'Please enter a valid email address (e.g. citizen@example.com).')
        return
      }
    }

    // 8. Residential Address validation
    if (!trimmedAddress) {
      setFieldError('address', 'Residential Address is required.')
      return
    }

    await onSubmit({
      nationalNo: trimmedNationalNo,
      firstName: trimmedFirstName,
      secondName: trimmedSecondName,
      thirdName: trimmedThirdName || null,
      lastName: trimmedLastName,
      dateOfBirth,
      gender: Number(gender || 0),
      nationalityCountryID: countryIdNum,
      phone: trimmedPhone,
      email: trimmedEmail || null,
      address: trimmedAddress,
      photoFile: selectedPhotoFile,
      isPhotoRemoved,
    })
  }

  const activeErrorMessage = error || validationError
  const hasPhotoToShow = Boolean(localPhotoPreview || (hasStoredPhoto && !isPhotoRemoved))

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
      {/* Unified Error Alert Banner */}
      {activeErrorMessage && (
        <ErrorAlert
          title="Validation Notice"
          message={activeErrorMessage}
          onDismiss={() => {
            setValidationError('')
            setErrorField(null)
            onClearError?.()
          }}
        />
      )}

      {/* Card 1: Personal Identity & Biometric Photo */}
      <Card className="border-[#1e3458] bg-[#0c1930] p-6 sm:p-8">
        <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
          <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
            <UserCheck className="size-5" />
          </div>
          <div>
            <CardTitle className="text-lg text-white">Personal Identity</CardTitle>
            <CardDescription className="text-slate-400 mt-0.5">
              {mode === 'create'
                ? 'Official civil identity details matching citizen documentation'
                : `Update official civil identity details for citizen #${personId ?? ''}`}
            </CardDescription>
          </div>
        </CardHeader>

        {/* Unified Photo Upload & Preview Section */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-5 p-4 rounded-xl bg-[#091326] border border-[#1b2f52] mb-6">
          <div className="shrink-0">
            <PersonPhoto
              personId={personId}
              hasPhoto={hasStoredPhoto && !isPhotoRemoved}
              photoKey={storedPhotoKey}
              localPreviewUrl={localPhotoPreview}
              fullName={`${firstName} ${lastName}`.trim() || 'Person'}
              size="xl"
              accessToken={accessToken}
            />
          </div>

          <div className="flex-1 space-y-2">
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold uppercase tracking-wider text-slate-300">Personal Photo</span>
              <span className="text-[11px] text-slate-500 font-normal">(Optional)</span>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Upload an official passport-style portrait. Format: JPEG, PNG, or WebP. Maximum size: 5 MB.
            </p>

            <div className="flex flex-wrap items-center gap-2 pt-1">
              <input
                id="person-photo-input"
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={handlePhotoChange}
                disabled={saving}
                className="hidden"
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={saving}
                onClick={() => {
                  document.getElementById('person-photo-input')?.click()
                }}
                className="gap-2 text-xs font-semibold text-white bg-[#0e1a33] hover:bg-[#16274a] border border-[#263e68] hover:border-cyan-400/50 rounded-lg cursor-pointer"
              >
                <Upload className="size-3.5 text-cyan-400" />
                <span>{hasPhotoToShow ? 'Change Photo' : 'Upload Photo'}</span>
              </Button>

              {hasPhotoToShow && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleRemovePhoto}
                  disabled={saving}
                  className="gap-1.5 text-xs text-rose-300 hover:text-white hover:bg-rose-950/40 cursor-pointer"
                >
                  <Trash2 className="size-3.5 text-rose-400" />
                  <span>Remove</span>
                </Button>
              )}
            </div>
          </div>
        </div>

        {/* Identity Grid: National No + 4 Legal Names + DOB + Gender */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {/* National Number (Digits only, exactly 10) */}
          <div className="sm:col-span-2 lg:col-span-4">
            <Label htmlFor="person-national-no" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              National Number <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="person-national-no"
              type="text"
              required
              maxLength={10}
              inputMode="numeric"
              pattern="[0-9]*"
              placeholder="Enter 10-digit civil national number (e.g. 9901023456)"
              value={nationalNo}
              error={errorField === 'nationalNo'}
              onChange={(e) => {
                const digitsOnly = e.target.value.replace(/\D/g, '').slice(0, 10)
                setNationalNo(digitsOnly)
                clearFieldError('nationalNo')
              }}
              disabled={saving}
              className="font-mono text-sm"
            />
            <span className="text-[11px] text-slate-500 mt-1 block">Exactly 10 numeric digits required.</span>
          </div>

          {/* First Name */}
          <div>
            <Label htmlFor="person-first-name" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              First Name <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="person-first-name"
              type="text"
              required
              maxLength={50}
              placeholder="Given name"
              value={firstName}
              error={errorField === 'firstName'}
              onChange={(e) => {
                const sanitized = sanitizePersonName(e.target.value)
                setFirstName(sanitized)
                clearFieldError('firstName')
              }}
              disabled={saving}
            />
          </div>

          {/* Second Name */}
          <div>
            <Label htmlFor="person-second-name" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Second Name <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="person-second-name"
              type="text"
              required
              maxLength={50}
              placeholder="Father's name"
              value={secondName}
              error={errorField === 'secondName'}
              onChange={(e) => {
                const sanitized = sanitizePersonName(e.target.value)
                setSecondName(sanitized)
                clearFieldError('secondName')
              }}
              disabled={saving}
            />
          </div>

          {/* Third Name (Optional) */}
          <div>
            <Label htmlFor="person-third-name" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Third Name <span className="text-slate-500 font-normal">(Optional)</span>
            </Label>
            <Input
              id="person-third-name"
              type="text"
              maxLength={50}
              placeholder="Grandfather's name"
              value={thirdName}
              error={errorField === 'thirdName'}
              onChange={(e) => {
                const sanitized = sanitizePersonName(e.target.value)
                setThirdName(sanitized)
                clearFieldError('thirdName')
              }}
              disabled={saving}
            />
          </div>

          {/* Last Name */}
          <div>
            <Label htmlFor="person-last-name" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Last Name <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="person-last-name"
              type="text"
              required
              maxLength={50}
              placeholder="Family name"
              value={lastName}
              error={errorField === 'lastName'}
              onChange={(e) => {
                const sanitized = sanitizePersonName(e.target.value)
                setLastName(sanitized)
                clearFieldError('lastName')
              }}
              disabled={saving}
            />
          </div>

          {/* Date of Birth */}
          <div className="sm:col-span-1 lg:col-span-2">
            <Label htmlFor="person-dob" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Date of Birth <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="person-dob"
              type="date"
              required
              max={maxDateOfBirth}
              min={minDateOfBirth}
              value={dateOfBirth}
              error={errorField === 'dateOfBirth'}
              onChange={(e) => {
                setDateOfBirth(e.target.value)
                clearFieldError('dateOfBirth')
              }}
              disabled={saving}
              className="font-mono text-sm"
            />
            <span className="text-[11px] text-slate-500 mt-1 block">Citizen must be at least 18 years old.</span>
          </div>

          {/* Standardized Gender Dropdown */}
          <div className="sm:col-span-1 lg:col-span-2">
            <Label htmlFor="person-gender" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Gender <span className="text-rose-400">*</span>
            </Label>
            <Select
              value={gender}
              onValueChange={(val) => {
                setGender(val)
                clearFieldError('gender')
              }}
              disabled={saving}
            >
              <SelectTrigger
                id="person-gender"
                aria-label="Select gender"
                className={`w-full h-11 px-3.5 rounded-xl bg-[#0e1a33] border-[#263e68] text-white text-sm hover:border-cyan-400/50 focus:border-cyan-400 focus:ring-2 focus:ring-cyan-500/25 transition-all ${
                  errorField === 'gender' ? 'border-rose-500 ring-1 ring-rose-500' : ''
                }`}
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-[#0c1930] border-[#223553] text-white">
                <SelectItem value="0">Male</SelectItem>
                <SelectItem value="1">Female</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </Card>

      {/* Card 2: Contact, Residency & Nationality */}
      <Card className="border-[#1e3458] bg-[#0c1930] p-6 sm:p-8">
        <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
          <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
            <MapPin className="size-5" />
          </div>
          <div>
            <CardTitle className="text-lg text-white">Contact & Residency Details</CardTitle>
            <CardDescription className="text-slate-400 mt-0.5">
              Verified communication phone, residential address, and nationality
            </CardDescription>
          </div>
        </CardHeader>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {/* Nationality Country */}
          <div>
            <Label htmlFor="person-nationality" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Nationality <span className="text-rose-400">*</span>
            </Label>
            <Select
              value={effectiveNationalityCountryID || undefined}
              onValueChange={(val) => {
                setNationalityCountryID(val)
                clearFieldError('nationalityCountryID')
              }}
              disabled={saving || loadingCountries}
            >
              <SelectTrigger
                id="person-nationality"
                aria-label="Select nationality"
                className={`w-full h-11 px-3.5 rounded-xl bg-[#0e1a33] border-[#263e68] text-white text-sm hover:border-cyan-400/50 focus:border-cyan-400 focus:ring-2 focus:ring-cyan-500/25 transition-all ${
                  errorField === 'nationalityCountryID' ? 'border-rose-500 ring-1 ring-rose-500' : ''
                }`}
              >
                <SelectValue placeholder="Select country" />
              </SelectTrigger>
              <SelectContent className="max-h-64 bg-[#0c1930] border-[#223553] text-white">
                {countries.map((country) => (
                  <SelectItem
                    key={country.countryId}
                    value={String(country.countryId)}
                  >
                    {country.countryName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Phone (Jordanian format: 077/078/079 + 7 digits) */}
          <div>
            <Label htmlFor="person-phone" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Phone Number <span className="text-rose-400">*</span>
            </Label>
            <div className="relative flex items-center">
              <span className="absolute left-3.5 text-slate-500 pointer-events-none" aria-hidden="true">
                <PhoneIcon className="size-4" />
              </span>
              <Input
                id="person-phone"
                type="tel"
                required
                maxLength={10}
                inputMode="numeric"
                pattern="[0-9]*"
                placeholder="07XXXXXXXX"
                value={phone}
                error={errorField === 'phone'}
                onChange={(e) => {
                  const digitsOnly = e.target.value.replace(/\D/g, '').slice(0, 10)
                  setPhone(digitsOnly)
                  clearFieldError('phone')
                }}
                disabled={saving}
                className="pl-10 pr-3.5 font-mono"
              />
            </div>
            <span className="text-[11px] text-slate-500 mt-1 block">Jordanian format: 077, 078, or 079 + 7 digits</span>
          </div>

          {/* Email (Optional) */}
          <div>
            <Label htmlFor="person-email" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Email Address <span className="text-slate-500 font-normal">(Optional)</span>
            </Label>
            <div className="relative flex items-center">
              <span className="absolute left-3.5 text-slate-500 pointer-events-none" aria-hidden="true">
                <Mail className="size-4" />
              </span>
              <Input
                id="person-email"
                type="email"
                maxLength={100}
                placeholder="citizen@example.com"
                value={email}
                error={errorField === 'email'}
                onChange={(e) => {
                  setEmail(e.target.value)
                  clearFieldError('email')
                }}
                disabled={saving}
                className="pl-10 pr-3.5"
              />
            </div>
          </div>

          {/* Residential Address */}
          <div className="sm:col-span-2 lg:col-span-3">
            <Label htmlFor="person-address" className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2">
              Residential Address <span className="text-rose-400">*</span>
            </Label>
            <Textarea
              id="person-address"
              required
              rows={2}
              maxLength={500}
              placeholder="Enter detailed residence address: City, District, Street / Building No."
              value={address}
              error={errorField === 'address'}
              onChange={(e) => {
                setAddress(e.target.value)
                clearFieldError('address')
              }}
              disabled={saving}
              className="min-h-[72px]"
            />
          </div>
        </div>
      </Card>

      {/* Card 3: Action Buttons & Security Assurance */}
      <Card className="border-[#1e3458] bg-[#0c1930] p-5 sm:p-6 flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-2.5 text-xs text-slate-400">
          <ShieldCheck className="size-4.5 text-emerald-400 shrink-0" />
          <span>
            {mode === 'create'
              ? 'All citizen records are validated and encrypted under the Public Security Directorate.'
              : 'All changes will be updated directly in the DVLD central database.'}
          </span>
        </div>

        <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
          <Button
            type="button"
            variant="outline"
            size="default"
            onClick={onCancel}
            disabled={saving}
            className="h-11 px-5 text-sm font-semibold text-slate-300 bg-[#0e1a33] hover:bg-[#16274a] border border-[#263e68] hover:border-slate-500 rounded-xl cursor-pointer transition-all"
          >
            {cancelLabel || 'Cancel'}
          </Button>

          <Button
            type="submit"
            disabled={saving || loadingCountries}
            className="h-11 px-6 gap-2 text-sm font-bold text-white bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:via-cyan-500 hover:to-cyan-400 border-none rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] hover:shadow-[0_0_25px_rgba(0,229,255,0.6)] cursor-pointer transition-all"
          >
            {saving ? (
              <>
                <span className="size-4 rounded-full border-2 border-white/30 border-t-white animate-spin" aria-hidden="true" />
                <span>{mode === 'create' ? 'Saving Person...' : 'Saving Changes...'}</span>
              </>
            ) : (
              <>
                <Save className="size-4" />
                <span>{submitLabel || (mode === 'create' ? 'Save Person Record' : 'Save Changes')}</span>
              </>
            )}
          </Button>
        </div>
      </Card>
    </form>
  )
}
