import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  Copy,
  CheckCircle2,
  AlertTriangle,
  Info,
  Loader2,
  ShieldAlert,
  IdCard,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/InternationalLicense'
import { getLicenseDetailsById, replaceLicense } from '../../services/licenseService'
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
  onReplaced?: (newLicenseId: number) => void
}

type ReplacementOption = 'Damaged License' | 'Lost License'

const REPLACEMENT_FEES: Record<ReplacementOption, number> = {
  'Damaged License': 5.0,
  'Lost License': 10.0,
}

export default function ReplaceLicensePage({ accessToken, onBack, onReplaced }: Props) {
  const [licenseIdInput, setLicenseIdInput] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [replacementReason, setReplacementReason] = useState<ReplacementOption>('Damaged License')
  const [newLicenseId, setNewLicenseId] = useState<number | null>(null)
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [successNotice, setSuccessNotice] = useState('')

  function handleLicenseIdChange(val: string) {
    setLicenseIdInput(val)
    setError('')
    setSuccessNotice('')
    setLicense(null)
    setNewLicenseId(null)
  }

  async function handleSearch(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (loading) return

    setError('')
    setSuccessNotice('')
    setLicense(null)
    setNewLicenseId(null)

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

  async function handleReplace() {
    if (!license || submitting || newLicenseId) return

    setSubmitting(true)
    setError('')
    setSuccessNotice('')

    try {
      const response = await replaceLicense(accessToken, {
        oldLicenseId: license.licenseId,
        replacementReason,
      })

      setNewLicenseId(response.licenseId)
      setSuccessNotice(
        `Replacement License issued successfully! New License ID: #${response.licenseId}`,
      )
      onReplaced?.(response.licenseId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to replace the driving license.')
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
    if (isInactive) {
      ineligibilityReason = 'Cannot replace an inactive license.'
    } else if (isExpired) {
      ineligibilityReason =
        'Cannot replace an expired license. A replacement is only valid for unexpired licenses. Please use License Renewal instead.'
    }
  }

  const canSubmit = Boolean(license && !ineligibilityReason && !newLicenseId && !submitting)
  const applicationFee = REPLACEMENT_FEES[replacementReason]

  return (
    <section className="page-content space-y-6">
      <PageHero
        pageId="Replace License"
        title="Replace Driving"
        highlight="License"
        description="Issue a replacement driving license for lost or damaged credentials with original expiration date retention."
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
          <span>Back to Licenses</span>
        </Button>
      </div>

      {error && <ErrorAlert title="Replacement Notice" message={error} />}

      {successNotice && (
        <div className="flex items-start gap-3 p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Replacement Issued</h4>
            <p className="text-xs text-emerald-300/90">{successNotice}</p>
          </div>
        </div>
      )}

      {/* License Search Card */}
      <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-[#1b2f52]/60 pb-4">
          <CardTitle className="text-base font-bold flex items-center gap-2">
            <Search className="size-4 text-cyan-400" />
            <span>Find Original License</span>
          </CardTitle>
          <CardDescription className="text-xs text-slate-400">
            Enter the License ID of the active license to be replaced.
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

      {/* License Details & Replacement Configuration Card */}
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
                  Select the replacement reason and review the application details.
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
                    <span>Detained</span>
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
                  <h4 className="text-sm font-bold text-white">Replacement Ineligible</h4>
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

            {/* Replacement Reason Selection */}
            <div className="space-y-2">
              <Label className="text-xs font-bold text-slate-300">Replacement Reason</Label>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <label
                  className={cn(
                    'flex items-center justify-between p-4 rounded-xl border cursor-pointer transition-all',
                    replacementReason === 'Damaged License'
                      ? 'bg-[#0f284d] border-cyan-500/60 shadow-md ring-1 ring-cyan-500/30'
                      : 'bg-[#081326] border-[#1b2f52] hover:border-slate-600',
                  )}
                >
                  <div className="flex items-center gap-3">
                    <input
                      type="radio"
                      name="replacementReason"
                      value="Damaged License"
                      checked={replacementReason === 'Damaged License'}
                      onChange={() => setReplacementReason('Damaged License')}
                      className="accent-cyan-500 size-4 cursor-pointer"
                    />
                    <div>
                      <span className="text-sm font-bold text-white block">Damaged License</span>
                      <span className="text-xs text-slate-400">
                        Replace physical license damaged in use
                      </span>
                    </div>
                  </div>
                  <span className="text-xs font-extrabold text-cyan-300 px-2.5 py-1 rounded-md bg-[#081326] border border-[#1b2f52]">
                    5.00 JOD
                  </span>
                </label>

                <label
                  className={cn(
                    'flex items-center justify-between p-4 rounded-xl border cursor-pointer transition-all',
                    replacementReason === 'Lost License'
                      ? 'bg-[#0f284d] border-cyan-500/60 shadow-md ring-1 ring-cyan-500/30'
                      : 'bg-[#081326] border-[#1b2f52] hover:border-slate-600',
                  )}
                >
                  <div className="flex items-center gap-3">
                    <input
                      type="radio"
                      name="replacementReason"
                      value="Lost License"
                      checked={replacementReason === 'Lost License'}
                      onChange={() => setReplacementReason('Lost License')}
                      className="accent-cyan-500 size-4 cursor-pointer"
                    />
                    <div>
                      <span className="text-sm font-bold text-white block">Lost License</span>
                      <span className="text-xs text-slate-400">
                        Issue replacement for lost card credentials
                      </span>
                    </div>
                  </div>
                  <span className="text-xs font-extrabold text-cyan-300 px-2.5 py-1 rounded-md bg-[#081326] border border-[#1b2f52]">
                    10.00 JOD
                  </span>
                </label>
              </div>
            </div>

            {/* Attributes Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Original License ID
                </span>
                <strong className="text-sm font-bold text-white font-mono block">
                  #{license.licenseId}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Original Issue Date
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {formatDate(license.issueDate)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Retained Expiration Date
                </span>
                <strong className="text-sm font-bold text-cyan-300 block">
                  {formatDate(license.expirationDate)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Replacement Fee
                </span>
                <strong className="text-sm font-bold text-emerald-400 block">
                  {formatCurrency(applicationFee)}
                </strong>
              </div>
            </div>

            {/* Expiration Retention Notice */}
            <div className="flex items-start gap-2.5 p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] text-xs text-slate-300">
              <Info className="size-4 text-cyan-400 shrink-0 mt-0.5" />
              <span>
                <strong>Expiration Retention Policy:</strong> The new replacement driving license
                will retain the exact expiration date (
                <strong className="text-white">{formatDate(license.expirationDate)}</strong>) of the
                original license.
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
                onClick={handleReplace}
                disabled={!canSubmit}
                className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl h-10 px-6 gap-2 cursor-pointer transition-all shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {submitting ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Issuing Replacement...</span>
                  </>
                ) : (
                  <>
                    <Copy className="size-4" />
                    <span>Issue Replacement License</span>
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
