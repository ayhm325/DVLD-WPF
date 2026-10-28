
import { useEffect, useMemo, useState } from 'react'
import { getCountries } from '../services/countryService'
import type { Country } from '../services/countryService'

type CountriesPageProps = {
  accessToken: string
}

export default function CountriesPage({
  accessToken,
}: CountriesPageProps) {
  const [countries, setCountries] = useState<Country[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

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
  }, [accessToken])

  const filteredCountries = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) return countries

    return countries.filter(
      (country) =>
        country.countryName.toLowerCase().includes(query) ||
        String(country.countryId).includes(query),
    )
  }, [countries, search])

  return (
    <section className="page-content">
      <div>
        <p className="eyebrow">SYSTEM SETTINGS</p>
        <h2>Countries Management</h2>
        <p>Browse and search countries registered in DVLD.</p>
      </div>

      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Countries Directory</h2>
            <p>Countries available in the system</p>
          </div>

          <span className="record-count">
            {loading ? 'Loading...' : `${countries.length} Countries`}
          </span>
        </div>

        <div className="table-toolbar">
          <input
            type="search"
            className="search-input"
            placeholder="Search by country name or ID..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            aria-label="Search countries"
          />
        </div>

        {loading && (
          <p className="empty-state">Loading countries...</p>
        )}

        {!loading && error && (
          <div className="empty-state" role="alert">
            <p>{error}</p>
            <button
              type="button"
              className="secondary-button"
              onClick={() => {
                setLoading(true)
                setError('')
                void getCountries(accessToken)
                  .then(setCountries)
                  .catch((err: unknown) => {
                    setError(
                      err instanceof Error
                        ? err.message
                        : 'Failed to load countries.',
                    )
                  })
                  .finally(() => setLoading(false))
              }}
            >
              Retry
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="table-wrapper">
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
                    <td>{country.countryId}</td>
                    <td>{country.countryName}</td>
                  </tr>
                ))}

                {filteredCountries.length === 0 && (
                  <tr>
                    <td colSpan={2} className="empty-state">
                      {countries.length === 0
                        ? 'No countries are registered.'
                        : 'No countries match your search.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
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
