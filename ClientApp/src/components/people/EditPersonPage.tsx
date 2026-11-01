import { useState, useEffect, useMemo, type FormEvent, type ChangeEvent } from 'react'
import {
  Save,
  ArrowLeft,
  User,
  IdCard,
  MapPin,
  Phone,
  Mail,
  Calendar,
  Globe,
  Sparkles,
} from 'lucide-react'
import { getCountries } from '../../services/countryService'
import type { Country } from '../../types/Country'
import { getPersonById, updatePerson } from '../../services/peopleService'
import { sanitizePersonName, validatePersonName } from '../../utils/nameValidation'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import { Card, CardHeader, CardTitle, CardDescription } from '../ui/card'
import { Input } from '../ui/input'
import { Label } from '../ui/label'
import { Button } from '../ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'

export type EditPersonPageProps = {
  accessToken: string
  personId: number | null
  onBack: () => void
  onPersonUpdated: () => void
}

function calculateEighteenYearsAgo(): string {
  const d = new Date()
  d.setFullYear(d.getFullYear() - 18)
  return d.toISOString().split('T')[0]
}

function calculateMinDateOfBirth(): string {
  const d = new Date()
  d.setFullYear(d.getFullYear() - 120)
  return d.toISOString().split('T')[0]
}

export default function EditPersonPage({
  accessToken,
  personId,
  onBack,
  onPersonUpdated,
}: EditPersonPageProps) {
  const maxDateOfBirth = useMemo(() => calculateEighteenYearsAgo(), [])
  const minDateOfBirth = useMemo(() => calculateMinDateOfBirth(), [])

  const [countries, setCountries] = useState<Country[]>([])
  const [loadingCountries, setLoadingCountries] = useState(true)
  const [loadingPerson, setLoadingPerson] = useState(Boolean(personId))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(!personId ? 'No person ID was provided for editing.' : '')
  const [errorField, setErrorField] = useState<string | null>(null)

  const [gender, setGender] = useState('0')
  const [nationalityCountryID, setNationalityCountryID] = useState('')
  const [imagePreview, setImagePreview] = useState('')

  // Form field state
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

  // Load countries
  useEffect(() => {
    let cancelled = false

    async function loadCountries() {
      try {
        setLoadingCountries(true)
        const result = await getCountries(accessToken)
        if (!cancelled) {
          setCountries(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load nationality countries.',
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

  // Load person data
  useEffect(() => {
    if (!personId) {
      return
    }

    let cancelled = false

    async function loadPerson() {
      try {
        setLoadingPerson(true)
        setError('')
        const p = await getPersonById(accessToken, personId!)
        if (cancelled) return

        setNationalNo(p.nationalNo || '')
        setFirstName(p.firstName || '')
        setSecondName(p.secondName || '')
        setThirdName(p.thirdName || '')
        setLastName(p.lastName || '')
        if (p.dateOfBirth) {
          setDateOfBirth(p.dateOfBirth.split('T')[0])
        }
        setGender(String(p.gender ?? 0))
        setPhone(p.phone || '')
        setAddress(p.address || '')
        setEmail(p.email || '')
        if (p.nationalityCountryID) {
          setNationalityCountryID(String(p.nationalityCountryID))
        }
        if (p.imagePath) {
          setImagePreview(p.imagePath)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : `Failed to load details for person #${personId}. Record may not exist.`,
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingPerson(false)
        }
      }
    }

    void loadPerson()

    return () => {
      cancelled = true
    }
  }, [accessToken, personId])

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!personId) return

    clearFieldError()

    const trimmedNationalNo = nationalNo.trim()
    const trimmedFirstName = firstName.trim()
    const trimmedSecondName = secondName.trim()
    const trimmedThirdName = thirdName.trim()
    const trimmedLastName = lastName.trim()
    const trimmedPhone = phone.trim()
    const trimmedEmail = email.trim()
    const trimmedAddress = address.trim()

    // 1. National Number validation
    if (!trimmedNationalNo) {
      setFieldError('nationalNo', 'National Number is required.')
      return
    }
    if (!/^\d{10}$/.test(trimmedNationalNo)) {
      setFieldError('nationalNo', 'National Number must be exactly 10 digits.')
      return
    }

    // 2. First Name validation
    const firstNameValidation = validatePersonName(trimmedFirstName, 'First Name', true)
    if (!firstNameValidation.valid) {
      setFieldError('firstName', firstNameValidation.error ?? 'First Name is invalid.')
      return
    }

    // 3. Second Name validation
    const secondNameValidation = validatePersonName(trimmedSecondName, 'Second Name', true)
    if (!secondNameValidation.valid) {
      setFieldError('secondName', secondNameValidation.error ?? 'Second Name is invalid.')
      return
    }

    // 4. Third Name validation (optional)
    if (trimmedThirdName) {
      const thirdNameValidation = validatePersonName(trimmedThirdName, 'Third Name', false)
      if (!thirdNameValidation.valid) {
        setFieldError('thirdName', thirdNameValidation.error ?? 'Third Name is invalid.')
        return
      }
    }

    // 5. Last Name validation
    const lastNameValidation = validatePersonName(trimmedLastName, 'Last Name', true)
    if (!lastNameValidation.valid) {
      setFieldError('lastName', lastNameValidation.error ?? 'Last Name is invalid.')
      return
    }

    // 6. Date of birth validation
    if (!dateOfBirth) {
      setFieldError('dateOfBirth', 'Date of Birth is required.')
      return
    }
    const dob = new Date(dateOfBirth)
    const eighteenAgo = new Date(maxDateOfBirth)
    if (dob > eighteenAgo) {
      setFieldError('dateOfBirth', 'Person must be at least 18 years of age.')
      return
    }

    // 7. Nationality country validation
    const countryIdNum = Number(nationalityCountryID)
    if (!nationalityCountryID || Number.isNaN(countryIdNum) || countryIdNum <= 0) {
      setFieldError('nationalityCountryID', 'Please select a Nationality Country.')
      return
    }

    // 8. Phone Number validation
    if (!trimmedPhone) {
      setFieldError('phone', 'Phone Number is required.')
      return
    }
    if (trimmedPhone.length < 5) {
      setFieldError('phone', 'Phone Number must be at least 5 characters.')
      return
    }

    // 9. Email validation (optional)
    if (trimmedEmail) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(trimmedEmail)) {
        setFieldError('email', 'Email Address format is invalid.')
        return
      }
    }

    // 10. Residential Address validation
    if (!trimmedAddress) {
      setFieldError('address', 'Residential Address is required.')
      return
    }

    setSaving(true)

    try {
      await updatePerson(accessToken, personId, {
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

      onPersonUpdated()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : `Failed to update person record #${personId}. Please verify inputs and try again.`,
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      <PageHero
        pageId="Edit Person"
        title="Edit"
        highlight="Person"
        description={
          personId
            ? `Update identity, contact, and residency details for person #${personId}.`
            : 'Update person record in the DVLD central database.'
        }
      />

      <div className="flex items-center">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <ArrowLeft className="size-4" />
          <span>Back to People Directory</span>
        </Button>
      </div>

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

      {loadingCountries || loadingPerson ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <LoadingSpinner
            message={
              loadingPerson
                ? `Loading details for person #${personId ?? ''}...`
                : 'Loading nationality countries registry...'
            }
            size="lg"
            layout="col"
          />
        </Card>
      ) : (
        <form onSubmit={handleSubmit} noValidate className="space-y-6">
          {/* Card 1: Personal Identity */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-6 sm:p-8">
            <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
              <div className="flex size-10 items-center justify-center rounded-xl bg-blue-500/10 text-cyan-400 border border-blue-500/20">
                <IdCard className="size-5" />
              </div>
              <div>
                <CardTitle className="text-lg text-white">Personal Identity</CardTitle>
                <CardDescription className="text-slate-400">
                  Update official identity registration for person #{personId}
                </CardDescription>
              </div>
            </CardHeader>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
              <div className="lg:col-span-3 space-y-5">
                {/* National No */}
                <div className="max-w-xs space-y-1.5">
                  <Label htmlFor="nationalNo" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                    <IdCard className="size-3.5 text-cyan-400" />
                    <span>National No.</span>
                    <span className="text-red-400">*</span>
                  </Label>
                  <Input
                    id="nationalNo"
                    type="text"
                    required
                    maxLength={10}
                    placeholder="10-digit National ID"
                    value={nationalNo}
                    onChange={(e: ChangeEvent<HTMLInputElement>) => {
                      clearFieldError('nationalNo')
                      setNationalNo(e.target.value.replace(/\D/g, '').slice(0, 10))
                    }}
                    className={`font-mono text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                      errorField === 'nationalNo' ? 'border-red-500 ring-1 ring-red-500' : ''
                    }`}
                  />
                </div>

                {/* 4 Name Fields */}
                <div className="space-y-1.5">
                  <Label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                    <User className="size-3.5 text-cyan-400" />
                    <span>Full Legal Name</span>
                    <span className="text-red-400">*</span>
                  </Label>
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
                    <div>
                      <Input
                        id="firstName"
                        type="text"
                        required
                        placeholder="First Name *"
                        value={firstName}
                        onChange={(e: ChangeEvent<HTMLInputElement>) => {
                          clearFieldError('firstName')
                          setFirstName(sanitizePersonName(e.target.value))
                        }}
                        className={`text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                          errorField === 'firstName' ? 'border-red-500 ring-1 ring-red-500' : ''
                        }`}
                      />
                    </div>
                    <div>
                      <Input
                        id="secondName"
                        type="text"
                        required
                        placeholder="Second Name *"
                        value={secondName}
                        onChange={(e: ChangeEvent<HTMLInputElement>) => {
                          clearFieldError('secondName')
                          setSecondName(sanitizePersonName(e.target.value))
                        }}
                        className={`text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                          errorField === 'secondName' ? 'border-red-500 ring-1 ring-red-500' : ''
                        }`}
                      />
                    </div>
                    <div>
                      <Input
                        id="thirdName"
                        type="text"
                        placeholder="Third Name (optional)"
                        value={thirdName}
                        onChange={(e: ChangeEvent<HTMLInputElement>) => {
                          clearFieldError('thirdName')
                          setThirdName(sanitizePersonName(e.target.value))
                        }}
                        className={`text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                          errorField === 'thirdName' ? 'border-red-500 ring-1 ring-red-500' : ''
                        }`}
                      />
                    </div>
                    <div>
                      <Input
                        id="lastName"
                        type="text"
                        required
                        placeholder="Last Name *"
                        value={lastName}
                        onChange={(e: ChangeEvent<HTMLInputElement>) => {
                          clearFieldError('lastName')
                          setLastName(sanitizePersonName(e.target.value))
                        }}
                        className={`text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                          errorField === 'lastName' ? 'border-red-500 ring-1 ring-red-500' : ''
                        }`}
                      />
                    </div>
                  </div>
                </div>

                {/* DOB & Gender */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-1">
                  <div className="space-y-1.5">
                    <Label htmlFor="dateOfBirth" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                      <Calendar className="size-3.5 text-cyan-400" />
                      <span>Date of Birth</span>
                      <span className="text-red-400">* (Min 18 yrs)</span>
                    </Label>
                    <Input
                      id="dateOfBirth"
                      type="date"
                      required
                      min={minDateOfBirth}
                      max={maxDateOfBirth}
                      value={dateOfBirth}
                      onChange={(e: ChangeEvent<HTMLInputElement>) => {
                        clearFieldError('dateOfBirth')
                        setDateOfBirth(e.target.value)
                      }}
                      className={`font-mono text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                        errorField === 'dateOfBirth' ? 'border-red-500 ring-1 ring-red-500' : ''
                      }`}
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                      <User className="size-3.5 text-cyan-400" />
                      <span>Gender</span>
                      <span className="text-red-400">*</span>
                    </Label>
                    <div className="flex items-center gap-4 h-10 px-3 rounded-xl bg-[#081222] border border-[#223553]">
                      <label className="flex items-center gap-2 cursor-pointer text-sm font-medium text-slate-200">
                        <input
                          type="radio"
                          name="edit-person-gender"
                          value="0"
                          checked={gender === '0'}
                          onChange={() => setGender('0')}
                          className="accent-cyan-400 cursor-pointer"
                        />
                        <span>Male</span>
                      </label>
                      <label className="flex items-center gap-2 cursor-pointer text-sm font-medium text-slate-200">
                        <input
                          type="radio"
                          name="edit-person-gender"
                          value="1"
                          checked={gender === '1'}
                          onChange={() => setGender('1')}
                          className="accent-cyan-400 cursor-pointer"
                        />
                        <span>Female</span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>

              {/* Photo placeholder */}
              <div className="flex flex-col items-center justify-center p-4 rounded-xl border border-dashed border-[#223553] bg-[#071324] space-y-3">
                <div className="relative size-24 rounded-full overflow-hidden border-2 border-[#1e3458] bg-[#0d1d36] flex items-center justify-center text-slate-400">
                  {imagePreview ? (
                    <img src={imagePreview} alt="Person" className="size-full object-cover" />
                  ) : (
                    <User className="size-12 text-slate-500" />
                  )}
                </div>
                <div className="text-center">
                  <p className="text-xs font-semibold text-slate-300">Biometric Photo</p>
                  <p className="text-[10px] text-slate-500">Stored on file</p>
                </div>
              </div>
            </div>
          </Card>

          {/* Card 2: Contact & Residency */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-6 sm:p-8">
            <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-6 space-y-0">
              <div className="flex size-10 items-center justify-center rounded-xl bg-blue-500/10 text-cyan-400 border border-blue-500/20">
                <MapPin className="size-5" />
              </div>
              <div>
                <CardTitle className="text-lg text-white">Contact & Residency</CardTitle>
                <CardDescription className="text-slate-400">
                  Communication channels and address details
                </CardDescription>
              </div>
            </CardHeader>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              {/* Nationality Country */}
              <div className="space-y-1.5">
                <Label htmlFor="nationalityCountry" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <Globe className="size-3.5 text-cyan-400" />
                  <span>Nationality Country</span>
                  <span className="text-red-400">*</span>
                </Label>
                <Select
                  value={nationalityCountryID}
                  onValueChange={(val) => {
                    clearFieldError('nationalityCountryID')
                    setNationalityCountryID(val)
                  }}
                >
                  <SelectTrigger
                    id="nationalityCountry"
                    className={`w-full bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                      errorField === 'nationalityCountryID' ? 'border-red-500 ring-1 ring-red-500' : ''
                    }`}
                  >
                    <SelectValue placeholder="Select country..." />
                  </SelectTrigger>
                  <SelectContent className="max-h-60 bg-[#0c1930] border-[#223553] text-white">
                    {countries.map((c) => (
                      <SelectItem key={c.countryId} value={String(c.countryId)}>
                        {c.countryName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Phone */}
              <div className="space-y-1.5">
                <Label htmlFor="phone" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <Phone className="size-3.5 text-cyan-400" />
                  <span>Phone Number</span>
                  <span className="text-red-400">*</span>
                </Label>
                <Input
                  id="phone"
                  type="tel"
                  required
                  placeholder="+962 7X XXX XXXX"
                  value={phone}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => {
                    clearFieldError('phone')
                    setPhone(e.target.value)
                  }}
                  className={`font-mono text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                    errorField === 'phone' ? 'border-red-500 ring-1 ring-red-500' : ''
                  }`}
                />
              </div>

              {/* Email */}
              <div className="space-y-1.5">
                <Label htmlFor="email" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <Mail className="size-3.5 text-cyan-400" />
                  <span>Email Address</span>
                  <span className="text-slate-500">(Optional)</span>
                </Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="citizen@domain.gov"
                  value={email}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => {
                    clearFieldError('email')
                    setEmail(e.target.value)
                  }}
                  className={`text-sm bg-[#081222] border-[#223553] text-white focus:border-cyan-400 rounded-xl ${
                    errorField === 'email' ? 'border-red-500 ring-1 ring-red-500' : ''
                  }`}
                />
              </div>

              {/* Residential Address */}
              <div className="space-y-1.5 md:col-span-2">
                <Label htmlFor="address" className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <MapPin className="size-3.5 text-cyan-400" />
                  <span>Residential Address</span>
                  <span className="text-red-400">*</span>
                </Label>
                <textarea
                  id="address"
                  required
                  rows={3}
                  placeholder="Street name, building number, district, city..."
                  value={address}
                  onChange={(e) => {
                    clearFieldError('address')
                    setAddress(e.target.value)
                  }}
                  className={`w-full p-3 text-sm bg-[#081222] border border-[#223553] text-white focus:border-cyan-400 focus:outline-none rounded-xl transition-all ${
                    errorField === 'address' ? 'border-red-500 ring-1 ring-red-500' : ''
                  }`}
                />
              </div>
            </div>
          </Card>

          {/* Form Actions Card */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-4 sm:p-6">
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
              <div className="flex items-center gap-2 text-xs text-slate-400">
                <Sparkles className="size-4 text-cyan-400" />
                <span>All changes will be updated directly in the DVLD central database.</span>
              </div>

              <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
                <Button
                  type="button"
                  variant="outline"
                  onClick={onBack}
                  disabled={saving}
                  className="h-11 px-5 text-sm font-semibold text-slate-300 bg-[#0e1a33] hover:bg-[#16274a] border border-[#263e68] hover:border-slate-500 rounded-xl cursor-pointer transition-all"
                >
                  Cancel
                </Button>

                <Button
                  type="submit"
                  disabled={saving || loadingCountries || loadingPerson}
                  className="h-11 px-6 gap-2 text-sm font-bold text-white bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:via-cyan-500 hover:to-cyan-400 border-none rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] hover:shadow-[0_0_25px_rgba(0,229,255,0.6)] cursor-pointer transition-all"
                >
                  {saving ? (
                    <>
                      <span className="size-4 rounded-full border-2 border-white/30 border-t-white animate-spin" aria-hidden="true" />
                      <span>Saving Changes...</span>
                    </>
                  ) : (
                    <>
                      <Save className="size-4" />
                      <span>Save Changes</span>
                    </>
                  )}
                </Button>
              </div>
            </div>
          </Card>
        </form>
      )}
    </section>
  )
}
