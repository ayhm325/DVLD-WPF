import { useCallback, useMemo, useState } from 'react'
import {
  ArrowLeft,
  CalendarClock,
  Clock,
  Eye,
  FileSignature,
  Car,
  CheckCircle2,
  XCircle,
  PlusCircle,
  AlertCircle,
  FileText,
  Award,
  ChevronDown,
  ChevronRight,
  ShieldCheck,
  CalendarDays,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import DataTable, { type DataTableColumn, type DataTableRowAction } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
import ConfirmModal from '../common/ConfirmModal'
import EmptyState from '../common/EmptyState'
import { Button } from '../ui/button'
import { Card, CardHeader, CardTitle, CardDescription } from '../ui/card'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type {
  TestAppointment,
  TestResponse,
} from '../../types/TestAppointment'
import {
  getAppointmentsByLocalAppId,
  cancelTestAppointment,
  getTestByAppointmentId,
} from '../../services/testAppointmentService'
import { getLocalDrivingLicenseApplicationById } from '../../services/localDrivingLicenseApplicationService'
import { formatJordanAppointmentDateTime } from '../../utils/jordanTime'
import { isAppointmentActive, resolveAppointmentLifecycle } from '../../utils/status'
import { useAsyncData } from '../../hooks'
import { cn } from '@/lib/utils'

export interface ManageTestAppointmentsPageProps {
  accessToken: string
  localAppId: number | null
  onBack: () => void
  onNavigate?: (page: string) => void
  onScheduleAppointment?: (
    localAppId: number,
    testTypeId: 1 | 2 | 3,
    returnPage?: string,
  ) => void
  onShowAppointmentDetails?: (appointmentId: number, returnPage?: string) => void
  onTakeExamination?: (appointmentId: number, returnPage?: string) => void
}

const TEST_TYPE_META: Record<
  number,
  {
    name: string
    shortName: string
    icon: typeof Eye
    color: string
    bgColor: string
    borderColor: string
    badgeBg: string
    badgeText: string
    badgeBorder: string
  }
> = {
  1: {
    name: 'Vision Test',
    shortName: 'Vision',
    icon: Eye,
    color: 'text-cyan-400',
    bgColor: 'bg-cyan-500/10',
    borderColor: 'border-cyan-500/30',
    badgeBg: 'bg-cyan-950/50',
    badgeText: 'text-cyan-300',
    badgeBorder: 'border-cyan-500/30',
  },
  2: {
    name: 'Written (Theory) Test',
    shortName: 'Written',
    icon: FileSignature,
    color: 'text-indigo-400',
    bgColor: 'bg-indigo-500/10',
    borderColor: 'border-indigo-500/30',
    badgeBg: 'bg-indigo-950/50',
    badgeText: 'text-indigo-300',
    badgeBorder: 'border-indigo-500/30',
  },
  3: {
    name: 'Practical (Street) Test',
    shortName: 'Street',
    icon: Car,
    color: 'text-emerald-400',
    bgColor: 'bg-emerald-500/10',
    borderColor: 'border-emerald-500/30',
    badgeBg: 'bg-emerald-950/50',
    badgeText: 'text-emerald-300',
    badgeBorder: 'border-emerald-500/30',
  },
}

interface ApplicationAppointmentsData {
  application: LocalDrivingLicenseApplication | null
  appointments: TestAppointment[]
}

export default function ManageTestAppointmentsPage({
  accessToken,
  localAppId,
  onBack,
  onNavigate,
  onScheduleAppointment,
  onShowAppointmentDetails,
  onTakeExamination,
}: ManageTestAppointmentsPageProps) {
  const [actionFeedback, setActionFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  // Expandable test details (examiner observations)
  const [expandedAppointmentId, setExpandedAppointmentId] = useState<number | null>(null)
  const [testDetailsMap, setTestDetailsMap] = useState<Record<number, TestResponse | null>>({})
  const [loadingTestId, setLoadingTestId] = useState<number | null>(null)

  // Cancellation modal
  const [appointmentToCancel, setAppointmentToCancel] = useState<TestAppointment | null>(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)

  // Data fetcher for application context and ALL test appointments
  const fetcher = useCallback(async (): Promise<ApplicationAppointmentsData> => {
    if (!localAppId || localAppId <= 0) {
      throw new Error('A valid local driving license application ID is required.')
    }

    const [appData, allAppts] = await Promise.all([
      getLocalDrivingLicenseApplicationById(accessToken, localAppId),
      getAppointmentsByLocalAppId(accessToken, localAppId),
    ])

    return {
      application: appData,
      appointments: allAppts,
    }
  }, [accessToken, localAppId])

  const {
    data,
    loading,
    error: loadError,
    setError: setLoadError,
    refresh: loadData,
  } = useAsyncData(fetcher, {
    enabled: Boolean(localAppId && localAppId > 0),
    initialData: { application: null, appointments: [] },
    defaultErrorMessage: `Failed to retrieve appointment records for application #${localAppId ?? ''}.`,
  })

  const application = data.application
  const rawAppointments = data.appointments
  const error = cancelError || loadError

  // Chronological sorting: newest appointment first, deterministically tie-broken by testAppointmentId
  const appointments = useMemo(() => {
    return [...rawAppointments].sort((a, b) => {
      const timeA = new Date(a.appointmentDate).getTime()
      const timeB = new Date(b.appointmentDate).getTime()
      if (!Number.isNaN(timeA) && !Number.isNaN(timeB) && timeA !== timeB) {
        return timeB - timeA // Descending: newest first
      }
      return b.testAppointmentId - a.testAppointmentId
    })
  }, [rawAppointments])

  // Active appointment across any stage
  const activeAppointment = useMemo(() => {
    return appointments.find(isAppointmentActive) ?? null
  }, [appointments])

  // Next eligible test stage
  const nextStage: 1 | 2 | 3 = useMemo(() => {
    if (!application) return 1
    if (application.passedTest === 0) return 1
    if (application.passedTest === 1) return 2
    return 3
  }, [application])

  const allStagesPassed = Boolean(application && application.passedTest >= 3)

  // Expand test details toggle
  const handleToggleTestDetails = useCallback(
    (appointmentId: number) => {
      setExpandedAppointmentId((prev) => {
        if (prev === appointmentId) return null

        if (testDetailsMap[appointmentId] === undefined) {
          setLoadingTestId(appointmentId)
          getTestByAppointmentId(accessToken, appointmentId)
            .then((tests: TestResponse[]) => {
              setTestDetailsMap((prevMap) => ({
                ...prevMap,
                [appointmentId]: tests && tests.length > 0 ? tests[0] : null,
              }))
            })
            .catch(() => {
              setTestDetailsMap((prevMap) => ({
                ...prevMap,
                [appointmentId]: null,
              }))
            })
            .finally(() => {
              setLoadingTestId(null)
            })
        }

        return appointmentId
      })
    },
    [accessToken, testDetailsMap],
  )

  // Cancel appointment confirmation
  const handleConfirmCancel = async () => {
    if (!appointmentToCancel) return

    setCancelling(true)
    setCancelError(null)

    try {
      await cancelTestAppointment(accessToken, appointmentToCancel.testAppointmentId)
      setActionFeedback({
        type: 'success',
        message: `Appointment #${appointmentToCancel.testAppointmentId} for ${appointmentToCancel.testTypeName} has been successfully cancelled.`,
      })
      setAppointmentToCancel(null)
      await loadData()
    } catch (err) {
      setCancelError(err instanceof Error ? err.message : 'Failed to cancel appointment.')
    } finally {
      setCancelling(false)
    }
  }

  // Handle scheduling action
  const handleScheduleNextTest = () => {
    if (!application) return
    if (onScheduleAppointment) {
      onScheduleAppointment(
        application.localDrivingLicenseApplicationId,
        nextStage,
        'Manage Test Appointments',
      )
    } else {
      onNavigate?.('Schedule Appointment')
    }
  }

  // Table Columns
  const columns = useMemo<DataTableColumn<TestAppointment>[]>(() => [
    {
      key: 'testAppointmentId',
      header: 'ID',
      sortable: true,
      sortValue: (r) => r.testAppointmentId,
      render: (r) => (
        <span className="font-mono text-xs font-bold text-cyan-400">
          #{r.testAppointmentId}
        </span>
      ),
    },
    {
      key: 'testTypeName',
      header: 'Examination Stage',
      sortable: true,
      sortValue: (r) => r.testTypeId,
      render: (r) => {
        const meta = TEST_TYPE_META[r.testTypeId] || {
          name: r.testTypeName || `Stage #${r.testTypeId}`,
          shortName: r.testTypeName || 'Exam',
          icon: Award,
          color: 'text-slate-300',
          badgeBg: 'bg-slate-900/60',
          badgeText: 'text-slate-300',
          badgeBorder: 'border-slate-700/60',
        }
        const Icon = meta.icon
        return (
          <div className="flex items-center gap-2">
            <span
              className={cn(
                'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg border text-xs font-semibold shadow-xs',
                meta.badgeBg,
                meta.badgeText,
                meta.badgeBorder,
              )}
            >
              <Icon className="size-3.5 shrink-0" />
              <span>{meta.name}</span>
            </span>
            {r.retakeTestApplicationId ? (
              <span className="text-[10px] font-mono uppercase px-1.5 py-0.5 rounded-sm bg-amber-950/60 text-amber-300 border border-amber-500/30 font-medium">
                Retake
              </span>
            ) : null}
          </div>
        )
      },
    },
    {
      key: 'appointmentDate',
      header: 'Scheduled Date & Facility Time',
      sortable: true,
      sortValue: (r) => new Date(r.appointmentDate).getTime(),
      render: (r) => {
        const isActive = isAppointmentActive(r)
        return (
          <div className="flex items-center gap-2 text-xs">
            <Clock className={cn('size-3.5 shrink-0', isActive ? 'text-cyan-400' : 'text-slate-500')} />
            <span className={cn('font-mono', isActive ? 'text-white font-medium' : 'text-slate-300')}>
              {formatJordanAppointmentDateTime(r.appointmentDate)}
            </span>
          </div>
        )
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
      key: 'status',
      header: 'Lifecycle State',
      sortable: true,
      sortValue: (r) => resolveAppointmentLifecycle(r).state,
      render: (r) => {
        const lifecycle = resolveAppointmentLifecycle(r)
        return (
          <StatusBadge
            variant={lifecycle.statusVariant}
            label={lifecycle.resultLabel !== 'Not Taken' ? `${lifecycle.statusLabel} (${lifecycle.resultLabel})` : lifecycle.statusLabel}
          />
        )
      },
    },
    {
      key: 'details',
      header: 'Observations & Notes',
      align: 'center',
      render: (r) => {
        const isExpanded = expandedAppointmentId === r.testAppointmentId
        const isLoadingNotes = loadingTestId === r.testAppointmentId
        const lifecycle = resolveAppointmentLifecycle(r)
        const isCompleted = lifecycle.state.startsWith('Completed')

        if (!isCompleted && lifecycle.state !== 'NoShow') {
          return (
            <span className="text-slate-500 text-[11px] italic font-sans">
              Pending Examination
            </span>
          )
        }

        return (
          <button
            type="button"
            onClick={() => handleToggleTestDetails(r.testAppointmentId)}
            className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium text-slate-300 hover:text-white bg-slate-900/60 hover:bg-slate-800/80 border border-slate-700/60 transition-all cursor-pointer"
          >
            {isLoadingNotes ? (
              <span className="text-[11px] text-cyan-400 animate-pulse">Loading...</span>
            ) : (
              <>
                <span className="text-[11px]">{isExpanded ? 'Hide Notes' : 'View Notes'}</span>
                {isExpanded ? (
                  <ChevronDown className="size-3 text-slate-400" />
                ) : (
                  <ChevronRight className="size-3 text-slate-400" />
                )}
              </>
            )}
          </button>
        )
      },
    },
    {
      key: 'actions',
      header: 'Actions',
      align: 'right',
      render: (r) => {
        const canTake = !r.isLocked && (r.status === 'Scheduled' || r.status === 'Open')
        const canCancel = !r.isLocked && (r.status === 'Scheduled' || r.status === 'Open')

        return (
          <div className="flex items-center justify-end gap-1.5">
            {canTake && (
              <button
                type="button"
                onClick={() => {
                  if (onTakeExamination) {
                    onTakeExamination(r.testAppointmentId, 'Manage Test Appointments')
                  } else {
                    onNavigate?.('Take Examination')
                  }
                }}
                className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold text-emerald-300 bg-emerald-950/40 hover:bg-emerald-900/60 border border-emerald-500/30 hover:border-emerald-400 transition-all cursor-pointer shadow-xs"
                title="Conduct and record examination outcome"
              >
                <Award className="size-3.5" />
                <span>Take Exam</span>
              </button>
            )}

            {canCancel && (
              <button
                type="button"
                onClick={() => {
                  setCancelError(null)
                  setAppointmentToCancel(r)
                }}
                className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-semibold text-rose-300 bg-rose-950/40 hover:bg-rose-900/60 border border-rose-500/30 hover:border-rose-400 transition-all cursor-pointer shadow-xs"
                title="Cancel scheduled appointment slot"
              >
                <XCircle className="size-3.5" />
                <span>Cancel</span>
              </button>
            )}

            <button
              type="button"
              onClick={() => {
                if (onShowAppointmentDetails) {
                  onShowAppointmentDetails(r.testAppointmentId, 'Manage Test Appointments')
                } else {
                  onNavigate?.('Appointment Details')
                }
              }}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-medium text-cyan-300 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-500/30 hover:border-cyan-400 transition-all cursor-pointer shadow-xs"
              title="View comprehensive audit details"
            >
              <Eye className="size-3.5" />
              <span>Details</span>
            </button>
          </div>
        )
      },
    },
  ], [
    expandedAppointmentId,
    loadingTestId,
    handleToggleTestDetails,
    onTakeExamination,
    onShowAppointmentDetails,
    onNavigate,
  ])

  // Row Context Menu
  const rowContextMenu = useCallback(
    (apt: TestAppointment): DataTableRowAction<TestAppointment>[] => {
      const actions: DataTableRowAction<TestAppointment>[] = [
        {
          key: 'details',
          label: 'View Appointment Details',
          icon: <Eye className="size-4" />,
          onClick: () => {
            if (onShowAppointmentDetails) {
              onShowAppointmentDetails(apt.testAppointmentId, 'Manage Test Appointments')
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
              onTakeExamination(apt.testAppointmentId, 'Manage Test Appointments')
            } else {
              onNavigate?.('Take Examination')
            }
          },
        })
        actions.push({
          key: 'cancel',
          label: 'Cancel Appointment',
          icon: <XCircle className="size-4 text-rose-400" />,
          danger: true,
          onClick: () => {
            setCancelError(null)
            setAppointmentToCancel(apt)
          },
        })
      }

      return actions
    },
    [onShowAppointmentDetails, onTakeExamination, onNavigate],
  )

  return (
    <section className="page-content space-y-6">
      {/* 1. Informational PageHero (Immediately below Topbar, zero interactive controls inside) */}
      <PageHero
        pageId="Manage Test Appointments"
        title="Test Appointments"
        highlight="History"
        description="Complete chronological examination history and appointment management across Vision, Written, and Practical (Street) tests."
      />

      {/* 2. Page Actions Toolbar (Below PageHero) */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer self-start"
        >
          <ArrowLeft className="size-4" />
          <span>Back to Local Applications</span>
        </Button>

        {application && (
          <Button
            type="button"
            size="sm"
            onClick={handleScheduleNextTest}
            disabled={allStagesPassed || Boolean(activeAppointment)}
            title={
              allStagesPassed
                ? 'Candidate has already passed all three driving tests.'
                : activeAppointment
                  ? `An appointment is already active for ${activeAppointment.testTypeName} (#${activeAppointment.testAppointmentId}).`
                  : `Schedule the candidate's next test stage: ${TEST_TYPE_META[nextStage]?.name ?? 'Next Test'}`
            }
            className={cn(
              'font-semibold text-xs gap-1.5 shadow-sm rounded-xl cursor-pointer transition-all',
              allStagesPassed || Boolean(activeAppointment)
                ? 'bg-slate-800 text-slate-400 border border-slate-700/60 cursor-not-allowed opacity-75'
                : 'bg-cyan-600 hover:bg-cyan-500 text-white shadow-cyan-900/30',
            )}
          >
            <PlusCircle className="size-4" />
            <span>
              {allStagesPassed
                ? 'All Stages Completed'
                : activeAppointment
                  ? `Active Appointment (#${activeAppointment.testAppointmentId})`
                  : `Schedule ${TEST_TYPE_META[nextStage]?.shortName ?? 'Next'} Test`}
            </span>
          </Button>
        )}
      </div>

      {/* Action feedback */}
      {actionFeedback && (
        <div
          className={cn(
            'p-4 rounded-xl border flex items-center justify-between gap-3 text-xs',
            actionFeedback.type === 'success'
              ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-200'
              : 'bg-rose-950/40 border-rose-500/40 text-rose-200',
          )}
        >
          <div className="flex items-center gap-2.5">
            {actionFeedback.type === 'success' ? (
              <CheckCircle2 className="size-4 text-emerald-400 shrink-0" />
            ) : (
              <AlertCircle className="size-4 text-rose-400 shrink-0" />
            )}
            <span>{actionFeedback.message}</span>
          </div>
          <button
            type="button"
            onClick={() => setActionFeedback(null)}
            className="text-slate-400 hover:text-white cursor-pointer"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Error Alert */}
      {error && (
        <ErrorAlert
          title="Could Not Load Appointment Records"
          message={error}
          onDismiss={() => {
            setCancelError(null)
            setLoadError('')
          }}
          onRetry={loadData}
        />
      )}

      {loading && !application ? (
        <div className="flex items-center justify-center min-h-[300px] w-full">
          <LoadingSpinner size="lg" />
        </div>
      ) : (
        <>
          {/* Section B: Applicant & Application Context Card */}
          {application && (
            <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
              <CardHeader className="border-b border-[#1b2f52]/60 p-5 pb-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="flex items-center gap-3">
                    <div className="flex size-10 items-center justify-center rounded-xl border border-cyan-500/30 bg-cyan-500/10 text-cyan-400 shadow-xs shrink-0">
                      <CalendarClock className="size-5" />
                    </div>
                    <div>
                      <CardTitle className="text-base font-bold flex items-center gap-2">
                        <span>{application.fullName}</span>
                        <span className="text-slate-400 font-normal text-xs font-mono">
                          ({application.nationalNo})
                        </span>
                      </CardTitle>
                      <CardDescription className="text-xs text-slate-400 mt-0.5">
                        Local Application #{application.localDrivingLicenseApplicationId} · {application.licenseClassName || `Class ${application.licenseClassId}`}
                      </CardDescription>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 shrink-0">
                    <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border border-indigo-500/30 bg-indigo-950/40 text-indigo-300">
                      <ShieldCheck className="size-3.5 text-indigo-400" />
                      <span>{application.passedTest} / 3 Stages Passed</span>
                    </span>

                    <StatusBadge
                      variant={
                        application.applicationStatus === 'Completed'
                          ? 'success'
                          : application.applicationStatus === 'Cancelled'
                            ? 'danger'
                            : 'warning'
                      }
                      label={application.applicationStatus || 'Active'}
                    />
                  </div>
                </div>
              </CardHeader>

              {/* Grid details */}
              <div className="p-5 grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                <div>
                  <span className="text-slate-400 block text-[11px]">Application Date</span>
                  <span className="text-slate-200 font-mono font-medium mt-0.5 block">
                    {application.applicationDate?.split('T')[0] || '—'}
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px]">License Class</span>
                  <span className="text-white font-medium mt-0.5 block truncate" title={application.licenseClassName ?? undefined}>
                    {application.licenseClassName}
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px]">Application Tariff</span>
                  <span className="text-slate-200 font-mono font-medium mt-0.5 block">
                    {application.applicationFees.toFixed(2)} JOD
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px]">Current Stage Status</span>
                  <span className={cn('font-semibold mt-0.5 block', allStagesPassed ? 'text-emerald-400' : 'text-cyan-300')}>
                    {allStagesPassed
                      ? 'Ready for License Issuance'
                      : `Next: ${TEST_TYPE_META[nextStage]?.name ?? 'Examination'}`}
                  </span>
                </div>
              </div>
            </Card>
          )}

          {/* Section C: Complete History Summary Metrics */}
          <StatsGrid columns={4}>
            <StatCard
              label="Total Appointments"
              value={appointments.length}
              icon={<CalendarDays className="size-5" />}
              variant="default"
              description="Chronological records booked"
            />
            <StatCard
              label="Examination Progress"
              value={`${application?.passedTest ?? 0} of 3`}
              icon={<Award className="size-5" />}
              variant="blue"
              description={
                allStagesPassed
                  ? 'All 3 examination stages passed'
                  : `Next requirement: ${TEST_TYPE_META[nextStage]?.shortName ?? 'Test'}`
              }
            />
            <StatCard
              label="Active Appointment"
              value={activeAppointment ? `#${activeAppointment.testAppointmentId}` : 'None'}
              icon={<Clock className="size-5" />}
              variant={activeAppointment ? 'amber' : 'slate'}
              description={
                activeAppointment
                  ? `${activeAppointment.testTypeName} slot booked`
                  : 'Ready to schedule next stage'
              }
            />
            <StatCard
              label="Latest Outcome"
              value={
                appointments.length > 0
                  ? resolveAppointmentLifecycle(appointments[0]).resultLabel !== 'Not Taken'
                    ? resolveAppointmentLifecycle(appointments[0]).resultLabel
                    : resolveAppointmentLifecycle(appointments[0]).statusLabel
                  : 'No Records'
              }
              icon={<CheckCircle2 className="size-5" />}
              variant={
                appointments.length > 0 && resolveAppointmentLifecycle(appointments[0]).resultLabel === 'Passed'
                  ? 'emerald'
                  : appointments.length > 0 && resolveAppointmentLifecycle(appointments[0]).resultLabel === 'Failed'
                    ? 'rose'
                    : 'default'
              }
              description={
                appointments.length > 0
                  ? `${appointments[0].testTypeName} (Trial)`
                  : 'Awaiting initial booking'
              }
            />
          </StatsGrid>

          {/* Section D: Recorded Appointments Table */}
          <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
            <CardHeader className="border-b border-[#1b2f52]/60 p-5">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <CardTitle className="text-sm font-bold text-white flex items-center gap-2">
                    <CalendarClock className="size-4 text-cyan-400" />
                    <span>Unified Test Appointment Timeline</span>
                  </CardTitle>
                  <CardDescription className="text-xs text-slate-400 mt-0.5">
                    Chronological record of all driving test appointments for this candidate across Vision, Written, and Practical stages ({appointments.length} total records).
                  </CardDescription>
                </div>
              </div>
            </CardHeader>

            {appointments.length === 0 ? (
              <div className="p-8">
                <EmptyState
                  icon={<CalendarClock className="size-10 text-slate-400" />}
                  message="No Test Appointments Recorded"
                  description="No test appointments have been booked yet for this Local Driving License Application. You can schedule the candidate's initial Vision Test appointment directly."
                >
                  <Button
                    type="button"
                    size="sm"
                    onClick={handleScheduleNextTest}
                    className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold text-xs gap-1.5 cursor-pointer rounded-xl shadow-md mt-2"
                  >
                    <PlusCircle className="size-4" />
                    <span>Schedule Initial Test</span>
                  </Button>
                </EmptyState>
              </div>
            ) : (
              <div className="p-4 space-y-3">
                <DataTable<TestAppointment>
                  data={appointments}
                  columns={columns}
                  getRowKey={(r: TestAppointment) => r.testAppointmentId}
                  rowContextMenu={rowContextMenu}
                  emptyMessage="No appointments found."
                />

                {/* Inline Expanded Observation Drawer */}
                {expandedAppointmentId !== null && (
                  <div className="p-4 rounded-xl border border-slate-700/60 bg-slate-950/70 space-y-2 animate-in fade-in duration-200">
                    <div className="flex items-center justify-between text-xs border-b border-slate-800 pb-2">
                      <span className="font-semibold text-slate-200 flex items-center gap-2">
                        <FileText className="size-4 text-cyan-400" />
                        <span>Official Test Record Observations for Appointment #{expandedAppointmentId}</span>
                      </span>
                      <button
                        type="button"
                        onClick={() => setExpandedAppointmentId(null)}
                        className="text-slate-400 hover:text-white cursor-pointer text-xs"
                      >
                        Close
                      </button>
                    </div>

                    {testDetailsMap[expandedAppointmentId] ? (
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs pt-1">
                        <div>
                          <span className="text-slate-400 block text-[11px]">Official Outcome</span>
                          <span
                            className={cn(
                              'font-bold mt-0.5 inline-block',
                              testDetailsMap[expandedAppointmentId]?.testResult
                                ? 'text-emerald-400'
                                : 'text-rose-400',
                            )}
                          >
                            {testDetailsMap[expandedAppointmentId]?.testResult ? 'PASSED' : 'FAILED'}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Examiner</span>
                          <span className="text-slate-200 font-medium mt-0.5 block">
                            {testDetailsMap[expandedAppointmentId]?.createdByUserName ||
                              `Staff User #${testDetailsMap[expandedAppointmentId]?.createdByUserId}`}
                          </span>
                        </div>
                        <div className="sm:col-span-3">
                          <span className="text-slate-400 block text-[11px]">Examiner Remarks</span>
                          <p className="text-slate-200 mt-1 bg-slate-900/60 p-2.5 rounded-lg border border-slate-800 italic leading-relaxed">
                            {testDetailsMap[expandedAppointmentId]?.notes?.trim() ||
                              'No specific observations or remarks were recorded for this test examination.'}
                          </p>
                        </div>
                      </div>
                    ) : (
                      <p className="text-xs text-slate-400 italic py-2">
                        No official test result record was found for this appointment.
                      </p>
                    )}
                  </div>
                )}
              </div>
            )}
          </Card>
        </>
      )}

      {/* Appointment Cancellation Modal */}
      {appointmentToCancel && (
        <ConfirmModal
          isOpen={Boolean(appointmentToCancel)}
          title="Cancel Scheduled Appointment"
          message={`Are you sure you want to cancel Appointment #${appointmentToCancel.testAppointmentId} for ${appointmentToCancel.testTypeName}? Once cancelled, the slot will be locked and terminal.`}
          confirmText={cancelling ? 'Cancelling...' : 'Confirm Cancellation'}
          cancelText="Keep Appointment"
          confirmVariant="danger"
          loading={cancelling}
          onConfirm={handleConfirmCancel}
          onCancel={() => {
            setAppointmentToCancel(null)
            setCancelError(null)
          }}
        />
      )}
    </section>
  )
}
