import { useCallback, useMemo, useState } from 'react'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import ConfirmModal from '../common/ConfirmModal'
import { Button } from '../ui/button'
import type { TestAppointment } from '../../types/TestAppointment'
import { getAllAppointments, cancelTestAppointment } from '../../services/testAppointmentService'
import { resolveAppointmentLifecycle } from '../../utils/status'
import { formatJordanAppointmentDateTime, getJordanDateString } from '../../utils/jordanTime'
import { useAsyncData } from '../../hooks'
import {
  CalendarDays,
  Clock,
  CheckCircle2,
  Plus,
  Calendar,
} from 'lucide-react'
import { computeTabStats, type TabType, type TabStats } from './appointmentStats'
import AppointmentSummary from './AppointmentSummary'
import TestAppointmentsTable from './TestAppointmentsTable'

export interface TestAppointmentsPageProps {
  accessToken: string
  onNavigate?: (page: string) => void
  onShowAppointmentDetails?: (appointmentId: number) => void
  onScheduleAppointment?: (
    localAppId?: number | null,
    testTypeId?: 1 | 2 | 3,
    source?: string,
  ) => void
  onTakeExamination?: (appointmentId: number, returnPage?: string) => void
}

export default function TestAppointmentsPage({
  accessToken,
  onNavigate,
  onShowAppointmentDetails,
  onScheduleAppointment,
  onTakeExamination,
}: TestAppointmentsPageProps) {
  // View tabs: All, Today, History
  const [activeTab, setActiveTab] = useState<TabType>('all')

  // Search and filters
  const [searchQuery, setSearchQuery] = useState('')
  const [testTypeFilter, setTestTypeFilter] = useState<string>('all')
  const [statusFilter, setStatusFilter] = useState<string>('all')

  // Modals / Actions
  const [appointmentToCancel, setAppointmentToCancel] = useState<TestAppointment | null>(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)
  const [actionFeedback, setActionFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  // Asynchronous data fetcher using project standard hook
  const fetcher = useCallback((): Promise<TestAppointment[]> => {
    return getAllAppointments(accessToken)
  }, [accessToken])

  const {
    data: appointments = [],
    loading,
    error,
    refresh: fetchData,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Failed to retrieve test appointments from server.',
  })

  // Current Jordan facility date string (YYYY-MM-DD)
  const todayJordanStr = useMemo(() => getJordanDateString(new Date(), 0), [])

  // Derived collections based on the 3 tabs
  const todayAppointments = useMemo(() => {
    return appointments.filter((apt) => {
      const aptDateStr = (apt.appointmentDateFormatted || apt.appointmentDate || '').substring(0, 10)
      return aptDateStr === todayJordanStr
    })
  }, [appointments, todayJordanStr])

  const historyAppointments = useMemo(() => {
    return appointments
      .filter((apt) => {
        const lifecycle = resolveAppointmentLifecycle(apt)
        return lifecycle.isTerminal || apt.isLocked || apt.testResult >= 0
      })
      .sort((a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime())
  }, [appointments])

  // Context-aware KPI metrics computed strictly per active tab dataset
  const currentStats = useMemo<TabStats>(() => {
    switch (activeTab) {
      case 'today':
        return computeTabStats(todayAppointments)
      case 'history':
        return computeTabStats(historyAppointments)
      case 'all':
      default:
        return computeTabStats(appointments)
    }
  }, [activeTab, appointments, todayAppointments, historyAppointments])

  // Active base dataset according to selected tab
  const baseData = useMemo(() => {
    switch (activeTab) {
      case 'today':
        return todayAppointments
      case 'history':
        return historyAppointments
      case 'all':
      default:
        return appointments
    }
  }, [activeTab, appointments, todayAppointments, historyAppointments])

  // Apply search query and dropdown filters
  const filteredAppointments = useMemo(() => {
    const query = searchQuery.trim().toLowerCase()

    return baseData.filter((apt) => {
      const lifecycle = resolveAppointmentLifecycle(apt)

      // Test Type Filter
      if (testTypeFilter !== 'all' && String(apt.testTypeId) !== testTypeFilter) {
        return false
      }

      // Status Filter (includes granular passed/failed breakdown support)
      if (statusFilter !== 'all') {
        const rawState = lifecycle.state.toLowerCase()
        if (statusFilter === 'scheduled' && !rawState.includes('scheduled')) return false
        if (statusFilter === 'completed' && !rawState.includes('completed')) return false
        if (statusFilter === 'passed' && lifecycle.resultLabel !== 'Passed') return false
        if (statusFilter === 'failed' && lifecycle.resultLabel !== 'Failed') return false
        if (statusFilter === 'noshow' && !rawState.includes('noshow')) return false
        if (statusFilter === 'cancelled' && !rawState.includes('cancelled')) return false
      }

      // Query Search
      if (!query) return true

      const matchId = String(apt.testAppointmentId).includes(query)
      const matchAppId = String(apt.localDrivingLicenseApplicationId).includes(query)
      const matchTestType = apt.testTypeName.toLowerCase().includes(query)
      const matchStatus = lifecycle.statusLabel.toLowerCase().includes(query)
      const matchResult = lifecycle.resultLabel.toLowerCase().includes(query)
      const matchName = apt.applicantFullName?.toLowerCase().includes(query) ?? false
      const matchNationalNo = apt.nationalNo?.toLowerCase().includes(query) ?? false

      return matchId || matchAppId || matchTestType || matchStatus || matchResult || matchName || matchNationalNo
    })
  }, [baseData, searchQuery, testTypeFilter, statusFilter])

  // Interactive KPI card click handler
  const handleKpiClick = (targetStatus: string) => {
    setStatusFilter((prev) => (prev === targetStatus ? 'all' : targetStatus))
  }

  const handleCancelAppointment = useCallback((appointment: TestAppointment) => {
    setCancelError(null)
    setAppointmentToCancel(appointment)
  }, [])

  const handleConfirmCancel = useCallback(async () => {
    if (!appointmentToCancel) return
    setCancelling(true)
    setCancelError(null)
    try {
      await cancelTestAppointment(accessToken, appointmentToCancel.testAppointmentId)
      setActionFeedback({
        type: 'success',
        message: `Appointment #${appointmentToCancel.testAppointmentId} has been successfully cancelled and locked.`,
      })
      setAppointmentToCancel(null)
      await fetchData()
    } catch (err) {
      setCancelError(err instanceof Error ? err.message : 'Failed to cancel appointment.')
    } finally {
      setCancelling(false)
    }
  }, [accessToken, appointmentToCancel, fetchData])

  const handleResetFilters = useCallback(() => {
    setSearchQuery('')
    setTestTypeFilter('all')
    setStatusFilter('all')
  }, [])

  return (
    <section className="page-content">
      {/* 1. Main Page Header without action buttons */}
      <PageHero pageId="Test Appointments" />

      {/* 2. Context-Aware 5 KPI Cards */}
      <AppointmentSummary
        activeTab={activeTab}
        currentStats={currentStats}
        loading={loading}
        error={Boolean(error)}
        todayJordanStr={todayJordanStr}
        statusFilter={statusFilter}
        onKpiClick={handleKpiClick}
      />

      {/* 3. Standard Panel Surface */}
      <div className="panel">
        <div className="panel-header">
          <div>
            <h2>Test Appointments Directory</h2>
            <p>Monitor examination schedules, inspect lifecycle states, and review test records.</p>
          </div>

          <div className="flex items-center gap-3">
            <span className="record-count">
              {loading
                ? 'Loading...'
                : filteredAppointments.length === appointments.length
                  ? `${appointments.length} Appointments`
                  : `${filteredAppointments.length} of ${appointments.length} Appointments`}
            </span>

            {/* Book Test Appointment Action navigated to dedicated page */}
            <Button
              type="button"
              onClick={() => {
                if (onScheduleAppointment) {
                  onScheduleAppointment(null, 1, 'Test Appointments')
                } else {
                  onNavigate?.('Schedule Appointment')
                }
              }}
              className="bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-bold text-xs gap-1.5 shadow-[0_0_15px_rgba(34,211,238,0.35)] cursor-pointer h-9 px-4 rounded-xl"
            >
              <Plus className="size-3.5" />
              <span>Book Test Appointment</span>
            </Button>
          </div>
        </div>

        {/* Unified Views Navigation Tabs */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4 px-6 pt-4 pb-2 border-b border-[#1b3154]">
          <div className="flex items-center gap-2 overflow-x-auto">
            {/* Tab 1: All Appointments */}
            <button
              type="button"
              onClick={() => {
                setActiveTab('all')
                setStatusFilter('all')
              }}
              className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer ${
                activeTab === 'all'
                  ? 'bg-gradient-to-r from-blue-600 to-cyan-500 text-white shadow-[0_0_15px_rgba(34,211,238,0.3)] font-bold'
                  : 'bg-[#0f1d35] text-slate-300 hover:text-white hover:bg-[#142646] border border-[#1e3458]'
              }`}
            >
              <CalendarDays className="size-3.5" />
              <span>All Appointments</span>
              <span
                className={`px-2 py-0.5 rounded-full text-[11px] font-mono ${
                  activeTab === 'all' ? 'bg-black/30 text-white' : 'bg-[#1b2f52] text-cyan-300'
                }`}
              >
                {appointments.length}
              </span>
            </button>

            {/* Tab 2: Today's Appointments */}
            <button
              type="button"
              onClick={() => {
                setActiveTab('today')
                setStatusFilter('all')
              }}
              className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer ${
                activeTab === 'today'
                  ? 'bg-gradient-to-r from-blue-600 to-cyan-500 text-white shadow-[0_0_15px_rgba(34,211,238,0.3)] font-bold'
                  : 'bg-[#0f1d35] text-slate-300 hover:text-white hover:bg-[#142646] border border-[#1e3458]'
              }`}
            >
              <Calendar className="size-3.5" />
              <span>Today's Appointments</span>
              <span
                className={`px-2 py-0.5 rounded-full text-[11px] font-mono ${
                  activeTab === 'today' ? 'bg-black/30 text-white' : 'bg-[#1b2f52] text-cyan-300'
                }`}
              >
                {todayAppointments.length}
              </span>
            </button>

            {/* Tab 3: Appointment History */}
            <button
              type="button"
              onClick={() => {
                setActiveTab('history')
                setStatusFilter('all')
              }}
              className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer ${
                activeTab === 'history'
                  ? 'bg-gradient-to-r from-blue-600 to-cyan-500 text-white shadow-[0_0_15px_rgba(34,211,238,0.3)] font-bold'
                  : 'bg-[#0f1d35] text-slate-300 hover:text-white hover:bg-[#142646] border border-[#1e3458]'
              }`}
            >
              <Clock className="size-3.5" />
              <span>Appointment History</span>
              <span
                className={`px-2 py-0.5 rounded-full text-[11px] font-mono ${
                  activeTab === 'history' ? 'bg-black/30 text-white' : 'bg-[#1b2f52] text-cyan-300'
                }`}
              >
                {historyAppointments.length}
              </span>
            </button>
          </div>

          {/* View Indicator Label */}
          <div className="text-xs text-slate-400 font-sans hidden sm:block">
            {activeTab === 'today'
              ? `Filtered for Jordan Facility Date: ${todayJordanStr}`
              : activeTab === 'history'
                ? 'Past completed, no-show, and cancelled examination history'
                : 'All active and historical appointment records'}
          </div>
        </div>

        {actionFeedback && (
          <div style={{ margin: '0 25px 20px' }}>
            <div className="rounded-xl border border-emerald-500/30 bg-emerald-950/40 p-4 text-emerald-300 text-xs flex items-center justify-between shadow-sm">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="size-4 text-emerald-400 shrink-0" />
                <span>{actionFeedback.message}</span>
              </div>
              <button
                type="button"
                onClick={() => setActionFeedback(null)}
                className="text-slate-400 hover:text-white ml-2 text-xs font-mono cursor-pointer"
                aria-label="Dismiss message"
              >
                ✕
              </button>
            </div>
          </div>
        )}

        {error && (
          <div style={{ margin: '0 25px 20px' }}>
            <ErrorAlert message={error} onDismiss={() => void fetchData()} />
          </div>
        )}

        {/* 4. Appointments Table with Toolbar, Columns, and Actions */}
        <TestAppointmentsTable
          appointments={filteredAppointments}
          loading={loading}
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          testTypeFilter={testTypeFilter}
          onTestTypeFilterChange={setTestTypeFilter}
          statusFilter={statusFilter}
          onStatusFilterChange={setStatusFilter}
          onResetFilters={handleResetFilters}
          onRefresh={() => void fetchData()}
          activeTab={activeTab}
          onShowAppointmentDetails={onShowAppointmentDetails}
          onNavigate={onNavigate}
          onTakeExamination={onTakeExamination}
          onCancelAppointment={handleCancelAppointment}
        />
      </div>

      {/* 5. Cancellation Confirmation Dialog */}
      <ConfirmModal
        isOpen={Boolean(appointmentToCancel)}
        title="Cancel Test Appointment"
        message={
          cancelError
            ? `Cancellation Error: ${cancelError}`
            : appointmentToCancel
              ? `Are you sure you want to cancel Appointment #${appointmentToCancel.testAppointmentId} (${appointmentToCancel.testTypeName} on ${formatJordanAppointmentDateTime(appointmentToCancel.appointmentDate, appointmentToCancel.appointmentDateFormatted)})? This action will permanently cancel the appointment slot and lock it. Notice: Appointments can only be cancelled at least 2 calendar days prior to the scheduled date.`
              : ''
        }
        confirmText="Yes, Cancel Appointment"
        cancelText="Keep Appointment"
        confirmVariant="danger"
        loading={cancelling}
        onConfirm={handleConfirmCancel}
        onCancel={() => {
          if (!cancelling) {
            setAppointmentToCancel(null)
            setCancelError(null)
          }
        }}
      />
    </section>
  )
}
