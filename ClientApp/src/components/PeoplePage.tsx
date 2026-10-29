import PeopleTable from './PeopleTable'
import PageHero from './common/PageHero'
import type { Person } from '../types/Person'

export type PeoplePageProps = {
  filteredPeople: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  loadingPeople: boolean
  peopleError: string
  onRetryPeople: () => void
  onAddPerson: () => void
}

export default function PeoplePage({
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
}: PeoplePageProps) {
  return (
    <section className="page-content">
      <PageHero pageId="People" />

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
