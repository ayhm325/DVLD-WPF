import { useEffect, useMemo, useState } from 'react'
import { getLicenseClasses } from '../services/licenseClassService'
import type { LicenseClass } from '../services/licenseClassService'
import PageHero from './common/PageHero'
import {
  SearchIcon,
  CloseIcon,
  RefreshIcon,
  LicenseIcon,
  FilterIcon,
} from './common/Icons'

type Props = {
  accessToken: string
}

type SearchBy = 'all' | 'name' | 'id' | 'desc'
type AgeFilter = 'all' | '18' | '21'

export default function LicenseClassesPage({ accessToken }: Props) {
  const [items, setItems] = useState<LicenseClass[]>([])
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [ageFilter, setAgeFilter] = useState<AgeFilter>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')

      try {
        const result = await getLicenseClasses(accessToken)

        if (!cancelled) setItems(result)
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load license classes.',
          )
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshKey])

  function handleResetFilters() {
    setSearch('')
    setSearchBy('all')
    setAgeFilter('all')
  }

  const isFiltered = Boolean(search.trim() || searchBy !== 'all' || ageFilter !== 'all')

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    return items.filter((item) => {
      // 1. Age check
      if (ageFilter === '18' && item.minAllowedAge < 18) return false
      if (ageFilter === '21' && item.minAllowedAge < 21) return false

      // 2. Query check
      if (!query) return true

      if (searchBy === 'name') {
        return item.licenseClassName.toLowerCase().includes(query)
      }
      if (searchBy === 'id') {
        return String(item.licenseClassId).includes(query)
      }
      if (searchBy === 'desc') {
        return item.licenseClassDescription.toLowerCase().includes(query)
      }

      // 'all'
      return (
        item.licenseClassName.toLowerCase().includes(query) ||
        String(item.licenseClassId).includes(query) ||
        item.licenseClassDescription.toLowerCase().includes(query)
      )
    })
  }, [items, search, searchBy, ageFilter])

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

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={searchBy}
            onChange={(event) => setSearchBy(event.target.value as SearchBy)}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="name">Class Name</option>
            <option value="id">Class ID</option>
            <option value="desc">Description</option>
          </select>

          <div className="search-wrapper">
            <span className="search-icon-left" aria-hidden="true">
              <SearchIcon size={16} />
            </span>

            <input
              type="text"
              className="search-input"
              placeholder={placeholderText}
              aria-label="Search license classes"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />

            {search && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => setSearch('')}
                aria-label="Clear search"
              >
                <CloseIcon size={14} />
              </button>
            )}
          </div>

          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={ageFilter}
            onChange={(event) => setAgeFilter(event.target.value as AgeFilter)}
            aria-label="Filter by minimum age"
          >
            <option value="all">All Ages</option>
            <option value="18">Min Age 18+</option>
            <option value="21">Min Age 21+</option>
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
          <div className="login-error" style={{ margin: '0 25px 20px' }} role="alert">
            <p>{error}</p>
            <button
              type="button"
              className="primary-button"
              onClick={() => setRefreshKey((k) => k + 1)}
            >
              Retry
            </button>
          </div>
        )}

        {loading ? (
          <p className="empty-state">Loading license classes...</p>
        ) : !error ? (
          <div className="table-container">
            <table className="people-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Class Name</th>
                  <th>Description</th>
                  <th>Minimum Age</th>
                  <th>Validity (Years)</th>
                  <th>Fees</th>
                </tr>
              </thead>

              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.licenseClassId}>
                    <td>
                      <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
                        #{item.licenseClassId}
                      </span>
                    </td>
                    <td className="person-name">{item.licenseClassName}</td>
                    <td>{item.licenseClassDescription}</td>
                    <td>{item.minAllowedAge}</td>
                    <td>{item.defaultValidityLength}</td>
                    <td>{item.licenseClassFees.toFixed(2)}</td>
                  </tr>
                ))}

                {filteredItems.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty-state">
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, padding: '20px 0' }}>
                        <LicenseIcon size={36} />
                        <span>
                          {items.length === 0
                            ? 'No license classes found.'
                            : 'No license classes match your search.'}
                        </span>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        ) : null}

        {!loading && !error && (
          <div className="panel-footer">
            Showing {filteredItems.length} of {items.length} license classes
          </div>
        )}
      </section>
    </section>
  )
}
