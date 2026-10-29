import {
  UsersIcon,
  DriversIcon,
  LicenseIcon,
  ApplicationsIcon,
} from './common/Icons'
import PageHero from './common/PageHero'
import StatsGrid from './common/StatsGrid'
import StatCard from './common/StatCard'
import ErrorAlert from './common/ErrorAlert'
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
  people,
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
      <PageHero pageId="Dashboard" />

      <StatsGrid columns={4}>
        <StatCard
          label="Total People"
          icon={<UsersIcon size={20} />}
          value={loading ? '...' : error ? '—' : statistics?.totalPeople ?? '—'}
          description="Registered people"
        />

        <StatCard
          label="Total Drivers"
          icon={<DriversIcon size={20} />}
          value={loading ? '...' : error ? '—' : statistics?.totalDrivers ?? '—'}
          description="Registered drivers"
        />

        <StatCard
          label="Active Licenses"
          icon={<LicenseIcon size={20} />}
          value={loading ? '...' : error ? '—' : statistics?.activeLicenses ?? '—'}
          description="Currently active licenses"
        />

        <StatCard
          label="Pending Applications"
          icon={<ApplicationsIcon size={20} />}
          value={loading ? '...' : error ? '—' : statistics?.pendingApplications ?? '—'}
          description="Applications awaiting processing"
        />
      </StatsGrid>

      <ErrorAlert
        message={error}
        onRetry={retry}
        retryLabel="Retry statistics"
      />

      <ErrorAlert
        message={peopleError}
        style={{ marginBottom: 20 }}
        onRetry={onRetryPeople}
        retryLabel="Retry people"
      />

      <PeopleTable
        people={people}
        searchName={searchName}
        onSearchChange={onSearchChange}
        onAddPerson={onAddPerson}
        onRefresh={onRetryPeople}
        loading={loadingPeople}
      />
    </section>
  )
}
