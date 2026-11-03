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
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/InternationalLicense'
import { getLicenseDetailsById } from '../../services/licenseService'
import { detainLicense } from '../../services/detainedLicenseService'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { isLicenseExpiredInJordan } from '../../utils/jordanTime'
import { cn } from '@/lib/utils'
import PageHero from '../common/PageHero'
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
  const [fineFeesInput, setFineFeesInput] = useState('50')
  const [detainId, setDetainId] = useState<number | null>(null)
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [successNotice, setSuccessNotice] = useState('')

  function handleLicenseIdChange(val: string) {
    setLicenseIdInput(val)
    setError('')
    setSuccessNotice('')
    setLicense(null)
    setDetainId(null)
  }

  async function handleSearch(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (loading) return

    setError('')
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
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not find driving license details.')
    } finally {
      setLoading(false)
    }
  }

  async function handleDetain() {
    if (!license || submitting || detainId) return

    const fine = parseFloat(fineFeesInput.trim())
    if (isNaN(fine) || fine < 0) {
      setError('Please enter a valid non-negative fine fee amount.')
      return
    }

    setSubmitting(true)
    setError('')
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

  const fineAmount = parseFloat(fineFeesInput.trim())
  const isFineValid = !isNaN(fineAmount) && fineAmount >= 0
  const canSubmit = Boolean(license && !ineligibilityReason && isFineValid && !detainId && !submitting)

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
          <form onSubmit={handleSearch} className="flex flex-col sm:flex-row gap-3 max-w-xl">
            <div className="flex-1 space-y-1.5">
              <Label htmlFor="licenseIdInput" className="text-xs text-slate-300">
                License ID
              </Label>
              <Input
                id="licenseIdInput"
                type="number"
                min="1"
                placeholder="e.g. 10"
                value={licenseIdInput}
                onChange={(e) => handleLicenseIdChange(e.target.value)}
                disabled={loading || submitting}
                className="bg-[#081326] border-[#1b2f52] text-white focus:border-cyan-500"
              />
            </div>
            <div className="sm:self-end">
              <Button
                type="submit"
                disabled={loading || !licenseIdInput.trim() || submitting}
                className="w-full sm:w-auto h-10 px-5 bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-md"
              >
                {loading ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Searching...</span>
                  </>
                ) : (
                  <>
                    <Search className="size-4" />
                    <span>Search</span>
                  </>
                )}
              </Button>
            </div>
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

              {/* Status Badges */}
              <div className="flex items-center gap-2 shrink-0">
                <span
                  className={cn(
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                    license.isActive
                      ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                      : 'bg-rose-500/15 border-rose-500/30 text-rose-300',
                  )}
                >
                  <span
                    className={cn(
                      'size-1.5 rounded-full',
                      license.isActive ? 'bg-emerald-400' : 'bg-rose-400',
                    )}
                  />
                  <span>{license.isActive ? 'Active' : 'Inactive'}</span>
                </span>

                <span
                  className={cn(
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                    isExpired
                      ? 'bg-rose-500/15 border-rose-500/30 text-rose-300'
                      : 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300',
                  )}
                >
                  <span>{isExpired ? 'Expired (Ineligible)' : 'Valid (Eligible)'}</span>
                </span>

                {isDetained && (
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border bg-rose-500/15 border-rose-500/30 text-rose-300">
                    <ShieldAlert className="size-3 text-rose-400" />
                    <span>Already Detained</span>
                  </span>
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

            {/* Owner Header Info */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4 p-4 rounded-xl bg-[#081326] border border-[#1b2f52]">
              <div className="size-14 rounded-xl overflow-hidden bg-[#0c192e] border border-[#1b2f52] flex items-center justify-center shrink-0">
                {license.imagePath ? (
                  <img
                    src={license.imagePath}
                    alt={license.fullName}
                    className="size-full object-cover"
                  />
                ) : (
                  <IdCard className="size-8 text-slate-500" />
                )}
              </div>
              <div className="space-y-1 flex-1">
                <h3 className="text-base font-bold text-white">{license.fullName}</h3>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-300">
                  <span>
                    National No: <strong className="text-white">{license.nationalNo}</strong>
                  </span>
                  <span>·</span>
                  <span>
                    Driver ID: <strong className="text-white font-mono">#{license.driverId}</strong>
                  </span>
                  <span>·</span>
                  <span>
                    Class: <strong className="text-cyan-300">{license.licenseClass}</strong>
                  </span>
                </div>
              </div>
            </div>

            {/* Attributes Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  License ID
                </span>
                <strong className="text-sm font-bold text-white font-mono block">
                  #{license.licenseId}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Issue Date
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {formatDate(license.issueDate)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Expiration Date
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {formatDate(license.expirationDate)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Class Validity Fees
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {formatCurrency(license.licenseClassFees)}
                </strong>
              </div>
            </div>

            {/* Fine Fees Input */}
            <div className="space-y-1.5 max-w-sm">
              <Label htmlFor="fineFeesInput" className="text-xs font-bold text-slate-300">
                Detention Fine Fees (JOD)
              </Label>
              <Input
                id="fineFeesInput"
                type="number"
                step="0.01"
                min="0"
                placeholder="e.g. 50.00"
                value={fineFeesInput}
                onChange={(e) => setFineFeesInput(e.target.value)}
                disabled={!canSubmit && !detainId}
                className="bg-[#081326] border-[#1b2f52] text-white focus:border-rose-500 font-mono text-sm"
              />
              <span className="text-[11px] text-slate-400 block">
                This fine must be settled by the applicant before the license can be released.
              </span>
            </div>

            {/* Detention Operational Notice */}
            <div className="flex items-start gap-2.5 p-3.5 rounded-xl bg-[#2a131b] border border-rose-500/30 text-xs text-rose-200">
              <ShieldAlert className="size-4 text-rose-400 shrink-0 mt-0.5" />
              <span>
                <strong>Operational Impact:</strong> Detaining this license will immediately
                deactivate it in the database and register an official detention record. The driver
                will not be permitted to drive until fine fees and release fees are paid.
              </span>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-end gap-3 pt-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onBack}
                disabled={submitting}
                className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white"
              >
                Cancel
              </Button>
              <Button
                type="button"
                onClick={handleDetain}
                disabled={!canSubmit}
                className="bg-rose-600 hover:bg-rose-500 text-white font-semibold rounded-xl h-10 px-6 gap-2 cursor-pointer transition-all shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
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
