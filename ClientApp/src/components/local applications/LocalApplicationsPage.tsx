import { useEffect, useMemo, useState } from 'react'
import LocalApplicationsList from './LocalApplicationsList'
import LocalApplicationForm from './LocalApplicationForm'
import PageHero from '../common/PageHero'
import { getLocalDrivingLicenseApplications } from '../../services/localDrivingLicenseApplicationService'
import type {
  LocalDrivingLicenseApplication,
  LocalDrivingLicenseApplicationsPage as PageResult,
} from '../../types/LocalDrivingLicenseApplication'
import {
  PlusIcon,
  SearchIcon,
  CloseIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ApplicationsIcon,
  CheckIcon,
  ClockIcon,
  FilterIcon,
} from '../common/Icons'
import './local-applications.css'

type Props = {
  accessToken: string
}

const PAGE_SIZE = 10

type View =
  | { mode: 'list' }
  | { mode: 'create' }
  | { mode: 'edit'; application: LocalDrivingLicenseApplication }

type SearchBy = 'all' | 'name' | 'nationalNo' | 'appId' | 'class'

export default function LocalApplicationsPage({ accessToken }: Props) {
  const [view, setView] = useState<View>({ mode: 'list' })
  const [page, setPage] = useState<PageResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [search, setSearch] = useState('')
  const [searchBy, setSearchBy] = useState<SearchBy>('all')
  const [status, setStatus] = useState('All')
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function fetchApplications() {
      try {
        const result = await getLocalDrivingLicenseApplications(
          accessToken,
          pageNumber,
          PAGE_SIZE,
        )

        if (cancelled) return

        setPage(result)
        setError('')
      } catch (err) {
        if (cancelled) return

        setError(
          err instanceof Error
            ? err.message
            : 'Could not load local applications.',
        )
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void fetchApplications()

    return () => {
      cancelled = true
    }
  }, [accessToken, pageNumber, refreshKey])

  function handleResetFilters() {
    setSearch('')
    setSearchBy('all')
    setStatus('All')
    setPageNumber(1)
  }

  const isFiltered = Boolean(search.trim() || searchBy !== 'all' || status !== 'All')

  const filteredItems = useMemo(() => {
    const items = page?.items ?? []
    const query = search.trim().toLowerCase()

    return items.filter((item) => {
      // 1. Status check
      const statusText = (
        item.statusText || item.applicationStatus || ''
      ).toLowerCase()

      const matchesStatus =
        status === 'All' || statusText === status.toLowerCase()

      if (!matchesStatus) return false

      // 2. Query check
      if (!query) return true

      if (searchBy === 'name') {
        return (item.fullName ?? '').toLowerCase().includes(query)
      }
      if (searchBy === 'nationalNo') {
        return (item.nationalNo ?? '').toLowerCase().includes(query)
      }
      if (searchBy === 'appId') {
        return String(item.localDrivingLicenseApplicationId).includes(query)
      }
      if (searchBy === 'class') {
        return (item.licenseClassName ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return (
        (item.fullName ?? '').toLowerCase().includes(query) ||
        (item.nationalNo ?? '').toLowerCase().includes(query) ||
        String(item.localDrivingLicenseApplicationId).includes(query) ||
        (item.licenseClassName ?? '').toLowerCase().includes(query)
      )
    })
  }, [page, search, searchBy, status])

  const placeholderText = useMemo(() => {
    switch (searchBy) {
      case 'name':
        return 'Search by applicant name...'
      case 'nationalNo':
        return 'Search by national number...'
      case 'appId':
        return 'Search by application ID...'
      case 'class':
        return 'Search by driving class...'
      default:
        return 'Search by name, national no, app ID, class...'
    }
  }, [searchBy])

  function refreshApplications() {
    setLoading(true)
    setRefreshKey((value) => value + 1)
  }

  function changePage(nextPage: number) {
    setLoading(true)
    setPageNumber(nextPage)
  }

  function returnToList(saved: boolean) {
    setView({ mode: 'list' })

    if (saved) {
      setLoading(true)
      setPageNumber(1)
      setRefreshKey((value) => value + 1)
    }
  }

  if (view.mode !== 'list') {
    return (
      <section className="page-content local-app-page">
        <LocalApplicationForm
          accessToken={accessToken}
          mode={view.mode}
          application={
            view.mode === 'edit' ? view.application : undefined
          }
          onCancel={() => returnToList(false)}
          onSaved={() => returnToList(true)}
        />
      </section>
    )
  }

  return (
    <section className="page-content local-app-page">
      <PageHero pageId="Local Applications" />

      <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
        <article className="stat-card">
          <div className="stat-card-top">
            <span>Total Applications</span>
            <span className="stat-icon" aria-hidden="true">
              <ApplicationsIcon size={20} />
            </span>
          </div>
          <strong>{loading ? '...' : error ? '—' : (page?.totalCount ?? 0)}</strong>
          <p>Recorded applications in DVLD</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Loaded on Page</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#059669', background: '#ecfdf5' }}>
              <CheckIcon size={20} />
            </span>
          </div>
          <strong style={{ color: '#059669' }}>
            {loading ? '...' : error ? '—' : (page?.items.length ?? 0)}
          </strong>
          <p>{error ? '—' : `Applications on page ${page?.pageNumber ?? 1} of ${page?.totalPages ?? 1}`}</p>
        </article>

        <article className="stat-card">
          <div className="stat-card-top">
            <span>Required Tests</span>
            <span className="stat-icon" aria-hidden="true" style={{ color: '#64748b', background: '#f1f5f9' }}>
              <ClockIcon size={20} />
            </span>
          </div>
          <strong>3</strong>
          <p>Vision, written, and street tests</p>
        </article>
      </div>

      <section className="panel">
        <header className="panel-header">
          <div>
            <h2>Driving License Applications</h2>
            <p>Applications registered and loaded from the DVLD system.</p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span className="record-count">
              {loading
                ? 'Loading...'
                : error
                  ? '—'
                  : isFiltered
                    ? `${filteredItems.length} of ${page?.totalCount ?? 0} Applications`
                    : `${page?.totalCount ?? 0} Applications`}
            </span>

            <button
              type="button"
              className="primary-button"
              onClick={() => setView({ mode: 'create' })}
            >
              <PlusIcon size={16} />
              <span>New Application</span>
            </button>
          </div>
        </header>

        <div className="table-toolbar">
          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 150 }}
            value={searchBy}
            onChange={(event) => {
              setSearchBy(event.target.value as SearchBy)
              setPageNumber(1)
            }}
            aria-label="Filter criterion"
          >
            <option value="all">All Fields</option>
            <option value="name">Applicant Name</option>
            <option value="nationalNo">National No.</option>
            <option value="appId">Application ID</option>
            <option value="class">Driving Class</option>
          </select>

          <div className="search-wrapper">
            <span className="search-icon-left" aria-hidden="true">
              <SearchIcon size={16} />
            </span>

            <input
              type="text"
              className="search-input"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPageNumber(1)
              }}
              placeholder={placeholderText}
              aria-label="Search applications"
            />

            {search && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => {
                  setSearch('')
                  setPageNumber(1)
                }}
                aria-label="Clear search"
              >
                <CloseIcon size={14} />
              </button>
            )}
          </div>

          <select
            className="search-input"
            style={{ width: 'auto', minWidth: 140 }}
            value={status}
            onChange={(event) => {
              setStatus(event.target.value)
              setPageNumber(1)
            }}
            aria-label="Filter by application status"
          >
            <option value="All">All Statuses</option>
            <option value="New">New</option>
            <option value="Cancelled">Cancelled</option>
            <option value="Completed">Completed</option>
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
        </div>

        {error && (
          <div className="login-error" style={{ margin: '0 25px 20px' }} role="alert">
            <p>{error}</p>
            <button
              type="button"
              className="primary-button"
              onClick={refreshApplications}
              disabled={loading}
            >
              Retry
            </button>
          </div>
        )}

        <LocalApplicationsList
          items={filteredItems}
          loading={loading}
          onEdit={(application) =>
            setView({ mode: 'edit', application })
          }
        />

        <footer className="panel-footer" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
          <span>
            {error
              ? 'Failed to load applications'
              : `Showing ${filteredItems.length} of ${page?.totalCount ?? 0} applications`}
          </span>

          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <button
              type="button"
              className="secondary-button"
              style={{ minHeight: 34, padding: '6px 12px', fontSize: 11 }}
              disabled={!page?.hasPreviousPage || loading}
              onClick={() => changePage(Math.max(1, pageNumber - 1))}
            >
              <ChevronLeftIcon size={14} />
              <span>Previous</span>
            </button>

            <span style={{ fontSize: 11, color: 'var(--dvld-muted)' }}>
              Page <strong style={{ color: 'var(--dvld-heading)' }}>{page?.pageNumber ?? pageNumber}</strong> of{' '}
              <strong style={{ color: 'var(--dvld-heading)' }}>{error ? '—' : page?.totalPages ?? 0}</strong>
            </span>

            <button
              type="button"
              className="secondary-button"
              style={{ minHeight: 34, padding: '6px 12px', fontSize: 11 }}
              disabled={!page?.hasNextPage || loading}
              onClick={() => changePage(pageNumber + 1)}
            >
              <span>Next</span>
              <ChevronRightIcon size={14} />
            </button>

            <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginLeft: 6, fontSize: 11, color: 'var(--dvld-muted)' }}>
              Rows
              <select
                className="search-input"
                style={{ width: 'auto', minHeight: 30, padding: '4px 8px', fontSize: 11 }}
                value={PAGE_SIZE}
                disabled
                aria-label="Rows per page"
              >
                <option value={10}>10</option>
              </select>
            </label>
          </div>
        </footer>
      </section>
    </section>
  )
}
