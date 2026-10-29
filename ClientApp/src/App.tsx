import { useCallback, useEffect, useMemo, useState } from 'react'

import './App.css'

import Sidebar from './components/Sidebar'
import Topbar from './components/Topbar'
import PeopleTable from './components/PeopleTable'
import Login from './components/Login'
import AddPersonModal from './components/AddPersonModal'
import ConfirmModal from './components/common/ConfirmModal'
import LoadingSpinner from './components/common/LoadingSpinner'
import AccessDenied from './components/common/AccessDenied'
import {
  UsersIcon,
  DriversIcon,
  LicenseIcon,
  ApplicationsIcon,
  SettingsIcon,
} from './components/common/Icons'

import CountriesPage from './components/CountriesPage'
import ApplicationTypesPage from './components/ApplicationTypesPage'
import LicenseClassesPage from './components/LicenseClassesPage'
import TestTypesPage from './components/TestTypesPage'
import DriversPage from './components/DriversPage'
import LicensesPage from './components/LicensesPage'
import InternationalLicensesPage from './components/international license/InternationalLicensesPage'
import IssueInternationalLicensePage from './components/international license/IssueInternationalLicensePage'
import LocalApplicationsPage from './components/local applications/LocalApplicationsPage'

import { useAuth } from './context/useAuth'
import { isPagePermitted } from './utils/permissions'
import { getPeople } from './services/peopleService'
import type { Person } from './types/Person'

import { getDrivers } from './services/driverService'
import type { Driver } from './services/driverService'

import { getDashboardStatistics } from './services/dashboardService'
import type { DashboardStatistics } from './services/dashboardService'

const settingsPages = [
  'Application Types',
  'License Classes',
  'Test Types',
  'Countries',
]

