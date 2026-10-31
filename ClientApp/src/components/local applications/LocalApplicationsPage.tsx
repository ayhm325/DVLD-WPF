import { useCallback, useMemo, useState } from 'react'
import LocalApplicationsList from './LocalApplicationsList'
import LocalApplicationForm from './LocalApplicationForm'
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
import { getLocalDrivingLicenseApplications } from '../../services/localDrivingLicenseApplicationService'
import type {
  LocalDrivingLicenseApplication,
} from '../../types/LocalDrivingLicenseApplication'
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

type Props = {
  accessToken: string
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

export default function LocalApplicationsPage({ accessToken }: Props) {
  const [view, setView] = useState<View>({ mode: 'list' })

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
      // 1. Status check
      const statusText = (
        item.statusText || item.applicationStatus || ''
      ).toLowerCase()

      const matchesStatus =
        currentFilters.status === 'All' ||
        statusText === currentFilters.status.toLowerCase()

      if (!matchesStatus) return false

      // 2. Query check
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

      // 'all'
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

  if (view.mode !== 'list') {
    return (
      <section className="page-content local-app-page">
        <LocalApplicationForm
          accessToken={accessToken}
          mode={view.mode}
          application={
            view.mode === 'edit' ? view.application : undefined
          }
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
          description={error ? '—' : `Applications on page ${page?.pageNumber ?? 1} of ${page?.totalPages ?? 1}`}
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
          onEdit={(application) =>
            setView({ mode: 'edit', application })
          }
        />

        <footer className="panel-footer" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
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
              Page <strong style={{ color: 'var(--dvld-heading)' }}>{page?.pageNumber ?? pagination.page}</strong> of{' '}
              <strong style={{ color: 'var(--dvld-heading)' }}>{error ? '—' : page?.totalPages ?? 0}</strong>
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

            <span style={{ display: 'flex', alignItems: 'center', gap: 6, marginLeft: 6, fontSize: 11, color: 'var(--dvld-muted)' }}>
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
    </section>
  )
}
