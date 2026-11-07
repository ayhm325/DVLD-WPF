import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  Lock,
  CheckCircle2,
  AlertTriangle,
  Loader2,
  ShieldAlert,
  IdCard,
  Calendar,
  CalendarCheck,
  Coins,
  DollarSign,
  Hash,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/License'
import { getLicenseDetailsById } from '../../services/licenseService'
import { detainLicense } from '../../services/detainedLicenseService'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { isLicenseExpiredInJordan } from '../../utils/jordanTime'
import { cn } from '@/lib/utils'
import PageHero from '../common/PageHero'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import ErrorAlert from '../common/ErrorAlert'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  Input,
  Label,
} from '../ui'

type Props = {
  accessToken: string
  onBack: () => void
  onDetained?: (detainId: number) => void
}

export default function DetainLicensePage({ accessToken, onBack, onDetained }: Props) {
  const [licenseIdInput, setLicenseIdInput] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [fineFeesInput, setFineFeesInput] = useState('0')
  const [fineError, setFineError] = useState('')
  const [detainId, setDetainId] = useState<number | null>(null)
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [successNotice, setSuccessNotice] = useState('')

  function handleLicenseIdChange(val: string) {
    setLicenseIdInput(val)
    setError('')
    setFineError('')
    setSuccessNotice('')
    setLicense(null)
    setDetainId(null)
    setFineFeesInput('0')
  }

  function handleFineFeesChange(val: string) {
    setFineError('')
    if (val === '') {
      setFineFeesInput('')
      return
    }
    if (val.includes('-')) {
      setFineError('Fine fee cannot be negative.')
      return
    }
    if (/^\d*\.?\d{0,2}$/.test(val)) {
      setFineFeesInput(val)
    }
  }

  async function handleSearch(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (loading) return

    setError('')
    setFineError('')
    setSuccessNotice('')
    setLicense(null)
    setDetainId(null)

    const id = Number(licenseIdInput.trim())
    if (!Number.isInteger(id) || id <= 0) {
      setError('Please enter a valid positive license ID.')
      return
    }

    setLoading(true)
    try {
      const details = await getLicenseDetailsById(accessToken, id)
      setLicense(details)
      setFineFeesInput('0')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not find driving license details.')
    } finally {
      setLoading(false)
    }
  }

  async function handleDetain() {
    if (!license || submitting || detainId) return

    const trimmed = fineFeesInput.trim()
    if (!trimmed) {
      setFineError('Detention fine fee is required. Please enter an amount.')
      setError('Detention fine fee is required. Please enter an amount.')
      return
    }

    const fine = parseFloat(trimmed)
    if (isNaN(fine) || fine < 0) {
      setFineError('Please enter a valid non-negative fine fee amount.')
      setError('Please enter a valid non-negative fine fee amount.')
      return
    }

    setSubmitting(true)
    setError('')
    setFineError('')
    setSuccessNotice('')

    try {
      const response = await detainLicense(accessToken, {
        licenseId: license.licenseId,
        fineFees: fine,
      })

      setDetainId(response.detainId)
      setSuccessNotice(
        `License #${license.licenseId} detained successfully! Detention Record ID: #${response.detainId}`,
      )
      onDetained?.(response.detainId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to detain the driving license.')
    } finally {
      setSubmitting(false)
    }
  }

  // Authoritative Jordan-time expiration check
  const isExpired = license ? isLicenseExpiredInJordan(license.expirationDate) : false
  const isInactive = license ? !license.isActive : false
  const isDetained = license ? license.isDetained : false

  // Ineligibility reason if any
  let ineligibilityReason: string | null = null
  if (license) {
    if (isDetained) {
      ineligibilityReason = 'This license is already detained.'
    } else if (isInactive) {
      ineligibilityReason = 'Only an active license can be detained.'
    } else if (isExpired) {
      ineligibilityReason =
        'An expired license cannot be detained. A license remains eligible regarding expiration throughout its expiration date in Jordan local time.'
    }
  }

  const fineTrimmed = fineFeesInput.trim()
  const fineAmount = parseFloat(fineTrimmed)
  const isFineValid = fineTrimmed !== '' && !isNaN(fineAmount) && fineAmount >= 0
  const canSubmit = Boolean(
    license && !ineligibilityReason && isFineValid && !detainId && !submitting,
  )

  return (
    <section className="page-content space-y-6">
      <PageHero
        pageId="Detain License"
        title="Detain Driving"
        highlight="License"
        description="Record an official license detention, set fine fees, and immediately transition the license to inactive state."
      />

      <div className="flex items-center">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="h-9 gap-2 text-xs font-semibold text-white bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-blue-500/40 rounded-xl shadow-xs transition-all cursor-pointer"
        >
          <ArrowLeft className="size-4 text-cyan-400" />
          <span>Back to Detained Licenses</span>
        </Button>
      </div>

      {error && <ErrorAlert title="Detention Notice" message={error} />}

      {successNotice && (
        <div className="flex items-start gap-3 p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Detention Registered</h4>
            <p className="text-xs text-emerald-300/90">{successNotice}</p>
          </div>
        </div>
      )}

      {/* License Search Card */}
      <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-[#1b2f52]/60 pb-4">
          <CardTitle className="text-base font-bold flex items-center gap-2">
            <Search className="size-4 text-cyan-400" />
            <span>Find License to Detain</span>
          </CardTitle>
          <CardDescription className="text-xs text-slate-400">
            Enter the License ID of the active license to place in detention.
          </CardDescription>
        </CardHeader>
        <div className="p-6">
          <form
            onSubmit={handleSearch}
            className="flex flex-col sm:flex-row items-stretch sm:items-end gap-3 max-w-lg"
          >
            <div className="flex-1 space-y-1.5">
              <Label htmlFor="licenseIdInput" className="text-xs font-semibold text-slate-300">
                License ID
              </Label>
              <div className="relative">
                <span className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 text-xs font-mono font-bold">
                  #
                </span>
                <Input
                  id="licenseIdInput"
                  type="number"
                  min="1"
                  placeholder="e.g. 10"
                  value={licenseIdInput}
                  onChange={(e) => handleLicenseIdChange(e.target.value)}
                  disabled={loading || submitting}
                  className="h-10.5 pl-8.5 bg-[#081326] border-[#1b2f52] text-white focus:border-cyan-500 rounded-xl font-mono text-sm"
                />
              </div>
            </div>
            <Button
              type="submit"
              disabled={loading || !licenseIdInput.trim() || submitting}
              className="h-10.5 px-6 bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-md shrink-0 disabled:opacity-50"
            >
              {loading ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  <span>Searching...</span>
                </>
              ) : (
                <>
                  <Search className="size-4" />
                  <span>Search License</span>
                </>
              )}
            </Button>
          </form>
        </div>
      </Card>

      {/* License & Detention Details Card */}
      {license && (
        <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden space-y-6">
          <CardHeader className="border-b border-[#1b2f52]/60 pb-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <CardTitle className="text-base font-bold flex items-center gap-2">
                  <IdCard className="size-4 text-cyan-400" />
                  <span>License Information</span>
                </CardTitle>
                <CardDescription className="text-xs text-slate-400">
                  Verify the owner details and set the applicable fine amount.
                </CardDescription>
              </div>

              {/* Status Badges - Using shared StatusBadge component */}
              <div className="flex flex-wrap items-center gap-2 shrink-0">
                <StatusBadge
                  variant={license.isActive ? 'success' : 'danger'}
                  label={license.isActive ? 'Active' : 'Inactive'}
                />
                <StatusBadge
                  variant={isExpired ? 'danger' : 'info'}
                  label={isExpired ? 'Expired (Ineligible)' : 'Valid (Eligible)'}
                />
                {isDetained && (
                  <StatusBadge
                    variant="danger"
                    label="Already Detained"
                  />
                )}
              </div>
            </div>
          </CardHeader>

          <div className="p-6 pt-0 space-y-6">
            {/* Ineligibility Warning */}
            {ineligibilityReason && (
              <div className="flex items-start gap-3 p-4 rounded-xl border border-rose-500/30 bg-rose-500/10 text-rose-200">
                <AlertTriangle className="size-5 text-rose-400 shrink-0 mt-0.5" />
                <div className="space-y-1">
                  <h4 className="text-sm font-bold text-white">Detention Ineligible</h4>
                  <p className="text-xs text-rose-300/90 leading-relaxed">{ineligibilityReason}</p>
                </div>
              </div>
            )}

            {/* Owner Identity Section */}
            <div className="p-5 rounded-2xl bg-gradient-to-r from-[#081326] via-[#0b1b36] to-[#081326] border border-[#1b2f52] flex flex-col sm:flex-row items-start sm:items-center gap-4.5 shadow-sm">
              <PersonPhoto
                personId={license.personId}
                photoKey={license.imagePath}
                hasPhoto={Boolean(license.imagePath)}
                fullName={license.fullName}
                size="md"
                accessToken={accessToken}
                className="size-16 rounded-2xl border-2 border-cyan-500/30 shadow-[0_0_15px_rgba(34,211,238,0.15)] shrink-0"
              />
              <div className="space-y-2 flex-1 min-w-0">
                <div className="flex flex-wrap items-center gap-2.5">
                  <h3 className="text-lg font-bold text-white tracking-tight truncate">
                    {license.fullName}
                  </h3>
                  <span className="text-[11px] font-mono font-semibold px-2.5 py-0.5 rounded-md bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    Driver #{license.driverId}
                  </span>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-1.5 text-xs text-slate-300">
                  <div className="flex items-center gap-1.5 truncate">
                    <span className="text-slate-400">National No:</span>
                    <strong className="text-white font-mono font-semibold">{license.nationalNo}</strong>
                  </div>
                  <div className="flex items-center gap-1.5 truncate">
                    <span className="text-slate-400">Gender:</span>
                    <strong className="text-slate-200">{license.gender || '—'}</strong>
                  </div>
                  <div className="flex items-center gap-1.5 truncate">
                    <span className="text-slate-400">DOB:</span>
                    <strong className="text-slate-200">{formatDate(license.dateOfBirth)}</strong>
                  </div>
                  <div className="sm:col-span-2 lg:col-span-3 flex items-center gap-1.5 text-xs truncate pt-0.5">
                    <span className="text-slate-400">Class:</span>
                    <strong className="text-cyan-300 font-medium">{license.licenseClass}</strong>
                  </div>
                </div>
              </div>
            </div>

            {/* Attributes Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3.5">
              <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    License ID
                  </span>
                  <Hash className="size-3.5 text-cyan-400/80" />
                </div>
                <strong className="text-base font-extrabold text-white font-mono block">
                  #{license.licenseId}
                </strong>
              </div>

              <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Issue Date
                  </span>
                  <Calendar className="size-3.5 text-slate-400/80" />
                </div>
                <strong className="text-sm font-semibold text-slate-200 block">
                  {formatDate(license.issueDate)}
                </strong>
              </div>

              <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Expiration Date
                  </span>
                  <CalendarCheck
                    className={cn('size-3.5', isExpired ? 'text-rose-400' : 'text-emerald-400')}
                  />
                </div>
                <strong
                  className={cn(
                    'text-sm font-semibold block',
                    isExpired ? 'text-rose-300' : 'text-slate-200',
                  )}
                >
                  {formatDate(license.expirationDate)}
                </strong>
              </div>

              <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Class Validity Fees
                  </span>
                  <Coins className="size-3.5 text-amber-400/80" />
                </div>
                <strong className="text-sm font-bold text-emerald-400 block">
                  {formatCurrency(license.licenseClassFees)}
                </strong>
              </div>
            </div>

            {/* Detention Fine Fees Assessment Card */}
            <div className="p-5 rounded-2xl bg-[#081326] border border-[#1b2f52] space-y-4">
              <div className="flex items-center gap-3 pb-3 border-b border-[#1b2f52]/60">
                <div className="size-8 rounded-lg bg-rose-500/10 border border-rose-500/20 flex items-center justify-center shrink-0">
                  <DollarSign className="size-4.5 text-rose-400" />
                </div>
                <div>
                  <h4 className="text-sm font-bold text-white">Detention Fine Assessment</h4>
                  <p className="text-xs text-slate-400">
                    Specify the penalty fine fee applicable to this license detention record.
                  </p>
                </div>
              </div>

              <div className="max-w-md space-y-2">
                <Label
                  htmlFor="fineFeesInput"
                  className="text-xs font-semibold text-slate-300 flex items-center justify-between"
                >
                  <span>Fine Fee Amount (JOD)</span>
                  <span className="text-[11px] text-slate-400 font-normal">
                    Required · Non-negative decimal
                  </span>
                </Label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-xs font-mono font-bold text-slate-400">
                    JOD
                  </span>
                  <Input
                    id="fineFeesInput"
                    type="text"
                    inputMode="decimal"
                    placeholder="0.00"
                    value={fineFeesInput}
                    onChange={(e) => handleFineFeesChange(e.target.value)}
                    disabled={submitting || Boolean(detainId) || Boolean(ineligibilityReason)}
                    className={cn(
                      'h-10.5 pl-13 pr-4 bg-[#0c192e] text-white font-mono text-sm font-bold rounded-xl transition-all',
                      fineError
                        ? 'border-rose-500 focus:border-rose-500 focus:ring-1 focus:ring-rose-500/40'
                        : 'border-[#1b2f52] focus:border-rose-500 focus:ring-1 focus:ring-rose-500/30',
                    )}
                  />
                </div>
                {fineError ? (
                  <p className="text-xs text-rose-400 flex items-center gap-1.5 mt-1">
                    <AlertTriangle className="size-3.5 shrink-0" />
                    <span>{fineError}</span>
                  </p>
                ) : (
                  <span className="text-[11px] text-slate-400 block leading-relaxed">
                    This fine fee must be settled by the applicant before the license can be
                    released.
                  </span>
                )}
              </div>
            </div>

            {/* Detention Operational Notice */}
            <div className="flex items-start gap-3.5 p-4 rounded-xl bg-gradient-to-r from-[#241118] to-[#1a0f16] border border-rose-500/30 text-xs text-rose-200">
              <ShieldAlert className="size-5 text-rose-400 shrink-0 mt-0.5" />
              <div className="space-y-0.5 leading-relaxed">
                <strong className="text-white font-semibold block text-xs">
                  Operational Impact & Legal Status:
                </strong>
                <span className="text-rose-200/90 text-xs">
                  Detaining this license will immediately deactivate it in the database and register
                  an official detention record. The driver will not be permitted to drive until fine
                  fees and release fees are paid.
                </span>
              </div>
            </div>

            {/* Actions Footer */}
            <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-3 border-t border-[#1b2f52]/60">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onBack}
                disabled={submitting}
                className="h-10 px-5 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] text-slate-300 hover:text-white rounded-xl transition-all cursor-pointer"
              >
                Cancel
              </Button>
              <Button
                type="button"
                onClick={handleDetain}
                disabled={!canSubmit}
                className="h-10 px-6 bg-rose-600 hover:bg-rose-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-lg hover:shadow-rose-600/20 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {submitting ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Processing Detention...</span>
                  </>
                ) : (
                  <>
                    <Lock className="size-4" />
                    <span>Detain License</span>
                  </>
                )}
              </Button>
            </div>
          </div>
        </Card>
      )}
    </section>
  )
}
