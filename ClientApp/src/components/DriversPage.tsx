import { useMemo, useState } from 'react'
import type { Driver } from '../services/driverService'
import {
  DriversIcon,
  LicenseIcon,
  RefreshIcon,
  SearchIcon,
  CloseIcon,
} from './common/Icons'

type DriversPageProps = {
  drivers: Driver[]
  loading: boolean
  error: string
  onRetry: () => void
}

export default function DriversPage({
  drivers,
  loading,
  error,
  onRetry,
}: DriversPageProps) {
  const [search, setSearch] = useState('')

  const filteredDrivers = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return drivers

    return drivers.filter((driver) =>
      [
        String(driver.driverId),
        String(driver.personId),
        driver.fullName,
        driver.nationalNo,
      ].some((value) => value.toLowerCase().includes(query)),
    )
  }, [drivers, search])

  return (
    <section className="page-content">
      <div className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">DRIVER DIRECTORY</p>
            <h2>Drivers Management</h2>
            <p>Browse and search registered drivers.</p>
          </div>

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

        <div style={{ padding: '0 25px 20px' }}>
          <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' }}>
            <article className="stat-card">
              <div className="stat-card-top">
                <span>Total Drivers</span>
                <span className="stat-icon">
                  <DriversIcon size={20} />
                </span>
              </div>
              <strong>{drivers.length}</strong>
              <p>Registered drivers</p>
            </article>

            <article className="stat-card">
              <div className="stat-card-top">
                <span>Active Licenses</span>
                <span className="stat-icon">
                  <LicenseIcon size={20} />
                </span>
              </div>
              <strong>
                {drivers.reduce(
                  (total, driver) => total + driver.activeLicenses,
                  0,
                )}
              </strong>
              <p>Across listed drivers</p>
            </article>
          </div>
        </div>

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
              placeholder="Search by name, national number, driver ID..."
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

          <span className="record-count">{filteredDrivers.length} Drivers</span>
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
                  : 'No drivers match your search.'}
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
