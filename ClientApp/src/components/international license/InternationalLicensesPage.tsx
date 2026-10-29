import { useEffect, useMemo, useState } from 'react'
import type { InternationalLicense } from '../../types/InternationalLicense'
import { getInternationalLicenses } from '../../services/internationalLicenseService'
import InternationalLicenseTable from './InternationalLicenseTable'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import SearchInput from '../common/SearchInput'
import ErrorAlert from '../common/ErrorAlert'
import {
  PlusIcon,
  GlobeIcon,
  CheckIcon,
  ClockIcon,
  RefreshIcon,
  FilterIcon,
} from '../common/Icons'
import './international-licenses.css'

type Props = {
  accessToken: string
  onIssueNew?: () => void
  onOpenDetails?: (id: number) => void
}

type FilterKey =
  | 'all'
  | 'internationalLicenseId'
  | 'applicationId'
  | 'driverId'
  | 'issuedUsingLocalLicenseId'

export default function InternationalLicensesPage({
  accessToken,
  onIssueNew,
  onOpenDetails,
}: Props) {
  const [items, setItems] = useState<InternationalLicense[]>([])
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<FilterKey>('all')
  const [status, setStatus] = useState<'All' | 'Active' | 'Inactive'>('All')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function fetchLicenses() {
      try {
        const result = await getInternationalLicenses(accessToken)

        if (cancelled) return

        setItems(result)
        setError('')
      } catch (err) {
        if (cancelled) return

        setError(
          err instanceof Error
            ? err.message
            : 'Could not load international licenses.',
        )
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void fetchLicenses()

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshKey])

  function refresh() {
    setLoading(true)
    setRefreshKey((value) => value + 1)
  }

  function handleResetFilters() {
    setSearch('')
    setFilter('all')
    setStatus('All')
  }

  const isFiltered = Boolean(search.trim() || filter !== 'all' || status !== 'All')

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    return items.filter((item) => {
      // 1. Status check
      const matchesStatus =
        status === 'All' ||
        (status === 'Active' ? item.isActive : !item.isActive)

      if (!matchesStatus) return false

      // 2. Query check
      if (!query) return true

      if (filter === 'all') {
        return (
          String(item.internationalLicenseId).includes(query) ||
          String(item.applicationId).includes(query) ||
          String(item.driverId).includes(query) ||
          String(item.issuedUsingLocalLicenseId).includes(query)
        )
      }

      return String(item[filter]).toLowerCase().includes(query)
    })
  }, [items, search, filter, status])

  const placeholderText = useMemo(() => {
    switch (filter) {
      case 'internationalLicenseId':
        return 'Search by international license ID...'
      case 'applicationId':
        return 'Search by application ID...'
      case 'driverId':
        return 'Search by driver ID...'
      case 'issuedUsingLocalLicenseId':
        return 'Search by local license ID...'
      default:
        return 'Search by any ID (intl, app, driver, local)...'
    }
  }, [filter])

  return (
    <section className="page-content intl-page">
      <PageHero pageId="International Licenses" />

      <StatsGrid columns={3}>
        <StatCard
          label="Total Licenses"
          icon={<GlobeIcon size={20} />}
          value={loading || error ? '—' : items.length}
          description="International licenses recorded"
        />

        <StatCard
          label="Active Licenses"
          icon={<CheckIcon size={20} />}
          iconStyle={{ color: '#059669', background: '#ecfdf5' }}
          value={<span style={{ color: '#059669' }}>{loading || error ? '—' : items.filter((item) => item.isActive).length}</span>}
          description="Currently active licenses"
        />

        <StatCard
          label="Inactive Licenses"
          icon={<ClockIcon size={20} />}
          iconStyle={{ color: '#64748b', background: '#f1f5f9' }}
          value={loading || error ? '—' : items.filter((item) => !item.isActive).length}
          description="Not currently active"
        />
      </StatsGrid>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>International Licenses Directory</h2>
            <p>Review issued international licenses and verify validity.</p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span className="record-count">
              {loading
                ? 'Loading...'
                : filteredItems.length === items.length
                  ? `${items.length} Licenses`
                  : `${filteredItems.length} of ${items.length} Licenses`}
            </span>

            {onIssueNew && (
              <button
                type="button"
                className="primary-button"
                onClick={onIssueNew}
              >
                <PlusIcon size={16} />
                <span>Issue International License</span>
              </button>
            )}
          </div>
        </div>

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 150 }}
            value={filter}
            onChange={(event) =>
              setFilter(event.target.value as FilterKey)
            }
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="internationalLicenseId">Int'l License ID</option>
            <option value="applicationId">Application ID</option>
            <option value="driverId">Driver ID</option>
            <option value="issuedUsingLocalLicenseId">Local License ID</option>
          </select>

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder={placeholderText}
            ariaLabel="Search international licenses"
          />

          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={status}
            onChange={(event) =>
              setStatus(event.target.value as typeof status)
            }
            aria-label="Filter by status"
          >
            <option value="All">All statuses</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
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
            onClick={refresh}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>{loading ? 'Loading...' : 'Refresh'}</span>
          </button>
        </div>

        {loading && <p className="empty-state">Loading international licenses...</p>}

        {!loading && error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert
              message={error}
              onRetry={refresh}
              retryLabel="Try again"
            />
          </div>
        )}

        {!loading && !error && (
          <InternationalLicenseTable
            items={filteredItems}
            onOpenDetails={onOpenDetails}
          />
        )}

        {!loading && !error && (
          <footer className="panel-footer" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>
              Showing <strong>{filteredItems.length}</strong> of{' '}
              <strong>{items.length}</strong> licenses
            </span>
            <span>Use the row action to inspect a license.</span>
          </footer>
        )}
      </section>
    </section>
  )
}
