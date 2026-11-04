import { useState, useMemo, useCallback, type FormEvent } from 'react'
import {
  ArrowLeft,
  Search,
  RotateCcw,
  RotateCw,
  CheckCircle2,
  AlertTriangle,
  Info,
  Loader2,
  ShieldAlert,
  IdCard,
  Clock,
  ChevronDown,
  ChevronUp,
  ChevronLeft,
  ChevronRight,
  Filter,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/InternationalLicense'
import type { License } from '../../types/License'
import {
  getLicenses,
  getLicenseDetailsById,
  renewLicense,
} from '../../services/licenseService'
import { formatDate, formatCurrency, formatNumber } from '../../utils/formatters'
import { isLicenseExpiredInJordan } from '../../utils/jordanTime'
import { getActiveStatusProps } from '../../utils/status'
import { cn } from '@/lib/utils'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import TableToolbar, { type SearchFieldOption } from '../common/TableToolbar'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
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
import { useAsyncData, useTableFilters, usePagination } from '../../hooks'

type Props = {
  accessToken: string
  onBack: () => void
  onRenewed?: (newLicenseId: number) => void
}

const RENEWAL_APPLICATION_FEE = 7.0

type SearchBy = 'all' | 'licenseId' | 'driver' | 'class'

interface ExpiredLicenseFilters extends Record<string, unknown> {
  eligibilityFilter: 'all' | 'eligible' | 'ineligible'
}

const SEARCH_OPTIONS: SearchFieldOption<SearchBy>[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'licenseId', label: 'License ID' },
  { value: 'driver', label: 'Driver Name' },
  { value: 'class', label: 'License Class' },
]

