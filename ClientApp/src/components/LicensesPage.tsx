import { useEffect, useMemo, useState } from 'react'
import { getLicenses } from '../services/licenseService'
import type { License } from '../services/licenseService'
import PageHero from './common/PageHero'
import {
  LicenseIcon,
  CheckIcon,
  ClockIcon,
  RefreshIcon,
  SearchIcon,
  CloseIcon,
} from './common/Icons'

type LicensesPageProps = {
  accessToken: string
}

export default function LicensesPage({
  accessToken,
}: LicensesPageProps) {
  const [licenses, setLicenses] = useState<License[]>([])
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function loadLicenses() {
      setLoading(true)
      setError('')

      try {
        const result = await getLicenses(accessToken)

        if (!cancelled) {
          setLicenses(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load licenses.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadLicenses()

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshKey])

  const filteredLicenses = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase()

    return licenses.filter((license) => {
      const matchesSearch =
        String(license.licenseId).includes(normalizedSearch) ||
        String(license.driverId).includes(normalizedSearch) ||
        String(license.licenseClassId).includes(normalizedSearch) ||
        (license.driverName ?? '')
          .toLowerCase()
          .includes(normalizedSearch) ||
        (license.licenseClassName ?? '')
          .toLowerCase()
          .includes(normalizedSearch)

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && license.isActive) ||
        (statusFilter === 'inactive' && !license.isActive)

      return matchesSearch && matchesStatus
    })
  }, [licenses, search, statusFilter])

  function formatDate(value: string) {
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
      <PageHero
        pageId="Licenses"
        action={
          <button
            type="button"
            className="secondary-button"
            onClick={() => setRefreshKey((value) => value + 1)}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        }
      />

      <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total Licenses</span>
            <span className="stat-icon">
              <LicenseIcon size={20} />
            </span>
          </div>
          <strong>{loading ? '...' : error ? '—' : licenses.length}</strong>
          <p>All returned records</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Active Licenses</span>
            <span className="stat-icon" style={{ color: '#059669', background: '#ecfdf5' }}>
              <CheckIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#059669' }}>
            {loading ? '...' : error ? '—' : licenses.filter((license) => license.isActive).length}
          </strong>
          <p>Marked active by the system</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Inactive Licenses</span>
            <span className="stat-icon" style={{ color: '#64748b', background: '#f1f5f9' }}>
              <ClockIcon size={20} />
            </span>
          </div>
          <strong>
            {loading ? '...' : error ? '—' : licenses.filter((license) => !license.isActive).length}
          </strong>
          <p>Not currently active</p>
        </article>
      </div>

      <div className="panel">
        <div className="table-toolbar">
          <div className="search-wrapper">
            <span className="search-icon-left" aria-hidden="true">
              <SearchIcon size={16} />
            </span>

            <input
              type="text"
              className="search-input"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by license ID, driver, or class..."
              aria-label="Search licenses"
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
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            aria-label="Filter by license status"
          >
            <option value="all">All statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>

          <span className="record-count">{filteredLicenses.length} Licenses</span>
        </div>

        {loading && <p className="empty-state">Loading licenses...</p>}

        {!loading && error && (
          <div className="login-error" style={{ margin: '0 25px 20px' }}>
            <p>{error}</p>
            <button
              type="button"
              className="primary-button"
              onClick={() => setRefreshKey((value) => value + 1)}
            >
              Try again
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="table-container">
            <table className="people-table">
              <thead>
                <tr>
                  <th>License ID</th>
                  <th>Driver</th>
                  <th>License Class</th>
                  <th>Issue Date</th>
                  <th>Expiration Date</th>
                  <th>Paid Fees</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                {filteredLicenses.map((license) => (
                  <tr key={license.licenseId}>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-primary)', fontWeight: 700 }}>
                        #{license.licenseId}
                      </span>
                    </td>

                    <td>
                      <strong>
                        {license.driverName || 'Unknown driver'}
                      </strong>
                      <div style={{ fontSize: 11, color: 'var(--dvld-muted)' }}>
                        Driver ID: {license.driverId}
                      </div>
                    </td>

                    <td>
                      <span style={{ fontWeight: 600 }}>
                        {license.licenseClassName || `Class #${license.licenseClassId}`}
                      </span>
                    </td>

                    <td>{formatDate(license.issueDate)}</td>
                    <td>{formatDate(license.expirationDate)}</td>
                    <td style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>
                      {formatFees(license.paidFees)}
                    </td>

                    <td>
                      <span
                        className={`gender-badge ${license.isActive ? 'is-male' : ''}`}
                        style={
                          license.isActive
                            ? { color: '#059669', background: '#ecfdf5', borderColor: '#a7f3d0' }
                            : { color: '#64748b', background: '#f1f5f9', borderColor: '#e2e8f0' }
                        }
                      >
                        <span
                          style={{
                            width: 6,
                            height: 6,
                            borderRadius: '50%',
                            background: 'currentColor',
                            display: 'inline-block',
                          }}
                        />
                        {license.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                  </tr>
                ))}

                {filteredLicenses.length === 0 && (
                  <tr>
                    <td colSpan={7} className="empty-state">
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
                        <LicenseIcon size={36} />
                        <span>
                          {licenses.length === 0
                            ? 'No licenses found.'
                            : 'No licenses match your search.'}
                        </span>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredLicenses.length} of {licenses.length} licenses
          </div>
        )}
      </div>
    </section>
  )
}
