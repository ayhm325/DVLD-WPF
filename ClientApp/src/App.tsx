import { useCallback, useState, useEffect } from 'react'
import { Code } from 'lucide-react'

import './App.css'

import Sidebar from './components/Sidebar'
import Topbar from './components/Topbar'
import Login from './components/Login'
import AddPersonModal from './components/AddPersonModal'
import ConfirmModal from './components/common/ConfirmModal'
import LoadingSpinner from './components/common/LoadingSpinner'
import CommandPalette from './components/common/CommandPalette'
import AppRouter from './components/AppRouter'

import { useAuth } from './context/useAuth'
import { usePeople, useModal } from './hooks'
import { getPeople } from './services/peopleService'

export default function App() {
  const { accessToken, user, isAuthenticated, isLoading, logout } = useAuth()
  const [activePage, setActivePage] = useState('Dashboard')

  const mobileSidebar = useModal()
  const logoutModal = useModal()
  const commandPalette = useModal()
  const { open: openCommandPalette } = commandPalette
  const addPersonModal = useModal()

  // Listen for Ctrl+K / Cmd+K global shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Avoid triggering when composing text with an IME
      if (e.isComposing || e.keyCode === 229) return

      const isModifier = e.ctrlKey || e.metaKey
      const isKKey = e.key.toLowerCase() === 'k' || e.code === 'KeyK'

      if (isModifier && !e.altKey && !e.shiftKey && isKKey) {
        // Do not activate while typing in inputs, textareas, or contenteditable elements
        const target = e.target as HTMLElement | null
        const isEditable = target && (
          target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.isContentEditable
        )
        if (isEditable) return

        e.preventDefault()
        e.stopPropagation()
        openCommandPalette()
      }
    }

    window.addEventListener('keydown', handleKeyDown, true)
    return () => window.removeEventListener('keydown', handleKeyDown, true)
  }, [openCommandPalette])

  // International license navigation state
  const [showIssueInternational, setShowIssueInternational] = useState(false)
  const [internationalRefreshKey, setInternationalRefreshKey] = useState(0)
  const [statisticsRefreshKey, setStatisticsRefreshKey] = useState(0)

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
    mobileSidebar.close()

    if (page === 'Dashboard') {
      setStatisticsRefreshKey((current) => current + 1)
    }
  }, [mobileSidebar])

  const handleConfirmLogout = useCallback(() => {
    logoutModal.close()
    mobileSidebar.close()
    setActivePage('Dashboard')
    logout('You have successfully signed out of DVLD.')
  }, [logout, logoutModal, mobileSidebar])

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
        isOpen={mobileSidebar.isOpen}
        onCloseMobile={mobileSidebar.close}
        onRequestLogout={logoutModal.open}
      />

      <main className="main-content">
        <Topbar
          activePage={
            activePage === 'International Licenses' && showIssueInternational
              ? 'Issue International License'
              : activePage
          }
          onRequestLogout={logoutModal.open}
          onOpenCommandPalette={commandPalette.open}
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
          onAddPerson={addPersonModal.open}
        />

        {/* Add Person Modal */}
        {addPersonModal.isOpen && (
          <AddPersonModal
            accessToken={accessToken}
            onClose={addPersonModal.close}
            onPersonAdded={async () => {
              try {
                const updatedPeople = await getPeople(accessToken)

                setPeople(updatedPeople)
                addPersonModal.close()
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

                addPersonModal.close()
              }
            }}
          />
        )}

        {/* Logout Confirmation Dialog */}
        <ConfirmModal
          isOpen={logoutModal.isOpen}
          title="Sign Out of DVLD"
          message="Are you sure you want to sign out? Your active session token will be cleared and you will be returned to the sign-in screen."
          confirmText="Sign Out"
          cancelText="Stay Signed In"
          confirmVariant="danger"
          onConfirm={handleConfirmLogout}
          onCancel={logoutModal.close}
        />

        {/* Global Dark Command Navigation Palette (Ctrl+K) */}
        <CommandPalette
          isOpen={commandPalette.isOpen}
          onClose={commandPalette.close}
          onNavigate={handleNavigate}
          userRole={user?.role}
        />

        <footer className="app-footer relative overflow-hidden flex items-center justify-between gap-4 px-6 sm:px-8 py-4 bg-gradient-to-r from-[#070e1c] via-[#0a1832] to-[#0d1e3d] border-t border-blue-500/40 shadow-[0_-4px_20px_rgba(6,16,38,0.5),inset_0_1px_0_rgba(96,165,250,0.25)]">
          {/* Layered flowing blue and cyan decorative waves */}
          <svg
            className="pointer-events-none absolute inset-0 h-full w-full select-none opacity-75 z-0"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 1200 60"
            preserveAspectRatio="none"
            aria-hidden="true"
          >
            <defs>
              <linearGradient id="footer-wave-grad-1" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#3b82f6" stopOpacity="0" />
                <stop offset="25%" stopColor="#38bdf8" stopOpacity="0.6" />
                <stop offset="65%" stopColor="#60a5fa" stopOpacity="0.7" />
                <stop offset="100%" stopColor="#818cf8" stopOpacity="0" />
              </linearGradient>
              <linearGradient id="footer-wave-grad-2" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#06b6d4" stopOpacity="0" />
                <stop offset="45%" stopColor="#22d3ee" stopOpacity="0.5" />
                <stop offset="85%" stopColor="#3b82f6" stopOpacity="0.6" />
                <stop offset="100%" stopColor="#38bdf8" stopOpacity="0" />
              </linearGradient>
              <filter id="footer-wave-glow" x="-10%" y="-10%" width="120%" height="120%">
                <feGaussianBlur stdDeviation="3.5" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>
            <path
              d="M 0 45 Q 300 15 600 35 T 1200 20"
              fill="none"
              stroke="url(#footer-wave-grad-1)"
              strokeWidth="2.5"
              filter="url(#footer-wave-glow)"
            />
            <path
              d="M 0 30 Q 350 50 700 25 T 1200 40"
              fill="none"
              stroke="url(#footer-wave-grad-2)"
              strokeWidth="1.8"
              opacity="0.8"
            />
            <circle cx="350" cy="25" r="2.5" fill="#38bdf8" filter="url(#footer-wave-glow)" />
            <circle cx="750" cy="35" r="2" fill="#22d3ee" filter="url(#footer-wave-glow)" />
            <circle cx="150" cy="40" r="1.5" fill="#60a5fa" filter="url(#footer-wave-glow)" />
            <circle cx="950" cy="28" r="1.5" fill="#38bdf8" filter="url(#footer-wave-glow)" />
          </svg>

          <div className="relative z-10 flex items-center gap-2">
            <span className="size-2 rounded-full bg-cyan-400 shadow-[0_0_8px_#22d3ee] mr-1" aria-hidden="true" />
            <span className="font-bold tracking-tight text-white text-xs sm:text-sm">
              <span className="bg-gradient-to-r from-blue-400 via-sky-300 to-cyan-300 bg-clip-text text-transparent drop-shadow-[0_0_12px_rgba(56,189,248,0.4)]">
                DVLD
              </span>{' '}
              <span className="text-slate-300 font-medium">Management System</span>
            </span>
          </div>

          <div className="relative z-10 flex items-center gap-3">
            <span className="footer-badge inline-flex items-center gap-1.5 px-3 py-1 rounded-full border border-blue-400/50 bg-[#081834]/90 text-blue-200 text-[10px] font-bold tracking-wider uppercase shadow-[0_0_16px_rgba(59,130,246,0.35)]">
              <Code className="size-3.5 text-cyan-400" aria-hidden="true" />
              FRONTEND PREVIEW
            </span>
          </div>
        </footer>
      </main>
    </div>
  )
}
