import { useCallback, useEffect, useMemo, useState } from 'react'
import LocalApplicationsList from './LocalApplicationsList'
import LocalApplicationForm from './LocalApplicationForm'
import ApplicationDetailsModal from './ApplicationDetailsModal'
import TestAppointmentsModal from './TestAppointmentsModal'
import IssueDrivingLicenseModal from './IssueDrivingLicenseModal'
import LicenseDetailsModal from '../licenses/LicenseDetailsModal'
import ConfirmModal from '../common/ConfirmModal'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import TableToolbar from '../common/TableToolbar'
import ErrorAlert from '../common/ErrorAlert'
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
  getLicensesByPersonId,
  getLicenseDetailsByLocalAppId,
} from '../../services/licenseService'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type { License } from '../../types/License'
import { formatNumber } from '../../utils/formatters'
import { useAsyncData, useTableFilters, usePagination } from '../../hooks'
import {
  Plus as PlusIcon,
  ChevronLeft as ChevronLeftIcon,
  ChevronRight as ChevronRightIcon,
  FileText as ApplicationsIcon,
  Check as CheckIcon,
  Clock as ClockIcon,
} from 'lucide-react'
import './local-applications.css'

export type LocalApplicationsPageProps = {
  accessToken: string
  onShowLicenseHistory?: (personId: number) => void
  onShowAppointmentDetails?: (appointmentId: number) => void
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

interface LocalAppFilters extends Record<string, unknown> {
  status: string
}

export default function LocalApplicationsPage({
  accessToken,
  onShowLicenseHistory,
  onShowAppointmentDetails,
}: LocalApplicationsPageProps) {
  const [view, setView] = useState<View>({ mode: 'list' })

  // Modals state
  const [selectedAppForDetails, setSelectedAppForDetails] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [deleteTarget, setDeleteTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [cancelTarget, setCancelTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [testAppointmentTarget, setTestAppointmentTarget] = useState<{
    application: LocalDrivingLicenseApplication
    testTypeId: 1 | 2 | 3
  } | null>(null)
  const [issueLicenseTarget, setIssueLicenseTarget] =
    useState<LocalDrivingLicenseApplication | null>(null)
  const [selectedLicenseForDetails, setSelectedLicenseForDetails] =
    useState<License | null>(null)

  const [actionLoading, setActionLoading] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [personLicenseHistoryMap, setPersonLicenseHistoryMap] = useState<
    Record<number, boolean>
  >({})

  const pagination = usePagination({
    pageSize: PAGE_SIZE,
    initialPage: 1,
  })

  const fetcher = useCallback(() => {
    return getLocalDrivingLicenseApplications(
      accessToken,
      pagination.page,
      PAGE_SIZE,
    )
  }, [accessToken, pagination.page])

  const {
    data: page,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    dependencies: [pagination.page],
    defaultErrorMessage: 'Could not load local applications.',
  })

  // Pre-fetch person license counts for rows on the current page to accurately enable/disable "Show Person License History"
  useEffect(() => {
    if (!accessToken || !page?.items || page.items.length === 0) return

    let cancelled = false
    const uniquePersonIds = Array.from(
      new Set(
        page.items
          .map((x) => x.applicantPersonId)
          .filter((id) => Boolean(id) && id > 0),
      ),
    )

    Promise.all(
      uniquePersonIds.map(async (personId) => {
        try {
          const licenses = await getLicensesByPersonId(accessToken, personId)
          return { personId, hasHistory: licenses.length > 0 }
        } catch {
          return { personId, hasHistory: false }
        }
      }),
    ).then((results) => {
      if (cancelled) return
      const map: Record<number, boolean> = {}
      for (const r of results) {
        map[r.personId] = r.hasHistory
      }
      setPersonLicenseHistoryMap(map)
    })

    return () => {
      cancelled = true
    }
  }, [accessToken, page?.items])

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredItems,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<LocalDrivingLicenseApplication, SearchBy, LocalAppFilters>({
    data: page?.items ?? [],
    initialSearchField: 'all',
    initialFilters: { status: 'All' },
    onReset: pagination.resetPage,
    filterItem: (item, query, field, currentFilters) => {
      const statusText = (
        item.statusText || item.applicationStatus || ''
      ).toLowerCase()

      const matchesStatus =
        currentFilters.status === 'All' ||
        statusText === currentFilters.status.toLowerCase()

      if (!matchesStatus) return false
      if (!query) return true

      if (field === 'name') {
        return (item.fullName ?? '').toLowerCase().includes(query)
      }
      if (field === 'nationalNo') {
        return (item.nationalNo ?? '').toLowerCase().includes(query)
      }
      if (field === 'appId') {
        return String(item.localDrivingLicenseApplicationId).includes(query)
      }
      if (field === 'class') {
        return (item.licenseClassName ?? '').toLowerCase().includes(query)
      }

      return (
        (item.fullName ?? '').toLowerCase().includes(query) ||
        (item.nationalNo ?? '').toLowerCase().includes(query) ||
        String(item.localDrivingLicenseApplicationId).includes(query) ||
        (item.licenseClassName ?? '').toLowerCase().includes(query)
      )
    },
  })

  const status = filters.status

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
                    ? `${filteredItems.length} of ${page?.totalCount ?? 0} Applications`
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
                  setFilter('status', val)
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
          items={filteredItems}
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
          onScheduleTest={(application, testTypeId) =>
            setTestAppointmentTarget({ application, testTypeId })
          }
          onIssueLicense={(application) => setIssueLicenseTarget(application)}
          onShowLicense={handleShowLicense}
          onShowPersonLicenseHistory={(personId) => onShowLicenseHistory?.(personId)}
          personLicenseHistoryMap={personLicenseHistoryMap}
        />

        <footer
          className="panel-footer"
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 16,
          }}
        >
          <span>
            {error
              ? 'Failed to load applications'
              : `Showing ${filteredItems.length} of ${page?.totalCount ?? 0} applications`}
          </span>

          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <button
              type="button"
              className="secondary-button"
              style={{ minHeight: 34, padding: '6px 12px', fontSize: 11 }}
              disabled={!page?.hasPreviousPage || loading}
              onClick={pagination.previousPage}
            >
              <ChevronLeftIcon size={14} />
              <span>Previous</span>
            </button>

            <span style={{ fontSize: 11, color: 'var(--dvld-muted)' }}>
              Page{' '}
              <strong style={{ color: 'var(--dvld-heading)' }}>
                {page?.pageNumber ?? pagination.page}
              </strong>{' '}
              of{' '}
              <strong style={{ color: 'var(--dvld-heading)' }}>
                {error ? '—' : page?.totalPages ?? 0}
              </strong>
            </span>

            <button
              type="button"
              className="secondary-button"
              style={{ minHeight: 34, padding: '6px 12px', fontSize: 11 }}
              disabled={!page?.hasNextPage || loading}
              onClick={pagination.nextPage}
            >
              <span>Next</span>
              <ChevronRightIcon size={14} />
            </button>

            <span
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                marginLeft: 6,
                fontSize: 11,
                color: 'var(--dvld-muted)',
              }}
            >
              Rows
              <span
                style={{
                  minHeight: 28,
                  padding: '3px 10px',
                  display: 'inline-flex',
                  alignItems: 'center',
                  fontSize: 11,
                  fontWeight: 600,
                  color: 'var(--dvld-heading)',
                  background: 'var(--dvld-surface-subtle)',
                  border: '1px solid var(--dvld-border)',
                  borderRadius: 'var(--dvld-radius-sm)',
                }}
              >
                {PAGE_SIZE}
              </span>
            </span>
          </div>
        </footer>
      </section>

      {/* Application Details Modal */}
      <ApplicationDetailsModal
        isOpen={Boolean(selectedAppForDetails)}
        onClose={() => setSelectedAppForDetails(null)}
        application={selectedAppForDetails}
        accessToken={accessToken}
      />

      {/* Test Appointments Modal */}
      <TestAppointmentsModal
        isOpen={Boolean(testAppointmentTarget)}
        onClose={() => setTestAppointmentTarget(null)}
        application={testAppointmentTarget?.application ?? null}
        testTypeId={testAppointmentTarget?.testTypeId ?? 1}
        accessToken={accessToken}
        onAppointmentScheduled={refreshApplications}
        onShowAppointmentDetails={onShowAppointmentDetails}
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
