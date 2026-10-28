
import { useEffect, useMemo, useState } from 'react'
import { getLicenseClasses } from '../services/licenseClassService'
import type { LicenseClass } from '../services/licenseClassService'

type Props = {
  accessToken: string
}

export default function LicenseClassesPage({ accessToken }: Props) {
  const [items, setItems] = useState<LicenseClass[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

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
  }, [accessToken])

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return items

    return items.filter(
      (item) =>
        item.licenseClassName.toLowerCase().includes(query) ||
        String(item.licenseClassId).includes(query) ||
        item.licenseClassDescription.toLowerCase().includes(query),
    )
  }, [items, search])

  return (
    <section className="page-content">
      <div>
        <p className="eyebrow">SYSTEM SETTINGS</p>
        <h2>License Classes</h2>
        <p>Browse license categories, requirements, validity, and fees.</p>
      </div>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>License Classes Directory</h2>
            <p>License classes configured in DVLD</p>
          </div>

          <span className="record-count">
            {loading ? 'Loading...' : `${items.length} Classes`}
          </span>
        </div>

        <div className="table-toolbar">
          <input
            type="search"
            className="search-input"
            placeholder="Search by name, ID, or description..."
            aria-label="Search license classes"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <button
            type="button"
            className="secondary-button"
            disabled={loading}
            onClick={() => window.location.reload()}
          >
            Refresh
          </button>
        </div>

        {error && (
          <div className="empty-state" role="alert">
            <p className="login-error">{error}</p>
            <button
              type="button"
              className="secondary-button"
              onClick={() => window.location.reload()}
            >
              Retry
            </button>
          </div>
        )}

        {loading ? (
          <p className="empty-state">Loading license classes...</p>
        ) : !error ? (
          <div className="table-wrapper">
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
                    <td>{item.licenseClassId}</td>
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
                      {items.length === 0
                        ? 'No license classes found.'
                        : 'No license classes match your search.'}
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
