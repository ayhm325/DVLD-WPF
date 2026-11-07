import { useCallback, useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Award,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Clock,
  Loader2,
} from 'lucide-react'
import PageHero from '../common/PageHero'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import StatusBadge from '../common/StatusBadge'
import { Button } from '../ui/button'
import { Card, CardHeader, CardTitle, CardDescription } from '../ui/card'
import { Textarea } from '../ui/textarea'
import { Label } from '../ui/label'
import type { TestAppointment, ExaminationAttemptStats } from '../../types/TestAppointment'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import {
  getAppointmentById,
  saveTestResult,
  getAttemptStats,
} from '../../services/testAppointmentService'
import { getLocalDrivingLicenseApplicationById } from '../../services/localDrivingLicenseApplicationService'
import {
  formatJordanAppointmentDateTime,
  isAppointmentInFuture,
} from '../../utils/jordanTime'
import { resolveAppointmentLifecycle } from '../../utils/status'
import { useAsyncData } from '../../hooks'
import { cn } from '@/lib/utils'

export interface TakeExaminationPageProps {
  accessToken: string
  appointmentId: number | null
  onBack: () => void
  onSuccess?: (testId: number) => void
  returnPage?: string
}

interface ExamPageData {
  appointment: TestAppointment | null
  application: LocalDrivingLicenseApplication | null
  attemptStats: ExaminationAttemptStats | null
}

