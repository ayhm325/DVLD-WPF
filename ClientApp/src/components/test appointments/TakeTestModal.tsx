import { useState, useCallback } from 'react'
import ModalShell from '../common/ModalShell'
import ErrorAlert from '../common/ErrorAlert'
import LoadingSpinner from '../common/LoadingSpinner'
import type { TestAppointment } from '../../types/TestAppointment'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import { saveTestResult, getAttemptStats } from '../../services/testAppointmentService'
import { formatJordanAppointmentDateTime, isAppointmentInFuture } from '../../utils/jordanTime'
import { useAsyncData } from '../../hooks'
import {
  Award,
  CheckCircle2,
  XCircle,
  User,
  AlertTriangle,
  Clock,
} from 'lucide-react'

export interface TakeTestModalProps {
  isOpen: boolean
  onClose: () => void
  appointment: TestAppointment | null
  application?: LocalDrivingLicenseApplication | null
  accessToken: string
  onSuccess: (testId: number) => void
}

export default function TakeTestModal({
  isOpen,
  onClose,
  appointment,
  application,
  accessToken,
  onSuccess,
}: TakeTestModalProps) {
  const [testResult, setTestResult] = useState<boolean | null>(null)
  const [notes, setNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchAttemptStats = useCallback(async () => {
    if (!appointment) return null
    return await getAttemptStats(
      accessToken,
      appointment.localDrivingLicenseApplicationId,
      appointment.testTypeId,
    )
  }, [accessToken, appointment])

  const {
    data: attemptStats,
    loading: loadingStats,
  } = useAsyncData(fetchAttemptStats, {
    enabled: Boolean(isOpen && appointment),
    initialData: null,
  })

  if (!appointment) return null

  const isFuture = isAppointmentInFuture(appointment.appointmentDate)
  const isRetake = Boolean(appointment.retakeTestApplicationId)
  const currentTrial = attemptStats?.nextTrialNumber ?? (attemptStats ? attemptStats.totalAttempts + 1 : 1)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (testResult === null) {
      setError('Please select whether the candidate Passed or Failed the examination.')
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      const createdTestId = await saveTestResult(accessToken, {
        testAppointmentId: appointment.testAppointmentId,
        testResult,
        notes: notes.trim() || null,
      })

      onSuccess(createdTestId)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to record examination result.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      preventClose={submitting}
      size="lg"
      title={
        <div className="flex items-center gap-3">
          <div className="size-9 rounded-xl bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400">
            <Award className="size-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-base font-bold text-white tracking-tight">
                Take Examination — {appointment.testTypeName}
              </span>
              <span className="font-mono text-xs px-2 py-0.5 rounded bg-cyan-950/60 text-cyan-400 border border-cyan-500/30">
                Appt #{appointment.testAppointmentId}
              </span>
            </div>
            <div className="text-xs text-slate-400">
              Record official test outcome and examiner notes in Jordan facility time.
            </div>
          </div>
        </div>
      }
      footer={
        <div className="flex items-center justify-between w-full">
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="px-4 py-2 rounded-xl text-xs font-semibold bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white transition-all cursor-pointer border border-slate-700 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={testResult === null || submitting}
            className={`px-5 py-2.5 rounded-xl text-xs font-bold transition-all shadow-md flex items-center gap-2 cursor-pointer ${
              testResult === true
                ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-emerald-900/30'
                : testResult === false
                ? 'bg-rose-600 hover:bg-rose-500 text-white shadow-rose-900/30'
                : 'bg-cyan-600/50 text-slate-300 cursor-not-allowed border border-cyan-500/20'
            } disabled:opacity-50`}
          >
            {submitting ? (
              <>
                <LoadingSpinner size="sm" />
                <span>Saving Result…</span>
              </>
            ) : (
              <>
                <Award className="size-4" />
                <span>
                  {testResult === true
                    ? 'Confirm & Save Pass'
                    : testResult === false
                    ? 'Confirm & Save Fail'
                    : 'Save Examination Result'}
                </span>
              </>
            )}
          </button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4 py-1 text-slate-200 text-xs">
        {/* Error notification */}
        {error && (
          <ErrorAlert
            title="Examination Recording Error"
            message={error}
            variant="error"
            onDismiss={() => setError(null)}
          />
        )}

        {/* Candidate & Appointment Summary */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4 space-y-3 shadow-inner">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800 pb-2.5">
            <div className="flex items-center gap-2">
              <User className="size-4 text-cyan-400" />
              <span className="text-xs text-slate-400">Candidate:</span>
              <span className="text-xs font-bold text-white">
                {application?.fullName || 'Driving License Candidate'}
              </span>
            </div>
            <div className="flex items-center gap-3">
              <span className="font-mono text-[11px] text-slate-400">
                Local App <strong className="text-cyan-300">#{appointment.localDrivingLicenseApplicationId}</strong>
              </span>
              {isRetake && (
                <span className="px-2 py-0.5 rounded text-[10px] font-mono font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30">
                  Retake Application #{appointment.retakeTestApplicationId}
                </span>
              )}
            </div>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-[11px]">
            <div>
              <span className="text-slate-400 block mb-0.5">License Class</span>
              <span className="font-semibold text-slate-200">
                {application?.licenseClassName || 'Standard Class'}
              </span>
            </div>
            <div>
              <span className="text-slate-400 block mb-0.5">Test Type</span>
              <span className="font-semibold text-cyan-300">
                {appointment.testTypeName}
              </span>
            </div>
            <div>
              <span className="text-slate-400 block mb-0.5">Examination Trial</span>
              <span className="font-mono font-semibold text-amber-300">
                {loadingStats ? '...' : `Attempt #${currentTrial}`}
              </span>
            </div>
            <div>
              <span className="text-slate-400 block mb-0.5">Paid Fees</span>
              <span className="font-mono font-semibold text-emerald-300">
                {appointment.paidFees.toFixed(2)} JOD
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2 pt-1 border-t border-slate-800/80 text-[11px]">
            <Clock className="size-3.5 text-slate-400" />
            <span className="text-slate-400">Scheduled Facility Slot:</span>
            <span className="font-mono font-semibold text-white">
              {formatJordanAppointmentDateTime(
                appointment.appointmentDate,
                appointment.appointmentDateFormatted,
              )}
            </span>
          </div>
        </div>

        {/* Future Schedule Warning */}
        {isFuture && (
          <div className="rounded-xl border border-amber-500/30 bg-amber-950/40 p-3.5 flex items-start gap-3 text-amber-200">
            <AlertTriangle className="size-4 text-amber-400 shrink-0 mt-0.5" />
            <div className="space-y-1 text-xs">
              <span className="font-bold block text-amber-100">
                Upcoming Appointment Notice
              </span>
              <span className="text-[11px] text-amber-300/90 leading-relaxed block">
                This examination appointment is scheduled for a future slot (
                <strong>
                  {formatJordanAppointmentDateTime(
                    appointment.appointmentDate,
                    appointment.appointmentDateFormatted,
                  )}
                </strong>
                ). Official results should normally be recorded during or after the examination session.
                Please ensure that the examination has been administered before submitting.
              </span>
            </div>
          </div>
        )}

        {/* Result Selection Cards */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
            Examination Outcome <span className="text-rose-400">*</span>
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {/* Pass Option */}
            <button
              type="button"
              onClick={() => setTestResult(true)}
              className={`p-4 rounded-xl border text-left transition-all cursor-pointer flex items-start gap-3.5 ${
                testResult === true
                  ? 'border-emerald-500/80 bg-emerald-950/60 ring-2 ring-emerald-500/40 shadow-[0_0_15px_rgba(16,185,129,0.2)]'
                  : 'border-slate-800 bg-slate-900/60 hover:border-slate-700 hover:bg-slate-900/90'
              }`}
            >
              <div
                className={`size-8 rounded-lg flex items-center justify-center shrink-0 ${
                  testResult === true
                    ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
                    : 'bg-slate-800 text-slate-400'
                }`}
              >
                <CheckCircle2 className="size-5" />
              </div>
              <div className="space-y-0.5">
                <span className="text-sm font-bold text-white block">
                  Passed Examination
                </span>
                <span className="text-[11px] text-slate-400 leading-snug block">
                  Candidate successfully satisfied all testing standards and requirements.
                </span>
              </div>
            </button>

            {/* Fail Option */}
            <button
              type="button"
              onClick={() => setTestResult(false)}
              className={`p-4 rounded-xl border text-left transition-all cursor-pointer flex items-start gap-3.5 ${
                testResult === false
                  ? 'border-rose-500/80 bg-rose-950/60 ring-2 ring-rose-500/40 shadow-[0_0_15px_rgba(244,63,94,0.2)]'
                  : 'border-slate-800 bg-slate-900/60 hover:border-slate-700 hover:bg-slate-900/90'
              }`}
            >
              <div
                className={`size-8 rounded-lg flex items-center justify-center shrink-0 ${
                  testResult === false
                    ? 'bg-rose-500/20 text-rose-400 border border-rose-500/40'
                    : 'bg-slate-800 text-slate-400'
                }`}
              >
                <XCircle className="size-5" />
              </div>
              <div className="space-y-0.5">
                <span className="text-sm font-bold text-white block">
                  Failed Examination
                </span>
                <span className="text-[11px] text-slate-400 leading-snug block">
                  Candidate did not satisfy the passing criteria. Next trial will require retake booking.
                </span>
              </div>
            </button>
          </div>
        </div>

        {/* Examiner Notes */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
            Examiner Notes & Observations <span className="text-slate-500">(Optional)</span>
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={submitting}
            rows={3}
            placeholder="Record examination observations, score, or specific reasons for failure..."
            className="w-full rounded-xl border border-slate-800 bg-slate-950/80 px-3 py-2.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-cyan-500/40 focus:border-cyan-500/40 transition-all font-sans resize-none disabled:opacity-50"
          />
        </div>
      </form>
    </ModalShell>
  )
}
