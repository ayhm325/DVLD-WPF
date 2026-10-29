import { useEffect, useMemo, useState } from 'react'
import { getLicenses } from '../services/licenseService'
import type { License } from '../services/licenseService'
import PageHero from './common/PageHero'
import StatsGrid from './common/StatsGrid'
import StatCard from './common/StatCard'
import SearchInput from './common/SearchInput'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import {
  LicenseIcon,
  CheckIcon,
  ClockIcon,
  RefreshIcon,
  FilterIcon,
} from './common/Icons'

type LicensesPageProps = {
  accessToken: string
}

type SearchBy = 'all' | 'licenseId' | 'appId' | 'driver' | 'class'

export default function LicensesPage({
  accessToken,
}: LicensesPageProps) {
  const [licenses, setLicenses] = useState<License[]>([])
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [statusFilter, setStatusFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let isCancelled = false

    async function loadLicenses() {
      setLoading(true)
      setError('')

      try {
        const data = await getLicenses(accessToken)

        if (!isCancelled) {
          setLicenses(data)
        }
      } catch (err) {
        if (!isCancelled) {
          setError(
            err instanceof Error ? err.message : 'Failed to load licenses.',
          )
        }
      } finally {
        if (!isCancelled) {
          setLoading(false)
        }
      }
    }

    void loadLicenses()

    return () => {
      isCancelled = true
    }
  }, [accessToken, refreshKey])

  const handleResetFilters = () => {
    setSearch('')
    setSearchBy('all')
    setStatusFilter('all')
  }

  const isFiltered = Boolean(search.trim() || statusFilter !== 'all' || searchBy !== 'all')

  const filteredLicenses = useMemo(() => {
    const query = search.trim().toLowerCase()

    return licenses.filter((license) => {
      // 1. Status check
      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && license.isActive) ||
        (statusFilter === 'inactive' && !license.isActive)

      if (!matchesStatus) return false

      // 2. Query check
      if (!query) return true

      if (searchBy === 'licenseId') {
        return String(license.licenseId).includes(query)
      }
      if (searchBy === 'appId') {
        return String(license.applicationId).includes(query)
      }
      if (searchBy === 'driver') {
        return (license.driverName ?? '').toLowerCase().includes(query)
      }
      if (searchBy === 'class') {
        return (license.licenseClassName ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return (
        String(license.licenseId).includes(query) ||
        String(license.applicationId).includes(query) ||
        (license.driverName ?? '').toLowerCase().includes(query) ||
        (license.licenseClassName ?? '').toLowerCase().includes(query)
      )
    })
  }, [licenses, search, searchBy, statusFilter])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'licenseId':
        return 'Search by license ID...'
      case 'appId':
        return 'Search by application ID...'
      case 'driver':
        return 'Search by driver name...'
      case 'class':
        return 'Search by license class...'
      default:
        return 'Search by license ID, driver, or class...'
    }
  }, [searchBy])

  function formatDate(value?: string) {
    if (!value) return '—'

    const date = new Date(value)

    if (Number.isNaN(date.getTime())) return '—'

    return date.toLocaleDateString()
  }

  function formatFees(value: number) {
    return new Intl.NumberFormat('en-JO', {
      style: 'currency',
      currency: 'JOD',
      minimumFractionDigits: 2,
    }).format(value)
  }

  return (
    <section className="page-content">
      <PageHero pageId="Licenses" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Licenses"
          icon={<LicenseIcon size={20} />}
          value={loading ? '...' : error ? '—' : licenses.length}
          description="All returned records"
        />

        <StatCard
          label="Active Licenses"
          icon={<CheckIcon size={20} />}
          iconStyle={{ color: '#059669', background: '#ecfdf5' }}
          value={<span style={{ color: '#059669' }}>{loading ? '...' : error ? '—' : licenses.filter((license) => license.isActive).length}</span>}
          description="Marked active by the system"
        />

        <StatCard
          label="Inactive Licenses"
          icon={<ClockIcon size={20} />}
          iconStyle={{ color: '#64748b', background: '#f1f5f9' }}
          value={loading ? '...' : error ? '—' : licenses.filter((license) => !license.isActive).length}
          description="Not currently active"
        />
      </StatsGrid>

      <div className="panel">
        <div className="panel-header">
          <div>
            <h2>License Directory</h2>
            <p>View and search issued driving licenses.</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : filteredLicenses.length === licenses.length
                ? `${licenses.length} Licenses`
                : `${filteredLicenses.length} of ${licenses.length} Licenses`}
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
            <option value="licenseId">License ID</option>
            <option value="appId">Application ID</option>
            <option value="driver">Driver Name</option>
            <option value="class">License Class</option>
          </select>

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder={placeholderText}
            ariaLabel="Search licenses"
          />

          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            aria-label="Filter by license status"
          >
            <option value="all">All statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
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
            onClick={() => setRefreshKey((value) => value + 1)}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        </div>

        {loading && <p className="empty-state">Loading licenses...</p>}

        {!loading && error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={() => setRefreshKey((value) => value + 1)}
              retryLabel="Try again"
            />
          </div>
        )}

        {!loading && !error && (
          <div className="table-container">
            <table className="people-table">
              <thead>
                <tr>
                  <th>License ID</th>
                  <th>App ID</th>
                  <th>Driver Name</th>
                  <th>Class</th>
                  <th>Issue Date</th>
                  <th>Expiration Date</th>
                  <th>Fees</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                {filteredLicenses.map((license) => (
                  <tr key={license.licenseId}>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{license.licenseId}
                      </span>
                    </td>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{license.applicationId}
                      </span>
                    </td>
                    <td>
                      <strong style={{ color: 'var(--dvld-heading)' }}>
                        {license.driverName ?? '—'}
                      </strong>
                    </td>
                    <td>{license.licenseClassName ?? '—'}</td>
                    <td>{formatDate(license.issueDate)}</td>
                    <td>{formatDate(license.expirationDate)}</td>
                    <td>{formatFees(license.paidFees)}</td>
                    <td>
                      <span
                        className={`badge ${
                          license.isActive ? 'badge-success' : 'badge-danger'
                        }`}
                      >
                        {license.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                  </tr>
                ))}

                {filteredLicenses.length === 0 && (
                  <tr>
                    <td colSpan={8}>
                      <EmptyState
                        icon={<LicenseIcon size={36} />}
                        message={
                          licenses.length === 0
                            ? 'No licenses found.'
                            : 'No licenses match your filters.'}
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
