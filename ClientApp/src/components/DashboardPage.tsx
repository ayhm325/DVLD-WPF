import {
  UsersIcon,
  DriversIcon,
  LicenseIcon,
  ApplicationsIcon,
} from './common/Icons'
import PeopleTable from './PeopleTable'
import { useDashboardStatistics } from '../hooks/useDashboardStatistics'
import type { Person } from '../types/Person'

export type DashboardPageProps = {
  accessToken: string
  statisticsRefreshKey?: number
  people: Person[]
  filteredPeople: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  loadingPeople: boolean
  peopleError: string
  onRetryPeople: () => void
  onAddPerson: () => void
}

export default function DashboardPage({
  accessToken,
  statisticsRefreshKey = 0,
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
}: DashboardPageProps) {
  const { statistics, loading, error, retry } = useDashboardStatistics(
    accessToken,
    statisticsRefreshKey,
  )

  return (
    <section className="page-content">
      <div className="welcome-banner">
        <div>
          <p className="eyebrow">CONTROL CENTER</p>
          <h2>Welcome to DVLD</h2>
          <p>
            Manage people, drivers, and licensing operations from one place.
          </p>
        </div>

        <div className="banner-mark">D</div>
      </div>

      <div className="stats-grid">
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total People</span>
            <span className="stat-icon" aria-hidden="true">
              <UsersIcon size={20} />
            </span>
          </div>
          <strong>
            {loading ? '...' : error ? '—' : statistics?.totalPeople ?? '—'}
          </strong>
          <p>Registered people</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total Drivers</span>
            <span className="stat-icon" aria-hidden="true">
              <DriversIcon size={20} />
            </span>
          </div>
          <strong>
            {loading ? '...' : error ? '—' : statistics?.totalDrivers ?? '—'}
          </strong>
          <p>Registered drivers</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Active Licenses</span>
            <span className="stat-icon" aria-hidden="true">
              <LicenseIcon size={20} />
            </span>
          </div>
          <strong>
            {loading ? '...' : error ? '—' : statistics?.activeLicenses ?? '—'}
          </strong>
          <p>Currently active licenses</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Pending Applications</span>
            <span className="stat-icon" aria-hidden="true">
              <ApplicationsIcon size={20} />
            </span>
          </div>
          <strong>
            {loading
              ? '...'
              : error
                ? '—'
                : statistics?.pendingApplications ?? '—'}
          </strong>
          <p>Applications awaiting processing</p>
        </article>
      </div>

      {error && (
        <div className="login-error" role="alert">
          <span>{error}</span>
          <button type="button" className="primary-button" onClick={retry}>
            Retry statistics
          </button>
        </div>
      )}

      {loadingPeople && <p className="empty-state">Loading people...</p>}

      {peopleError && (
        <div className="login-error" role="alert">
          <span>{peopleError}</span>
          <button
            type="button"
            className="primary-button"
            onClick={onRetryPeople}
          >
            Retry people
          </button>
        </div>
      )}

      {!loadingPeople && !peopleError && (
        <PeopleTable
          people={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          onAddPerson={onAddPerson}
        />
      )}
    </section>
  )
}
