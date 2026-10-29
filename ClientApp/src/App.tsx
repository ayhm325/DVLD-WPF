import { useCallback, useState } from 'react'

import './App.css'

import Sidebar from './components/Sidebar'
import Topbar from './components/Topbar'
import Login from './components/Login'
import AddPersonModal from './components/AddPersonModal'
import ConfirmModal from './components/common/ConfirmModal'
import LoadingSpinner from './components/common/LoadingSpinner'
import AppRouter from './components/AppRouter'

import { useAuth } from './context/useAuth'
import { usePeople } from './hooks/usePeople'
import { getPeople } from './services/peopleService'

export default function App() {
  const { accessToken, user, isAuthenticated, isLoading, logout } = useAuth()
  const [activePage, setActivePage] = useState('Dashboard')
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false)
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false)

  // International license navigation state
  const [showIssueInternational, setShowIssueInternational] = useState(false)
  const [internationalRefreshKey, setInternationalRefreshKey] = useState(0)
  const [statisticsRefreshKey, setStatisticsRefreshKey] = useState(0)

  // Add person modal
  const [isAddPersonOpen, setIsAddPersonOpen] = useState(false)

  // People data lifecycle managed through dedicated custom hook
  const {
    people,
    filteredPeople,
    searchName,
    setSearchName,
    loadingPeople,
    peopleError,
    refreshPeople,
    setPeople,
    setPeopleError,
  } = usePeople(accessToken, user?.role, activePage)

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
    }
  }, [])

  const handleConfirmLogout = useCallback(() => {
    setIsLogoutModalOpen(false)
    setIsMobileSidebarOpen(false)
    setActivePage('Dashboard')
    logout('You have successfully signed out of DVLD.')
  }, [logout])

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

        <AppRouter
          activePage={activePage}
          accessToken={accessToken}
          userRole={user?.role}
          onNavigate={handleNavigate}
          showIssueInternational={showIssueInternational}
          setShowIssueInternational={setShowIssueInternational}
          internationalRefreshKey={internationalRefreshKey}
          onInternationalIssued={handleInternationalIssued}
          statisticsRefreshKey={statisticsRefreshKey}
          people={people}
          filteredPeople={filteredPeople}
          searchName={searchName}
          onSearchChange={setSearchName}
          loadingPeople={loadingPeople}
          peopleError={peopleError}
          onRetryPeople={refreshPeople}
          onAddPerson={() => setIsAddPersonOpen(true)}
        />

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
                refreshPeople()
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
