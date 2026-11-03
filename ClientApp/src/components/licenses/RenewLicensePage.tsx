import { useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  RotateCcw,
  CheckCircle2,
  AlertTriangle,
  Info,
  Loader2,
  ShieldAlert,
  IdCard,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/InternationalLicense'
import { getLicenseDetailsById, renewLicense } from '../../services/licenseService'
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
  Textarea,
} from '../ui'

type Props = {
  accessToken: string
  onBack: () => void
  onRenewed?: (newLicenseId: number) => void
}

const RENEWAL_APPLICATION_FEE = 7.0

export default function RenewLicensePage({ accessToken, onBack, onRenewed }: Props) {
  const [licenseIdInput, setLicenseIdInput] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [notes, setNotes] = useState('')
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

  async function handleRenew() {
    if (!license || submitting || newLicenseId) return

    setSubmitting(true)
    setError('')
    setSuccessNotice('')

    try {
      const response = await renewLicense(accessToken, {
        oldLicenseId: license.licenseId,
        notes: notes.trim() || null,
      })

      setNewLicenseId(response.licenseId)
      setSuccessNotice(`Driving License renewed successfully! New License ID: #${response.licenseId}`)
      onRenewed?.(response.licenseId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to renew the driving license.')
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
      ineligibilityReason = 'Cannot renew an inactive license.'
    } else if (!isExpired) {
      ineligibilityReason =
        'A driving license remains valid throughout its expiration date in Jordan local time. Renewal is allowed ONLY after expiration (starting the following calendar day). Early renewal is strictly prohibited.'
    }
  }

  const canSubmit = Boolean(license && !ineligibilityReason && !newLicenseId && !submitting)
  const totalFees = license ? RENEWAL_APPLICATION_FEE + license.licenseClassFees : 0

  return (
    <section className="page-content space-y-6">
      <PageHero
        pageId="Renew License"
        title="Renew Driving"
        highlight="License"
        description="Renew an expired driving license according to Jordan facility time rules and official class validity lengths."
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

      {error && <ErrorAlert title="Renewal Notice" message={error} />}

      {successNotice && (
        <div className="flex items-start gap-3 p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Renewal Completed</h4>
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
            Enter the License ID of the expired license you wish to renew.
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

      {/* License & Owner Details Card */}
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
                  Review the license credentials and owner details before proceeding with renewal.
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
                      ? 'bg-amber-500/15 border-amber-500/30 text-amber-300'
                      : 'bg-cyan-500/15 border-cyan-500/30 text-cyan-300',
                  )}
                >
                  <span>{isExpired ? 'Expired (Eligible)' : 'Valid (Not Expired)'}</span>
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
              <div className="flex items-start gap-3 p-4 rounded-xl border border-amber-500/30 bg-amber-500/10 text-amber-200">
                <AlertTriangle className="size-5 text-amber-400 shrink-0 mt-0.5" />
                <div className="space-y-1">
                  <h4 className="text-sm font-bold text-white">Renewal Ineligible</h4>
                  <p className="text-xs text-amber-300/90 leading-relaxed">{ineligibilityReason}</p>
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
                <strong className="text-sm font-bold text-amber-300 block">
                  {formatDate(license.expirationDate)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  License Class Fee
                </span>
                <strong className="text-sm font-bold text-emerald-400 block">
                  {formatCurrency(license.licenseClassFees)}
                </strong>
              </div>
            </div>

            {/* Financial & Fee Breakdown */}
            <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-3">
              <div className="flex items-center gap-2 text-xs font-bold text-slate-300">
                <Info className="size-4 text-cyan-400" />
                <span>Renewal Fee Breakdown</span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
                <div className="p-3 rounded-lg bg-[#0c192e] border border-[#1b2f52] flex justify-between items-center">
                  <span className="text-slate-400">Application Fee:</span>
                  <span className="font-bold text-white">
                    {formatCurrency(RENEWAL_APPLICATION_FEE)}
                  </span>
                </div>
                <div className="p-3 rounded-lg bg-[#0c192e] border border-[#1b2f52] flex justify-between items-center">
                  <span className="text-slate-400">Class Validity Fee:</span>
                  <span className="font-bold text-white">
                    {formatCurrency(license.licenseClassFees)}
                  </span>
                </div>
                <div className="p-3 rounded-lg bg-[#0e2447] border border-cyan-500/30 flex justify-between items-center">
                  <span className="text-cyan-300 font-semibold">Total Payable:</span>
                  <span className="font-extrabold text-cyan-200 text-sm">
                    {formatCurrency(totalFees)}
                  </span>
                </div>
              </div>
            </div>

            {/* Optional Notes */}
            <div className="space-y-1.5">
              <Label htmlFor="notes" className="text-xs text-slate-300">
                Renewal Notes (Optional)
              </Label>
              <Textarea
                id="notes"
                placeholder="Enter any administrative notes or remarks regarding this renewal..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                disabled={!canSubmit && !newLicenseId}
                className="bg-[#081326] border-[#1b2f52] text-white min-h-[70px] text-xs focus:border-cyan-500"
              />
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
                onClick={handleRenew}
                disabled={!canSubmit}
                className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl h-10 px-6 gap-2 cursor-pointer transition-all shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {submitting ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Processing Renewal...</span>
                  </>
                ) : (
                  <>
                    <RotateCcw className="size-4" />
                    <span>Renew License</span>
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
