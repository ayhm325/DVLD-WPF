import PeopleTable from './PeopleTable'
import PageHero from './common/PageHero'
import { UsersIcon, UserIcon } from './common/Icons'
import type { Person } from '../types/Person'

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

      <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total People</span>
            <span className="stat-icon" aria-hidden="true">
              <UsersIcon size={20} />
            </span>
          </div>
          <strong>{loadingPeople ? '...' : peopleError ? '—' : totalPeople}</strong>
          <p>Registered citizens</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Male</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#2563eb', background: '#eff6ff' }}>
              <UserIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#2563eb' }}>
            {loadingPeople ? '...' : peopleError ? '—' : maleCount}
          </strong>
          <p>Registered male citizens</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Female</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#ec4899', background: '#fdf2f8' }}>
              <UserIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#ec4899' }}>
            {loadingPeople ? '...' : peopleError ? '—' : femaleCount}
          </strong>
          <p>Registered female citizens</p>
        </article>
      </div>

      {peopleError && (
        <div className="login-error" style={{ marginBottom: 20 }} role="alert">
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
