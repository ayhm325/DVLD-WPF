import { useCallback, useEffect, useMemo, useState } from 'react'
import LocalApplicationsList from './LocalApplicationsList'
import LocalApplicationForm from './LocalApplicationForm'
import ApplicationDetailsModal from './ApplicationDetailsModal'
import IssueDrivingLicenseModal from './IssueDrivingLicenseModal'
import LicenseDetailsModal from '../licenses/LicenseDetailsModal'
import ConfirmModal from '../common/ConfirmModal'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import TableToolbar from '../common/TableToolbar'
import ErrorAlert from '../common/ErrorAlert'
import PaginationControls from '../common/PaginationControls'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
import {
  getLocalDrivingLicenseApplications,
  deleteLocalDrivingLicenseApplication,
  cancelLocalDrivingLicenseApplication,
} from '../../services/localDrivingLicenseApplicationService'
import {
  getLicenseDetailsByLocalAppId,
} from '../../services/licenseService'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type { License } from '../../types/License'
import { formatNumber } from '../../utils/formatters'
import { useAsyncData, usePagination, useDebounce } from '../../hooks'
import {
  Plus as PlusIcon,
  FileText as ApplicationsIcon,
  Check as CheckIcon,
  Clock as ClockIcon,
} from 'lucide-react'
import './local-applications.css'

export type LocalApplicationsPageProps = {
  accessToken: string
  onShowLicenseHistory?: (personId: number) => void
  onShowAppointmentDetails?: (appointmentId: number) => void
  onManageTestAppointments?: (
    localAppId: number,
    sourceOrTestType?: 1 | 2 | 3 | string,
    source?: string,
  ) => void
  onScheduleAppointment?: (
    localAppId: number,
    testTypeId: 1 | 2 | 3,
    source?: string,
    reopenModalOnReturn?: boolean,
  ) => void
  reopenModalTarget?: {
    localAppId: number
    testTypeId: 1 | 2 | 3
  } | null
  onClearReopenModalTarget?: () => void
}

const PAGE_SIZE = 10

type View =
  | { mode: 'list' }
  | { mode: 'create' }
  | { mode: 'edit'; application: LocalDrivingLicenseApplication }

type SearchBy = 'all' | 'name' | 'nationalNo' | 'appId' | 'class'

const SEARCH_OPTIONS: { value: SearchBy; label: string }[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'name', label: 'Applicant Name' },
  { value: 'nationalNo', label: 'National No.' },
  { value: 'appId', label: 'Application ID' },
  { value: 'class', label: 'Driving Class' },
]

