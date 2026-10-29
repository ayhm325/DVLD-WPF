
import { useEffect, useMemo, useState } from 'react'
import { getLicenses } from '../services/licenseService'
import type { License } from '../services/licenseService'

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
      <div className="page-header">
        <div>
          <p className="eyebrow">LICENSE MANAGEMENT</p>
          <h2>Licenses</h2>
          <p>View and search issued driving licenses.</p>
        </div>

        <button
          type="button"
          className="primary-button"
          onClick={() => setRefreshKey((value) => value + 1)}
          disabled={loading}
        >
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      <div className="stats-grid">
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total Licenses</span>
            <span className="stat-icon">▤</span>
          </div>
          <strong>{licenses.length}</strong>
          <p>All returned records</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Active Licenses</span>
            <span className="stat-icon">✓</span>
          </div>
          <strong>
            {licenses.filter((license) => license.isActive).length}
          </strong>
          <p>Marked active by the system</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Inactive Licenses</span>
            <span className="stat-icon">○</span>
          </div>
          <strong>
            {licenses.filter((license) => !license.isActive).length}
          </strong>
          <p>Not currently active</p>
        </article>
      </div>

      <div className="panel">
        <div className="license-toolbar">
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search by license ID, driver, or class..."
            aria-label="Search licenses"
          />

          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            aria-label="Filter by license status"
          >
            <option value="all">All statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>

        {loading && <p className="license-message">Loading licenses...</p>}

        {!loading && error && (
          <div className="license-error">
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
          <div className="license-table-wrapper">
            <table className="license-table">
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
                    <td>#{license.licenseId}</td>

                    <td>
                      <strong>
                        {license.driverName || 'Unknown driver'}
                      </strong>
                      <small>Driver ID: {license.driverId}</small>
                    </td>

                    <td>
                      {license.licenseClassName ||
                        `Class #${license.licenseClassId}`}
                    </td>

                    <td>{formatDate(license.issueDate)}</td>
                    <td>{formatDate(license.expirationDate)}</td>
                    <td>{formatFees(license.paidFees)}</td>

                    <td>
                      <span
                        className={`license-status ${
                          license.isActive ? 'is-active' : 'is-inactive'
                        }`}
                      >
                        {license.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                  </tr>
                ))}

                {filteredLicenses.length === 0 && (
                  <tr>
                    <td colSpan={7} className="license-empty">
                      {licenses.length === 0
                        ? 'No licenses found.'
                        : 'No licenses match your search.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {!loading && !error && (
          <p className="license-results">
            Showing {filteredLicenses.length} of {licenses.length} licenses
          </p>
        )}
      </div>
    </section>
  )
}
