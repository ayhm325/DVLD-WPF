import type { Person } from '../types/Person'
import { PlusIcon, SearchIcon, CloseIcon, UsersIcon } from './common/Icons'

type PeopleTableProps = {
  people: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  onAddPerson: () => void
}

export default function PeopleTable({
  people,
  searchName,
  onSearchChange,
  onAddPerson,
}: PeopleTableProps) {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>People Directory</h2>
          <p>Manage and search registered people</p>
        </div>

        <div className="people-header-actions">
          <span className="record-count">
            {people.length} People
          </span>

          <button
            type="button"
            className="primary-button"
            onClick={onAddPerson}
          >
            <PlusIcon size={16} />
            <span>Add Person</span>
          </button>
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
            placeholder="Search by name or national number..."
            value={searchName}
            onChange={(event) => onSearchChange(event.target.value)}
          />

          {searchName && (
            <button
              type="button"
              className="search-clear-btn"
              onClick={() => onSearchChange('')}
              aria-label="Clear search"
            >
              <CloseIcon size={14} />
            </button>
          )}
        </div>
      </div>

      <div className="table-container">
        <table className="people-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Full Name</th>
              <th>National No.</th>
              <th>Date of Birth</th>
              <th>Gender</th>
            </tr>
          </thead>

          <tbody>
            {people.map((person) => (
              <tr key={person.personId}>
                <td>
                  <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                    #{person.personId}
                  </span>
                </td>
                <td>
                  <div className="person-cell">
                    <span className="person-avatar" aria-hidden="true">
                      {person.fullName ? person.fullName.charAt(0).toUpperCase() : 'P'}
                    </span>
                    <strong>{person.fullName}</strong>
                  </div>
                </td>
                <td>
                  <span style={{ fontVariantNumeric: 'tabular-nums', letterSpacing: '0.3px', fontWeight: 600 }}>
                    {person.nationalNo}
                  </span>
                </td>
                <td>{person.dateOfBirth ? person.dateOfBirth.slice(0, 10) : '—'}</td>
                <td>
                  <span className={`gender-badge ${person.gender === 0 ? 'is-male' : 'is-female'}`}>
                    {person.gender === 0 ? 'Male' : 'Female'}
                  </span>
                </td>
              </tr>
            ))}

            {people.length === 0 && (
              <tr>
                <td colSpan={5} className="empty-state">
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
                    <UsersIcon size={36} />
                    <span>No people found.</span>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
