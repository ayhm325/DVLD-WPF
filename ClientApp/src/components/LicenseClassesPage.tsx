import { useCallback, useMemo } from 'react'
import { getLicenseClasses } from '../services/licenseClassService'
import type { LicenseClass } from '../types/LicenseClass'
import { formatCurrency } from '../utils/formatters'
import { useAsyncData, useTableFilters } from '../hooks'
import PageHero from './common/PageHero'
import TableToolbar from './common/TableToolbar'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import DataTable, { type DataTableColumn } from './common/DataTable'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select'
import {
  IdCard as LicenseIcon,
} from 'lucide-react'

type Props = {
  accessToken: string
}

type SearchBy = 'all' | 'name' | 'id' | 'desc'
type AgeFilter = 'all' | '18' | '21'

interface LicenseClassFilters extends Record<string, unknown> {
  ageFilter: AgeFilter
}

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: 'all', label: 'All Fields' },
  { value: 'name', label: 'Class Name' },
  { value: 'id', label: 'Class ID' },
  { value: 'desc', label: 'Description' },
]

const columns: DataTableColumn<LicenseClass>[] = [
  {
    key: 'id',
    header: 'ID',
    headerClassName: 'w-16 col-compact',
    className: 'col-compact',
    render: (item) => (
      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
        #{item.licenseClassId}
      </span>
    ),
  },
  {
    key: 'name',
    header: 'Class Name',
    headerClassName: 'w-48 whitespace-nowrap',
    className: 'person-name font-semibold whitespace-nowrap',
    accessor: (item) => item.licenseClassName,
  },
  {
    key: 'description',
    header: 'Description',
    headerClassName: 'table-cell-description',
    className: 'table-cell-description',
    accessor: (item) => item.licenseClassDescription,
  },
  {
    key: 'minAge',
    header: 'Minimum Age',
    headerClassName: 'w-28 whitespace-nowrap',
    accessor: (item) => item.minAllowedAge,
  },
  {
    key: 'validity',
    header: 'Validity (Years)',
    headerClassName: 'w-32 whitespace-nowrap',
    accessor: (item) => item.defaultValidityLength,
  },
  {
    key: 'fees',
    header: 'Fees',
    headerClassName: 'w-24 whitespace-nowrap',
    render: (item) => formatCurrency(item.licenseClassFees, ''),
  },
]

export default function LicenseClassesPage({ accessToken }: Props) {
  const fetcher = useCallback(() => getLicenseClasses(accessToken), [accessToken])

  const {
    data: items,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Failed to load license classes.',
  })

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredItems,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<LicenseClass, SearchBy, LicenseClassFilters>({
    data: items,
    initialSearchField: 'all',
    initialFilters: { ageFilter: 'all' },
    filterItem: (item, query, field, currentFilters) => {
      // 1. Age check
      if (currentFilters.ageFilter === '18' && item.minAllowedAge < 18) return false
      if (currentFilters.ageFilter === '21' && item.minAllowedAge < 21) return false

      // 2. Query check
      if (!query) return true

      if (field === 'name') {
        return item.licenseClassName.toLowerCase().includes(query)
      }
      if (field === 'id') {
        return String(item.licenseClassId).includes(query)
      }
      if (field === 'desc') {
        return item.licenseClassDescription.toLowerCase().includes(query)
      }

      // 'all'
      return (
        item.licenseClassName.toLowerCase().includes(query) ||
        String(item.licenseClassId).includes(query) ||
        item.licenseClassDescription.toLowerCase().includes(query)
      )
    },
  })

  const ageFilter = filters.ageFilter

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'name':
        return 'Search by license class name...'
      case 'id':
        return 'Search by class ID...'
      case 'desc':
        return 'Search by description...'
      default:
        return 'Search by name, ID, or description...'
    }
  }, [searchBy])

  return (
    <section className="page-content">
      <PageHero pageId="License Classes" />

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>License Classes Directory</h2>
            <p>Browse license categories, requirements, validity, and fees.</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : error
                ? '—'
                : filteredItems.length === items.length
                  ? `${items.length} Classes`
                  : `${filteredItems.length} of ${items.length} Classes`}
          </span>
        </div>

        <TableToolbar
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={(val) => setSearchBy(val as SearchBy)}
          searchFieldMinWidth={140}
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={placeholderText}
          searchAriaLabel="Search license classes"
          filters={
            <div style={{ minWidth: 140 }} className="shrink-0">
              <Select
                value={ageFilter}
                onValueChange={(val) => setFilter('ageFilter', val as AgeFilter)}
              >
                <SelectTrigger
                  className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
                  aria-label="Filter by minimum age"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Ages</SelectItem>
                  <SelectItem value="18">Min Age 18+</SelectItem>
                  <SelectItem value="21">Min Age 21+</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={() => void refresh()}
          refreshing={loading}
        />

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={() => void refresh()}
              retryLabel="Retry"
            />
          </div>
        )}

        {!error && (
          <DataTable<LicenseClass>
            data={filteredItems}
            columns={columns}
            getRowKey={(item) => item.licenseClassId}
            rowClassName="align-top"
            loading={loading}
            loadingMessage="Loading license classes..."
            emptyState={
              <EmptyState
                icon={<LicenseIcon size={36} />}
                message={
                  items.length === 0
                    ? 'No license classes found.'
                    : 'No license classes match your search.'
                }
              />
            }
            minWidth={780}
          />
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredItems.length} of {items.length} license classes
          </div>
        )}
      </section>
    </section>
  )
}
