import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  Copy,
  CheckCircle2,
  AlertTriangle,
  Info,
  Loader2,
  IdCard,
  Calendar,
  CalendarCheck,
  Coins,
  Hash,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/InternationalLicense'
import { getLicenseDetailsById, replaceLicense } from '../../services/licenseService'
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
  CardContent,
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
        <div className="flex items-start gap-3 p-4.5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Replacement Issued</h4>
            <p className="text-xs text-emerald-300/90">{successNotice}</p>
          </div>
        </div>
      )}

      {/* License Search Card */}
      <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-[#1b2f52]/60 px-6 py-5">
          <CardTitle className="text-base font-bold flex items-center gap-2.5">
            <Search className="size-4.5 text-cyan-400" />
            <span>Find Original License</span>
          </CardTitle>
          <CardDescription className="text-xs text-slate-400 mt-1">
            Enter the License ID of the active license to be replaced.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-6">
          <form
            onSubmit={handleSearch}
            className="flex flex-col sm:flex-row items-stretch sm:items-end gap-3.5 max-w-lg"
          >
            <div className="flex-1 space-y-2">
              <Label htmlFor="licenseIdInput" className="text-xs font-semibold text-slate-300">
                License ID
              </Label>
              <div className="relative">
                <span className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-500 text-xs font-mono font-bold">
                  #
                </span>
                <Input
                  id="licenseIdInput"
                  type="number"
                  min="1"
                  placeholder="e.g. 2"
                  value={licenseIdInput}
                  onChange={(e) => handleLicenseIdChange(e.target.value)}
                  disabled={loading || submitting}
                  className="h-10.5 pl-8 pr-4 bg-[#081326] border-[#1b2f52] focus:border-cyan-500 text-white font-mono text-sm rounded-xl transition-colors"
                />
              </div>
            </div>
            <div>
              <Button
                type="submit"
                disabled={loading || !licenseIdInput.trim() || submitting}
                className="w-full sm:w-auto h-10.5 px-6 bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-md hover:shadow-cyan-600/20 disabled:opacity-50 disabled:cursor-not-allowed"
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
        </CardContent>
      </Card>

      {/* License Details & Replacement Configuration Card */}
      {license && (
        <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
          <CardHeader className="border-b border-[#1b2f52]/60 px-6 py-5">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div className="space-y-1">
                <CardTitle className="text-base font-bold flex items-center gap-2.5">
                  <IdCard className="size-4.5 text-cyan-400" />
                  <span>License Information</span>
                </CardTitle>
                <CardDescription className="text-xs text-slate-400">
                  Select the replacement reason and review the application details.
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
                    label="Detained"
                  />
                )}
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-6 space-y-6">
            {/* Ineligibility Warning */}
            {ineligibilityReason && (
              <div className="flex items-start gap-3.5 p-4.5 rounded-xl border border-rose-500/30 bg-rose-500/10 text-rose-200">
                <AlertTriangle className="size-5 text-rose-400 shrink-0 mt-0.5" />
                <div className="space-y-1">
                  <h4 className="text-sm font-bold text-white">Replacement Ineligible</h4>
                  <p className="text-xs text-rose-300/90 leading-relaxed">{ineligibilityReason}</p>
                </div>
              </div>
            )}

            {/* Owner Identity Section with Verified Shared PersonPhoto */}
            <div className="p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-[#081326] via-[#0b1b36] to-[#081326] border border-[#1b2f52] flex flex-col sm:flex-row items-start sm:items-center gap-5 shadow-sm">
              <PersonPhoto
                personId={license.personId}
                photoKey={license.imagePath}
                hasPhoto={Boolean(license.imagePath)}
                fullName={license.fullName}
                size="md"
                accessToken={accessToken}
                className="size-16 sm:size-18 rounded-2xl border-2 border-cyan-500/30 shadow-[0_0_15px_rgba(34,211,238,0.15)] shrink-0"
              />
              <div className="space-y-2 flex-1 min-w-0">
                <div className="flex flex-wrap items-center gap-2.5">
                  <h3 className="text-base font-bold text-white tracking-tight">{license.fullName}</h3>
                  <span className="text-xs px-2.5 py-0.5 rounded-md bg-blue-500/10 border border-blue-500/20 text-blue-300 font-mono">
                    Person #{license.personId}
                  </span>
                </div>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-slate-400">
                  <span className="flex items-center gap-1.5">
                    <span className="text-slate-500 font-medium">National No:</span>
                    <strong className="text-slate-200 font-mono">{license.nationalNo}</strong>
                  </span>
                  <span className="text-slate-600 hidden sm:inline">·</span>
                  <span className="flex items-center gap-1.5">
                    <span className="text-slate-500 font-medium">Driver ID:</span>
                    <strong className="text-cyan-400 font-mono">#{license.driverId}</strong>
                  </span>
                  <span className="text-slate-600 hidden sm:inline">·</span>
                  <span className="flex items-center gap-1.5">
                    <span className="text-slate-500 font-medium">Class:</span>
                    <strong className="text-blue-300">{license.licenseClass}</strong>
                  </span>
                </div>
              </div>
            </div>

            {/* Replacement Reason Selection */}
            <div className="p-5 sm:p-6 rounded-2xl bg-[#081326] border border-[#1b2f52] space-y-4">
              <div className="flex items-center justify-between border-b border-[#1b2f52]/60 pb-3.5">
                <div className="flex items-center gap-3">
                  <div className="size-8 rounded-lg bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center shrink-0">
                    <Copy className="size-4 text-cyan-400" />
                  </div>
                  <div>
                    <h4 className="text-sm font-bold text-white">Replacement Reason</h4>
                    <p className="text-xs text-slate-400 mt-0.5">
                      Select the condition justifying the credential reissuance.
                    </p>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-1">
                <label
                  className={cn(
                    'relative flex items-center justify-between p-4.5 rounded-xl border cursor-pointer transition-all',
                    replacementReason === 'Damaged License'
                      ? 'bg-gradient-to-r from-[#0d2342] to-[#0a1930] border-cyan-500/60 shadow-lg shadow-cyan-500/10 ring-1 ring-cyan-500/40'
                      : 'bg-[#081326] border-[#1b2f52] hover:border-slate-600 hover:bg-[#0c1a33]',
                  )}
                >
                  <div className="flex items-center gap-3.5">
                    <input
                      type="radio"
                      name="replacementReason"
                      value="Damaged License"
                      checked={replacementReason === 'Damaged License'}
                      onChange={() => setReplacementReason('Damaged License')}
                      className="accent-cyan-500 size-4.5 cursor-pointer"
                    />
                    <div className="space-y-0.5">
                      <span className="text-sm font-bold text-white block">Damaged License</span>
                      <span className="text-xs text-slate-400 block">
                        Replace physical license damaged in use
                      </span>
                    </div>
                  </div>
                  <span className="text-xs font-mono font-bold text-cyan-300 px-3 py-1.5 rounded-lg bg-[#0c192e] border border-cyan-500/30">
                    5.00 JOD
                  </span>
                </label>

                <label
                  className={cn(
                    'relative flex items-center justify-between p-4.5 rounded-xl border cursor-pointer transition-all',
                    replacementReason === 'Lost License'
                      ? 'bg-gradient-to-r from-[#0d2342] to-[#0a1930] border-cyan-500/60 shadow-lg shadow-cyan-500/10 ring-1 ring-cyan-500/40'
                      : 'bg-[#081326] border-[#1b2f52] hover:border-slate-600 hover:bg-[#0c1a33]',
                  )}
                >
                  <div className="flex items-center gap-3.5">
                    <input
                      type="radio"
                      name="replacementReason"
                      value="Lost License"
                      checked={replacementReason === 'Lost License'}
                      onChange={() => setReplacementReason('Lost License')}
                      className="accent-cyan-500 size-4.5 cursor-pointer"
                    />
                    <div className="space-y-0.5">
                      <span className="text-sm font-bold text-white block">Lost License</span>
                      <span className="text-xs text-slate-400 block">
                        Issue replacement for lost card credentials
                      </span>
                    </div>
                  </div>
                  <span className="text-xs font-mono font-bold text-cyan-300 px-3 py-1.5 rounded-lg bg-[#0c192e] border border-cyan-500/30">
                    10.00 JOD
                  </span>
                </label>
              </div>
            </div>

            {/* Attributes Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="p-4.5 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Original License ID
                  </span>
                  <Hash className="size-4 text-slate-400/80" />
                </div>
                <strong className="text-sm font-bold text-white font-mono block">
                  #{license.licenseId}
                </strong>
              </div>

              <div className="p-4.5 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Original Issue Date
                  </span>
                  <Calendar className="size-4 text-slate-400/80" />
                </div>
                <strong className="text-sm font-semibold text-slate-200 block">
                  {formatDate(license.issueDate)}
                </strong>
              </div>

              <div className="p-4.5 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Retained Expiration Date
                  </span>
                  <CalendarCheck
                    className={cn('size-4', isExpired ? 'text-rose-400' : 'text-emerald-400')}
                  />
                </div>
                <strong
                  className={cn(
                    'text-sm font-semibold block',
                    isExpired ? 'text-rose-300' : 'text-cyan-300',
                  )}
                >
                  {formatDate(license.expirationDate)}
                </strong>
              </div>

              <div className="p-4.5 rounded-xl bg-[#081326] border border-[#1b2f52] hover:border-[#263750] transition-colors space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] uppercase font-bold text-slate-400 tracking-wider">
                    Replacement Fee
                  </span>
                  <Coins className="size-4 text-amber-400/80" />
                </div>
                <strong className="text-sm font-bold text-emerald-400 block font-mono">
                  {formatCurrency(applicationFee)}
                </strong>
              </div>
            </div>

            {/* Expiration Retention Notice */}
            <div className="flex items-start gap-4 p-5 rounded-xl bg-gradient-to-r from-[#0d1e38] to-[#0a1628] border border-cyan-500/30 text-xs text-cyan-200">
              <Info className="size-5 text-cyan-400 shrink-0 mt-0.5" />
              <div className="space-y-1 leading-relaxed">
                <strong className="text-white font-semibold block text-xs">
                  Expiration Retention Policy:
                </strong>
                <span className="text-cyan-200/90 text-xs leading-relaxed">
                  The new replacement driving license will retain the exact expiration date (
                  <strong className="text-white font-bold">{formatDate(license.expirationDate)}</strong>) of the original license.
                </span>
              </div>
            </div>

            {/* Actions Footer */}
            <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3.5 pt-4 border-t border-[#1b2f52]/60">
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
                onClick={handleReplace}
                disabled={!canSubmit}
                className="h-10 px-6 bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-lg hover:shadow-cyan-600/20 disabled:opacity-50 disabled:cursor-not-allowed"
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
          </CardContent>
        </Card>
      )}
    </section>
  )
}
