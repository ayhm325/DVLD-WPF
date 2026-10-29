import { useEffect, useMemo, useState } from 'react'
import { getCountries } from '../services/countryService'
import type { Country } from '../services/countryService'
import PageHero from './common/PageHero'
import SearchInput from './common/SearchInput'
import ErrorAlert from './common/ErrorAlert'
import EmptyState from './common/EmptyState'
import {
  RefreshIcon,
  GlobeIcon,
  FilterIcon,
} from './common/Icons'

type CountriesPageProps = {
  accessToken: string
}

type SearchBy = 'all' | 'name' | 'id'

export default function CountriesPage({
  accessToken,
}: CountriesPageProps) {
  const [countries, setCountries] = useState<Country[]>([])
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function loadCountries() {
      setLoading(true)
      setError('')

      try {
        const result = await getCountries(accessToken)

        if (!cancelled) {
          setCountries(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load countries.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadCountries()

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshKey])

  function handleResetFilters() {
    setSearch('')
    setSearchBy('all')
  }

  const isFiltered = Boolean(search.trim() || searchBy !== 'all')

  const filteredCountries = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return countries

    return countries.filter((country) => {
      if (searchBy === 'name') {
        return country.countryName.toLowerCase().includes(query)
      }
      if (searchBy === 'id') {
        return String(country.countryId).includes(query)
      }

      // 'all'
      return (
        country.countryName.toLowerCase().includes(query) ||
        String(country.countryId).includes(query)
      )
    })
  }, [countries, search, searchBy])

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

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={searchBy}
            onChange={(event) => setSearchBy(event.target.value as SearchBy)}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="name">Country Name</option>
            <option value="id">Country ID</option>
          </select>

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder={placeholderText}
            ariaLabel="Search countries"
          />

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

          <button
            type="button"
            className="secondary-button"
            disabled={loading}
            onClick={() => setRefreshKey((k) => k + 1)}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        </div>

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={() => setRefreshKey((k) => k + 1)}
              retryLabel="Retry"
            />
          </div>
        )}

        {loading ? (
          <p className="empty-state">Loading countries...</p>
        ) : !error ? (
          <div className="table-container">
            <table className="people-table">
              <thead>
                <tr>
                  <th>Country ID</th>
                  <th>Country Name</th>
                </tr>
              </thead>

              <tbody>
                {filteredCountries.map((country) => (
                  <tr key={country.countryId}>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{country.countryId}
                      </span>
                    </td>
                    <td className="person-name">{country.countryName}</td>
                  </tr>
                ))}

                {filteredCountries.length === 0 && (
                  <tr>
                    <td colSpan={2}>
                      <EmptyState
                        icon={<GlobeIcon size={36} />}
                        message={
                          countries.length === 0
                            ? 'No countries are registered.'
                            : 'No countries match your search.'
                        }
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        ) : null}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredCountries.length} of {countries.length} countries
          </div>
        )}
      </section>
    </section>
  )
}
