import { useEffect, useState, useMemo } from 'react'
import type { FormEvent, ChangeEvent } from 'react'
import {
  User,
  UserCheck,
  Save,
  Upload,
  Trash2,
  Phone as PhoneIcon,
  MapPin,
  Mail,
  ShieldCheck,
} from 'lucide-react'
import { getCountries } from '../services/countryService'
import type { Country } from '../types/Country'
import { createPerson } from '../services/peopleService'
import { sanitizePersonName, validatePersonName } from '../utils/nameValidation'
import PageHero from './common/PageHero'
import ErrorAlert from './common/ErrorAlert'
import LoadingSpinner from './common/LoadingSpinner'
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
} from './ui'

export type AddPersonPageProps = {
  accessToken: string
  onBack: () => void
  onPersonAdded: () => void
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

export default function AddPersonPage({
  accessToken,
  onBack,
  onPersonAdded,
}: AddPersonPageProps) {
  const maxDateOfBirth = useMemo(() => calculateEighteenYearsAgo(), [])
  const minDateOfBirth = useMemo(() => calculateMinDateOfBirth(), [])

  const [countries, setCountries] = useState<Country[]>([])
  const [loadingCountries, setLoadingCountries] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [errorField, setErrorField] = useState<string | null>(null)

  const [gender, setGender] = useState('0')
  const [nationalityCountryID, setNationalityCountryID] = useState('')
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState('')

  // Form field state for preserving values on failed submission
  const [nationalNo, setNationalNo] = useState('')
  const [firstName, setFirstName] = useState('')
  const [secondName, setSecondName] = useState('')
  const [thirdName, setThirdName] = useState('')
  const [lastName, setLastName] = useState('')
  const [dateOfBirth, setDateOfBirth] = useState(maxDateOfBirth)
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [email, setEmail] = useState('')

  function setFieldError(field: string, message: string) {
    setErrorField(field)
    setError(message)
  }

  function clearFieldError(field?: string) {
    if (!field || errorField === field) {
      setErrorField(null)
      setError('')
    }
  }

  // Load countries from the API
  useEffect(() => {
    let cancelled = false

    async function loadCountries() {
      try {
        setLoadingCountries(true)
        setError('')

        const result = await getCountries(accessToken)

        if (!cancelled) {
          setCountries(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load countries registry.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingCountries(false)
        }
      }
    }

    void loadCountries()

    return () => {
      cancelled = true
    }
  }, [accessToken])

  // Validate the selected image and generate preview
  function handleImageChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      setFieldError('image', 'Please select a valid image file (JPEG, PNG, WebP).')
      event.target.value = ''
      setImageFile(null)
      setImagePreview('')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setFieldError('image', 'Image size must not exceed 5 MB.')
      event.target.value = ''
      setImageFile(null)
      setImagePreview('')
      return
    }

    clearFieldError('image')
    setImageFile(file)

    const reader = new FileReader()
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        setImagePreview(reader.result)
      }
    }
    reader.onerror = () => {
      setFieldError('image', 'Failed to preview the selected image.')
      setImagePreview('')
    }
    reader.readAsDataURL(file)
  }

  // Remove the selected image
  function handleRemoveImage() {
    setImageFile(null)
    setImagePreview('')

    const imageInput = document.getElementById(
      'person-image-input',
    ) as HTMLInputElement | null
    if (imageInput) {
      imageInput.value = ''
    }
    clearFieldError('image')
  }

  // Validate inputs and submit person creation
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    setError('')
    setErrorField(null)

    const trimmedFirstName = firstName.trim()
    const trimmedSecondName = secondName.trim()
    const trimmedThirdName = thirdName.trim()
    const trimmedLastName = lastName.trim()
    const trimmedNationalNo = nationalNo.trim()
    const trimmedPhone = phone.trim()
    const trimmedAddress = address.trim()
    const trimmedEmail = email.trim()

    // 1. National Number validation: exactly 10 digits (top field in civil identity form)
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

    // 3. Date of Birth validation
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

    // 4. Gender validation
    if (gender !== '0' && gender !== '1') {
      setFieldError('gender', 'Please select a valid gender.')
      return
    }

    // 5. Nationality validation
    const countryIdNum = Number(nationalityCountryID)
    if (!nationalityCountryID || !Number.isInteger(countryIdNum) || countryIdNum <= 0) {
      setFieldError('nationality', 'Please select a valid nationality country.')
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

    // 7. Email validation (Optional, but if provided must be valid)
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

    setSaving(true)

    try {
      await createPerson(accessToken, {
        nationalNo: trimmedNationalNo,
        firstName: trimmedFirstName,
        secondName: trimmedSecondName,
        thirdName: trimmedThirdName || null,
        lastName: trimmedLastName,
        dateOfBirth,
        gender: Number(gender || 0),
        address: trimmedAddress,
        phone: trimmedPhone,
        email: trimmedEmail || null,
        nationalityCountryID: countryIdNum,
        imagePath: null,
      })

      onPersonAdded()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to create person record. Please verify inputs and try again.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* Page Hero Header without action buttons */}
      <PageHero
        pageId="Add Person"
        title="Add New"
        highlight="Person"
        description="Register a new citizen or resident record in the DVLD central database. Complete all required identity, contact, and residency fields."
      />

      {/* Shared Error notification banner */}
      {error && (
        <ErrorAlert
          title="Validation Notice"
          message={error}
          onDismiss={() => {
            setError('')
            setErrorField(null)
          }}
        />
      )}

      {loadingCountries ? (
        <Card className="p-12 text-center">
          <LoadingSpinner
            message="Loading nationality countries registry..."
            size="lg"
            layout="col"
          />
        </Card>
      ) : (
        <form onSubmit={handleSubmit} noValidate className="space-y-6">
          {/* Card 1: Personal Identity & Biometric Photo */}
          <Card className="p-6 sm:p-8">
            <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
              <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                <UserCheck className="size-5" />
              </div>
              <div>
                <CardTitle>Personal Identity</CardTitle>
                <CardDescription className="mt-0.5">
                  Official civil identity details matching citizen documentation
                </CardDescription>
              </div>
            </CardHeader>

            {/* Photo Upload Section */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-5 p-4 rounded-xl bg-[#091326] border border-[#1b2f52] mb-6">
              <div className="size-24 rounded-2xl border-2 border-dashed border-blue-500/40 bg-[#070f20] overflow-hidden flex items-center justify-center shrink-0 shadow-[0_0_15px_rgba(0,140,255,0.15)]">
                {imagePreview ? (
                  <img
                    src={imagePreview}
                    alt="Citizen photo preview"
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <div className="flex flex-col items-center justify-center text-slate-500">
                    <User className="size-8" />
                    <span className="text-[10px] uppercase font-bold tracking-wider mt-1">No Photo</span>
                  </div>
                )}
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
                    id="person-image-input"
                    type="file"
                    accept="image/*"
                    onChange={handleImageChange}
                    disabled={saving}
                    className="hidden"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={saving}
                    onClick={() => {
                      document.getElementById('person-image-input')?.click()
                    }}
                    className="gap-2 text-xs font-semibold text-white bg-[#0e1a33] hover:bg-[#16274a] border border-[#263e68] hover:border-cyan-400/50 rounded-lg cursor-pointer"
                  >
                    <Upload className="size-3.5 text-cyan-400" />
                    <span>{imageFile ? 'Change Photo' : 'Upload Photo'}</span>
                  </Button>

                  {imageFile && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={handleRemoveImage}
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

            {/* Identity Grid: 4 Names + National No + DOB + Gender */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
              {/* National Number (Digits only, max 10) */}
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
                />
                <span className="text-[11px] text-slate-500 mt-1 block">Citizen must be at least 18 years old.</span>
              </div>

              {/* Gender */}
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
                >
                  <SelectTrigger
                    id="person-gender"
                    aria-label="Select gender"
                    className="w-full h-11 px-3.5 rounded-xl bg-[#0e1a33] border-[#263e68] text-white text-sm hover:border-cyan-400/50 focus:border-cyan-400 focus:ring-2 focus:ring-cyan-500/25 transition-all"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="0">Male</SelectItem>
                    <SelectItem value="1">Female</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </Card>

          {/* Card 2: Contact, Residency & Nationality */}
          <Card className="p-6 sm:p-8">
            <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
              <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                <MapPin className="size-5" />
              </div>
              <div>
                <CardTitle>Contact & Residency Details</CardTitle>
                <CardDescription className="mt-0.5">
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
                  value={nationalityCountryID || undefined}
                  onValueChange={(val) => {
                    setNationalityCountryID(val)
                    clearFieldError('nationality')
                  }}
                >
                  <SelectTrigger
                    id="person-nationality"
                    aria-label="Select nationality"
                    className="w-full h-11 px-3.5 rounded-xl bg-[#0e1a33] border-[#263e68] text-white text-sm hover:border-cyan-400/50 focus:border-cyan-400 focus:ring-2 focus:ring-cyan-500/25 transition-all"
                  >
                    <SelectValue placeholder="Select country" />
                  </SelectTrigger>
                  <SelectContent className="max-h-64">
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

              {/* Phone (Digits only, Jordanian format) */}
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
                    className="pl-10 pr-3.5"
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
                <Input
                  id="person-address"
                  type="text"
                  required
                  maxLength={200}
                  placeholder="Enter detailed residence address: City, District, Street / Building No."
                  value={address}
                  error={errorField === 'address'}
                  onChange={(e) => {
                    setAddress(e.target.value)
                    clearFieldError('address')
                  }}
                  disabled={saving}
                />
              </div>
            </div>
          </Card>

          {/* Card 3: Action Buttons & Security Assurance */}
          <Card className="p-5 sm:p-6 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2.5 text-xs text-slate-400">
              <ShieldCheck className="size-4.5 text-emerald-400 shrink-0" />
              <span>All citizen records are validated and encrypted under the Public Security Directorate.</span>
            </div>

            <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
              <Button
                type="button"
                variant="outline"
                size="default"
                onClick={onBack}
                disabled={saving}
                className="h-11 px-5 text-sm font-semibold text-slate-300 bg-[#0e1a33] hover:bg-[#16274a] border border-[#263e68] hover:border-slate-500 rounded-xl cursor-pointer transition-all"
              >
                Cancel
              </Button>

              <Button
                type="submit"
                disabled={saving || loadingCountries}
                className="h-11 px-6 gap-2 text-sm font-bold text-white bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:via-cyan-500 hover:to-cyan-400 border-none rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] hover:shadow-[0_0_25px_rgba(0,229,255,0.6)] cursor-pointer transition-all"
              >
                {saving ? (
                  <>
                    <span className="size-4 rounded-full border-2 border-white/30 border-t-white animate-spin" aria-hidden="true" />
                    <span>Saving Person...</span>
                  </>
                ) : (
                  <>
                    <Save className="size-4" />
                    <span>Save Person Record</span>
                  </>
                )}
              </Button>
            </div>
          </Card>
        </form>
      )}
    </section>
  )
}
