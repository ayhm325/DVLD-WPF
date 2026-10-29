import { useCallback, useEffect, useMemo, useState } from 'react'
import type { InternationalLicense } from '../../types/InternationalLicense'
import { getInternationalLicenses } from '../../services/internationalLicenseService'
import InternationalLicenseTable from './InternationalLicenseTable'
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

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      setItems(await getInternationalLicenses(accessToken))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load international licenses.')
    } finally {
      setLoading(false)
    }
  }, [accessToken])

  useEffect(() => { void load() }, [load])

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()
    return items.filter((item) => {
      const matchesSearch = !query || String(item[filter]).toLowerCase().includes(query)
      const matchesStatus =
        status === 'All' ||
        (status === 'Active' ? item.isActive : !item.isActive)
      return matchesSearch && matchesStatus
    })
  }, [items, search, filter, status])

  return (
    <section className="page-content intl-page">
      <header className="intl-page-heading">
        <div>
          <p className="eyebrow">LICENSE MANAGEMENT / INTERNATIONAL</p>
          <h2>International Licenses</h2>
          <p>Review issued international licenses and issue a new one from an eligible local license.</p>
        </div>
        <button className="intl-primary-button" type="button" onClick={onIssueNew}>
          <span aria-hidden="true">＋</span> Issue International License
        </button>
      </header>

      <div className="intl-summary-grid">
        <article className="intl-summary-card">
          <span className="intl-summary-icon">▤</span>
          <span className="intl-summary-label">Total licenses</span>
          <strong>{loading ? '—' : items.length}</strong>
          <small>International licenses recorded</small>
        </article>
        <article className="intl-summary-card">
          <span className="intl-summary-icon active">✓</span>
          <span className="intl-summary-label">Active</span>
          <strong>{loading ? '—' : items.filter((x) => x.isActive).length}</strong>
          <small>Currently active licenses</small>
        </article>
        <article className="intl-summary-card">
          <span className="intl-summary-icon muted">◷</span>
          <span className="intl-summary-label">Inactive</span>
          <strong>{loading ? '—' : items.filter((x) => !x.isActive).length}</strong>
          <small>Not currently active</small>
        </article>
      </div>

      <section className="intl-panel">
        <div className="intl-panel-heading">
          <div>
            <h3>Issued International Licenses</h3>
            <p>Search by one of the identifiers used by DVLD.</p>
          </div>
          <button className="intl-secondary-button" type="button" onClick={() => void load()} disabled={loading}>
            ↻ Refresh
          </button>
        </div>

        <div className="intl-toolbar">
          <label className="intl-search">
            <span aria-hidden="true">⌕</span>
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search licenses..." />
          </label>
          <label className="intl-control">
            <span>Search by</span>
            <select value={filter} onChange={(e) => setFilter(e.target.value as FilterKey)}>
              {filters.map((x) => <option key={x.value} value={x.value}>{x.label}</option>)}
            </select>
          </label>
          <label className="intl-control">
            <span>Status</span>
            <select value={status} onChange={(e) => setStatus(e.target.value as typeof status)}>
              <option>All</option><option>Active</option><option>Inactive</option>
            </select>
          </label>
        </div>

        {error && <div className="intl-error" role="alert"><strong>Unable to load licenses</strong><p>{error}</p><button type="button" onClick={() => void load()}>Try again</button></div>}
        {loading ? <div className="intl-state">Loading international licenses…</div> :
          !error && <InternationalLicenseTable items={filteredItems} onOpenDetails={onOpenDetails} />}
        {!loading && !error && (
          <footer className="intl-table-footer">
            <span>Showing <strong>{filteredItems.length}</strong> of <strong>{items.length}</strong> licenses</span>
            <span>Use the row action to inspect a license.</span>
          </footer>
        )}
      </section>
    </section>
  )
}
