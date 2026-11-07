import { useState, useMemo, useCallback, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  CheckCircle2,
  AlertTriangle,
  Loader2,
  Clock,
  ChevronDown,
  ChevronUp,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/License'
import {
  getLicenses,
  getLicenseDetailsById,
  renewLicense,
} from '../../services/licenseService'
import { formatNumber } from '../../utils/formatters'
import { isLicenseExpiredInJordan } from '../../utils/jordanTime'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  Input,
  Label,
} from '../ui'
import { useAsyncData } from '../../hooks'
import ExpiredLicensesTable from './ExpiredLicensesTable'
import RenewLicenseForm from './RenewLicenseForm'
import RenewLicenseReceiptCard from './RenewLicenseReceiptCard'

export interface RenewLicensePageProps {
  accessToken: string
  onBack: () => void
  onRenewed?: (newLicenseId: number) => void
}

export const RENEWAL_APPLICATION_FEE = 7.0

export default function RenewLicensePage({
  accessToken,
  onBack,
  onRenewed,
}: RenewLicensePageProps) {
  // Manual Lookup & Selected License State
  const [licenseIdInput, setLicenseIdInput] = useState('')
  const [license, setLicense] = useState<LocalLicenseInfo | null>(null)
  const [notes, setNotes] = useState('')
  const [newLicenseId, setNewLicenseId] = useState<number | null>(null)
  const [loadingDetails, setLoadingDetails] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [successNotice, setSuccessNotice] = useState('')
  const [isManualSearchOpen, setIsManualSearchOpen] = useState(false)

  // 1. Fetch All Licenses to discover Expired Licenses from the real API
  const fetcher = useCallback(() => getLicenses(accessToken), [accessToken])

  const {
    data: allLicenses = [],
    loading: loadingLicenses,
    error: licensesError,
    refresh: refreshLicenses,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Could not load licenses from server.',
  })

  // 2. Filter down to licenses expired according to Jordan facility local date
  const expiredLicenses = useMemo(() => {
    return allLicenses.filter((lic) => lic.isExpired ?? isLicenseExpiredInJordan(lic.expirationDate))
  }, [allLicenses])

  // Summary Metrics (100% authoritative from backend lifecycle and Jordan facility date)
  const stats = useMemo(() => {
    const totalExpired = expiredLicenses.length
    const eligible = expiredLicenses.filter((lic) => lic.isEligibleForRenewal).length
    const ineligible = totalExpired - eligible
    return { totalExpired, eligible, ineligible }
  }, [expiredLicenses])

  // Reusable authoritative license loader (shared between Manual search and Table selection)
  const selectLicenseById = useCallback(
    async (id: number) => {
      if (!Number.isInteger(id) || id <= 0) {
        setError('Please specify a valid positive license ID.')
        return
      }

      setError('')
      setSuccessNotice('')
      setLicense(null)
      setNewLicenseId(null)
      setNotes('')
      setLoadingDetails(true)

      try {
        const details = await getLicenseDetailsById(accessToken, id)
        setLicense(details)
        setLicenseIdInput(String(id))
        // Smoothly scroll to the renewal details form
        window.scrollTo({ top: 180, behavior: 'smooth' })
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : `Could not retrieve details for License #${id}.`,
        )
      } finally {
        setLoadingDetails(false)
      }
    },
    [accessToken],
  )

  function handleLicenseIdChange(val: string) {
    setLicenseIdInput(val)
    setError('')
    setSuccessNotice('')
    setLicense(null)
    setNewLicenseId(null)
  }

  function handleManualSearchSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (loadingDetails) return
    const id = Number(licenseIdInput.trim())
    void selectLicenseById(id)
  }

  function handleRenewAnother() {
    setLicense(null)
    setLicenseIdInput('')
    setNewLicenseId(null)
    setSuccessNotice('')
    setError('')
    setNotes('')
  }

  // Renewal Submission Handler
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
      setSuccessNotice(
        `Driving License #${license.licenseId} renewed successfully! New License ID is #${response.licenseId}.`,
      )
      onRenewed?.(response.licenseId)
      // Refresh expired licenses list so the renewed license drops out of the expired queue
      void refreshLicenses()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to renew the driving license.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  // Authoritative backend expiration & eligibility check
  const isExpired = license ? (license.isExpired ?? isLicenseExpiredInJordan(license.expirationDate)) : false
  const isEligible = license ? Boolean(license.isEligibleForRenewal) : false
  const isDetained = license ? license.isDetained : false

  // Ineligibility reason directly authoritative from backend
  const ineligibilityReason: string | null = license
    ? (license.ineligibilityReason ?? (!isEligible ? 'This license is not eligible for renewal.' : null))
    : null

  const canSubmit = Boolean(license && isEligible && !newLicenseId && !submitting)
  const totalFees = license ? RENEWAL_APPLICATION_FEE + license.licenseClassFees : 0

  return (
    <section className="page-content space-y-6">
      {/* Strict informational PageHero without any action buttons */}
      <PageHero
        pageId="Renew License"
        title="Renew Driving"
        highlight="License"
        description="Review expired driving licenses and select an eligible license to begin renewal."
      />

      {/* Global Actions Bar outside of PageHero */}
      <div className="flex flex-wrap items-center justify-between gap-3">
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

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setIsManualSearchOpen((prev) => !prev)}
          className="h-9 gap-2 text-xs font-semibold text-cyan-300 bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-cyan-500/40 rounded-xl shadow-xs transition-all cursor-pointer"
        >
          <Search className="size-3.5 text-cyan-400" />
          <span>{isManualSearchOpen ? 'Hide Manual Search' : 'Find by License ID'}</span>
          {isManualSearchOpen ? (
            <ChevronUp className="size-3.5 text-slate-400" />
          ) : (
            <ChevronDown className="size-3.5 text-slate-400" />
          )}
        </Button>
      </div>

      {error && <ErrorAlert title="Renewal Notice" message={error} />}
      {licensesError && <ErrorAlert title="Data Load Error" message={licensesError} />}

      {/* Success Receipt Card */}
      {successNotice && newLicenseId && license && (
        <RenewLicenseReceiptCard
          oldLicenseId={license.licenseId}
          newLicenseId={newLicenseId}
          message={successNotice}
          driverName={license.fullName}
          licenseClassName={license.licenseClass}
          totalFees={totalFees}
          onBack={onBack}
          onRenewAnother={handleRenewAnother}
        />
      )}

      {/* Summary Statistics Cards */}
      <StatsGrid columns={3}>
        <StatCard
          label="Total Expired Licenses"
          variant="violet"
          icon={<Clock size={20} />}
          value={formatNumber(stats.totalExpired)}
          description="Expired according to Jordan facility time"
        />
        <StatCard
          label="Eligible for Renewal"
          variant="emerald"
          icon={<CheckCircle2 size={20} />}
          value={formatNumber(stats.eligible)}
          description="Active licenses expired and ready"
        />
        <StatCard
          label="Ineligible (Inactive)"
          variant="rose"
          icon={<AlertTriangle size={20} />}
          value={formatNumber(stats.ineligible)}
          description="Inactive status prevents renewal"
        />
      </StatsGrid>

      {/* Manual Search Panel (Preserved B7 workflow, collapsible) */}
      {isManualSearchOpen && (
        <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden animate-in fade-in duration-200">
          <CardHeader className="border-b border-[#1b2f52]/60 pb-3">
            <CardTitle className="text-sm font-bold flex items-center gap-2">
              <Search className="size-4 text-cyan-400" />
              <span>Manual License Lookup</span>
            </CardTitle>
            <CardDescription className="text-xs text-slate-400">
              Enter any License ID directly to inspect its credentials and perform renewal.
            </CardDescription>
          </CardHeader>
          <div className="p-5">
            <form onSubmit={handleManualSearchSubmit} className="flex flex-col sm:flex-row gap-3 max-w-xl">
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
                  disabled={loadingDetails || submitting}
                  className="bg-[#081326] border-[#1b2f52] text-white focus:border-cyan-500"
                />
              </div>
              <div className="sm:self-end">
                <Button
                  type="submit"
                  disabled={loadingDetails || !licenseIdInput.trim() || submitting}
                  className="w-full sm:w-auto h-10 px-5 bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl gap-2 cursor-pointer transition-all shadow-md"
                >
                  {loadingDetails ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      <span>Loading...</span>
                    </>
                  ) : (
                    <>
                      <Search className="size-4" />
                      <span>Find License</span>
                    </>
                  )}
                </Button>
              </div>
            </form>
          </div>
        </Card>
      )}

      {/* Selected License Renewal Card (B8 Workflow) */}
      {license && (
        <RenewLicenseForm
          license={license}
          isExpired={isExpired}
          isEligible={isEligible}
          isDetained={isDetained}
          ineligibilityReason={ineligibilityReason}
          canSubmit={canSubmit}
          applicationFee={RENEWAL_APPLICATION_FEE}
          totalFees={totalFees}
          notes={notes}
          onNotesChange={setNotes}
          submitting={submitting}
          onClearSelection={() => setLicense(null)}
          onCancel={onBack}
          onSubmit={handleRenew}
        />
      )}

      {/* Expired Licenses Section (B5 Discovery) */}
      <ExpiredLicensesTable
        expiredLicenses={expiredLicenses}
        selectedLicenseId={license?.licenseId ?? null}
        loading={loadingLicenses}
        loadingDetails={loadingDetails}
        onSelectLicense={(id) => void selectLicenseById(id)}
        onRefresh={refreshLicenses}
      />
    </section>
  )
}
