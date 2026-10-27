
import { useEffect, useState } from 'react'
import './App.css'

import Sidebar from './components/Sidebar'
import Topbar from './components/Topbar'
import PeopleTable from './components/PeopleTable'
import Login from './components/Login'
import AddPersonModal from './components/AddPersonModal'

import { getPeople } from './services/peopleService'
import type { Person } from './types/Person'

import { getDrivers } from './services/driverService'
import type { Driver } from './services/driverService'

const settingsPages = [
  'Application Types',
  'License Classes',
  'Test Types',
  'Countries',
]

const pageDescriptions: Record<string, string> = {
  'Application Types': 'Manage application types and their fees.',
  'License Classes': 'View the available license classes.',
  'Test Types': 'Manage test types and their fees.',
  Countries: 'View the countries available in the system.',
}

export default function App() {
  // Authentication
  const [accessToken, setAccessToken] = useState<string | null>(null)

  // Navigation
  const [activePage, setActivePage] = useState('Dashboard')

  // People search
  const [searchName, setSearchName] = useState('')

  // People data
  const [people, setPeople] = useState<Person[]>([])
  const [loadingPeople, setLoadingPeople] = useState(false)
  const [peopleError, setPeopleError] = useState('')

  // Drivers data
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [loadingDrivers, setLoadingDrivers] = useState(false)
  const [driversError, setDriversError] = useState('')

  // Add Person Modal
  const [isAddPersonOpen, setIsAddPersonOpen] = useState(false)

  // Load people from API
  useEffect(() => {
    if (accessToken === null) return

    let cancelled = false

    async function loadPeople() {
      setLoadingPeople(true)
      setPeopleError('')

      try {
        const result = await getPeople(accessToken!)

        if (!cancelled) {
          setPeople(result)
        }
      } catch (error) {
        if (!cancelled) {
          setPeopleError(
            error instanceof Error
              ? error.message
              : 'Failed to load people.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingPeople(false)
        }
      }
    }

    void loadPeople()

    return () => {
      cancelled = true
    }
  }, [accessToken])

  // Load drivers from API
  useEffect(() => {
    if (accessToken === null) return

    let cancelled = false

    async function loadDrivers() {
      setLoadingDrivers(true)
      setDriversError('')

      try {
        const result = await getDrivers(accessToken!)

        if (!cancelled) {
          setDrivers(result)
        }
      } catch (error) {
        if (!cancelled) {
          setDriversError(
            error instanceof Error
              ? error.message
              : 'Failed to load drivers.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingDrivers(false)
        }
      }
    }

    void loadDrivers()

    return () => {
      cancelled = true
    }
  }, [accessToken])

  // Filter people by name or national number
  const filteredPeople = people.filter(
    (person) =>
      person.fullName.toLowerCase().includes(searchName.toLowerCase()) ||
      person.nationalNo.includes(searchName),
  )

  // Show login page when not authenticated
  if (accessToken === null) {
    return (
      <Login onLoginSuccess={(token) => setAccessToken(token)} />
    )
  }

  return (
    <div className="app-shell">
      {/* Sidebar */}
      <Sidebar
        activePage={activePage}
        onNavigate={setActivePage}
      />

      {/* Main content */}
      <main className="main-content">
        {/* Top navigation */}
        <Topbar activePage={activePage} />

        {/* Dashboard */}
        {activePage === 'Dashboard' && (
          <section className="page-content">
            {/* Welcome banner */}
            <div className="welcome-banner">
              <div>
                <p className="eyebrow">CONTROL CENTER</p>

                <h2>Welcome to DVLD</h2>

                <p>
                  Manage people, drivers, and licensing operations
                  from one place.
                </p>
              </div>

              <div className="banner-mark">D</div>
            </div>

            {/* Statistics cards */}
            <div className="stats-grid">
              {/* Total People */}
              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Total People</span>
                  <span className="stat-icon">♙</span>
                </div>

                <strong>
                  {loadingPeople ? '...' : people.length}
                </strong>

                <p>{peopleError || 'Registered people'}</p>
              </article>

              {/* Total Drivers */}
              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Total Drivers</span>
                  <span className="stat-icon">♧</span>
                </div>

                <strong>
                  {loadingDrivers ? '...' : drivers.length}
                </strong>

                <p>{driversError || 'Registered drivers'}</p>
              </article>

              {/* Licenses */}
              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Licenses</span>
                  <span className="stat-icon">▤</span>
                </div>

                <strong>0</strong>
                <p>Connect API to load data</p>
              </article>

              {/* Applications */}
              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Applications</span>
                  <span className="stat-icon">▣</span>
                </div>

                <strong>0</strong>
                <p>Connect API to load data</p>
              </article>
            </div>

            {/* People loading state */}
            {loadingPeople && <p>Loading people...</p>}

            {/* People error */}
            {peopleError && (
              <p className="login-error">{peopleError}</p>
            )}

            {/* People table */}
            {!loadingPeople && !peopleError && (
              <PeopleTable
                people={filteredPeople}
                searchName={searchName}
                onSearchChange={setSearchName}
                onAddPerson={() => setIsAddPersonOpen(true)}
              />
            )}
          </section>
        )}

        {/* People page */}
        {activePage === 'People' && (
          <section className="page-content">
            <h2>People Management</h2>

            {loadingPeople && <p>Loading people...</p>}

            {peopleError && (
              <p className="login-error">{peopleError}</p>
            )}

            {!loadingPeople && !peopleError && (
              <PeopleTable
                people={filteredPeople}
                searchName={searchName}
                onSearchChange={setSearchName}
                onAddPerson={() => setIsAddPersonOpen(true)}
              />
            )}
          </section>
        )}

        {/* Drivers page */}
        {activePage === 'Drivers' && (
          <section className="page-content">
            <div className="panel placeholder-panel">
              <div className="placeholder-icon">♧</div>

              <h2>Drivers Management</h2>

              {loadingDrivers && <p>Loading drivers...</p>}

              {driversError && (
                <p className="login-error">{driversError}</p>
              )}

              {!loadingDrivers && !driversError && (
                <p>Total drivers: {drivers.length}</p>
              )}
            </div>
          </section>
        )}

        {/* Settings pages */}
        {settingsPages.includes(activePage) && (
          <section className="page-content">
            <div className="panel placeholder-panel">
              <div className="placeholder-icon">⚙</div>

              <h2>{activePage}</h2>

              <p>{pageDescriptions[activePage]}</p>

              {activePage === 'Application Types' && (
                <p>
                  The API supports updating application types
                  for administrators.
                </p>
              )}

              {activePage === 'Test Types' && (
                <p>
                  The API supports updating test types
                  for administrators.
                </p>
              )}

              {(activePage === 'Countries' ||
                activePage === 'License Classes') && (
                <p>
                  The current API supports viewing these records,
                  but does not provide editing endpoints.
                </p>
              )}

              <button
                type="button"
                className="primary-button"
                onClick={() => setActivePage('Dashboard')}
              >
                Back to Dashboard
              </button>
            </div>
          </section>
        )}

        {/* Other temporary pages */}
        {![
          'Dashboard',
          'People',
          'Drivers',
          'Licenses',
          'Applications',
          ...settingsPages,
        ].includes(activePage) && (
          <section className="page-content">
            <div className="panel placeholder-panel">
              <div className="placeholder-icon">▤</div>

              <h2>{activePage} Management</h2>

              <p>
                This section will be implemented in a later step.
              </p>

              <button
                type="button"
                className="primary-button"
                onClick={() => setActivePage('Dashboard')}
              >
                Back to Dashboard
              </button>
            </div>
          </section>
        )}

        {/* Add Person Modal - shared between pages */}
        {isAddPersonOpen && (
          <AddPersonModal
            accessToken={accessToken}
            onClose={() => setIsAddPersonOpen(false)}
            onPersonAdded={async () => {
              try {
                const updatedPeople = await getPeople(accessToken)

                setPeople(updatedPeople)
                setIsAddPersonOpen(false)
                setSearchName('')
                setPeopleError('')
              } catch (error) {
                setPeopleError(
                  error instanceof Error
                    ? error.message
                    : 'Person was created, but the list could not be refreshed.',
                )

                setIsAddPersonOpen(false)
              }
            }}
          />
        )}

        {/* Footer */}
        <footer className="app-footer">
          DVLD Management System
          <span>Frontend Preview</span>
        </footer>
      </main>
    </div>
  )
}
