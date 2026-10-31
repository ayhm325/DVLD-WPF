import { useCallback, useMemo } from 'react'
import { getDrivers } from '../services/driverService'
import type { Driver } from '../types/Driver'
import { formatDate, formatNumber } from '../utils/formatters'
import { useAsyncData, useTableFilters } from '../hooks'
import PageHero from './common/PageHero'
import StatsGrid from './common/StatsGrid'
import StatCard from './common/StatCard'
import TableToolbar from './common/TableToolbar'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import DataTable, { type DataTableColumn } from './common/DataTable'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select'
import {
  Car as DriversIcon,
  IdCard as LicenseIcon,
  Check as CheckIcon,
} from 'lucide-react'

export type DriversPageProps = {
  accessToken?: string
  drivers?: Driver[]
  loading?: boolean
  error?: string
  onRetry?: () => void
}

type SearchBy = 'all' | 'driverId' | 'personId' | 'name' | 'nationalNo'
type LicenseFilter = 'all' | 'active' | 'none'

interface DriverFilters extends Record<string, unknown> {
  licenseFilter: LicenseFilter
}

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: 'all', label: 'All Fields' },
  { value: 'name', label: 'Full Name' },
  { value: 'nationalNo', label: 'National No.' },
  { value: 'driverId', label: 'Driver ID' },
  { value: 'personId', label: 'Person ID' },
]

const columns: DataTableColumn<Driver>[] = [
  {
    key: 'driverId',
    header: 'Driver ID',
    render: (driver) => (
      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
        #{driver.driverId}
      </span>
    ),
  },
  {
    key: 'personId',
    header: 'Person ID',
    render: (driver) => (
      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
        #{driver.personId}
      </span>
    ),
  },
  {
    key: 'fullName',
    header: 'Full Name',
    render: (driver) => (
      <div className="person-cell">
        <span className="person-avatar" aria-hidden="true">
          {driver.fullName ? driver.fullName.charAt(0).toUpperCase() : 'D'}
        </span>
        <strong>{driver.fullName}</strong>
      </div>
    ),
  },
  {
    key: 'nationalNo',
    header: 'National No.',
    render: (driver) => (
      <span style={{ fontVariantNumeric: 'tabular-nums', letterSpacing: '0.3px', fontWeight: 600 }}>
        {driver.nationalNo}
      </span>
    ),
  },
  {
    key: 'dateOfBirth',
    header: 'Date of Birth',
    render: (driver) => formatDate(driver.dateOfBirth),
  },
  {
    key: 'activeLicenses',
    header: 'Active Licenses',
    render: (driver) => (
      <span className="record-count" style={{ padding: '2px 8px', fontSize: 11 }}>
        {driver.activeLicenses}
      </span>
    ),
  },
  {
    key: 'createdDate',
    header: 'Created Date',
    render: (driver) => formatDate(driver.createdDate),
  },
]

export default function DriversPage({
  accessToken,
  drivers: externalDrivers,
  loading: externalLoading,
  error: externalError,
  onRetry: externalOnRetry,
}: DriversPageProps) {
  const fetcher = useCallback(() => getDrivers(accessToken!), [accessToken])

  const {
    data: internalDrivers,
    loading: internalLoading,
    error: internalError,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken && externalDrivers === undefined),
    initialData: [],
    defaultErrorMessage: 'Failed to load drivers.',
  })

  const drivers = externalDrivers ?? internalDrivers
  const loading = externalLoading ?? internalLoading
  const error = externalError ?? internalError
  const onRetry = externalOnRetry ?? (() => void refresh())

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredDrivers,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<Driver, SearchBy, DriverFilters>({
    data: drivers,
    initialSearchField: 'all',
    initialFilters: { licenseFilter: 'all' },
    filterItem: (driver, query, field, currentFilters) => {
      // 1. License filter
      if (currentFilters.licenseFilter === 'active' && (driver.activeLicenses || 0) <= 0) {
        return false
      }
      if (currentFilters.licenseFilter === 'none' && (driver.activeLicenses || 0) > 0) {
        return false
      }

      // 2. Query filter
      if (!query) return true

      if (field === 'driverId') {
        return String(driver.driverId).includes(query)
      }
      if (field === 'personId') {
        return String(driver.personId).includes(query)
      }
      if (field === 'name') {
        return (driver.fullName ?? '').toLowerCase().includes(query)
      }
      if (field === 'nationalNo') {
        return (driver.nationalNo ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return [
        String(driver.driverId),
        String(driver.personId),
        driver.fullName,
        driver.nationalNo,
      ].some((value) => (value ?? '').toLowerCase().includes(query))
    },
  })

  const licenseFilter = filters.licenseFilter

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'driverId':
        return 'Search by driver ID...'
      case 'personId':
        return 'Search by person ID...'
      case 'name':
        return 'Search by driver full name...'
      case 'nationalNo':
        return 'Search by national number...'
      default:
        return 'Search by name, national no, driver ID...'
    }
  }, [searchBy])

  const totalDrivers = drivers.length
  const totalActiveLicenses = drivers.reduce(
    (total, driver) => total + (driver.activeLicenses || 0),
    0,
  )
  const driversWithLicenses = drivers.filter(
    (d) => (d.activeLicenses || 0) > 0,
  ).length

  return (
    <section className="page-content">
      <PageHero pageId="Drivers" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Drivers"
          variant="blue"
          icon={<DriversIcon size={20} />}
          value={loading ? '...' : error ? '—' : formatNumber(totalDrivers)}
          description="Registered drivers"
        />

        <StatCard
          label="Active Licenses"
          variant="emerald"
          icon={<LicenseIcon size={20} />}
          value={loading ? '...' : error ? '—' : formatNumber(totalActiveLicenses)}
          description="Across listed drivers"
        />

        <StatCard
          label="Licensed Drivers"
          variant="emerald"
          icon={<CheckIcon size={20} />}
          value={loading ? '...' : error ? '—' : formatNumber(driversWithLicenses)}
          description="Holding active licenses"
        />
      </StatsGrid>

      <div className="panel">
        <div className="panel-header">
          <div>
            <h2>Driver Directory</h2>
            <p>Browse and search registered drivers in the system.</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : filteredDrivers.length === drivers.length
                ? `${drivers.length} Drivers`
                : `${filteredDrivers.length} of ${drivers.length} Drivers`}
          </span>
        </div>

        <TableToolbar
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={(val) => setSearchBy(val as SearchBy)}
          searchFieldMinWidth={140}
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchAriaLabel="Search drivers"
          filters={
            <div style={{ minWidth: 160 }} className="shrink-0">
              <Select
                value={licenseFilter}
                onValueChange={(val) => setFilter('licenseFilter', val as LicenseFilter)}
              >
                <SelectTrigger
                  className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by license status"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Drivers</SelectItem>
                  <SelectItem value="active">Has Active License</SelectItem>
                  <SelectItem value="none">No Active License</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={onRetry}
          refreshing={loading}
        />

        {loading && <p className="empty-state">Loading drivers...</p>}

        {!loading && error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={onRetry}
              retryLabel="Try again"
            />
          </div>
        )}

        {!error && (
          <DataTable<Driver>
            data={filteredDrivers}
            columns={columns}
            getRowKey={(driver) => driver.driverId}
            loading={loading}
            emptyState={
              <EmptyState
                icon={<DriversIcon size={36} />}
                message={
                  drivers.length === 0
                    ? 'No drivers found.'
                    : 'No drivers match your search filters.'
                }
              />
            }
            minWidth={850}
          />
        )}
      </div>
    </section>
  )
}
