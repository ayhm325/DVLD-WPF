import { useCallback, useMemo } from 'react'
import type { InternationalLicense } from '../../types/InternationalLicense'
import { getInternationalLicenses } from '../../services/internationalLicenseService'
import { formatNumber } from '../../utils/formatters'
import { useAsyncData, useTableFilters } from '../../hooks'
import InternationalLicenseTable from './InternationalLicenseTable'
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
  Plus as PlusIcon,
  Globe as GlobeIcon,
  Check as CheckIcon,
  Clock as ClockIcon,
} from 'lucide-react'
import './international-licenses.css'

type Props = {
  accessToken: string
  onIssueNew?: () => void
  onOpenDetails?: (id: number) => void
}

type FilterKey =
  | 'all'
  | 'internationalLicenseId'
  | 'applicationId'
  | 'driverId'
  | 'issuedUsingLocalLicenseId'

const SEARCH_OPTIONS: { value: FilterKey; label: string }[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'internationalLicenseId', label: "Int'l License ID" },
  { value: 'applicationId', label: 'Application ID' },
  { value: 'driverId', label: 'Driver ID' },
  { value: 'issuedUsingLocalLicenseId', label: 'Local License ID' },
]

interface InternationalLicenseFilters extends Record<string, unknown> {
  status: 'All' | 'Active' | 'Inactive'
}

export default function InternationalLicensesPage({
  accessToken,
  onIssueNew,
  onOpenDetails,
}: Props) {
  const fetcher = useCallback(() => getInternationalLicenses(accessToken), [accessToken])

  const {
    data: items,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Could not load international licenses.',
  })

  const {
    search,
    setSearch,
    searchField: filter,
    setSearchField: setFilter,
    filters,
    setFilter: setCustomFilter,
    filteredData: filteredItems,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<InternationalLicense, FilterKey, InternationalLicenseFilters>({
    data: items,
    initialSearchField: 'all',
    initialFilters: { status: 'All' },
    filterItem: (item, query, field, currentFilters) => {
      // 1. Status check
      const currentStatus = currentFilters.status
      const matchesStatus =
        currentStatus === 'All' ||
        (currentStatus === 'Active' ? item.isActive : !item.isActive)

      if (!matchesStatus) return false

      // 2. Query check
      if (!query) return true

      if (field === 'all') {
        return (
          String(item.internationalLicenseId).includes(query) ||
          String(item.applicationId).includes(query) ||
          String(item.driverId).includes(query) ||
          String(item.issuedUsingLocalLicenseId).includes(query)
        )
      }

      return String(item[field]).toLowerCase().includes(query)
    },
  })

  const status = filters.status

  const placeholderText = useMemo(() => {
    switch (filter) {
      case 'internationalLicenseId':
        return 'Search by international license ID...'
      case 'applicationId':
        return 'Search by application ID...'
      case 'driverId':
        return 'Search by driver ID...'
      case 'issuedUsingLocalLicenseId':
        return 'Search by local license ID...'
      default:
        return 'Search by any ID (intl, app, driver, local)...'
    }
  }, [filter])

  return (
    <section className="page-content intl-page">
      <PageHero pageId="International Licenses" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Licenses"
          variant="violet"
          icon={<GlobeIcon size={20} />}
          value={loading || error ? '—' : formatNumber(items.length)}
          description="International licenses recorded"
        />

        <StatCard
          label="Active Licenses"
          variant="emerald"
          icon={<CheckIcon size={20} />}
          value={loading || error ? '—' : formatNumber(items.filter((item) => item.isActive).length)}
          description="Currently active licenses"
        />

        <StatCard
          label="Inactive Licenses"
          variant="slate"
          icon={<ClockIcon size={20} />}
          value={loading || error ? '—' : formatNumber(items.filter((item) => !item.isActive).length)}
          description="Not currently active"
        />
      </StatsGrid>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>International Licenses Directory</h2>
            <p>Review issued international licenses and verify validity.</p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span className="record-count">
              {loading
                ? 'Loading...'
                : filteredItems.length === items.length
                  ? `${items.length} Licenses`
                  : `${filteredItems.length} of ${items.length} Licenses`}
            </span>

            {onIssueNew && (
              <button
                type="button"
                className="primary-button"
                onClick={onIssueNew}
              >
                <PlusIcon size={16} />
                <span>Issue International License</span>
              </button>
            )}
          </div>
        </div>

        <TableToolbar<FilterKey>
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchField={filter}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={setFilter}
          filters={
            <div style={{ minWidth: 140 }} className="shrink-0">
              <Select
                value={status}
                onValueChange={(val) =>
                  setCustomFilter('status', val as typeof status)
                }
              >
                <SelectTrigger
                  className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by status"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="All">All statuses</SelectItem>
                  <SelectItem value="Active">Active</SelectItem>
                  <SelectItem value="Inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          onReset={handleResetFilters}
          isFiltered={isFiltered}
          onRefresh={refresh}
          refreshing={loading}
        />

        {loading && <p className="empty-state">Loading international licenses...</p>}

        {!loading && error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={refresh}
              retryLabel="Try again"
            />
          </div>
        )}

        {!loading && !error && (
          <InternationalLicenseTable
            items={filteredItems}
            onOpenDetails={onOpenDetails}
          />
        )}

        {!loading && !error && (
          <footer className="panel-footer" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>
              Showing <strong>{filteredItems.length}</strong> of{' '}
              <strong>{items.length}</strong> licenses
            </span>
            <span>Use the row action to inspect a license.</span>
          </footer>
        )}
      </section>
    </section>
  )
}
