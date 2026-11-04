import { useState, useCallback } from 'react'
import {
  Calendar,
  Clock,
  User,
  FileText,
  CheckCircle2,
  XCircle,
  AlertCircle,
  ShieldAlert,
  DollarSign,
  Award,
  ArrowLeft,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
import ConfirmModal from '../common/ConfirmModal'
import EmptyState from '../common/EmptyState'
import { Card, CardHeader, CardTitle, CardDescription } from '../ui/card'
import { Button } from '../ui/button'
import type { TestAppointment, TestResponse } from '../../types/TestAppointment'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import {
  getAppointmentById,
  getTestByAppointmentId,
  cancelTestAppointment,
} from '../../services/testAppointmentService'
import { getLocalDrivingLicenseApplicationById } from '../../services/localDrivingLicenseApplicationService'
import { resolveAppointmentLifecycle } from '../../utils/status'
import { formatJordanAppointmentDateTime } from '../../utils/jordanTime'
import { useAsyncData } from '../../hooks'

export interface AppointmentDetailsPageProps {
  accessToken: string
  appointmentId: number | null
  onBack: () => void
  onNavigate?: (page: string) => void
  onTakeExamination?: (appointmentId: number, returnPage?: string) => void
  userRole?: string | null
}

interface AppointmentPageDetails {
  appointment: TestAppointment | null
  application: LocalDrivingLicenseApplication | null
  testRecord: TestResponse | null
}

export default function AppointmentDetailsPage({
  accessToken,
  appointmentId,
  onBack,
  onNavigate,
  onTakeExamination,
}: AppointmentDetailsPageProps) {
  // In-page actions state
  const [showCancelConfirm, setShowCancelConfirm] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [actionFeedback, setActionFeedback] = useState<{
    type: 'success' | 'error'
    message: string
  } | null>(null)

  // Asynchronous data fetcher using project standard useAsyncData hook
  const fetcher = useCallback(async (): Promise<AppointmentPageDetails> => {
    if (!appointmentId) {
      return { appointment: null, application: null, testRecord: null }
    }

    const appt = await getAppointmentById(accessToken, appointmentId)

    // Parallel fetch of related application and test records
    const [appResult, testsResult] = await Promise.allSettled([
      getLocalDrivingLicenseApplicationById(accessToken, appt.localDrivingLicenseApplicationId),
      getTestByAppointmentId(accessToken, appt.testAppointmentId),
    ])

    const app = appResult.status === 'fulfilled' ? appResult.value : null
    const test =
      testsResult.status === 'fulfilled' && testsResult.value && testsResult.value.length > 0
        ? testsResult.value[0]
        : null

    return { appointment: appt, application: app, testRecord: test }
  }, [accessToken, appointmentId])

  const {
    data,
    loading,
    error,
    setError,
    refresh: loadAppointmentData,
  } = useAsyncData(fetcher, {
    enabled: Boolean(appointmentId),
    initialData: { appointment: null, application: null, testRecord: null },
    defaultErrorMessage: `Failed to load details for appointment #${appointmentId ?? ''}.`,
  })

  const appointment = data.appointment
  const application = data.application
  const testRecord = data.testRecord

  // Handle appointment cancellation
  const handleConfirmCancel = async () => {
    if (!appointment) return

    setCancelling(true)
    try {
      await cancelTestAppointment(accessToken, appointment.testAppointmentId)
      setActionFeedback({
        type: 'success',
        message: `Appointment #${appointment.testAppointmentId} has been successfully cancelled and locked.`,
      })
      setShowCancelConfirm(false)
      await loadAppointmentData()
    } catch (err) {
      setActionFeedback({
        type: 'error',
        message: err instanceof Error ? err.message : 'Failed to cancel appointment.',
      })
      setShowCancelConfirm(false)
    } finally {
      setCancelling(false)
    }
  }

  const lifecycle = appointment ? resolveAppointmentLifecycle(appointment) : null
  const isRetake = Boolean(appointment?.retakeTestApplicationId)
  const canTakeTest =
    Boolean(appointment) &&
    !appointment?.isLocked &&
    (appointment?.status === 'Scheduled' || appointment?.status === 'Open')
  const canCancel =
    Boolean(appointment) &&
    !appointment?.isLocked &&
    (appointment?.status === 'Scheduled' || appointment?.status === 'Open')

  return (
    <section className="page-content space-y-6">
      {/* Informational PageHero (Zero interactive controls inside) */}
      <PageHero
        pageId="Appointment Details"
        title="Appointment"
        highlight="Details"
        description={
          appointment
            ? `Official examination record for ${appointment.testTypeName} #${appointment.testAppointmentId} in Jordan facility time.`
            : 'Review comprehensive driving examination record and audit details.'
        }
      />

      {/* Top Action and Navigation Bar (Outside PageHero) */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          className="gap-2 text-slate-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <ArrowLeft className="size-4" />
          <span>Back to Appointments</span>
        </Button>

        <div className="flex items-center gap-2.5 justify-end">
          {canTakeTest && (
            <Button
              type="button"
              size="sm"
              onClick={() => {
                setActionFeedback(null)
                if (onTakeExamination && appointment) {
                  onTakeExamination(appointment.testAppointmentId, 'Appointment Details')
                } else {
                  onNavigate?.('Take Examination')
                }
              }}
              className="gap-1.5 text-xs font-semibold bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl shadow-md cursor-pointer transition-all"
            >
              <Award className="size-3.5" />
              <span>Take Examination</span>
            </Button>
          )}

          {canCancel && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                setActionFeedback(null)
                setShowCancelConfirm(true)
              }}
              className="gap-1.5 text-xs font-semibold border-rose-500/40 text-rose-300 bg-rose-950/30 hover:bg-rose-900/50 hover:text-rose-200 rounded-xl cursor-pointer transition-all"
            >
              <XCircle className="size-3.5" />
              <span>Cancel Appointment</span>
            </Button>
          )}
        </div>
      </div>

      {/* Action Feedback Banner */}
      {actionFeedback && (
        <div
          className={`rounded-xl border p-4 flex items-center justify-between text-xs font-medium ${
            actionFeedback.type === 'success'
              ? 'border-emerald-500/40 bg-emerald-950/40 text-emerald-300'
              : 'border-rose-500/40 bg-rose-950/40 text-rose-300'
          }`}
        >
          <div className="flex items-center gap-2">
            {actionFeedback.type === 'success' ? (
              <CheckCircle2 className="size-4 shrink-0 text-emerald-400" />
            ) : (
              <AlertCircle className="size-4 shrink-0 text-rose-400" />
            )}
            <span>{actionFeedback.message}</span>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setActionFeedback(null)}
            className="h-6 px-2 text-xs hover:bg-white/10"
          >
            Dismiss
          </Button>
        </div>
      )}

      {/* Error Alert */}
      {error && (
        <ErrorAlert
          title="Appointment Notice"
          message={error}
          onDismiss={() => setError('')}
        />
      )}

      {loading ? (
        <Card className="p-12 text-center border-[#1e3458] bg-[#0c1930]">
          <LoadingSpinner
            message={`Loading examination record #${appointmentId ?? ''}...`}
            size="lg"
            layout="col"
          />
        </Card>
      ) : !appointment ? (
        <Card className="border-[#1e3458] bg-[#0c1930] p-12 text-center">
          <EmptyState
            message="Appointment Not Found"
            description={`We could not locate driving test appointment record #${appointmentId ?? ''}. It may have been removed or the ID is invalid.`}
          >
            <div className="mt-4">
              <Button
                variant="outline"
                size="sm"
                onClick={onBack}
                className="gap-2 text-slate-300 border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
              >
                <ArrowLeft className="size-4" />
                <span>Return to Appointments</span>
              </Button>
            </div>
          </EmptyState>
        </Card>
      ) : (
        <div className="space-y-6">
          {/* Top Summary Banner */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-5 shadow-inner">
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
              <div className="flex items-center gap-4">
                <div className="size-12 rounded-2xl bg-cyan-950/80 border border-cyan-500/30 flex items-center justify-center text-cyan-400 shrink-0 shadow-[0_0_15px_rgba(0,140,255,0.15)]">
                  <Calendar className="size-6" />
                </div>
                <div>
                  <div className="flex items-center gap-2.5 flex-wrap">
                    <span className="text-base font-bold text-white tracking-tight">
                      Appointment <span className="font-mono text-cyan-400">#{appointment.testAppointmentId}</span>
                    </span>
                    {lifecycle && (
                      <StatusBadge
                        variant={lifecycle.statusVariant}
                        label={lifecycle.statusLabel}
                        title={lifecycle.description}
                      />
                    )}
                    {isRetake && (
                      <span className="px-2 py-0.5 rounded text-[11px] font-mono font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30">
                        Retake #{appointment.retakeTestApplicationId}
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-slate-400 mt-1 flex items-center gap-2">
                    <Clock className="size-3.5 text-cyan-400 shrink-0" />
                    <span>
                      Scheduled Facility Slot:{' '}
                      <strong className="text-white">
                        {formatJordanAppointmentDateTime(
                          appointment.appointmentDate,
                          appointment.appointmentDateFormatted,
                        )}
                      </strong>{' '}
                      (Asia/Amman)
                    </span>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2 self-start md:self-auto">
                <span className="text-slate-400 text-xs font-medium">Lifecycle State:</span>
                <span className="font-bold text-cyan-300 bg-cyan-950/60 px-3 py-1 rounded-xl border border-cyan-500/30 text-xs">
                  {lifecycle?.state}
                </span>
              </div>
            </div>
          </Card>

          {/* 2-Column Details Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Card 1: Candidate & Application Details */}
            <Card className="border-[#1e3458] bg-[#0c1930] p-6">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458] mb-5 space-y-0">
                <div className="size-9 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                  <User className="size-4.5" />
                </div>
                <div>
                  <CardTitle className="text-base text-white">Candidate & Application</CardTitle>
                  <CardDescription className="text-xs text-slate-400">
                    Official civil identity and application record linkage
                  </CardDescription>
                </div>
              </CardHeader>

              <div className="space-y-3 text-xs">
                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Applicant Legal Name:</span>
                  <span className="font-semibold text-white text-right">
                    {application?.fullName ?? 'Associated Candidate'}
                  </span>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">National ID Number:</span>
                  <span className="font-mono text-cyan-300">
                    {application?.nationalNo ?? '—'}
                  </span>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Local Driving Application ID:</span>
                  <div className="flex items-center gap-2">
                    <span className="font-mono font-bold text-white">
                      #{appointment.localDrivingLicenseApplicationId}
                    </span>
                    {onNavigate && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => onNavigate('Local Applications')}
                        className="h-6 px-1.5 text-[11px] text-cyan-400 hover:text-cyan-300 hover:bg-cyan-950/50"
                      >
                        View Applications
                      </Button>
                    )}
                  </div>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Target License Class:</span>
                  <span className="text-slate-200 text-right font-medium">
                    {application?.licenseClassName ?? 'Driving License Class'}
                  </span>
                </div>

                <div className="flex justify-between items-center py-2">
                  <span className="text-slate-400">Examination Progression:</span>
                  <span className="text-slate-200 text-right font-medium">
                    {application
                      ? `${application.passedTest} of 3 Tests Passed`
                      : '—'}
                  </span>
                </div>
              </div>
            </Card>

            {/* Card 2: Schedule & Audit Metadata */}
            <Card className="border-[#1e3458] bg-[#0c1930] p-6">
              <CardHeader className="flex-row items-center gap-3 pb-4 border-b border-[#1e3458] mb-5 space-y-0">
                <div className="size-9 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                  <Clock className="size-4.5" />
                </div>
                <div>
                  <CardTitle className="text-base text-white">Schedule & Audit Metadata</CardTitle>
                  <CardDescription className="text-xs text-slate-400">
                    Facility reservation timestamps and security trail
                  </CardDescription>
                </div>
              </CardHeader>

              <div className="space-y-3 text-xs">
                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Test Classification:</span>
                  <span className="font-semibold text-white flex items-center gap-1.5">
                    <Award className="size-3.5 text-cyan-400" />
                    <span>{appointment.testTypeName}</span>
                  </span>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Facility Timezone:</span>
                  <span className="font-mono text-slate-300">Asia/Amman (UTC+03:00)</span>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Created By Staff:</span>
                  <span className="text-slate-200 font-medium">
                    {appointment.createdByUserName || 'Staff'}
                  </span>
                </div>

                <div className="flex justify-between items-center py-2 border-b border-slate-800/60">
                  <span className="text-slate-400">Locking Status:</span>
                  <span className="font-medium">
                    {appointment.isLocked ? (
                      <span className="text-amber-400 flex items-center gap-1">
                        <ShieldAlert className="size-3.5" /> Locked (Terminal/Finalized)
                      </span>
                    ) : (
                      <span className="text-emerald-400 flex items-center gap-1">
                        <CheckCircle2 className="size-3.5" /> Unlocked (Scheduled / Open)
                      </span>
                    )}
                  </span>
                </div>

                {appointment.noShowDate && (
                  <div className="flex justify-between items-center py-2 border-b border-slate-800/60 text-amber-300">
                    <span className="text-slate-400">Marked No-Show Date:</span>
                    <span className="font-medium">
                      {formatJordanAppointmentDateTime(appointment.noShowDate)}
                    </span>
                  </div>
                )}

                {appointment.cancelledAt && (
                  <div className="flex justify-between items-center py-2 border-b border-slate-800/60 text-rose-300">
                    <span className="text-slate-400">Cancelled At:</span>
                    <span className="font-medium">
                      {formatJordanAppointmentDateTime(appointment.cancelledAt)}
                    </span>
                  </div>
                )}

                {appointment.cancelledByUserId && (
                  <div className="flex justify-between items-center py-2 text-slate-300">
                    <span className="text-slate-400">Cancelled By Staff:</span>
                    <span className="font-mono text-xs">
                      Staff User #{appointment.cancelledByUserId}
                    </span>
                  </div>
                )}
              </div>
            </Card>
          </div>

          {/* Card 3: Official Examination Record */}
          <Card className="border-[#1e3458] bg-[#0c1930] p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-[#1e3458] pb-4">
              <div className="flex items-center gap-3">
                <div className="size-9 rounded-xl bg-blue-500/15 border border-blue-400/30 text-cyan-400 flex items-center justify-center shrink-0">
                  <FileText className="size-4.5" />
                </div>
                <div>
                  <CardTitle className="text-base text-white">Official Examination Record</CardTitle>
                  <CardDescription className="text-xs text-slate-400">
                    Test evaluation outcome, examiner attribution, and observations
                  </CardDescription>
                </div>
              </div>

              {testRecord?.testId && (
                <span className="font-mono text-xs font-semibold text-cyan-300 bg-cyan-950/60 border border-cyan-500/30 px-3 py-1 rounded-xl">
                  Test Record #{testRecord.testId}
                </span>
              )}
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="rounded-xl bg-[#081222] border border-[#223553] p-4 space-y-1.5">
                <span className="text-[11px] text-slate-400 block font-semibold uppercase tracking-wider">
                  Evaluation Outcome
                </span>
                <div>
                  {lifecycle?.resultLabel === 'Passed' ? (
                    <span className="inline-flex items-center gap-1.5 text-sm font-bold text-emerald-400">
                      <CheckCircle2 className="size-4" /> Passed
                    </span>
                  ) : lifecycle?.resultLabel === 'Failed' ? (
                    <span className="inline-flex items-center gap-1.5 text-sm font-bold text-rose-400">
                      <XCircle className="size-4" /> Failed
                    </span>
                  ) : lifecycle?.resultLabel === 'No-Show' ? (
                    <span className="inline-flex items-center gap-1.5 text-sm font-semibold text-amber-400">
                      <AlertCircle className="size-4" /> No-Show
                    </span>
                  ) : lifecycle?.resultLabel === 'Cancelled' ? (
                    <span className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-400">
                      <XCircle className="size-4 text-slate-500" /> Cancelled
                    </span>
                  ) : (
                    <span className="text-slate-400 italic text-sm">Pending Examination</span>
                  )}
                </div>
              </div>

              <div className="rounded-xl bg-[#081222] border border-[#223553] p-4 space-y-1.5">
                <span className="text-[11px] text-slate-400 block font-semibold uppercase tracking-wider">
                  Staff Examiner
                </span>
                <span className="text-sm font-semibold text-slate-200 block">
                  {testRecord?.createdByUserName || appointment.createdByUserName || 'Staff Examiner'}
                </span>
              </div>

              <div className="rounded-xl bg-[#081222] border border-[#223553] p-4 space-y-1.5">
                <span className="text-[11px] text-slate-400 block font-semibold uppercase tracking-wider">
                  Applicable Fee Snapshot
                </span>
                <span className="font-mono text-sm font-bold text-emerald-400 block">
                  {appointment.paidFees.toFixed(2)} JOD
                </span>
              </div>
            </div>

            <div className="rounded-xl bg-[#081222] border border-[#223553] p-4 space-y-2">
              <div className="text-xs text-slate-300 font-semibold uppercase tracking-wider">
                Examiner Observations & Notes
              </div>
              {testRecord?.notes ? (
                <div className="text-sm text-slate-200 italic font-sans leading-relaxed p-3 rounded-lg bg-slate-900/60 border border-slate-800">
                  "{testRecord.notes}"
                </div>
              ) : (
                <div className="text-xs text-slate-500 italic p-3 rounded-lg bg-slate-900/40 border border-slate-800/60">
                  {lifecycle?.state.startsWith('Completed')
                    ? 'No specific observations or notes were recorded during this examination.'
                    : 'Examination observations and notes are recorded upon examination by the authorized test examiner.'}
                </div>
              )}
            </div>
          </Card>

          {/* Card 4: Financial Record Snapshot Notice */}
          <Card className="border-slate-800/80 bg-slate-950/40 p-4 flex items-start gap-3.5 text-slate-400">
            <DollarSign className="size-5 text-emerald-400 shrink-0 mt-0.5" />
            <div className="space-y-1 text-xs leading-relaxed">
              <span className="font-semibold text-slate-300 block">
                Financial Record Snapshot (Display Only)
              </span>
              <p>
                The examination fee of{' '}
                <strong className="text-white font-mono">{appointment.paidFees.toFixed(2)} JOD</strong>{' '}
                represents the immutable historical fee snapshot locked at the time of appointment scheduling. No financial payment processing, receipt generation, or balance mutation operations are performed through this audit interface.
              </p>
            </div>
          </Card>
        </div>
      )}

      {/* Cancel Appointment Confirmation Modal */}
      {showCancelConfirm && appointment && (
        <ConfirmModal
          isOpen={showCancelConfirm}
          onCancel={() => setShowCancelConfirm(false)}
          onConfirm={handleConfirmCancel}
          title="Cancel Scheduled Appointment"
          message={`Are you sure you want to cancel Appointment #${appointment.testAppointmentId} for ${appointment.testTypeName}? Once cancelled, the slot will be locked and terminal.`}
          confirmText={cancelling ? 'Cancelling...' : 'Confirm Cancellation'}
          cancelText="Keep Appointment"
          confirmVariant="danger"
          loading={cancelling}
        />
      )}
    </section>
  )
}
