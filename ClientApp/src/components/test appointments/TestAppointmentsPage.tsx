import { useCallback, useMemo, useState } from 'react'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import TableToolbar from '../common/TableToolbar'
import ErrorAlert from '../common/ErrorAlert'
import ConfirmModal from '../common/ConfirmModal'
import DataTable, { type DataTableColumn, type DataTableRowAction } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import { Button } from '../ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
import type { TestAppointment } from '../../types/TestAppointment'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import { getAllAppointments, cancelTestAppointment } from '../../services/testAppointmentService'
import { getLocalDrivingLicenseApplications } from '../../services/localDrivingLicenseApplicationService'
import { resolveAppointmentLifecycle } from '../../utils/status'
import { formatJordanAppointmentDateTime, getJordanDateString } from '../../utils/jordanTime'
import { useAsyncData } from '../../hooks'
import {
  CalendarDays,
  Clock,
  CheckCircle2,
  AlertTriangle,
  Plus,
  Eye,
  FileText,
  Calendar,
  XCircle,
  AlertCircle,
  Award,
} from 'lucide-react'

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

type TabType = 'all' | 'today' | 'history'

interface AppointmentsPageData {
  appointments: TestAppointment[]
  applications: LocalDrivingLicenseApplication[]
}

interface TabStats {
  total: number
  scheduled: number
  completed: number
  passed: number
  failed: number
  noShow: number
  cancelled: number
}

