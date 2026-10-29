
import { useEffect, useMemo, useState } from 'react'
import type { InternationalLicense } from '../../types/InternationalLicense'
import { getInternationalLicenses } from '../../services/internationalLicenseService'
import InternationalLicenseTable from './InternationalLicenseTable'
import PageHero from '../common/PageHero'
import {
  PlusIcon,
  GlobeIcon,
  CheckIcon,
  ClockIcon,
  RefreshIcon,
  SearchIcon,
} from '../common/Icons'
import './international-licenses.css'

type Props = {
  accessToken: string
  onIssueNew?: () => void
  onOpenDetails?: (id: number) => void
}

const filters = [
  { label: 'International License ID', value: 'internationalLicenseId' },
  { label: 'Application ID', value: 'applicationId' },
  { label: 'Driver ID', value: 'driverId' },
  { label: 'Local License ID', value: 'issuedUsingLocalLicenseId' },
] as const

type FilterKey = (typeof filters)[number]['value']

export default function InternationalLicensesPage({
  accessToken,
  onIssueNew,
  onOpenDetails,
}: Props) {
  const [items, setItems] = useState<InternationalLicense[]>([])
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<FilterKey>('internationalLicenseId')
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

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    return items.filter((item) => {
      const matchesSearch =
        !query || String(item[filter]).toLowerCase().includes(query)

      const matchesStatus =
        status === 'All' ||
        (status === 'Active' ? item.isActive : !item.isActive)

      return matchesSearch && matchesStatus
    })
  }, [items, search, filter, status])

  return (
    <section className="page-content intl-page">
      <PageHero
        pageId="International Licenses"
        action={
          onIssueNew ? (
            <button
              className="intl-primary-button"
              type="button"
              onClick={onIssueNew}
            >
              <PlusIcon size={16} />
              <span>Issue International License</span>
            </button>
          ) : undefined
        }
      />

      <div className="intl-summary-grid">
        <article className="intl-summary-card">
          <span className="intl-summary-icon">
            <GlobeIcon size={20} />
          </span>
          <span className="intl-summary-label">Total licenses</span>
          <strong>{loading || error ? '—' : items.length}</strong>
          <small>International licenses recorded</small>
        </article>

        <article className="intl-summary-card">
          <span className="intl-summary-icon active">
            <CheckIcon size={20} />
          </span>
          <span className="intl-summary-label">Active</span>
          <strong>
            {loading || error ? '—' : items.filter((item) => item.isActive).length}
          </strong>
          <small>Currently active licenses</small>
        </article>

        <article className="intl-summary-card">
          <span className="intl-summary-icon muted">
            <ClockIcon size={20} />
          </span>
          <span className="intl-summary-label">Inactive</span>
          <strong>
            {loading || error ? '—' : items.filter((item) => !item.isActive).length}
          </strong>
          <small>Not currently active</small>
        </article>
      </div>

      <section className="intl-panel">
        <div className="intl-panel-heading">
          <div>
            <h3>Issued International Licenses</h3>
            <p>Search by one of the identifiers used by DVLD.</p>
          </div>

          <button
            className="intl-secondary-button"
            type="button"
            onClick={refresh}
            disabled={loading}
          >
            <RefreshIcon size={14} />
            <span>Refresh</span>
          </button>
        </div>

        <div className="intl-toolbar">
          <label className="intl-search">
            <SearchIcon size={16} />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search licenses..."
            />
          </label>

          <label className="intl-control">
            <span>Search by</span>
            <select
              value={filter}
              onChange={(event) =>
                setFilter(event.target.value as FilterKey)
              }
            >
              {filters.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <label className="intl-control">
            <span>Status</span>
            <select
              value={status}
              onChange={(event) =>
                setStatus(event.target.value as typeof status)
              }
            >
              <option>All</option>
              <option>Active</option>
              <option>Inactive</option>
            </select>
          </label>
        </div>

        {error && (
          <div className="intl-error" role="alert">
            <strong>Unable to load licenses</strong>
            <p>{error}</p>
            <button type="button" onClick={refresh} disabled={loading}>
              Try again
            </button>
          </div>
        )}

        {loading ? (
          <div className="intl-state">Loading international licenses…</div>
        ) : (
          !error && (
            <InternationalLicenseTable
              items={filteredItems}
              onOpenDetails={onOpenDetails}
            />
          )
        )}

        {!loading && !error && (
          <footer className="intl-table-footer">
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
