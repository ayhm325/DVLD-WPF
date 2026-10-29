import { useMemo, useState } from 'react'
import type { Person } from '../types/Person'
import {
  PlusIcon,
  SearchIcon,
  CloseIcon,
  UsersIcon,
  RefreshIcon,
  FilterIcon,
} from './common/Icons'

type SearchBy = 'all' | 'name' | 'nationalNo' | 'id' | 'phone'
type GenderFilter = 'all' | '0' | '1'

export type PeopleTableProps = {
  people: Person[]
  searchName?: string
  onSearchChange?: (value: string) => void
  onAddPerson: () => void
  onRefresh?: () => void
  loading?: boolean
}

export default function PeopleTable({
  people,
  searchName,
  onSearchChange,
  onAddPerson,
  onRefresh,
  loading = false,
}: PeopleTableProps) {
  const [internalSearch, setInternalSearch] = useState(searchName ?? '')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [genderFilter, setGenderFilter] = useState<GenderFilter>('all')

  const search = searchName !== undefined ? searchName : internalSearch

  function handleSearchChange(value: string) {
    setInternalSearch(value)
    onSearchChange?.(value)
  }

  function handleResetFilters() {
    handleSearchChange('')
    setSearchBy('all')
    setGenderFilter('all')
  }

  const isFiltered = Boolean(search.trim() || genderFilter !== 'all' || searchBy !== 'all')

  const filteredPeople = useMemo(() => {
    const query = search.trim().toLowerCase()

    return people.filter((person) => {
      if (genderFilter !== 'all' && String(person.gender) !== genderFilter) {
        return false
      }

      if (!query) return true

      if (searchBy === 'name') {
        return (person.fullName || '').toLowerCase().includes(query)
      }
      if (searchBy === 'nationalNo') {
        return (person.nationalNo || '').toLowerCase().includes(query)
      }
      if (searchBy === 'id') {
        return String(person.personId).includes(query)
      }
      if (searchBy === 'phone') {
        return (person.phone || '').toLowerCase().includes(query)
      }

      return (
        (person.fullName || '').toLowerCase().includes(query) ||
        (person.nationalNo || '').toLowerCase().includes(query) ||
        String(person.personId).includes(query) ||
        (person.phone || '').toLowerCase().includes(query)
      )
    })
  }, [people, search, searchBy, genderFilter])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'name':
        return 'Search by full name...'
      case 'nationalNo':
        return 'Search by national number...'
      case 'id':
        return 'Search by person ID...'
      case 'phone':
        return 'Search by phone number...'
      default:
        return 'Search by name, national no, ID, phone...'
    }
  }, [searchBy])

  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>People Directory</h2>
          <p>Manage and search registered people</p>
        </div>

        <div className="people-header-actions">
          <span className="record-count">
            {loading
              ? 'Loading...'
              : filteredPeople.length === people.length
                ? `${people.length} People`
                : `${filteredPeople.length} of ${people.length} People`}
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
        <select
          className="search-input"
          style={{ width: 'auto', minWidth: 140 }}
          value={searchBy}
          onChange={(event) => setSearchBy(event.target.value as SearchBy)}
          aria-label="Filter criterion"
        >
          <option value="all">All Fields</option>
          <option value="name">Full Name</option>
          <option value="nationalNo">National No.</option>
          <option value="id">Person ID</option>
          <option value="phone">Phone</option>
        </select>

        <div className="search-wrapper">
          <span className="search-icon-left" aria-hidden="true">
            <SearchIcon size={16} />
          </span>

          <input
            type="text"
            className="search-input"
            placeholder={placeholderText}
            value={search}
            onChange={(event) => handleSearchChange(event.target.value)}
            aria-label="Search people"
          />

          {search && (
            <button
              type="button"
              className="search-clear-btn"
              onClick={() => handleSearchChange('')}
              aria-label="Clear search"
            >
              <CloseIcon size={14} />
            </button>
          )}
        </div>

        <select
          className="search-input"
          style={{ width: 'auto', minWidth: 130 }}
          value={genderFilter}
          onChange={(event) => setGenderFilter(event.target.value as GenderFilter)}
          aria-label="Filter by gender"
        >
          <option value="all">All Genders</option>
          <option value="0">Male</option>
          <option value="1">Female</option>
        </select>

        {isFiltered && (
          <button
            type="button"
            className="secondary-button"
            onClick={handleResetFilters}
            title="Reset all filters"
          >
            <FilterIcon size={14} />
            <span>Reset</span>
          </button>
        )}

        {onRefresh && (
          <button
            type="button"
            className="secondary-button"
            onClick={onRefresh}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        )}
      </div>

      {loading ? (
        <p className="empty-state">Loading people...</p>
      ) : (
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
              {filteredPeople.map((person) => (
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

              {filteredPeople.length === 0 && (
                <tr>
                  <td colSpan={5} className="empty-state">
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
                      <UsersIcon size={36} />
                      <span>No people found matching your criteria.</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
