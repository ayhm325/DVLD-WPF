
import { useCallback, useEffect, useMemo, useState } from 'react'

import './App.css'

import Sidebar from './components/Sidebar'
import Topbar from './components/Topbar'
import PeopleTable from './components/PeopleTable'
import Login from './components/Login'
import AddPersonModal from './components/AddPersonModal'

import CountriesPage from './components/CountriesPage'
import ApplicationTypesPage from './components/ApplicationTypesPage'
import LicenseClassesPage from './components/LicenseClassesPage'
import TestTypesPage from './components/TestTypesPage'
import DriversPage from './components/DriversPage'

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

export default function App() {
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [activePage, setActivePage] = useState('Dashboard')

  // People
  const [searchName, setSearchName] = useState('')
  const [people, setPeople] = useState<Person[]>([])
  const [loadingPeople, setLoadingPeople] = useState(false)
  const [peopleError, setPeopleError] = useState('')

  // Drivers
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [loadingDrivers, setLoadingDrivers] = useState(false)
  const [driversError, setDriversError] = useState('')
  const [driversRefreshKey, setDriversRefreshKey] = useState(0)

  // Add person modal
  const [isAddPersonOpen, setIsAddPersonOpen] = useState(false)

  // Load people
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

  // Load drivers and refresh when requested
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
  }, [accessToken, driversRefreshKey])

  const refreshDrivers = useCallback(() => {
    setDriversRefreshKey((current) => current + 1)
  }, [])

  // Filter people
  const filteredPeople = useMemo(
    () =>
      people.filter(
        (person) =>
          person.fullName.toLowerCase().includes(searchName.toLowerCase()) ||
          person.nationalNo.includes(searchName),
      ),
    [people, searchName],
  )

  if (accessToken === null) {
    return <Login onLoginSuccess={(token) => setAccessToken(token)} />
  }

  return (
    <div className="app-shell">
      <Sidebar
        activePage={activePage}
        onNavigate={setActivePage}
      />

      <main className="main-content">
        <Topbar activePage={activePage} />

        {/* Dashboard */}
        {activePage === 'Dashboard' && (
          <section className="page-content">
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

            <div className="stats-grid">
              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Total People</span>
                  <span className="stat-icon">♙</span>
                </div>
                <strong>{loadingPeople ? '...' : people.length}</strong>
                <p>{peopleError || 'Registered people'}</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Total Drivers</span>
                  <span className="stat-icon">♧</span>
                </div>
                <strong>{loadingDrivers ? '...' : drivers.length}</strong>
                <p>{driversError || 'Registered drivers'}</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Licenses</span>
                  <span className="stat-icon">▤</span>
                </div>
                <strong>0</strong>
                <p>Connect API to load data</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Applications</span>
                  <span className="stat-icon">▣</span>
                </div>
                <strong>0</strong>
                <p>Connect API to load data</p>
              </article>
            </div>

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

        {/* People */}
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

        {/* Drivers */}
        {activePage === 'Drivers' && (
          <DriversPage
            drivers={drivers}
            loading={loadingDrivers}
            error={driversError}
            onRetry={refreshDrivers}
          />
        )}

        {/* Settings pages */}
        {activePage === 'Countries' && (
          <CountriesPage accessToken={accessToken} />
        )}

        {activePage === 'Application Types' && (
          <ApplicationTypesPage accessToken={accessToken} />
        )}

        {activePage === 'License Classes' && (
          <LicenseClassesPage accessToken={accessToken} />
        )}

        {activePage === 'Test Types' && (
          <TestTypesPage accessToken={accessToken} />
        )}

        {/* Remaining settings placeholders, if any */}
        {settingsPages
          .filter(
            (page) =>
              page !== 'Countries' &&
              page !== 'Application Types' &&
              page !== 'License Classes' &&
              page !== 'Test Types',
          )
          .includes(activePage) && (
          <section className="page-content">
            <div className="panel placeholder-panel">
              <div className="placeholder-icon">⚙</div>
              <h2>{activePage}</h2>

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

        {/* Add Person Modal */}
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

        <footer className="app-footer">
          DVLD Management System
          <span>Frontend Preview</span>
        </footer>
      </main>
    </div>
  )
}