export default function TakeExaminationPage({
  accessToken,
  appointmentId,
  onBack,
  onSuccess,
}: TakeExaminationPageProps) {
  // Outcome selection & notes
  const [testResult, setTestResult] = useState<boolean | null>(null)
  const [notes, setNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const fetcher = useCallback(async (): Promise<ExamPageData> => {
    if (!appointmentId || appointmentId <= 0) {
      throw new Error('A valid appointment ID is required to take an examination.')
    }

    const apptData = await getAppointmentById(accessToken, appointmentId)

    const [appData, statsData] = await Promise.all([
      getLocalDrivingLicenseApplicationById(
        accessToken,
        apptData.localDrivingLicenseApplicationId,
      ).catch(() => null),
      getAttemptStats(
        accessToken,
        apptData.localDrivingLicenseApplicationId,
        apptData.testTypeId,
      ).catch(() => null),
    ])

    return {
      appointment: apptData,
      application: appData,
      attemptStats: statsData,
    }
  }, [accessToken, appointmentId])

  const {
    data,
    loading,
    error: loadError,
    setError: setLoadError,
  } = useAsyncData(fetcher, {
    enabled: Boolean(appointmentId && appointmentId > 0),
    initialData: { appointment: null, application: null, attemptStats: null },
    defaultErrorMessage: `Failed to load details for Appointment #${appointmentId ?? ''}.`,
  })

  const appointment = data.appointment
  const application = data.application
  const attemptStats = data.attemptStats
  const error = actionError || loadError

  const isFuture = appointment ? isAppointmentInFuture(appointment.appointmentDate) : false
  const isRetake = Boolean(appointment?.retakeTestApplicationId)
  const currentTrial =
    attemptStats?.nextTrialNumber ?? (attemptStats ? attemptStats.totalAttempts + 1 : 1)
  const lifecycle = appointment ? resolveAppointmentLifecycle(appointment) : null

  const isAlreadyLocked = Boolean(appointment?.isLocked)
  const canRecord =
    Boolean(appointment) &&
    !isAlreadyLocked &&
    (appointment?.status === 'Scheduled' || appointment?.status === 'Open')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!appointment) return

    if (testResult === null) {
      setActionError('Please select an official examination outcome: Passed or Failed.')
      return
    }

    if (!canRecord) {
      setActionError('This appointment is locked or is not in a scheduled state.')
      return
    }

    setSubmitting(true)
    setActionError(null)

    try {
      const createdTestId = await saveTestResult(accessToken, {
        testAppointmentId: appointment.testAppointmentId,
        testResult,
        notes: notes.trim() || null,
      })

      setSuccessMessage(
        `Official examination result #${createdTestId} (${testResult ? 'Passed' : 'Failed'}) successfully recorded! The appointment is now completed.`,
      )

      if (onSuccess) {
        onSuccess(createdTestId)
      }

      // Allow user to see the success message briefly before returning
      setTimeout(() => {
        onBack()
      }, 1400)
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Failed to record examination result.')
      setSubmitting(false)
    }
  }

  return (
    <section className="page-content space-y-6">
      {/* 1. Informational PageHero (Immediately below Topbar, zero interactive controls inside) */}
      <PageHero
        pageId="Take Examination"
        title="Take"
        highlight="Examination"
        description="Record the official examination outcome, evaluation decisions, and examiner notes in Jordan facility time."
      />

      {/* 2. Page Actions Toolbar (Below PageHero) */}
      <div className="flex items-center justify-between gap-4">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          disabled={submitting}
          className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white hover:border-cyan-500/50 cursor-pointer transition-all gap-1.5"
        >
          <ArrowLeft className="size-4" />
          <span>Back</span>
        </Button>

        {appointment && (
          <span className="font-mono text-xs font-semibold px-3 py-1 rounded-full bg-slate-900 border border-slate-700 text-cyan-300">
            Appointment #{appointment.testAppointmentId}
          </span>
        )}
      </div>

      {/* Error notification */}
      {error && (
        <ErrorAlert
          title="Examination Recording Error"
          message={error}
          onDismiss={() => {
            setActionError(null)
            setLoadError('')
          }}
        />
      )}

      {/* Success notification */}
      {successMessage && (
        <div className="p-4 rounded-xl border border-emerald-500/40 bg-emerald-950/40 text-emerald-200 flex items-center gap-3 text-xs animate-in fade-in">
          <CheckCircle2 className="size-5 text-emerald-400 shrink-0" />
          <div className="space-y-0.5">
            <h4 className="font-bold text-white">Result Successfully Recorded</h4>
            <p className="text-emerald-300/90">{successMessage}</p>
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center min-h-[300px] w-full">
          <LoadingSpinner size="lg" />
        </div>
      ) : !appointment ? (
        <Card className="border-[#1b2f52] bg-[#0c192e] text-white p-8 text-center space-y-4">
          <AlertTriangle className="size-10 text-amber-400 mx-auto" />
          <div className="space-y-1">
            <h3 className="font-bold text-base text-white">Appointment Record Not Found</h3>
            <p className="text-xs text-slate-400">
              The requested appointment could not be located in the DVLD database.
            </p>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onBack}
            className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white"
          >
            Return to Appointments
          </Button>
        </Card>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Section B: Examination Information Card */}
          <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden">
            <CardHeader className="border-b border-[#1b2f52]/60 p-5 pb-4">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="size-11 rounded-xl bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400 shrink-0 shadow-inner">
                    <Award className="size-6" />
                  </div>
                  <div>
                    <CardTitle className="text-base font-bold flex items-center gap-2">
                      <span>{application?.fullName || 'Driving License Candidate'}</span>
                      {application?.nationalNo && (
                        <span className="text-slate-400 font-normal text-xs">
                          ({application.nationalNo})
                        </span>
                      )}
                    </CardTitle>
                    <CardDescription className="text-xs text-slate-400 mt-0.5">
                      {appointment.testTypeName} Examination · Local App #{appointment.localDrivingLicenseApplicationId}
                    </CardDescription>
                  </div>
                </div>

                <div className="flex items-center gap-2 shrink-0">
                  {lifecycle && (
                    <StatusBadge
                      variant={lifecycle.statusVariant}
                      label={lifecycle.statusLabel}
                      title={lifecycle.description}
                    />
                  )}
                  {isRetake && (
                    <span className="px-2.5 py-1 rounded-full text-xs font-mono font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30">
                      Retake #{appointment.retakeTestApplicationId}
                    </span>
                  )}
                </div>
              </div>
            </CardHeader>

            <div className="p-5 grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  Candidate Name
                </span>
                <strong className="text-sm font-semibold text-white truncate block">
                  {application?.fullName || '—'}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  National Number
                </span>
                <strong className="text-sm font-mono font-semibold text-cyan-300 block">
                  {application?.nationalNo || '—'}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  License Class
                </span>
                <strong className="text-sm font-semibold text-slate-200 truncate block">
                  {application?.licenseClassName || 'Standard Class'}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  Examination Attempt
                </span>
                <strong className="text-sm font-mono font-bold text-amber-300 block">
                  Trial #{currentTrial}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  Test Type
                </span>
                <strong className="text-sm font-semibold text-white block">
                  {appointment.testTypeName}
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  Paid Examination Fee
                </span>
                <strong className="text-sm font-mono font-bold text-emerald-400 block">
                  {appointment.paidFees.toFixed(2)} JOD
                </strong>
              </div>

              <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1 sm:col-span-2">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">
                  Scheduled Facility Slot
                </span>
                <div className="flex items-center gap-1.5 font-medium text-white text-xs">
                  <Clock className="size-3.5 text-cyan-400 shrink-0" />
                  <span>
                    {formatJordanAppointmentDateTime(
                      appointment.appointmentDate,
                      appointment.appointmentDateFormatted,
                    )}
                  </span>
                  <span className="text-[10px] text-slate-400">(Asia/Amman)</span>
                </div>
              </div>
            </div>
          </Card>

          {/* Section C: Upcoming Appointment Notice */}
          {isFuture && (
            <div className="rounded-xl border border-amber-500/30 bg-amber-950/40 p-4 flex items-start gap-3.5 text-amber-200">
              <AlertTriangle className="size-5 text-amber-400 shrink-0 mt-0.5" />
              <div className="space-y-1 text-xs">
                <span className="font-bold block text-sm text-amber-100">
                  Upcoming Appointment Slot Notice
                </span>
                <p className="text-amber-300/90 leading-relaxed">
                  This examination slot is scheduled for a future time (
                  <strong className="text-amber-200 font-mono">
                    {formatJordanAppointmentDateTime(
                      appointment.appointmentDate,
                      appointment.appointmentDateFormatted,
                    )}
                  </strong>
                  ). In accordance with licensing regulations, official examination outcomes must only
                  be recorded during or immediately following the physical examination session.
                  Please verify that the candidate has completed their examination before submitting.
                </p>
              </div>
            </div>
          )}

          {/* Section D: Examination Outcome Selection */}
          <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl p-6 space-y-4">
            <div className="space-y-1">
              <Label className="text-xs font-bold uppercase tracking-wider text-slate-300 flex items-center gap-1">
                <span>Official Examination Outcome</span>
                <span className="text-rose-400">*</span>
              </Label>
              <p className="text-xs text-slate-400">
                Select the candidate's final grading outcome. This decision is permanently recorded in the examination audit log.
              </p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-1">
              {/* Passed Examination */}
              <button
                type="button"
                onClick={() => setTestResult(true)}
                disabled={submitting || !canRecord}
                className={cn(
                  'p-5 rounded-2xl border text-left transition-all cursor-pointer flex items-start gap-4 outline-none focus-visible:ring-2 focus-visible:ring-emerald-400',
                  testResult === true
                    ? 'border-emerald-500 bg-emerald-950/60 ring-2 ring-emerald-500/40 shadow-[0_0_20px_rgba(16,185,129,0.25)]'
                    : 'border-[#1b2f52] bg-[#081326] hover:border-slate-700 hover:bg-[#0c1b35]',
                  (!canRecord || submitting) && 'opacity-50 cursor-not-allowed',
                )}
              >
                <div
                  className={cn(
                    'size-10 rounded-xl flex items-center justify-center shrink-0 border transition-all',
                    testResult === true
                      ? 'bg-emerald-500/25 text-emerald-300 border-emerald-500/50 shadow-sm'
                      : 'bg-[#0c192e] text-slate-500 border-[#1b2f52]',
                  )}
                >
                  <CheckCircle2 className="size-6" />
                </div>
                <div className="space-y-1">
                  <strong className="text-base font-bold text-white block">
                    Passed Examination
                  </strong>
                  <p className="text-xs text-slate-300/90 leading-relaxed">
                    The candidate successfully satisfied all standardized testing criteria, safety protocols, and examination questions.
                  </p>
                </div>
              </button>

              {/* Failed Examination */}
              <button
                type="button"
                onClick={() => setTestResult(false)}
                disabled={submitting || !canRecord}
                className={cn(
                  'p-5 rounded-2xl border text-left transition-all cursor-pointer flex items-start gap-4 outline-none focus-visible:ring-2 focus-visible:ring-rose-400',
                  testResult === false
                    ? 'border-rose-500 bg-rose-950/60 ring-2 ring-rose-500/40 shadow-[0_0_20px_rgba(244,63,94,0.25)]'
                    : 'border-[#1b2f52] bg-[#081326] hover:border-slate-700 hover:bg-[#0c1b35]',
                  (!canRecord || submitting) && 'opacity-50 cursor-not-allowed',
                )}
              >
                <div
                  className={cn(
                    'size-10 rounded-xl flex items-center justify-center shrink-0 border transition-all',
                    testResult === false
                      ? 'bg-rose-500/25 text-rose-300 border-rose-500/50 shadow-sm'
                      : 'bg-[#0c192e] text-slate-500 border-[#1b2f52]',
                  )}
                >
                  <XCircle className="size-6" />
                </div>
                <div className="space-y-1">
                  <strong className="text-base font-bold text-white block">
                    Failed Examination
                  </strong>
                  <p className="text-xs text-slate-300/90 leading-relaxed">
                    The candidate did not meet the mandatory passing benchmark. Retake examination booking will be required for the next attempt.
                  </p>
                </div>
              </button>
            </div>
          </Card>

          {/* Section E: Examiner Notes */}
          <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl p-6 space-y-3">
            <div className="space-y-1">
              <Label htmlFor="examiner-notes" className="text-xs font-bold uppercase tracking-wider text-slate-300">
                Examiner Notes & Observations <span className="text-slate-500 font-normal">(Optional)</span>
              </Label>
              <p className="text-xs text-slate-400">
                Record official evaluation comments, scores, or specific justification for failure.
              </p>
            </div>

            <Textarea
              id="examiner-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={submitting || !canRecord}
              rows={4}
              placeholder="Record detailed observations, test scores, infractions, or feedback for the candidate..."
              className="bg-[#081326] border-[#1b2f52] text-white text-xs focus:border-cyan-500 rounded-xl"
            />
          </Card>

          {/* Section F: Page Actions Area */}
          <div className="flex items-center justify-between gap-4 pt-2">
            <Button
              type="button"
              variant="outline"
              size="default"
              onClick={onBack}
              disabled={submitting}
              className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white h-11 px-6 rounded-xl cursor-pointer"
            >
              Cancel
            </Button>

            <Button
              type="submit"
              disabled={testResult === null || submitting || !canRecord}
              className={cn(
                'h-11 px-8 rounded-xl font-bold text-xs gap-2 cursor-pointer transition-all shadow-lg',
                testResult === true
                  ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-emerald-950/50'
                  : testResult === false
                  ? 'bg-rose-600 hover:bg-rose-500 text-white shadow-rose-950/50'
                  : 'bg-cyan-600 hover:bg-cyan-500 text-white opacity-50 cursor-not-allowed',
              )}
            >
              {submitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  <span>Recording Result…</span>
                </>
              ) : (
                <>
                  <Award className="size-4" />
                  <span>
                    {testResult === true
                      ? 'Save & Confirm Pass'
                      : testResult === false
                      ? 'Save & Confirm Fail'
                      : 'Save Examination Result'}
                  </span>
                </>
              )}
            </Button>
          </div>
        </form>
      )}
    </section>
  )
}
