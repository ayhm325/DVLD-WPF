import { useEffect, useMemo, useState } from 'react'
import { getDrivers } from '../services/driverService'
import type { Driver } from '../services/driverService'
import PageHero from './common/PageHero'
import {
  DriversIcon,
  LicenseIcon,
  RefreshIcon,
  SearchIcon,
  CloseIcon,
  FilterIcon,
  CheckIcon,
} from './common/Icons'

export type DriversPageProps = {
  accessToken?: string
  drivers?: Driver[]
  loading?: boolean
  error?: string
  onRetry?: () => void
}

type SearchBy = 'all' | 'driverId' | 'personId' | 'name' | 'nationalNo'
type LicenseFilter = 'all' | 'active' | 'none'

export default function DriversPage({
  accessToken,
  drivers: externalDrivers,
  loading: externalLoading,
  error: externalError,
  onRetry: externalOnRetry,
}: DriversPageProps) {
  const [internalDrivers, setInternalDrivers] = useState<Driver[]>([])
  const [internalLoading, setInternalLoading] = useState(
    Boolean(accessToken && externalDrivers === undefined),
  )
  const [internalError, setInternalError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [licenseFilter, setLicenseFilter] = useState<LicenseFilter>('all')

  useEffect(() => {
    if (!accessToken || externalDrivers !== undefined) return

    let cancelled = false

    async function load() {
      setInternalLoading(true)
      setInternalError('')

      try {
        const data = await getDrivers(accessToken!)
        if (!cancelled) setInternalDrivers(data)
      } catch (err) {
        if (!cancelled) {
          setInternalError(
            err instanceof Error ? err.message : 'Failed to load drivers.',
          )
        }
      } finally {
        if (!cancelled) setInternalLoading(false)
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [accessToken, externalDrivers, refreshKey])

  const drivers = externalDrivers ?? internalDrivers
  const loading = externalLoading ?? internalLoading
  const error = externalError ?? internalError
  const onRetry = externalOnRetry ?? (() => setRefreshKey((k) => k + 1))

  const handleResetFilters = () => {
    setSearch('')
    setSearchBy('all')
    setLicenseFilter('all')
  }

  const isFiltered = Boolean(search.trim() || searchBy !== 'all' || licenseFilter !== 'all')

  const filteredDrivers = useMemo(() => {
    const query = search.trim().toLowerCase()

    return drivers.filter((driver) => {
      // 1. License filter
      if (licenseFilter === 'active' && (driver.activeLicenses || 0) <= 0) {
        return false
      }
      if (licenseFilter === 'none' && (driver.activeLicenses || 0) > 0) {
        return false
      }

      // 2. Query filter
      if (!query) return true

      if (searchBy === 'driverId') {
        return String(driver.driverId).includes(query)
      }
      if (searchBy === 'personId') {
        return String(driver.personId).includes(query)
      }
      if (searchBy === 'name') {
        return (driver.fullName ?? '').toLowerCase().includes(query)
      }
      if (searchBy === 'nationalNo') {
        return (driver.nationalNo ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return [
        String(driver.driverId),
        String(driver.personId),
        driver.fullName,
        driver.nationalNo,
      ].some((value) => (value ?? '').toLowerCase().includes(query))
    })
  }, [drivers, search, searchBy, licenseFilter])

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

      <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total Drivers</span>
            <span className="stat-icon" aria-hidden="true">
              <DriversIcon size={20} />
            </span>
          </div>
          <strong>{loading ? '...' : error ? '—' : totalDrivers}</strong>
          <p>Registered drivers</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Active Licenses</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#059669', background: '#ecfdf5' }}>
              <LicenseIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#059669' }}>
            {loading ? '...' : error ? '—' : totalActiveLicenses}
          </strong>
          <p>Across listed drivers</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Licensed Drivers</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#2563eb', background: '#eff6ff' }}>
              <CheckIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#2563eb' }}>
            {loading ? '...' : error ? '—' : driversWithLicenses}
          </strong>
          <p>Holding active licenses</p>
        </article>
      </div>

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

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={searchBy}
            onChange={(event) => setSearchBy(event.target.value as SearchBy)}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="name">Full Name</option>
            <option value="nationalNo">National No.</option>
            <option value="driverId">Driver ID</option>
            <option value="personId">Person ID</option>
          </select>

          <div className="search-wrapper">
            <span className="search-icon-left" aria-hidden="true">
              <SearchIcon size={16} />
            </span>

            <input
              type="text"
              className="search-input"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={placeholderText}
              aria-label="Search drivers"
            />

            {search && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => setSearch('')}
                aria-label="Clear search"
              >
                <CloseIcon size={14} />
              </button>
            )}
          </div>

          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 160 }}
            value={licenseFilter}
            onChange={(event) => setLicenseFilter(event.target.value as LicenseFilter)}
            aria-label="Filter by license status"
          >
            <option value="all">All Drivers</option>
            <option value="active">Has Active License</option>
            <option value="none">No Active License</option>
          </select>

          {isFiltered && (
            <button
              type="button"
              className="secondary-button"
              onClick={handleResetFilters}
              title="Reset all filters"
            >
              <FilterIcon size={14} />
              <span>Reset</span>
            </button>
          )}

          <button
            type="button"
            className="secondary-button"
            onClick={onRetry}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        </div>

        {loading && <p className="empty-state">Loading drivers...</p>}

        {!loading && error && (
          <div role="alert" className="login-error" style={{ margin: '0 25px 20px' }}>
            <p>{error}</p>
            <button
              type="button"
              className="primary-button"
              onClick={onRetry}
            >
              Try again
            </button>
          </div>
        )}

        {!loading && !error && filteredDrivers.length === 0 && (
          <div className="empty-state">
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
              <DriversIcon size={36} />
              <span>
                {drivers.length === 0
                  ? 'No drivers found.'
                  : 'No drivers match your search filters.'}
              </span>
            </div>
          </div>
        )}

        {!loading && !error && filteredDrivers.length > 0 && (
          <div className="table-container">
            <table className="people-table">
              <thead>
                <tr>
                  <th>Driver ID</th>
                  <th>Person ID</th>
                  <th>Full Name</th>
                  <th>National No.</th>
                  <th>Date of Birth</th>
                  <th>Active Licenses</th>
                  <th>Created Date</th>
                </tr>
              </thead>

              <tbody>
                {filteredDrivers.map((driver) => (
                  <tr key={driver.driverId}>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{driver.driverId}
                      </span>
                    </td>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{driver.personId}
                      </span>
                    </td>
                    <td>
                      <div className="person-cell">
                        <span className="person-avatar" aria-hidden="true">
                          {driver.fullName ? driver.fullName.charAt(0).toUpperCase() : 'D'}
                        </span>
                        <strong>{driver.fullName}</strong>
                      </div>
                    </td>
                    <td>
                      <span style={{ fontVariantNumeric: 'tabular-nums', letterSpacing: '0.3px', fontWeight: 600 }}>
                        {driver.nationalNo}
                      </span>
                    </td>
                    <td>
                      {driver.dateOfBirth
                        ? new Date(driver.dateOfBirth).toLocaleDateString()
                        : '—'}
                    </td>
                    <td>
                      <span className="record-count" style={{ padding: '2px 8px', fontSize: 11 }}>
                        {driver.activeLicenses}
                      </span>
                    </td>
                    <td>
                      {driver.createdDate
                        ? new Date(driver.createdDate).toLocaleDateString()
                        : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
