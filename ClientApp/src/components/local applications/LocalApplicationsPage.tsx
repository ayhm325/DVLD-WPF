import { useCallback, useEffect, useMemo, useState } from 'react'
import LocalApplicationsList from './LocalApplicationsList'
import LocalApplicationForm from './LocalApplicationForm'
import { getLocalDrivingLicenseApplications } from '../../services/localDrivingLicenseApplicationService'
import type { LocalDrivingLicenseApplication, LocalDrivingLicenseApplicationsPage as PageResult } from '../../types/LocalDrivingLicenseApplication'
import './local-applications.css'

type Props = { accessToken: string }
const PAGE_SIZE = 10

type View = { mode: 'list' } | { mode: 'create' } | { mode: 'edit'; application: LocalDrivingLicenseApplication }

export default function LocalApplicationsPage({ accessToken }: Props) {
  const [view, setView] = useState<View>({ mode: 'list' })
  const [page, setPage] = useState<PageResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<'name' | 'nationalNo'>('name')
  const [status, setStatus] = useState('All')
  const [refreshKey, setRefreshKey] = useState(0)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const result = await getLocalDrivingLicenseApplications(accessToken, pageNumber, PAGE_SIZE)
      setPage(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load local applications.')
    } finally {
      setLoading(false)
    }
  }, [accessToken, pageNumber, refreshKey])

  useEffect(() => { void load() }, [load])

  const filteredItems = useMemo(() => {
    const items = page?.items ?? []
    const query = search.trim().toLowerCase()
    return items.filter((item) => {
      const searchable = searchBy === 'name' ? item.fullName : item.nationalNo
      const matchesSearch = !query || (searchable ?? '').toLowerCase().includes(query)
      const statusText = (item.statusText || item.applicationStatus).toLowerCase()
      const matchesStatus = status === 'All' || statusText === status.toLowerCase()
      return matchesSearch && matchesStatus
    })
  }, [page, search, searchBy, status])

  const returnToList = (saved: boolean) => {
    setView({ mode: 'list' })
    if (saved) setRefreshKey((value) => value + 1)
  }

  if (view.mode !== 'list') {
    return (
      <section className="page-content local-app-page">
        <LocalApplicationForm
          accessToken={accessToken}
          mode={view.mode}
          application={view.mode === 'edit' ? view.application : undefined}
          onCancel={() => returnToList(false)}
          onSaved={() => returnToList(true)}
        />
      </section>
    )
  }

  return (
    <section className="page-content local-app-page">
      <div className="local-app-heading">
        <div>
          <p className="eyebrow">LICENSING OPERATIONS</p>
          <h2>Local Driving License Applications</h2>
          <p>Track applications, review progress, and start a new driving license request.</p>
        </div>
        <div className="local-app-heading-mark" aria-hidden="true">LD</div>
      </div>

      <section className="local-app-panel">
        <header className="local-app-panel-header">
          <div>
            <span className="local-app-kicker">APPLICATION REGISTER</span>
            <h3>Driving License Applications</h3>
            <p>Applications are loaded from the DVLD API.</p>
          </div>
          <div className="local-app-header-actions">
            <span className="local-app-count">{page?.totalCount ?? 0} records</span>
            <button type="button" className="local-app-primary" onClick={() => setView({ mode: 'create' })}>
              <span aria-hidden="true">＋</span> New application
            </button>
          </div>
        </header>

        <div className="local-app-filters">
          <label className="local-app-filter-field">
            <span>Search field</span>
            <select value={searchBy} onChange={(event) => setSearchBy(event.target.value as 'name' | 'nationalNo')}>
              <option value="name">Full Name</option>
              <option value="nationalNo">National No.</option>
            </select>
          </label>
          <label className="local-app-search-field">
            <span>Search applications</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={searchBy === 'name' ? 'Search by applicant name…' : 'Search by national number…'} />
          </label>
          <label className="local-app-filter-field local-app-status-filter">
            <span>Status</span>
            <select value={status} onChange={(event) => setStatus(event.target.value)}>
              <option>All</option><option>New</option><option>Cancelled</option><option>Completed</option>
            </select>
          </label>
          <button type="button" className="local-app-icon-button" onClick={() => setRefreshKey((value) => value + 1)} disabled={loading} aria-label="Refresh applications" title="Refresh">↻</button>
        </div>

        {error && <div className="local-app-alert" role="alert"><div><strong>Applications could not be loaded</strong><p>{error}</p></div><button type="button" className="local-app-secondary" onClick={() => setRefreshKey((value) => value + 1)}>Try again</button></div>}
        <LocalApplicationsList
          items={filteredItems}
          loading={loading}
          onEdit={(application) => setView({ mode: 'edit', application })}
        />
        <footer className="local-app-pagination">
          <span>Showing {filteredItems.length} of {page?.totalCount ?? 0} applications</span>
          <div className="local-app-pagination-controls">
            <button type="button" className="local-app-secondary" disabled={!page?.hasPreviousPage || loading} onClick={() => setPageNumber((value) => Math.max(1, value - 1))}>Previous</button>
            <span>Page <strong>{page?.pageNumber ?? pageNumber}</strong> of <strong>{page?.totalPages ?? 0}</strong></span>
            <button type="button" className="local-app-secondary" disabled={!page?.hasNextPage || loading} onClick={() => setPageNumber((value) => value + 1)}>Next</button>
            <label className="local-app-page-size">Rows <select value={PAGE_SIZE} disabled aria-label="Rows per page"><option value={10}>10</option></select></label>
          </div>
        </footer>
      </section>
    </section>
  )
}
