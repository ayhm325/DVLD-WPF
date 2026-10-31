import { useMemo, useState } from 'react'
import {
  UserPlus,
  Users,
} from 'lucide-react'
import type { Person } from '../types/Person'
import { formatDate } from '../utils/formatters'
import TableToolbar from './common/TableToolbar'
import EmptyState from './common/EmptyState'
import DataTable, { type DataTableColumn } from './common/DataTable'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select'

type SearchBy = 'all' | 'name' | 'nationalNo' | 'id' | 'phone'

const SEARCH_OPTIONS: { value: SearchBy; label: string }[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'name', label: 'Full Name' },
  { value: 'nationalNo', label: 'National No.' },
  { value: 'id', label: 'Person ID' },
  { value: 'phone', label: 'Phone' },
]
type GenderFilter = 'all' | '0' | '1'

export type PeopleTableProps = {
  people: Person[]
  searchName?: string
  onSearchChange?: (value: string) => void
  onAddPerson: () => void
  onRefresh?: () => void
  loading?: boolean
}

const columns: DataTableColumn<Person>[] = [
  {
    key: 'personId',
    header: 'ID',
    headerClassName: 'w-16 col-compact',
    className: 'col-compact',
    render: (person) => (
      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-table-text-muted, #94a3b8)' }}>
        #{person.personId}
      </span>
    ),
  },
  {
    key: 'fullName',
    header: 'Full Name',
    render: (person) => (
      <div className="person-cell">
        <span className="person-avatar" aria-hidden="true">
          {person.fullName ? person.fullName.charAt(0).toUpperCase() : 'P'}
        </span>
        <strong>{person.fullName}</strong>
      </div>
    ),
  },
  {
    key: 'nationalNo',
    header: 'National No.',
    render: (person) => (
      <span style={{ fontVariantNumeric: 'tabular-nums', letterSpacing: '0.2px', color: 'var(--dvld-table-text-secondary, #c0cddf)' }}>
        {person.nationalNo}
      </span>
    ),
  },
  {
    key: 'dateOfBirth',
    header: 'Date of Birth',
    render: (person) => (
      <span style={{ color: 'var(--dvld-table-text-muted, #94a3b8)' }}>
        {formatDate(person.dateOfBirth)}
      </span>
    ),
  },
  {
    key: 'gender',
    header: 'Gender',
    render: (person) => (
      <span className={`gender-badge ${person.gender === 0 ? 'is-male' : 'is-female'}`}>
        {person.gender === 0 ? 'Male' : 'Female'}
      </span>
    ),
  },
]

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
          <p>Manage and search registered citizen and resident records</p>
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
            <UserPlus size={15} />
            <span>Add Person</span>
          </button>
        </div>
      </div>

      <TableToolbar<SearchBy>
        searchValue={search}
        onSearchChange={handleSearchChange}
        searchPlaceholder={placeholderText}
        searchField={searchBy}
        searchFieldOptions={SEARCH_OPTIONS}
        onSearchFieldChange={setSearchBy}
        filters={
          <div style={{ minWidth: 130 }} className="shrink-0">
            <Select
              value={genderFilter}
              onValueChange={(val) => setGenderFilter(val as GenderFilter)}
            >
              <SelectTrigger
                className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                aria-label="Filter by gender"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Genders</SelectItem>
                <SelectItem value="0">Male</SelectItem>
                <SelectItem value="1">Female</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
        onReset={handleResetFilters}
        isFiltered={isFiltered}
        onRefresh={onRefresh}
        refreshing={loading}
      />

      <DataTable<Person>
        data={filteredPeople}
        columns={columns}
        getRowKey={(person) => person.personId}
        loading={loading}
        loadingMessage="Loading people directory..."
        emptyState={
          <EmptyState
            icon={<Users size={36} className="text-slate-400 mx-auto mb-2" />}
            message="No people found matching your criteria."
          />
        }
        minWidth={750}
      />
    </section>
  )
}
