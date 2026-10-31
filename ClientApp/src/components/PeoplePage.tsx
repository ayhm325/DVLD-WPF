import PeopleTable from './PeopleTable'
import PageHero from './common/PageHero'
import StatsGrid from './common/StatsGrid'
import StatCard from './common/StatCard'
import ErrorAlert from './common/ErrorAlert'
import { Users as UsersIcon, User as UserIcon } from 'lucide-react'
import type { Person } from '../types/Person'
import { formatNumber } from '../utils/formatters'

export type PeoplePageProps = {
  people?: Person[]
  filteredPeople: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  loadingPeople: boolean
  peopleError: string
  onRetryPeople: () => void
  onAddPerson: () => void
}

export default function PeoplePage({
  people: rawPeople,
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
}: PeoplePageProps) {
  const people = rawPeople ?? filteredPeople

  const totalPeople = people.length
  const maleCount = people.filter((p) => p.gender === 0).length
  const femaleCount = people.filter((p) => p.gender === 1).length

  return (
    <section className="page-content">
      <PageHero pageId="People" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total People"
          variant="blue"
          icon={<UsersIcon size={20} />}
          value={loadingPeople ? '...' : peopleError ? '—' : formatNumber(totalPeople)}
          description="Registered citizens"
        />

        <StatCard
          label="Male"
          variant="blue"
          icon={<UserIcon size={20} />}
          value={loadingPeople ? '...' : peopleError ? '—' : formatNumber(maleCount)}
          description="Registered male citizens"
        />

        <StatCard
          label="Female"
          variant="rose"
          icon={<UserIcon size={20} />}
          value={loadingPeople ? '...' : peopleError ? '—' : formatNumber(femaleCount)}
          description="Registered female citizens"
        />
      </StatsGrid>

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
