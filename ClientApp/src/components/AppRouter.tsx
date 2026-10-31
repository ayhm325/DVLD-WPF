import AccessDenied from './common/AccessDenied'
import { isPagePermitted } from '../utils/permissions'
import DashboardPage from './DashboardPage'
import PeoplePage from './PeoplePage'
import DriversPage from './DriversPage'
import LicensesPage from './LicensesPage'
import InternationalLicensesPage from './international license/InternationalLicensesPage'
import IssueInternationalLicensePage from './international license/IssueInternationalLicensePage'
import LocalApplicationsPage from './local applications/LocalApplicationsPage'
import CountriesPage from './CountriesPage'
import ApplicationTypesPage from './ApplicationTypesPage'
import LicenseClassesPage from './LicenseClassesPage'
import TestTypesPage from './TestTypesPage'
import { IdCard as LicenseIcon, Settings as SettingsIcon } from 'lucide-react'
import type { Person } from '../types/Person'

export type AppRouterProps = {
  activePage: string
  accessToken: string
  userRole?: string | null
  onNavigate: (page: string) => void
  showIssueInternational: boolean
  setShowIssueInternational: (show: boolean) => void
  internationalRefreshKey: number
  onInternationalIssued: () => void
  statisticsRefreshKey: number
  people: Person[]
  filteredPeople: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  loadingPeople: boolean
  peopleError: string
  onRetryPeople: () => void
  onAddPerson: () => void
}

const settingsPages = [
  'Application Types',
  'License Classes',
  'Test Types',
  'Countries',
]

export default function AppRouter({
  activePage,
  accessToken,
  userRole,
  onNavigate,
  showIssueInternational,
  setShowIssueInternational,
  internationalRefreshKey,
  onInternationalIssued,
  statisticsRefreshKey,
  people,
  filteredPeople,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
}: AppRouterProps) {
  const isCurrentPagePermitted = isPagePermitted(activePage, userRole)

  if (!isCurrentPagePermitted) {
    return (
      <section className="page-content">
        <AccessDenied
          pageTitle={activePage}
          userRole={userRole ?? undefined}
          onBackToDashboard={() => onNavigate('Dashboard')}
        />
      </section>
    )
  }

  return (
    <>
      {/* Dashboard */}
      {activePage === 'Dashboard' && (
        <DashboardPage
          accessToken={accessToken}
          statisticsRefreshKey={statisticsRefreshKey}
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={onRetryPeople}
          onAddPerson={onAddPerson}
        />
      )}

      {/* People */}
      {activePage === 'People' && (
        <PeoplePage
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={onRetryPeople}
          onAddPerson={onAddPerson}
        />
      )}

      {/* Drivers */}
      {activePage === 'Drivers' && <DriversPage accessToken={accessToken} />}

      {/* Licenses */}
      {activePage === 'Licenses' && <LicensesPage accessToken={accessToken} />}

      {/* International Licenses */}
      {activePage === 'International Licenses' &&
        (showIssueInternational ? (
          <IssueInternationalLicensePage
            accessToken={accessToken}
            onBack={() => setShowIssueInternational(false)}
            onIssued={onInternationalIssued}
          />
        ) : (
          <InternationalLicensesPage
            key={internationalRefreshKey}
            accessToken={accessToken}
            onIssueNew={() => setShowIssueInternational(true)}
          />
        ))}

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
              onClick={() => onNavigate('Dashboard')}
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

            <p>This section will be implemented in a later step.</p>

            <button
              type="button"
              className="primary-button"
              onClick={() => onNavigate('Dashboard')}
            >
              Back to Dashboard
            </button>
          </div>
        </section>
      )}
    </>
  )
}
