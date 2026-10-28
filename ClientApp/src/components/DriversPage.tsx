
import { useMemo, useState } from 'react'
import type { Driver } from '../services/driverService'

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
        <div className="page-header">
          <div>
            <p className="eyebrow">DRIVER DIRECTORY</p>
            <h2>Drivers Management</h2>
            <p>Browse and search registered drivers.</p>
          </div>

          <button
            type="button"
            className="primary-button"
            onClick={onRetry}
            disabled={loading}
          >
            {loading ? 'Loading...' : 'Refresh'}
          </button>
        </div>

        <div className="stats-grid">
          <article className="stat-card">
            <div className="stat-card-top">
              <span>Total Drivers</span>
              <span className="stat-icon">♧</span>
            </div>
            <strong>{drivers.length}</strong>
            <p>Registered drivers</p>
          </article>

          <article className="stat-card">
            <div className="stat-card-top">
              <span>Active Licenses</span>
              <span className="stat-icon">▤</span>
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

        <div className="table-toolbar">
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search by name, national number, driver ID..."
            aria-label="Search drivers"
          />

          <span>{filteredDrivers.length} drivers</span>
        </div>

        {loading && <p>Loading drivers...</p>}

        {!loading && error && (
          <div role="alert" className="login-error">
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
          <p>
            {drivers.length === 0
              ? 'No drivers found.'
              : 'No drivers match your search.'}
          </p>
        )}

        {!loading && !error && filteredDrivers.length > 0 && (
          <div className="table-container">
            <table>
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
                    <td>{driver.driverId}</td>
                    <td>{driver.personId}</td>
                    <td>{driver.fullName}</td>
                    <td>{driver.nationalNo}</td>
                    <td>
                      {driver.dateOfBirth
                        ? new Date(driver.dateOfBirth).toLocaleDateString()
                        : '—'}
                    </td>
                    <td>{driver.activeLicenses}</td>
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
