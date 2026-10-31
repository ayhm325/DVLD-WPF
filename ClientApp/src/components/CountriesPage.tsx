import { useCallback, useMemo } from 'react'
import { getCountries } from '../services/countryService'
import type { Country } from '../types/Country'
import { useAsyncData, useTableFilters } from '../hooks'
import PageHero from './common/PageHero'
import TableToolbar from './common/TableToolbar'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import DataTable, { type DataTableColumn } from './common/DataTable'
import { Globe as GlobeIcon } from 'lucide-react'

type CountriesPageProps = {
  accessToken: string
}

type SearchBy = 'all' | 'name' | 'id'

const SEARCH_OPTIONS: Array<{ value: SearchBy; label: string }> = [
  { value: 'all', label: 'All Fields' },
  { value: 'name', label: 'Country Name' },
  { value: 'id', label: 'Country ID' },
]

const columns: DataTableColumn<Country>[] = [
  {
    key: 'countryId',
    header: 'Country ID',
    headerClassName: 'w-28 col-compact',
    render: (country) => (
      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
        #{country.countryId}
      </span>
    ),
  },
  {
    key: 'countryName',
    header: 'Country Name',
    className: 'person-name',
    accessor: (country) => country.countryName,
  },
]

export default function CountriesPage({
  accessToken,
}: CountriesPageProps) {
  const fetcher = useCallback(() => getCountries(accessToken), [accessToken])

  const {
    data: countries,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Failed to load countries.',
  })

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filteredData: filteredCountries,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<Country, SearchBy>({
    data: countries,
    initialSearchField: 'all',
    filterItem: (country, query, field) => {
      if (!query) return true

      if (field === 'name') {
        return country.countryName.toLowerCase().includes(query)
      }
      if (field === 'id') {
        return String(country.countryId).includes(query)
      }

      // 'all'
      return (
        country.countryName.toLowerCase().includes(query) ||
        String(country.countryId).includes(query)
      )
    },
  })

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'name':
        return 'Search by country name...'
      case 'id':
        return 'Search by country ID...'
      default:
        return 'Search by country name or ID...'
    }
  }, [searchBy])

  return (
    <section className="page-content">
      <PageHero pageId="Countries" />

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Countries Directory</h2>
            <p>Countries available in the system</p>
          </div>

          <span className="record-count">
            {loading
              ? 'Loading...'
              : error
                ? '—'
                : filteredCountries.length === countries.length
                  ? `${countries.length} Countries`
                  : `${filteredCountries.length} of ${countries.length} Countries`}
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
          searchAriaLabel="Search countries"
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
          <DataTable<Country>
            data={filteredCountries}
            columns={columns}
            getRowKey={(country) => country.countryId}
            loading={loading}
            loadingMessage="Loading countries..."
            emptyState={
              <EmptyState
                icon={<GlobeIcon size={36} />}
                message={
                  countries.length === 0
                    ? 'No countries are registered.'
                    : 'No countries match your search.'
                }
              />
            }
            minWidth={550}
          />
        )}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredCountries.length} of {countries.length} countries
          </div>
        )}
      </section>
    </section>
  )
}
