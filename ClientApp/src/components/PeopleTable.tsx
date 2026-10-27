
import type { Person } from '../types/Person'

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
          <span className="people-count">
            {people.length} People
          </span>

          <button
            type="button"
            className="primary-button"
            onClick={onAddPerson}
          >
            + Add Person
          </button>
        </div>
      </div>

      <div className="table-toolbar">
        <input
          type="text"
          className="search-input"
          placeholder="Search by name or national number..."
          value={searchName}
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </div>

      <div className="table-container">
        <table className="people-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Full Name</th>
              <th>National No.</th>
              <th>DATE OF BIRTH</th>
              <th>Gender</th>
            </tr>
          </thead>

          <tbody>
            {people.map((person) => (
              <tr key={person.personId}>
                <td>{person.personId}</td>
                <td className="person-name">{person.fullName}</td>
                <td>{person.nationalNo}</td>
                <td>{person.dateOfBirth.slice(0, 10)}</td>
                <td>{person.gender === 0 ? 'Male' : 'Female'}</td>
              </tr>
            ))}

            {people.length === 0 && (
              <tr>
                <td colSpan={5} className="empty-state">
                  No people found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