export default function RenewLicensePage({ accessToken, onBack, onRenewed }: Props) {
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

  // 3. Search and Filtering for Expired Licenses
  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredExpiredLicenses,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<License, SearchBy, ExpiredLicenseFilters>({
    data: expiredLicenses,
    initialSearchField: 'all',
    initialFilters: { eligibilityFilter: 'all' },
    filterItem: (lic, query, field, currentFilters) => {
      // Authoritative Eligibility filter
      if (currentFilters.eligibilityFilter === 'eligible' && !lic.isEligibleForRenewal) {
        return false
      }
      if (currentFilters.eligibilityFilter === 'ineligible' && lic.isEligibleForRenewal) {
        return false
      }

      // Query check
      if (!query) return true

      if (field === 'licenseId') {
        return String(lic.licenseId).includes(query)
      }
      if (field === 'driver') {
        return (lic.driverName ?? '').toLowerCase().includes(query)
      }
      if (field === 'class') {
        return (lic.licenseClassName ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return (
        String(lic.licenseId).includes(query) ||
        (lic.driverName ?? '').toLowerCase().includes(query) ||
        (lic.licenseClassName ?? '').toLowerCase().includes(query)
      )
    },
  })

  // 4. Client-side Pagination
  const pagination = usePagination<License>({
    data: filteredExpiredLicenses,
    pageSize: 10,
    initialPage: 1,
  })

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

  // Table Columns
  const columns = useMemo<DataTableColumn<License>[]>(
    () => [
      {
        key: 'licenseId',
        header: 'License ID',
        render: (lic) => (
          <span className="font-mono text-cyan-400 font-bold">
            #{lic.licenseId}
          </span>
        ),
      },
      {
        key: 'driverName',
        header: 'Driver Name',
        render: (lic) => (
          <strong className="text-white font-semibold">
            {lic.driverName ?? '—'}
          </strong>
        ),
      },
      {
        key: 'licenseClassName',
        header: 'License Class',
        render: (lic) => (
          <span className="text-slate-300 text-xs">
            {lic.licenseClassName ?? '—'}
          </span>
        ),
      },
      {
        key: 'expirationDate',
        header: 'Expiration Date',
        render: (lic) => (
          <span className="text-amber-300 font-mono text-xs">
            {formatDate(lic.expirationDate)}
          </span>
        ),
      },
      {
        key: 'status',
        header: 'License Status',
        render: (lic) => <StatusBadge {...getActiveStatusProps(lic.isActive)} />,
      },
      {
        key: 'eligibility',
        header: 'Renewal Eligibility',
        render: (lic) => {
          if (lic.isEligibleForRenewal) {
            return (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
                <span className="size-1.5 rounded-full bg-emerald-400" />
                Eligible
              </span>
            )
          }
          return (
            <span
              className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-500/15 border border-rose-500/30 text-rose-300 cursor-help"
              title={lic.ineligibilityReason ?? 'License is not eligible for renewal.'}
            >
              <span className="size-1.5 rounded-full bg-rose-400" />
              Ineligible
            </span>
          )
        },
      },
      {
        key: 'action',
        header: 'Action',
        align: 'right',
        render: (lic) => {
          const isSelected = license?.licenseId === lic.licenseId
          return (
            <Button
              type="button"
              variant={isSelected ? 'default' : 'outline'}
              size="sm"
              disabled={loadingDetails || !lic.isEligibleForRenewal}
              onClick={() => void selectLicenseById(lic.licenseId)}
              className={cn(
                'h-8 px-3 gap-1.5 text-xs font-semibold rounded-lg cursor-pointer transition-all',
                isSelected
                  ? 'bg-cyan-600 text-white border-cyan-400'
                  : 'bg-[#0e1b33] text-cyan-300 hover:text-white border-[#263750] hover:border-cyan-500/60 hover:bg-[#15284d]',
                !lic.isEligibleForRenewal && 'opacity-40 cursor-not-allowed hover:bg-[#0e1b33]',
              )}
              title={
                !lic.isEligibleForRenewal
                  ? (lic.ineligibilityReason ?? 'Cannot renew this license')
                  : 'Select license for renewal'
              }
            >
              {isSelected ? (
                <>
                  <CheckCircle2 className="size-3.5 text-cyan-300" />
                  <span>Selected</span>
                </>
              ) : (
                <>
                  <RotateCcw className="size-3.5" />
                  <span>Start Renewal</span>
                </>
              )}
            </Button>
          )
        },
      },
    ],
    [license?.licenseId, loadingDetails, selectLicenseById],
  )

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

      {successNotice && (
        <div className="flex items-start gap-3 p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
          <div className="space-y-1">
            <h4 className="text-sm font-bold text-white">Renewal Completed</h4>
            <p className="text-xs text-emerald-300/90">{successNotice}</p>
          </div>
        </div>
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
        <Card className="border-[#0090ff]/50 bg-[#0c192e] text-white shadow-2xl rounded-2xl overflow-hidden ring-1 ring-cyan-500/30">
          <CardHeader className="border-b border-[#1b2f52]/80 bg-[#09152b] p-5">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <CardTitle className="text-base font-bold flex items-center gap-2">
                  <IdCard className="size-5 text-cyan-400" />
                  <span>Selected License for Renewal: #{license.licenseId}</span>
                </CardTitle>
                <CardDescription className="text-xs text-slate-400 mt-0.5">
                  Review original license credentials and confirm renewal transaction.
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
                  <span>{isExpired ? 'Expired' : 'Valid (Not Expired)'}</span>
                </span>

                <span
                  className={cn(
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                    isEligible
                      ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                      : 'bg-rose-500/15 border-rose-500/30 text-rose-300',
                  )}
                >
                  <span
                    className={cn(
                      'size-1.5 rounded-full',
                      isEligible ? 'bg-emerald-400' : 'bg-rose-400',
                    )}
                  />
                  <span>{isEligible ? 'Eligible for Renewal' : 'Ineligible'}</span>
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

          <div className="p-6 space-y-6">
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
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
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
            <div className="flex items-center justify-between gap-3 pt-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setLicense(null)}
                disabled={submitting}
                className="text-slate-400 hover:text-white text-xs cursor-pointer"
              >
                Clear Selection
              </Button>

              <div className="flex items-center gap-3">
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
                      <RotateCw className="size-4" />
                      <span>Renew License</span>
                    </>
                  )}
                </Button>
              </div>
            </div>
          </div>
        </Card>
      )}

      {/* Expired Licenses Section (B5 Discovery) */}
      <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden space-y-4">
        <CardHeader className="border-b border-[#1b2f52]/60 p-5 pb-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <Clock className="size-4.5 text-cyan-400" />
                <span>Expired Licenses</span>
              </CardTitle>
              <CardDescription className="text-xs text-slate-400 mt-0.5">
                Review expired driving licenses and select an eligible license to begin renewal.
              </CardDescription>
            </div>
            <span className="text-xs font-mono font-semibold px-2.5 py-1 rounded-full bg-cyan-950/80 border border-cyan-500/30 text-cyan-300 w-fit">
              {filteredExpiredLicenses.length} {filteredExpiredLicenses.length === 1 ? 'License' : 'Licenses'} Found
            </span>
          </div>
        </CardHeader>

        <div className="p-5 pt-0 space-y-4">
          {/* Table Toolbar */}
          <TableToolbar<SearchBy>
            searchValue={search}
            onSearchChange={setSearch}
            searchPlaceholder="Search expired licenses by ID, driver, or class..."
            searchField={searchBy}
            searchFieldOptions={SEARCH_OPTIONS}
            onSearchFieldChange={setSearchBy}
            isFiltered={isFiltered}
            onReset={handleResetFilters}
            onRefresh={refreshLicenses}
            refreshing={loadingLicenses}
            filters={
              <div className="flex items-center gap-2">
                <Filter className="size-4 text-slate-400 shrink-0" />
                <Select
                  value={filters.eligibilityFilter}
                  onValueChange={(val) =>
                    setFilter('eligibilityFilter', val as ExpiredLicenseFilters['eligibilityFilter'])
                  }
                >
                  <SelectTrigger className="w-[180px] h-9 bg-[#081326] border-[#1b2f52] text-xs text-slate-200">
                    <SelectValue placeholder="Eligibility Filter" />
                  </SelectTrigger>
                  <SelectContent className="bg-[#0b162c] border-[#1b2f52] text-xs text-slate-200">
                    <SelectItem value="all">All Expired Licenses</SelectItem>
                    <SelectItem value="eligible">Eligible for Renewal</SelectItem>
                    <SelectItem value="ineligible">Ineligible for Renewal</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            }
          />

          {/* Expired Licenses DataTable */}
          <DataTable<License>
            data={pagination.paginatedData ?? []}
            columns={columns}
            getRowKey={(lic) => lic.licenseId}
            loading={loadingLicenses}
            loadingMessage="Loading expired driving licenses from DVLD server..."
            emptyMessage="No expired driving licenses found matching the selected filter."
            emptyIcon={<IdCard className="size-8 text-slate-500" />}
          />

          {/* Table Pagination */}
          {filteredExpiredLicenses.length > 0 && (
            <div className="flex items-center justify-between pt-2 text-xs text-slate-400">
              <span>
                Showing{' '}
                <strong className="text-white">
                  {(pagination.page - 1) * pagination.pageSize + 1}
                </strong>{' '}
                to{' '}
                <strong className="text-white">
                  {Math.min(
                    pagination.page * pagination.pageSize,
                    filteredExpiredLicenses.length,
                  )}
                </strong>{' '}
                of <strong className="text-white">{filteredExpiredLicenses.length}</strong> expired licenses
              </span>

              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={pagination.previousPage}
                  disabled={!pagination.hasPreviousPage}
                  className="h-8 gap-1 text-xs bg-[#081326] border-[#1b2f52] text-slate-300 hover:text-white"
                >
                  <ChevronLeft className="size-3.5" />
                  <span>Previous</span>
                </Button>
                <span className="font-mono text-xs px-2 text-slate-300">
                  Page {pagination.page} of {pagination.totalPages || 1}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={pagination.nextPage}
                  disabled={!pagination.hasNextPage}
                  className="h-8 gap-1 text-xs bg-[#081326] border-[#1b2f52] text-slate-300 hover:text-white"
                >
                  <span>Next</span>
                  <ChevronRight className="size-3.5" />
                </Button>
              </div>
            </div>
          )}
        </div>
      </Card>
    </section>
  )
}
