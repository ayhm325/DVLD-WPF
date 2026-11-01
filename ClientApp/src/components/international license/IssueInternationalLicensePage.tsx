import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  Globe,
  CheckCircle2,
  Info,
  Loader2,
} from 'lucide-react'
import type {
  InternationalLicenseDetails,
  LocalLicenseInfo,
} from '../../types/InternationalLicense'
import {
  getLocalLicenseInfo,
  issueInternationalLicense,
} from '../../services/internationalLicenseService'
import { formatDate, formatCurrency } from '../../utils/formatters'
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
  onIssued: () => void
}

export default function IssueInternationalLicensePage({
  accessToken,
  onBack,
  onIssued,
}: Props) {
  const [licenseId, setLicenseId] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [issued, setIssued] = useState<InternationalLicenseDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  function handleLicenseIdChange(value: string) {
    setLicenseId(value)
    setError('')
    setNotice('')
    setLicense(null)
    setIssued(null)
  }

  async function searchLicense(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading) return

    setError('')
    setNotice('')
    setLicense(null)
    setIssued(null)

    const id = Number(licenseId)

    if (!Number.isInteger(id) || id <= 0) {
      setError('Enter a valid positive local license ID.')
      return
    }

    setLoading(true)

    try {
      const result = await getLocalLicenseInfo(accessToken, id)

      setLicense(result)
      setNotice(
        'Local license information loaded. Review eligibility before issuing.',
      )
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Could not find local license information.',
      )
    } finally {
      setLoading(false)
    }
  }

  async function issue() {
    if (!license || saving || issued) return

    setSaving(true)
    setError('')
    setNotice('')

    try {
      const result = await issueInternationalLicense(
        accessToken,
        license.licenseId,
      )

      setIssued(result)
      setNotice(
        `International license #${result.internationalLicenseId} issued successfully.`,
      )
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Could not issue the international license.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* Modern Page Hero */}
      <PageHero
        pageId="Issue International License"
        title="Issue International"
        highlight="Permit"
        description="Verify an active, non-detained local driving license to issue an official 1949 convention international driving permit."
        actions={
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onBack}
            className="h-9 gap-2 text-xs font-semibold text-white bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-blue-500/40 rounded-xl shadow-xs transition-all cursor-pointer"
          >
            <ArrowLeft className="size-4 text-cyan-400" />
            <span>Back to International Licenses</span>
          </Button>
        }
      />

      {/* Shared Alerts */}
      {error && (
        <ErrorAlert
          title="Issuance Notice"
          message={error}
          onDismiss={() => setError('')}
        />
      )}

      {notice && (
        <ErrorAlert
          variant="success"
          title="Status Update"
          message={notice}
          onDismiss={() => setNotice('')}
        />
      )}

      {/* Step 1 Card: Search Local License */}
      <Card className="p-6 sm:p-8 space-y-6">
        <CardHeader className="flex-row items-center gap-3 pb-5 border-b border-[#1e3458] mb-2 space-y-0">
          <div className="size-10 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
            <Search className="size-5" />
          </div>
          <div>
            <CardTitle>Eligible Local License Lookup</CardTitle>
            <CardDescription className="mt-0.5">
              Enter the local driving license ID to verify license status, class eligibility, and driver profile.
            </CardDescription>
          </div>
        </CardHeader>

        <form onSubmit={searchLicense} className="space-y-4">
          <div className="flex flex-col sm:flex-row items-stretch sm:items-end gap-3">
            <div className="flex-1">
              <Label
                htmlFor="local-license-id"
                className="block text-xs font-bold uppercase tracking-wider text-slate-300 mb-2"
              >
                Local License ID <span className="text-rose-400">*</span>
              </Label>
              <Input
                id="local-license-id"
                type="number"
                min="1"
                value={licenseId}
                onChange={(event) =>
                  handleLicenseIdChange(event.target.value)
                }
                placeholder="Enter license ID (e.g. 1042)"
                required
                disabled={loading || saving}
              />
            </div>

            <Button
              type="submit"
              disabled={loading || saving}
              className="h-11 px-6 gap-2 bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white font-bold rounded-xl shadow-[0_0_15px_rgba(0,140,255,0.35)] shrink-0 cursor-pointer"
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
          </div>
          <span className="text-[11px] text-slate-500 block">
            Enter the ID of the local license to retrieve its current eligibility and profile.
          </span>
        </form>

        {/* Retrieved License Details */}
        {license && (
          <div className="space-y-6 pt-4 border-t border-[#1e3458]">
            {/* License Holder Profile Banner */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 rounded-xl bg-[#09152b] border border-[#1b2f52]">
              <div className="flex items-center gap-4 min-w-0">
                <div className="size-12 rounded-xl bg-gradient-to-br from-blue-500 to-cyan-500 text-white font-bold text-base flex items-center justify-center shrink-0 shadow-sm">
                  {license.fullName?.trim().charAt(0) || 'P'}
                </div>
                <div className="min-w-0 space-y-1">
                  <span className="text-[10px] font-extrabold uppercase tracking-wider text-slate-400 block">
                    LICENSE HOLDER
                  </span>
                  <h4 className="text-base font-bold text-white truncate">
                    {license.fullName}
                  </h4>
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-300">
                    <span>
                      National No.{' '}
                      <strong className="text-white">
                        {license.nationalNo}
                      </strong>
                    </span>
                    <span>·</span>
                    <span>
                      Driver ID{' '}
                      <strong className="text-white font-mono">
                        #{license.driverId}
                      </strong>
                    </span>
                  </div>
                </div>
              </div>

              <span
                className={cn(
                  'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold border shrink-0',
                  license.isActive && !license.isDetained
                    ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                    : 'bg-rose-500/15 border-rose-500/30 text-rose-300',
                )}
              >
                <span
                  className={cn(
                    'size-1.5 rounded-full',
                    license.isActive && !license.isDetained
                      ? 'bg-emerald-400'
                      : 'bg-rose-400',
                  )}
                />
                <span>
                  {license.isDetained
                    ? 'Detained'
                    : license.isActive
                      ? 'Active License'
                      : 'Inactive License'}
                </span>
              </span>
            </div>

            {/* License Attributes Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 sm:gap-4">
              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Local License
                </span>
                <strong className="text-sm font-bold text-white font-mono block">
                  #{license.licenseId}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  License Class
                </span>
                <strong className="text-sm font-bold text-cyan-300 truncate block">
                  {license.licenseClass}
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
                  Date of Birth
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {formatDate(license.dateOfBirth)}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                  Gender
                </span>
                <strong className="text-sm font-bold text-slate-200 block">
                  {license.gender || '—'}
                </strong>
              </div>
            </div>

            {/* Eligibility Note */}
            <div className="rounded-xl border border-blue-500/25 bg-[#0a1835]/60 p-4 flex items-start gap-3 text-xs text-slate-400">
              <Info className="size-4.5 text-cyan-400 shrink-0 mt-0.5" />
              <p className="leading-relaxed">
                The central licensing API validates eligibility against active status, suspension, and vehicle class rules before issuance.
              </p>
            </div>

            {/* Issuance Action Bar */}
            <div className="flex flex-col sm:flex-row items-center justify-end gap-3 pt-4 border-t border-[#1e3458]">
              {issued && (
                <Button
                  type="button"
                  variant="outline"
                  onClick={onIssued}
                  className="w-full sm:w-auto h-11 px-5 rounded-xl border-[#263e68] text-slate-300 hover:bg-[#16274a] cursor-pointer"
                >
                  Back to Licenses
                </Button>
              )}

              <Button
                type="button"
                onClick={() => void issue()}
                disabled={
                  saving ||
                  Boolean(issued) ||
                  !license.isActive ||
                  license.isDetained
                }
                className="w-full sm:w-auto h-11 px-6 gap-2 bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold rounded-xl shadow-[0_0_20px_rgba(0,140,255,0.4)] disabled:opacity-50 cursor-pointer"
              >
                {saving ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Issuing Permit...</span>
                  </>
                ) : issued ? (
                  <>
                    <CheckCircle2 className="size-4 text-emerald-300" />
                    <span>Permit Issued</span>
                  </>
                ) : (
                  <>
                    <Globe className="size-4" />
                    <span>Issue International License</span>
                  </>
                )}
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Step 2 Result Card: Issued International License */}
      {issued && (
        <Card className="p-6 sm:p-8 space-y-6 border-emerald-500/40 bg-[#091a2e]/95 shadow-[0_0_25px_rgba(16,185,129,0.15)]">
          <CardHeader className="flex-row items-center justify-between pb-5 border-b border-emerald-500/20 mb-2 space-y-0">
            <div className="flex items-center gap-3">
              <div className="size-10 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-400 flex items-center justify-center shrink-0">
                <CheckCircle2 className="size-5" />
              </div>
              <div>
                <CardTitle className="text-emerald-100">
                  International License Issued
                </CardTitle>
                <CardDescription className="text-emerald-300/80 mt-0.5">
                  The international driving permit has been registered and verified by the DVLD authority.
                </CardDescription>
              </div>
            </div>

            <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 shrink-0">
              <span className="size-1.5 rounded-full bg-emerald-400" />
              <span>Issued</span>
            </span>
          </CardHeader>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 sm:gap-4">
            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Int&apos;l License ID
              </span>
              <strong className="text-sm font-bold text-white font-mono block">
                #{issued.internationalLicenseId}
              </strong>
            </div>

            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Application ID
              </span>
              <strong className="text-sm font-bold text-white font-mono block">
                #{issued.applicationId}
              </strong>
            </div>

            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Issue Date
              </span>
              <strong className="text-sm font-bold text-slate-200 block">
                {formatDate(issued.issueDate)}
              </strong>
            </div>

            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Expiration Date
              </span>
              <strong className="text-sm font-bold text-slate-200 block">
                {formatDate(issued.expirationDate)}
              </strong>
            </div>

            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Permit Fees
              </span>
              <strong className="text-sm font-bold text-emerald-400 block">
                {formatCurrency(issued.fees, 'JOD')}
              </strong>
            </div>

            <div className="p-3.5 rounded-xl bg-[#071324] border border-emerald-500/20 space-y-1">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Created By
              </span>
              <strong className="text-sm font-bold text-slate-200 block truncate">
                {issued.createdByUserName || issued.createdByUserId}
              </strong>
            </div>
          </div>

          <div className="flex justify-end pt-4 border-t border-emerald-500/20">
            <Button
              type="button"
              onClick={onIssued}
              className="w-full sm:w-auto h-11 px-6 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold rounded-xl shadow-[0_0_20px_rgba(16,185,129,0.4)] cursor-pointer"
            >
              Return to Licenses
            </Button>
          </div>
        </Card>
      )}

      {/* Return Action */}
      <div className="flex justify-end pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={onBack}
          className="h-11 px-5 rounded-xl border-[#263e68] text-slate-300 hover:bg-[#16274a] cursor-pointer"
        >
          Cancel and Return
        </Button>
      </div>
    </section>
  )
}