export default function App() {
  const { accessToken, user, isAuthenticated, isLoading, logout } = useAuth()
  const [activePage, setActivePage] = useState('Dashboard')
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false)
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false)

  // International license navigation and list refresh
  const [showIssueInternational, setShowIssueInternational] = useState(false)
  const [internationalRefreshKey, setInternationalRefreshKey] = useState(0)

  // People
  const [searchName, setSearchName] = useState('')
  const [people, setPeople] = useState<Person[]>([])
  const [loadingPeople, setLoadingPeople] = useState(false)
  const [peopleError, setPeopleError] = useState('')
  const [peopleRefreshKey, setPeopleRefreshKey] = useState(0)

  // Drivers
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [loadingDrivers, setLoadingDrivers] = useState(false)
  const [driversError, setDriversError] = useState('')
  const [driversRefreshKey, setDriversRefreshKey] = useState(0)

  // Dashboard statistics are loaded together from one API endpoint.
  const [dashboardStatistics, setDashboardStatistics] = useState<DashboardStatistics | null>(null)
  const [loadingStatistics, setLoadingStatistics] = useState(false)
  const [statisticsError, setStatisticsError] = useState('')
  const [statisticsRefreshKey, setStatisticsRefreshKey] = useState(0)

  // Add person modal
  const [isAddPersonOpen, setIsAddPersonOpen] = useState(false)

  // Load people: only when authenticated, permitted for user role, and viewing Dashboard or People
  useEffect(() => {
    if (!accessToken || !isPagePermitted('People', user?.role)) return
    if (activePage !== 'Dashboard' && activePage !== 'People') return

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
  }, [accessToken, user?.role, activePage, peopleRefreshKey])

  // Load drivers: only when authenticated, permitted (StaffOnly), and viewing Drivers page
  useEffect(() => {
    if (!accessToken || !isPagePermitted('Drivers', user?.role) || activePage !== 'Drivers') {
      return
    }

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
  }, [accessToken, user?.role, activePage, driversRefreshKey])

  // Load dashboard counts through one endpoint: only when viewing Dashboard
  useEffect(() => {
    if (!accessToken || activePage !== 'Dashboard') return

    let cancelled = false

    async function loadStatistics() {
      setLoadingStatistics(true)
      setStatisticsError('')

      try {
        const result = await getDashboardStatistics(accessToken!)

        if (!cancelled) {
          setDashboardStatistics(result)
        }
      } catch (error) {
        if (!cancelled) {
          setStatisticsError(
            error instanceof Error
              ? error.message
              : 'Failed to load dashboard statistics.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingStatistics(false)
        }
      }
    }

    void loadStatistics()

    return () => {
      cancelled = true
    }
  }, [accessToken, activePage, statisticsRefreshKey])

  const refreshDrivers = useCallback(() => {
    setDriversRefreshKey((current) => current + 1)
  }, [])

  const refreshPeople = useCallback(() => {
    setPeopleRefreshKey((current) => current + 1)
  }, [])

  const handleInternationalIssued = useCallback(() => {
    setShowIssueInternational(false)
    setInternationalRefreshKey((current) => current + 1)
    setStatisticsRefreshKey((current) => current + 1)
  }, [])

  const handleNavigate = useCallback((page: string) => {
    setActivePage(page)
    setShowIssueInternational(false)
    setIsMobileSidebarOpen(false)

    if (page === 'Dashboard') {
      setStatisticsRefreshKey((current) => current + 1)
      setPeopleRefreshKey((current) => current + 1)
    } else if (page === 'People') {
      setPeopleRefreshKey((current) => current + 1)
    }
  }, [])

  const handleConfirmLogout = useCallback(() => {
    setIsLogoutModalOpen(false)
    setIsMobileSidebarOpen(false)
    setActivePage('Dashboard')
    logout('You have successfully signed out of DVLD.')
  }, [logout])

  // Filter people
  const filteredPeople = useMemo(() => {
    const query = searchName.trim().toLowerCase()
    if (!query) return people

    return people.filter(
      (person) =>
        (person.fullName?.toLowerCase() || '').includes(query) ||
        (person.nationalNo?.toLowerCase() || '').includes(query),
    )
  }, [people, searchName])

  const isCurrentPagePermitted = isPagePermitted(activePage, user?.role)

  if (isLoading) {
    return (
      <div className="login-page">
        <LoadingSpinner message="Checking DVLD session..." size="lg" />
      </div>
    )
  }

  if (!isAuthenticated || accessToken === null) {
    return <Login />
  }

  return (
    <div className="app-shell">
      <Sidebar
        activePage={activePage}
        onNavigate={handleNavigate}
        isOpen={isMobileSidebarOpen}
        onCloseMobile={() => setIsMobileSidebarOpen(false)}
        onRequestLogout={() => setIsLogoutModalOpen(true)}
      />

      <main className="main-content">
        <Topbar
          activePage={
            activePage === 'International Licenses' && showIssueInternational
              ? 'Issue International License'
              : activePage
          }
          onToggleSidebar={() => setIsMobileSidebarOpen((prev) => !prev)}
          onRequestLogout={() => setIsLogoutModalOpen(true)}
        />

        {!isCurrentPagePermitted ? (
          <section className="page-content">
            <AccessDenied
              pageTitle={activePage}
              userRole={user?.role}
              onBackToDashboard={() => setActivePage('Dashboard')}
            />
          </section>
        ) : (
          <>
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
                  <span className="stat-icon" aria-hidden="true">
                    <UsersIcon size={20} />
                  </span>
                </div>
                <strong>{loadingStatistics ? '...' : statisticsError ? '—' : dashboardStatistics?.totalPeople ?? '—'}</strong>
                <p>Registered people</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Total Drivers</span>
                  <span className="stat-icon" aria-hidden="true">
                    <DriversIcon size={20} />
                  </span>
                </div>
                <strong>{loadingStatistics ? '...' : statisticsError ? '—' : dashboardStatistics?.totalDrivers ?? '—'}</strong>
                <p>Registered drivers</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Active Licenses</span>
                  <span className="stat-icon" aria-hidden="true">
                    <LicenseIcon size={20} />
                  </span>
                </div>
                <strong>{loadingStatistics ? '...' : statisticsError ? '—' : dashboardStatistics?.activeLicenses ?? '—'}</strong>
                <p>Currently active licenses</p>
              </article>

              <article className="stat-card">
                <div className="stat-card-top">
                  <span>Pending Applications</span>
                  <span className="stat-icon" aria-hidden="true">
                    <ApplicationsIcon size={20} />
                  </span>
                </div>
                <strong>{loadingStatistics ? '...' : statisticsError ? '—' : dashboardStatistics?.pendingApplications ?? '—'}</strong>
                <p>Applications awaiting processing</p>
              </article>
            </div>

            {statisticsError && (
              <div className="login-error" role="alert">
                <span>{statisticsError}</span>
                <button
                  type="button"
                  className="primary-button"
                  onClick={() => setStatisticsRefreshKey((current) => current + 1)}
                >
                  Retry statistics
                </button>
              </div>
            )}

            {loadingPeople && <p className="empty-state">Loading people...</p>}

            {peopleError && (
              <div className="login-error" role="alert">
                <span>{peopleError}</span>
                <button
                  type="button"
                  className="primary-button"
                  onClick={refreshPeople}
                >
                  Retry people
                </button>
              </div>
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
            {loadingPeople && <p className="empty-state">Loading people...</p>}

            {peopleError && (
              <div className="login-error" role="alert">
                <span>{peopleError}</span>
                <button
                  type="button"
                  className="primary-button"
                  onClick={refreshPeople}
                >
                  Retry people
                </button>
              </div>
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

        {/* Licenses */}
        {activePage === 'Licenses' && (
          <LicensesPage accessToken={accessToken} />
        )}

        {/* International Licenses */}
        {activePage === 'International Licenses' && (
          showIssueInternational ? (
            <IssueInternationalLicensePage
              accessToken={accessToken}
              onBack={() => setShowIssueInternational(false)}
              onIssued={handleInternationalIssued}
            />
          ) : (
            <InternationalLicensesPage
              key={internationalRefreshKey}
              accessToken={accessToken}
              onIssueNew={() => setShowIssueInternational(true)}
            />
          )
        )}

        {/* Applications */}
        {activePage === 'Local Applications' && (
           <LocalApplicationsPage accessToken={accessToken} />
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
              <div className="placeholder-icon">
                <SettingsIcon size={32} />
              </div>
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
          'International Licenses',
          'Local Applications',
          'Applications',
          ...settingsPages,
        ].includes(activePage) && (
          <section className="page-content">
            <div className="panel placeholder-panel">
              <div className="placeholder-icon">
                <LicenseIcon size={32} />
              </div>
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
          </>
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
                setStatisticsRefreshKey((current) => current + 1)
                setPeopleRefreshKey((current) => current + 1)
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

        {/* Logout Confirmation Dialog */}
        <ConfirmModal
          isOpen={isLogoutModalOpen}
          title="Sign Out of DVLD"
          message="Are you sure you want to sign out? Your active session token will be cleared and you will be returned to the sign-in screen."
          confirmText="Sign Out"
          cancelText="Stay Signed In"
          confirmVariant="danger"
          onConfirm={handleConfirmLogout}
          onCancel={() => setIsLogoutModalOpen(false)}
        />

        <footer className="app-footer">
          DVLD Management System
          <span>Frontend Preview</span>
        </footer>
      </main>
    </div>
  )
}
