import { lazy, Suspense } from 'react'
import AccessDenied from './common/AccessDenied'
import LoadingSpinner from './common/LoadingSpinner'
import { isPagePermitted } from '../utils/permissions'
import { IdCard as LicenseIcon, Settings as SettingsIcon } from 'lucide-react'
import type { Person } from '../types/Person'
import type { Driver } from '../types/Driver'

const DashboardPage = lazy(() => import('./DashboardPage'))
const PeoplePage = lazy(() => import('./PeoplePage'))
const AddPersonPage = lazy(() => import('./AddPersonPage'))
const EditPersonPage = lazy(() => import('./people/EditPersonPage'))
const PersonDetailsPage = lazy(() => import('./people/PersonDetailsPage'))
const DriversPage = lazy(() => import('./DriversPage'))
const LicensesPage = lazy(() => import('./LicensesPage'))
const LicenseHistoryPage = lazy(() => import('./licenses/LicenseHistoryPage'))
const InternationalLicensesPage = lazy(
  () => import('./international license/InternationalLicensesPage'),
)
const IssueInternationalLicensePage = lazy(
  () => import('./international license/IssueInternationalLicensePage'),
)
const LocalApplicationsPage = lazy(
  () => import('./local applications/LocalApplicationsPage'),
)
const CountriesPage = lazy(() => import('./CountriesPage'))
const ApplicationTypesPage = lazy(() => import('./ApplicationTypesPage'))
const LicenseClassesPage = lazy(() => import('./LicenseClassesPage'))
const TestTypesPage = lazy(() => import('./TestTypesPage'))

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
  onPersonAdded?: () => void
  onShowDetails?: (person: Person) => void
  onEditPerson?: (person: Person) => void
  selectedPersonId?: number | null
  setSelectedPersonId?: (id: number | null) => void
  selectedDriver?: Driver | null
  setSelectedDriver?: (driver: Driver | null) => void
  onShowPersonInfo?: (personId: number) => void
  onShowLicenseHistory?: (personId: number, driver?: Driver) => void
  personDetailsSource?: string
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
  onPersonAdded,
  onShowDetails,
  onEditPerson,
  selectedPersonId,
  setSelectedPersonId,
  selectedDriver,
  onShowPersonInfo,
  onShowLicenseHistory,
  personDetailsSource,
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
    <Suspense
      fallback={
        <div className="flex items-center justify-center min-h-[400px] w-full py-16">
          <LoadingSpinner size="lg" />
        </div>
      }
    >
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
          onShowDetails={onShowDetails}
          onEditPerson={onEditPerson}
        />
      )}

      {/* People */}
      {activePage === 'People' && (
        <PeoplePage
          accessToken={accessToken}
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={onSearchChange}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={onRetryPeople}
          onAddPerson={onAddPerson}
          onShowDetails={onShowDetails}
          onEditPerson={onEditPerson}
        />
      )}

      {/* Add Person */}
      {activePage === 'Add Person' && (
        <AddPersonPage
          accessToken={accessToken}
          onBack={() => onNavigate('People')}
          onPersonAdded={onPersonAdded ?? (() => onNavigate('People'))}
        />
      )}

      {/* Edit Person */}
      {activePage === 'Edit Person' && (
        <EditPersonPage
          accessToken={accessToken}
          personId={selectedPersonId ?? null}
          onBack={() => onNavigate('People')}
          onPersonUpdated={onPersonAdded ?? (() => onNavigate('People'))}
        />
      )}

      {/* Person Details */}
      {activePage === 'Person Details' && (
        <PersonDetailsPage
          accessToken={accessToken}
          personId={selectedPersonId ?? null}
          onBack={() => onNavigate(personDetailsSource || 'People')}
          onEdit={(id) => {
            setSelectedPersonId?.(id)
            onNavigate('Edit Person')
          }}
        />
      )}

      {/* Drivers */}
      {activePage === 'Drivers' && (
        <DriversPage
          accessToken={accessToken}
          onShowPersonInfo={onShowPersonInfo}
          onShowLicenseHistory={onShowLicenseHistory}
        />
      )}

      {/* Person License History */}
      {activePage === 'Person License History' && (
        <LicenseHistoryPage
          accessToken={accessToken}
          personId={selectedPersonId}
          driver={selectedDriver}
          onBack={() => onNavigate('Drivers')}
        />
      )}

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
        <CountriesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === 'Application Types' && (
        <ApplicationTypesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === 'License Classes' && (
        <LicenseClassesPage accessToken={accessToken} userRole={userRole} />
      )}

      {activePage === 'Test Types' && (
        <TestTypesPage accessToken={accessToken} userRole={userRole} />
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
        'Add Person',
        'Edit Person',
        'Person Details',
        'Drivers',
        'Person License History',
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
    </Suspense>
  )
}