function computeTabStats(dataset: TestAppointment[]): TabStats {
  let scheduled = 0
  let completed = 0
  let passed = 0
  let failed = 0
  let noShow = 0
  let cancelled = 0

  dataset.forEach((apt) => {
    const lifecycle = resolveAppointmentLifecycle(apt)
    if (lifecycle.statusLabel === 'Cancelled') {
      cancelled++
    } else if (lifecycle.statusLabel === 'No-Show') {
      noShow++
    } else if (lifecycle.statusLabel === 'Completed') {
      completed++
      if (lifecycle.resultLabel === 'Passed') passed++
      else if (lifecycle.resultLabel === 'Failed') failed++
    } else if (lifecycle.statusLabel === 'Scheduled') {
      scheduled++
    }
  })

  return {
    total: dataset.length,
    scheduled,
    completed,
    passed,
    failed,
    noShow,
    cancelled,
  }
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
  const fetcher = useCallback(async (): Promise<AppointmentsPageData> => {
    const [apptData, appData] = await Promise.all([
      getAllAppointments(accessToken),
      getLocalDrivingLicenseApplications(accessToken, 1, 500)
        .then((res) => res.items)
        .catch(() => []),
    ])
    return { appointments: apptData, applications: appData }
  }, [accessToken])

  const {
    data,
    loading,
    error,
    refresh: fetchData,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: { appointments: [], applications: [] },
    defaultErrorMessage: 'Failed to retrieve test appointments from server.',
  })

  const appointments = data.appointments
  const applications = data.applications

  // Lookup map: LocalDrivingLicenseApplicationId -> LocalDrivingLicenseApplication
  const applicationMap = useMemo(() => {
    const map = new Map<number, LocalDrivingLicenseApplication>()
    applications.forEach((app) => {
      map.set(app.localDrivingLicenseApplicationId, app)
    })
    return map
  }, [applications])

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
      const app = applicationMap.get(apt.localDrivingLicenseApplicationId)
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
      const matchName = app?.fullName?.toLowerCase().includes(query) ?? false
      const matchNationalNo = app?.nationalNo?.toLowerCase().includes(query) ?? false

      return matchId || matchAppId || matchTestType || matchStatus || matchResult || matchName || matchNationalNo
    })
  }, [baseData, searchQuery, testTypeFilter, statusFilter, applicationMap])

  // Interactive KPI card click handler
  const handleKpiClick = (targetStatus: string) => {
    setStatusFilter((prev) => (prev === targetStatus ? 'all' : targetStatus))
  }

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

  // Data Table Columns
  const columns = useMemo<DataTableColumn<TestAppointment>[]>(() => [
    {
      key: 'testAppointmentId',
      header: 'Appt ID',
      sortable: true,
      sortValue: (r) => r.testAppointmentId,
      render: (r) => (
        <div className="flex items-center gap-1.5 font-mono">
          <span className="font-bold text-cyan-300">#{r.testAppointmentId}</span>
          {Boolean(r.retakeTestApplicationId) && (
            <span
              className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30"
              title={`Retake Examination for Application #${r.retakeTestApplicationId}`}
            >
              Retake
            </span>
          )}
        </div>
      ),
    },
    {
      key: 'appointmentDate',
      header: 'Appointment Date & Slot',
      sortable: true,
      sortValue: (r) => new Date(r.appointmentDate).getTime(),
      render: (r) => (
        <div className="space-y-0.5">
          <div className="font-medium text-white flex items-center gap-1.5">
            <Clock className="size-3 text-cyan-400 shrink-0" />
            <span>
              {formatJordanAppointmentDateTime(r.appointmentDate, r.appointmentDateFormatted)}
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-sans">Asia/Amman facility time</div>
        </div>
      ),
    },
    {
      key: 'applicant',
      header: 'Applicant / Candidate',
      render: (r) => {
        const app = applicationMap.get(r.localDrivingLicenseApplicationId)
        return (
          <div className="space-y-0.5">
            <div className="font-semibold text-slate-200">
              {app?.fullName ?? `Applicant (App #${r.localDrivingLicenseApplicationId})`}
            </div>
            {app?.nationalNo && (
              <div className="text-[10px] font-mono text-cyan-300/80">
                Nat ID: {app.nationalNo}
              </div>
            )}
          </div>
        )
      },
    },
    {
      key: 'localApplication',
      header: 'Local Application',
      render: (r) => {
        const app = applicationMap.get(r.localDrivingLicenseApplicationId)
        return (
          <div className="space-y-0.5">
            <div className="font-mono text-xs text-white">
              App #{r.localDrivingLicenseApplicationId}
            </div>
            <div className="text-[10px] text-slate-400 truncate max-w-[160px]">
              {app?.licenseClassName ?? 'Driving License'}
            </div>
          </div>
        )
      },
    },
    {
      key: 'testTypeName',
      header: 'Test Type',
      sortable: true,
      sortValue: (r) => r.testTypeName,
      render: (r) => (
        <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-semibold bg-slate-900/80 border border-slate-700/60 text-slate-200">
          <Award className="size-3.5 text-cyan-400" />
          <span>{r.testTypeName}</span>
        </div>
      ),
    },
    {
      key: 'lifecycleStatus',
      header: 'Lifecycle Status',
      render: (r) => {
        const lifecycle = resolveAppointmentLifecycle(r)
        return (
          <StatusBadge
            variant={lifecycle.statusVariant}
            label={lifecycle.statusLabel}
            title={lifecycle.description}
          />
        )
      },
    },
    {
      key: 'result',
      header: 'Exam Result',
      render: (r) => {
        const lifecycle = resolveAppointmentLifecycle(r)
        if (lifecycle.resultLabel === 'Passed') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
              <CheckCircle2 className="size-3" />
              <span>Passed</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'Failed') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">
              <XCircle className="size-3" />
              <span>Failed</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'No-Show') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-500/10 text-amber-300 border border-amber-500/20">
              <AlertCircle className="size-3" />
              <span>No-Show</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'Cancelled') {
          return (
            <span className="inline-flex items-center gap-1 text-[11px] text-slate-400 font-medium">
              <XCircle className="size-3 text-slate-500" />
              <span>Cancelled</span>
            </span>
          )
        }
        return <span className="text-slate-400 font-medium text-[11px]">Not Taken</span>
      },
    },
    {
      key: 'paidFees',
      header: 'Applicable Fee',
      sortable: true,
      sortValue: (r) => r.paidFees,
      render: (r) => (
        <span className="font-mono text-xs font-semibold text-emerald-400">
          {r.paidFees.toFixed(2)} JOD
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-1.5">
          {!r.isLocked && (r.status === 'Scheduled' || r.status === 'Open') && (
            <button
              type="button"
              onClick={() => {
                setCancelError(null)
                setAppointmentToCancel(r)
              }}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-medium text-rose-300 bg-rose-950/40 hover:bg-rose-900/60 border border-rose-500/30 hover:border-rose-400 transition-all cursor-pointer shadow-sm"
              title="Cancel scheduled appointment"
            >
              <XCircle className="size-3.5" />
              <span>Cancel</span>
            </button>
          )}
          <button
            type="button"
            onClick={() => {
              if (onShowAppointmentDetails) {
                onShowAppointmentDetails(r.testAppointmentId)
              } else {
                onNavigate?.('Appointment Details')
              }
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium text-cyan-300 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-500/30 hover:border-cyan-400 transition-all cursor-pointer shadow-sm"
            title="View comprehensive examination record and audit details"
          >
            <Eye className="size-3.5" />
            <span>Details</span>
          </button>
        </div>
      ),
    },
  ], [applicationMap, onNavigate, onShowAppointmentDetails])

  // Context Menu Actions (Right-Click)
  const rowContextMenu = useCallback(
    (apt: TestAppointment): DataTableRowAction<TestAppointment>[] => {
      const actions: DataTableRowAction<TestAppointment>[] = [
        {
          key: 'details',
          label: 'View Appointment Details',
          icon: <Eye className="size-4" />,
          onClick: () => {
            if (onShowAppointmentDetails) {
              onShowAppointmentDetails(apt.testAppointmentId)
            } else {
              onNavigate?.('Appointment Details')
            }
          },
        },
      ]

      if (!apt.isLocked && (apt.status === 'Scheduled' || apt.status === 'Open')) {
        actions.push({
          key: 'take-test',
          label: 'Take Examination',
          icon: <Award className="size-4 text-emerald-400" />,
          onClick: () => {
            if (onTakeExamination) {
              onTakeExamination(apt.testAppointmentId, 'Test Appointments')
            } else {
              onNavigate?.('Take Examination')
            }
          },
        })
        actions.push({
          key: 'cancel',
          label: 'Cancel Appointment',
          icon: <XCircle className="size-4 text-rose-400" />,
          onClick: () => {
            setCancelError(null)
            setAppointmentToCancel(apt)
          },
        })
      }

      if (onNavigate) {
        actions.push({
          key: 'goto-app',
          label: 'Go to Local Application',
          icon: <FileText className="size-4" />,
          onClick: () => onNavigate('Local Applications'),
        })
      }

      return actions
    },
    [onNavigate, onShowAppointmentDetails, onTakeExamination],
  )

  return (
    <section className="page-content">
      {/* 1. Main Page Header without action buttons */}
      <PageHero pageId="Test Appointments" />

      {/* 4. Context-Aware 5 KPI Cards */}
      <StatsGrid columns={5} className="lg:grid-cols-5">
        {activeTab === 'history' ? (
          <>
            {/* 1. Historical Total */}
            <StatCard
              label="Historical Total"
              value={loading ? '...' : error ? '—' : currentStats.total}
              icon={<Clock className="size-5" />}
              variant="blue"
              description="Concluded records"
              onClick={() => handleKpiClick('all')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'all'
                  ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 2. Passed */}
            <StatCard
              label="Passed Tests"
              value={loading ? '...' : error ? '—' : currentStats.passed}
              icon={<CheckCircle2 className="size-5" />}
              variant="emerald"
              description="Successful evaluations"
              onClick={() => handleKpiClick('passed')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'passed'
                  ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 3. Failed */}
            <StatCard
              label="Failed Tests"
              value={loading ? '...' : error ? '—' : currentStats.failed}
              icon={<XCircle className="size-5" />}
              variant="rose"
              description="Unsuccessful evaluations"
              onClick={() => handleKpiClick('failed')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'failed'
                  ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 4. No-Show */}
            <StatCard
              label="No-Show Tests"
              value={loading ? '...' : error ? '—' : currentStats.noShow}
              icon={<AlertCircle className="size-5" />}
              variant="amber"
              description="Applicant did not attend"
              onClick={() => handleKpiClick('noshow')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'noshow'
                  ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 5. Cancelled */}
            <StatCard
              label="Cancelled Records"
              value={loading ? '...' : error ? '—' : currentStats.cancelled}
              icon={<AlertTriangle className="size-5" />}
              variant="slate"
              description="Soft-cancelled prior to test"
              onClick={() => handleKpiClick('cancelled')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'cancelled'
                  ? 'ring-2 ring-slate-400 shadow-[0_0_12px_rgba(148,163,184,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
          </>
        ) : activeTab === 'today' ? (
          <>
            {/* 1. Today's Total */}
            <StatCard
              label="Today's Total"
              value={loading ? '...' : error ? '—' : currentStats.total}
              icon={<Calendar className="size-5" />}
              variant="blue"
              description={`Jordan facility: ${todayJordanStr}`}
              onClick={() => handleKpiClick('all')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'all'
                  ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 2. Scheduled Today */}
            <StatCard
              label="Scheduled Today"
              value={loading ? '...' : error ? '—' : currentStats.scheduled}
              icon={<Clock className="size-5" />}
              variant="purple"
              description="Awaiting examination today"
              onClick={() => handleKpiClick('scheduled')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'scheduled'
                  ? 'ring-2 ring-purple-400 shadow-[0_0_12px_rgba(168,85,247,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 3. Completed Today */}
            <StatCard
              label="Completed Today"
              value={loading ? '...' : error ? '—' : currentStats.completed}
              icon={<CheckCircle2 className="size-5" />}
              variant="emerald"
              description={`${currentStats.passed} Passed • ${currentStats.failed} Failed`}
              onClick={() => handleKpiClick('completed')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'completed'
                  ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 4. No-Show Today */}
            <StatCard
              label="No-Show Today"
              value={loading ? '...' : error ? '—' : currentStats.noShow}
              icon={<AlertCircle className="size-5" />}
              variant="amber"
              description="Missed slot today"
              onClick={() => handleKpiClick('noshow')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'noshow'
                  ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 5. Cancelled Today */}
            <StatCard
              label="Cancelled Today"
              value={loading ? '...' : error ? '—' : currentStats.cancelled}
              icon={<AlertTriangle className="size-5" />}
              variant="rose"
              description="Cancelled today"
              onClick={() => handleKpiClick('cancelled')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'cancelled'
                  ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
          </>
        ) : (
          <>
            {/* 1. Total Appointments */}
            <StatCard
              label="Total Appointments"
              value={loading ? '...' : error ? '—' : currentStats.total}
              icon={<CalendarDays className="size-5" />}
              variant="blue"
              description="All recorded appointments"
              onClick={() => handleKpiClick('all')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'all'
                  ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 2. Scheduled */}
            <StatCard
              label="Scheduled"
              value={loading ? '...' : error ? '—' : currentStats.scheduled}
              icon={<Clock className="size-5" />}
              variant="purple"
              description="Awaiting examination"
              onClick={() => handleKpiClick('scheduled')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'scheduled'
                  ? 'ring-2 ring-purple-400 shadow-[0_0_12px_rgba(168,85,247,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 3. Completed */}
            <StatCard
              label="Completed"
              value={loading ? '...' : error ? '—' : currentStats.completed}
              icon={<CheckCircle2 className="size-5" />}
              variant="emerald"
              description={`${currentStats.passed} Passed • ${currentStats.failed} Failed`}
              onClick={() => handleKpiClick('completed')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'completed'
                  ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 4. No-Show */}
            <StatCard
              label="No-Show"
              value={loading ? '...' : error ? '—' : currentStats.noShow}
              icon={<AlertCircle className="size-5" />}
              variant="amber"
              description="Applicant did not attend"
              onClick={() => handleKpiClick('noshow')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'noshow'
                  ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
            {/* 5. Cancelled */}
            <StatCard
              label="Cancelled"
              value={loading ? '...' : error ? '—' : currentStats.cancelled}
              icon={<AlertTriangle className="size-5" />}
              variant="rose"
              description="Cancelled appointments"
              onClick={() => handleKpiClick('cancelled')}
              className={`cursor-pointer transition-all ${
                statusFilter === 'cancelled'
                  ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                  : 'hover:border-slate-700'
              }`}
            />
          </>
        )}
      </StatsGrid>

      {/* Standard Panel Surface */}
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

        {/* 2. TableToolbar with EXACT order:
            1. Test Type filter (on the LEFT of search)
            2. Search input
            3. Appointment Status filter
            4. Refresh action */}
        <TableToolbar
          searchField={testTypeFilter}
          searchFieldOptions={[
            { value: 'all', label: 'All Test Types' },
            { value: '1', label: 'Vision Test' },
            { value: '2', label: 'Written Test' },
            { value: '3', label: 'Practical (Street) Test' },
          ]}
          onSearchFieldChange={setTestTypeFilter}
          searchFieldMinWidth={170}
          searchFieldAriaLabel="Filter by test type"
          searchValue={searchQuery}
          onSearchChange={setSearchQuery}
          searchPlaceholder="Search by ID, candidate name, National ID..."
          searchAriaLabel="Search test appointments"
          filters={
            <div style={{ minWidth: 170 }} className="shrink-0">
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger
                  className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg cursor-pointer"
                  aria-label="Filter by appointment status"
                >
                  <SelectValue placeholder="All Statuses" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Statuses</SelectItem>
                  <SelectItem value="scheduled">Scheduled (Active)</SelectItem>
                  <SelectItem value="completed">Completed</SelectItem>
                  <SelectItem value="passed">Completed — Passed</SelectItem>
                  <SelectItem value="failed">Completed — Failed</SelectItem>
                  <SelectItem value="noshow">No-Show</SelectItem>
                  <SelectItem value="cancelled">Cancelled</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
          isFiltered={Boolean(searchQuery || testTypeFilter !== 'all' || statusFilter !== 'all')}
          onReset={() => {
            setSearchQuery('')
            setTestTypeFilter('all')
            setStatusFilter('all')
          }}
          onRefresh={() => void fetchData()}
          refreshing={loading}
        />

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

        {/* Main Appointments Table */}
        <DataTable
          data={filteredAppointments}
          columns={columns}
          getRowKey={(r) => r.testAppointmentId}
          loading={loading}
          loadingMessage="Loading test appointments from DVLD database..."
          emptyMessage={
            searchQuery || testTypeFilter !== 'all' || statusFilter !== 'all'
              ? 'No appointment records matched your search and filter criteria.'
              : activeTab === 'today'
                ? 'No test appointments scheduled for today in Jordan facility time.'
                : activeTab === 'history'
                  ? 'No past examination history records found.'
                  : 'No test appointments recorded in the system yet.'
          }
          emptyIcon={<CalendarDays className="size-8 text-slate-600" />}
          minWidth={1000}
          rowContextMenu={rowContextMenu}
          onRowClick={(r) => {
            if (onShowAppointmentDetails) {
              onShowAppointmentDetails(r.testAppointmentId)
            } else {
              onNavigate?.('Appointment Details')
            }
          }}
          ariaLabel="Driving test appointments table"
        />
      </div>


      {/* Cancellation Confirmation Dialog */}
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