export default function LocalApplicationsPage({
  accessToken,
  onShowLicenseHistory,
  onManageTestAppointments,
  onScheduleAppointment,
  reopenModalTarget,
  onClearReopenModalTarget,
}: LocalApplicationsPageProps) {
  const [view, setView] = useState<View>({ mode: 'list' })

  // Modals state
  const [selectedAppForDetails, setSelectedAppForDetails] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [deleteTarget, setDeleteTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [cancelTarget, setCancelTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [issueLicenseTarget, setIssueLicenseTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [selectedLicenseForDetails, setSelectedLicenseForDetails] =
    useState<License | null>(null)

  const [actionLoading, setActionLoading] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [status, setStatus] = useState('All')

  const debouncedSearch = useDebounce(search, 300)

  const pagination = usePagination({
    pageSize: PAGE_SIZE,
    initialPage: 1,
  })

  const { resetPage } = pagination

  // Whenever debounced search or filters change, ensure pagination resets to page 1
  useEffect(() => {
    resetPage()
  }, [debouncedSearch, searchBy, status, resetPage])

  const fetcher = useCallback(() => {
    return getLocalDrivingLicenseApplications(
      accessToken,
      pagination.page,
      pagination.pageSize,
      debouncedSearch,
      searchBy,
      status,
    )
  }, [accessToken, pagination.page, pagination.pageSize, debouncedSearch, searchBy, status])

  const {
    data: page,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    dependencies: [pagination.page, pagination.pageSize, debouncedSearch, searchBy, status],
    defaultErrorMessage: 'Could not load local applications.',
  })

  // Auto-redirect to dedicated appointments page if returning from ScheduleAppointmentPage
  useEffect(() => {
    if (!reopenModalTarget) return

    const { localAppId, testTypeId } = reopenModalTarget
    onClearReopenModalTarget?.()
    if (onManageTestAppointments) {
      onManageTestAppointments(localAppId, testTypeId, 'Local Applications')
    }
  }, [reopenModalTarget, onClearReopenModalTarget, onManageTestAppointments])

  const isFiltered = useMemo(() => {
    return search.trim().length > 0 || searchBy !== 'all' || status !== 'All'
  }, [search, searchBy, status])

  const handleResetFilters = useCallback(() => {
    setSearch('')
    setSearchBy('all')
    setStatus('All')
    pagination.resetPage()
  }, [pagination])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'name':
        return 'Search by applicant name...'
      case 'nationalNo':
        return 'Search by national number...'
      case 'appId':
        return 'Search by application ID...'
      case 'class':
        return 'Search by driving class...'
      default:
        return 'Search by name, national no, app ID, class...'
    }
  }, [searchBy])

  function refreshApplications() {
    void refresh()
  }

  function returnToList(saved: boolean) {
    setView({ mode: 'list' })
    if (saved) {
      pagination.resetPage()
      void refresh()
    }
  }

  // Deletion Workflow
  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return
    setActionLoading(true)
    setActionError(null)

    try {
      await deleteLocalDrivingLicenseApplication(
        accessToken,
        deleteTarget.localDrivingLicenseApplicationId,
      )
      setDeleteTarget(null)
      refreshApplications()
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : 'Failed to delete application.',
      )
    } finally {
      setActionLoading(false)
    }
  }

  // Cancellation Workflow
  const handleCancelConfirm = async () => {
    if (!cancelTarget) return
    setActionLoading(true)
    setActionError(null)

    try {
      await cancelLocalDrivingLicenseApplication(
        accessToken,
        cancelTarget.localDrivingLicenseApplicationId,
      )
      setCancelTarget(null)
      refreshApplications()
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : 'Failed to cancel application.',
      )
    } finally {
      setActionLoading(false)
    }
  }

  // Show License Workflow
  const handleShowLicense = async (app: LocalDrivingLicenseApplication) => {
    try {
      const details = await getLicenseDetailsByLocalAppId(
        accessToken,
        app.localDrivingLicenseApplicationId,
      )
      setSelectedLicenseForDetails({
        licenseId: details.licenseId,
        applicationId: 0,
        driverId: details.driverId,
        driverName: details.fullName,
        licenseClassName: details.licenseClass,
        licenseClassId: 0,
        issueDate: details.issueDate,
        expirationDate: details.expirationDate,
        paidFees: details.licenseClassFees,
        isActive: details.isActive,
        notes: details.notes,
        issueReason: 1,
        issueReasonText: details.issueReason,
        createdByUserId: 0,
        createdByUserName: null,
      })
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : 'Could not retrieve license details.',
      )
    }
  }

  if (view.mode !== 'list') {
    return (
      <section className="page-content local-app-page">
        <LocalApplicationForm
          accessToken={accessToken}
          mode={view.mode}
          application={view.mode === 'edit' ? view.application : undefined}
          onCancel={() => returnToList(false)}
          onSaved={() => returnToList(true)}
        />
      </section>
    )
  }

  return (
    <section className="page-content local-app-page">
      <PageHero pageId="Local Applications" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Applications"
          variant="amber"
          icon={<ApplicationsIcon size={20} />}
          value={loading ? '...' : error ? '—' : formatNumber(page?.totalCount ?? 0)}
          description="Recorded applications in DVLD"
        />

        <StatCard
          label="Loaded on Page"
          variant="emerald"
          icon={<CheckIcon size={20} />}
          value={loading ? '...' : error ? '—' : formatNumber(page?.items.length ?? 0)}
          description={
            error
              ? '—'
              : `Applications on page ${page?.pageNumber ?? 1} of ${page?.totalPages ?? 1}`
          }
        />

        <StatCard
          label="Required Tests"
          variant="slate"
          icon={<ClockIcon size={20} />}
          value="3"
          description="Vision, written, and street tests"
        />
      </StatsGrid>

      <section className="panel">
        <header className="panel-header">
          <div>
            <h2>Driving License Applications</h2>
            <p>Applications registered and loaded from the DVLD system.</p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span className="record-count">
              {loading
                ? 'Loading...'
                : error
                  ? '—'
                  : isFiltered
                    ? `${page?.totalCount ?? 0} matching ${page?.totalCount === 1 ? 'Application' : 'Applications'}`
                    : `${page?.totalCount ?? 0} Applications`}
            </span>

            <button
              type="button"
              className="primary-button"
              onClick={() => setView({ mode: 'create' })}
            >
              <PlusIcon size={16} />
              <span>New Application</span>
            </button>
          </div>
        </header>

        <TableToolbar<SearchBy>
          searchValue={search}
          onSearchChange={(val) => {
            setSearch(val)
            pagination.resetPage()
          }}
          searchPlaceholder={placeholderText}
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={(val) => {
            setSearchBy(val)
            pagination.resetPage()
          }}
          filters={
            <div style={{ minWidth: 140 }} className="shrink-0">
              <Select
                value={status}
                onValueChange={(val) => {
                  setStatus(val)
                  pagination.resetPage()
                }}
              >
                <SelectTrigger
                  className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by application status"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="All">All Statuses</SelectItem>
                  <SelectItem value="New">New</SelectItem>
                  <SelectItem value="Cancelled">Cancelled</SelectItem>
                  <SelectItem value="Completed">Completed</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          onReset={handleResetFilters}
          isFiltered={isFiltered}
        />

        {actionError && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={actionError}
              onRetry={() => setActionError(null)}
              retryLabel="Dismiss"
            />
          </div>
        )}

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={refreshApplications}
              retryLabel="Retry"
            />
          </div>
        )}

        <LocalApplicationsList
          items={page?.items ?? []}
          loading={loading}
          onEdit={(application) => setView({ mode: 'edit', application })}
          onShowDetails={(application) => setSelectedAppForDetails(application)}
          onDelete={(application) => {
            setActionError(null)
            setDeleteTarget(application)
          }}
          onCancel={(application) => {
            setActionError(null)
            setCancelTarget(application)
          }}
          onScheduleTest={(application, testTypeId) => {
            if (onScheduleAppointment) {
              onScheduleAppointment(
                application.localDrivingLicenseApplicationId,
                testTypeId,
                'Local Applications',
              )
            } else if (onManageTestAppointments) {
              onManageTestAppointments(
                application.localDrivingLicenseApplicationId,
                testTypeId,
                'Local Applications',
              )
            }
          }}
          onManageTests={(application) => {
            if (onManageTestAppointments) {
              onManageTestAppointments(
                application.localDrivingLicenseApplicationId,
                'Local Applications',
              )
            }
          }}
          onIssueLicense={(application) => setIssueLicenseTarget(application)}
          onShowLicense={handleShowLicense}
          onShowPersonLicenseHistory={(personId) => onShowLicenseHistory?.(personId)}
        />

        {!error && (page?.totalCount ?? 0) > 0 && (
          <PaginationControls
            pageNumber={page?.pageNumber ?? pagination.page}
            pageSize={pagination.pageSize}
            totalCount={page?.totalCount ?? 0}
            totalPages={page?.totalPages ?? 1}
            hasPreviousPage={Boolean(page?.hasPreviousPage)}
            hasNextPage={Boolean(page?.hasNextPage)}
            onPreviousPage={pagination.previousPage}
            onNextPage={pagination.nextPage}
            onPageSizeChange={pagination.setPageSize}
            loading={loading}
            itemName="applications"
          />
        )}
      </section>

      {/* Application Details Modal */}
      <ApplicationDetailsModal
        isOpen={Boolean(selectedAppForDetails)}
        onClose={() => setSelectedAppForDetails(null)}
        application={selectedAppForDetails}
        accessToken={accessToken}
      />


      {/* Issue Driving License Modal */}
      <IssueDrivingLicenseModal
        isOpen={Boolean(issueLicenseTarget)}
        onClose={() => setIssueLicenseTarget(null)}
        application={issueLicenseTarget}
        accessToken={accessToken}
        onLicenseIssued={() => {
          setIssueLicenseTarget(null)
          refreshApplications()
        }}
      />

      {/* Show License Modal */}
      <LicenseDetailsModal
        isOpen={Boolean(selectedLicenseForDetails)}
        onClose={() => setSelectedLicenseForDetails(null)}
        accessToken={accessToken}
        target={
          selectedLicenseForDetails
            ? { type: 'local', item: selectedLicenseForDetails }
            : null
        }
      />

      {/* Delete Application Confirmation */}
      <ConfirmModal
        isOpen={Boolean(deleteTarget)}
        title="Delete Application"
        message={`Are you sure you want to delete application #${deleteTarget?.localDrivingLicenseApplicationId} for "${deleteTarget?.fullName}"? This action cannot be undone.`}
        confirmText="Delete"
        confirmVariant="danger"
        loading={actionLoading}
        onConfirm={handleDeleteConfirm}
        onCancel={() => {
          setDeleteTarget(null)
          setActionError(null)
        }}
      />

      {/* Cancel Application Confirmation */}
      <ConfirmModal
        isOpen={Boolean(cancelTarget)}
        title="Cancel Application"
        message={`Are you sure you want to cancel application #${cancelTarget?.localDrivingLicenseApplicationId}? Cancelled applications cannot be resumed or modified.`}
        confirmText="Cancel Application"
        confirmVariant="danger"
        loading={actionLoading}
        onConfirm={handleCancelConfirm}
        onCancel={() => {
          setCancelTarget(null)
          setActionError(null)
        }}
      />
    </section>
  )
}
