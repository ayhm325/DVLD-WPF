import { useEffect, useState, useCallback, useMemo, Fragment } from 'react'
import {
  Calendar,
  Clock,
  Eye,
  FileSignature,
  Car,
  AlertCircle,
  PlusCircle,
  CheckCircle2,
  XCircle,
  FileText,
  ChevronDown,
  ChevronRight,
  Award,
} from 'lucide-react'
import ModalShell from '../common/ModalShell'
import LoadingSpinner from '../common/LoadingSpinner'
import StatusBadge from '../common/StatusBadge'
import TakeTestModal from '../test appointments/TakeTestModal'
import {
  formatAppointmentSubmission,
  formatJordanAppointmentDateTime,
  getJordanDateString,
  formatAvailableDateLabel,
} from '../../utils/jordanTime'
import {
  getAppointmentsByLocalAppId,
  getSchedulePreparation,
  scheduleTestAppointment,
  getAppointmentAvailability,
  getTestByAppointmentId,
} from '../../services/testAppointmentService'
import { isAppointmentActive, resolveAppointmentLifecycle } from '../../utils/status'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type {
  AppointmentAvailabilityResponse,
  ScheduleTestPreparation,
  TestAppointment,
  TestResponse,
} from '../../types/TestAppointment'
import { Button } from '../ui/button'

export interface TestAppointmentsModalProps {
  isOpen: boolean
  onClose: () => void
  application?: LocalDrivingLicenseApplication | null
  testTypeId?: 1 | 2 | 3
  accessToken: string
  onAppointmentScheduled?: () => void
  applications?: LocalDrivingLicenseApplication[]
  initialShowScheduleForm?: boolean
  onShowAppointmentDetails?: (appointmentId: number) => void
}

const TEST_META = {
  1: {
    name: 'Vision Test',
    icon: Eye,
    color: 'text-cyan-400',
    bgColor: 'bg-cyan-500/10',
    borderColor: 'border-cyan-500/20',
  },
  2: {
    name: 'Written Test',
    icon: FileSignature,
    color: 'text-indigo-400',
    bgColor: 'bg-indigo-500/10',
    borderColor: 'border-indigo-500/20',
  },
  3: {
    name: 'Street Test',
    icon: Car,
    color: 'text-emerald-400',
    bgColor: 'bg-emerald-500/10',
    borderColor: 'border-emerald-500/20',
  },
}

export default function TestAppointmentsModal({
  isOpen,
  onClose,
  application,
  testTypeId,
  accessToken,
  onAppointmentScheduled,
  applications,
  initialShowScheduleForm = false,
  onShowAppointmentDetails,
}: TestAppointmentsModalProps) {
  // Support standalone page selection across available applications
  const [userSelectedAppId, setUserSelectedAppId] = useState<number | null>(null)
  const [userSelectedTestTypeId, setUserSelectedTestTypeId] = useState<1 | 2 | 3 | null>(null)

  const effectiveApp = useMemo(() => {
    if (application) return application
    if (userSelectedAppId !== null) {
      const match = applications?.find((a) => a.localDrivingLicenseApplicationId === userSelectedAppId)
      if (match) return match
    }
    return applications?.[0] ?? null
  }, [application, applications, userSelectedAppId])

  const effectiveTestTypeId = application
    ? (testTypeId ?? 1)
    : (userSelectedTestTypeId ?? testTypeId ?? 1)

  const [appointments, setAppointments] = useState<TestAppointment[]>([])
  const [schedulePrep, setSchedulePrep] = useState<ScheduleTestPreparation | null>(null)
  const [fetchAttempted, setFetchAttempted] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedAppointmentForTakeTest, setSelectedAppointmentForTakeTest] =
    useState<TestAppointment | null>(null)

  // New appointment scheduling form
  const [showScheduleForm, setShowScheduleForm] = useState(initialShowScheduleForm)
  const [submitting, setSubmitting] = useState(false)
  const [submitSuccess, setSubmitSuccess] = useState(false)

  // Backend-driven availability state
  const [availability, setAvailability] = useState<AppointmentAvailabilityResponse | null>(null)
  const [loadingAvailability, setLoadingAvailability] = useState(false)
  const [availabilityError, setAvailabilityError] = useState<string | null>(null)
  const [selectedDate, setSelectedDate] = useState<string>('')
  const [selectedSlot, setSelectedSlot] = useState<string>('')

  // Examination history & examiner notes details
  const [expandedAppointmentId, setExpandedAppointmentId] = useState<number | null>(null)
  const [testDetailsMap, setTestDetailsMap] = useState<Record<number, TestResponse | null>>({})
  const [loadingTestId, setLoadingTestId] = useState<number | null>(null)

  const meta = TEST_META[effectiveTestTypeId] || TEST_META[1]
  const Icon = meta.icon

  const loading = Boolean(isOpen && effectiveApp && !fetchAttempted)

  const loadData = useCallback(() => {
    if (!effectiveApp) return

    Promise.all([
      getAppointmentsByLocalAppId(accessToken, effectiveApp.localDrivingLicenseApplicationId),
      getSchedulePreparation(
        accessToken,
        effectiveApp.localDrivingLicenseApplicationId,
        effectiveTestTypeId,
      ).catch(() => null),
    ])
      .then(([allAppointments, prep]) => {
        const filtered = allAppointments.filter((x) => x.testTypeId === effectiveTestTypeId)
        setAppointments(filtered)
        setSchedulePrep(prep)
        setFetchAttempted(true)
      })
      .catch((err) => {
        setError(err instanceof Error ? err.message : 'Could not load test appointments.')
        setFetchAttempted(true)
      })
  }, [accessToken, effectiveApp, effectiveTestTypeId])

  const loadAvailability = useCallback(() => {
    if (!effectiveApp) return

    setLoadingAvailability(true)
    setAvailabilityError(null)

    // Request available slots starting 2 calendar days ahead in Jordan facility time across a 30-day window
    const startDate = getJordanDateString(new Date(), 2)
    const endDate = getJordanDateString(new Date(), 30)

    getAppointmentAvailability(
      accessToken,
      effectiveApp.localDrivingLicenseApplicationId,
      effectiveTestTypeId,
      startDate,
      endDate,
    )
      .then((data) => {
        setAvailability(data)
        if (data.availableDays.length > 0) {
          setSelectedDate((prevDate) => {
            const dayStillValid = data.availableDays.find((d) => d.date === prevDate)
            const chosenDate = dayStillValid ? prevDate : data.availableDays[0].date
            const chosenDay = data.availableDays.find((d) => d.date === chosenDate)
            setSelectedSlot((prevSlot) => {
              const slotStillValid = chosenDay?.slots.some((s) => s.startTime === prevSlot)
              return slotStillValid ? prevSlot : (chosenDay?.slots[0]?.startTime ?? '')
            })
            return chosenDate
          })
        } else {
          setSelectedDate('')
          setSelectedSlot('')
        }
      })
      .catch((err) => {
        setAvailabilityError(
          err instanceof Error ? err.message : 'Could not retrieve appointment availability.',
        )
      })
      .finally(() => {
        setLoadingAvailability(false)
      })
  }, [accessToken, effectiveApp, effectiveTestTypeId])

  useEffect(() => {
    if (!isOpen || !effectiveApp) {
      return
    }

    loadData()

    return () => {
      setShowScheduleForm(initialShowScheduleForm)
      setSubmitSuccess(false)
      setFetchAttempted(false)
      setAppointments([])
      setSchedulePrep(null)
      setError(null)
      setAvailability(null)
      setAvailabilityError(null)
      setSelectedDate('')
      setSelectedSlot('')
      setLoadingAvailability(false)
      setExpandedAppointmentId(null)
      setTestDetailsMap({})
      setLoadingTestId(null)
    }
  }, [isOpen, effectiveApp, loadData, initialShowScheduleForm])

  // Synchronize availability when effective application or testTypeId changes while schedule form is open
  useEffect(() => {
    if (!isOpen || !effectiveApp || !showScheduleForm) {
      return
    }

    let cancelled = false
    const startDate = getJordanDateString(new Date(), 2)
    const endDate = getJordanDateString(new Date(), 30)

    getAppointmentAvailability(
      accessToken,
      effectiveApp.localDrivingLicenseApplicationId,
      effectiveTestTypeId,
      startDate,
      endDate,
    )
      .then((data) => {
        if (cancelled) return
        setAvailability(data)
        setLoadingAvailability(false)
        if (data.availableDays.length > 0) {
          setSelectedDate((prevDate) => {
            const dayStillValid = data.availableDays.find((d) => d.date === prevDate)
            const chosenDate = dayStillValid ? prevDate : data.availableDays[0].date
            const chosenDay = data.availableDays.find((d) => d.date === chosenDate)
            setSelectedSlot((prevSlot) => {
              const slotStillValid = chosenDay?.slots.some((s) => s.startTime === prevSlot)
              return slotStillValid ? prevSlot : (chosenDay?.slots[0]?.startTime ?? '')
            })
            return chosenDate
          })
        } else {
          setSelectedDate('')
          setSelectedSlot('')
        }
      })
      .catch((err) => {
        if (cancelled) return
        setAvailabilityError(
          err instanceof Error ? err.message : 'Could not retrieve appointment availability.',
        )
        setLoadingAvailability(false)
      })

    return () => {
      cancelled = true
    }
  }, [isOpen, effectiveApp, effectiveTestTypeId, showScheduleForm, accessToken])

  const currentDaySlots = useMemo(() => {
    if (!availability || !selectedDate) return []
    return availability.availableDays.find((d) => d.date === selectedDate)?.slots ?? []
  }, [availability, selectedDate])

  const handleToggleTestDetails = useCallback(
    (appointmentId: number) => {
      setExpandedAppointmentId((prev) => {
        if (prev === appointmentId) {
          return null
        }

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
              setLoadingTestId((current) => (current === appointmentId ? null : current))
            })
        }

        return appointmentId
      })
    },
    [accessToken, testDetailsMap],
  )

  if (!effectiveApp) return null

  const hasActiveAppointment = appointments.some(isAppointmentActive)
  const isPassed =
    (effectiveTestTypeId === 1 && effectiveApp.passedTest >= 1) ||
    (effectiveTestTypeId === 2 && effectiveApp.passedTest >= 2) ||
    (effectiveTestTypeId === 3 && effectiveApp.passedTest >= 3)

  const canScheduleNew =
    !hasActiveAppointment && !isPassed && effectiveApp.applicationStatus.toLowerCase() === 'new'

  const handleScheduleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedDate || !selectedSlot) return

    setSubmitting(true)
    setError(null)

    try {
      const submissionDateTime = formatAppointmentSubmission(`${selectedDate}T${selectedSlot}`)
      await scheduleTestAppointment(accessToken, {
        localDrivingLicenseApplicationId: effectiveApp.localDrivingLicenseApplicationId,
        testTypeId: effectiveTestTypeId,
        appointmentDate: submissionDateTime,
      })

      setSubmitSuccess(true)
      setShowScheduleForm(false)
      setSelectedDate('')
      setSelectedSlot('')
      await loadData()
      loadAvailability()
      onAppointmentScheduled?.()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to schedule appointment.')
      // Refresh availability to update occupied slots
      loadAvailability()
    } finally {
      setSubmitting(false)
    }
  }

  const totalFees = (schedulePrep?.fees ?? 0) + (schedulePrep?.retakerFees ?? 0)

  return (
    <>
      <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div className="flex items-center gap-2.5 text-white">
          <div
            className={`flex size-8 items-center justify-center rounded-lg ${meta.bgColor} ${meta.color} border ${meta.borderColor} shadow-xs`}
          >
            <Icon className="size-4" />
          </div>
          <span className="font-bold text-base tracking-tight">{meta.name} Appointments</span>
          <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-800/80 text-cyan-300 border border-slate-700">
            App #{effectiveApp.localDrivingLicenseApplicationId}
          </span>
        </div>
      }
      description={`Schedule and inspect official ${meta.name.toLowerCase()} appointment records for this driving license application.`}
      size="xl"
    >
      <div className="space-y-5 max-h-[75vh] overflow-y-auto pr-1">
        {/* Candidate & Test Stage Selector (When opened from global entry point) */}
        {!application && applications && applications.length > 0 && (
          <div className="rounded-xl border border-[#1e3458] bg-[#0c1830] p-4 space-y-3">
            <div className="flex items-center justify-between border-b border-[#1e3458] pb-2">
              <span className="text-[11px] uppercase tracking-wider font-bold text-cyan-400 flex items-center gap-1.5">
                <FileText className="size-3.5" />
                <span>Select Candidate & Examination Stage</span>
              </span>
              <span className="text-[11px] text-slate-400">
                {applications.length} applications loaded
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 items-center">
              <div>
                <label htmlFor="modal-app-select" className="block text-xs font-medium text-slate-300 mb-1">
                  Local Driving Application
                </label>
                <select
                  id="modal-app-select"
                  value={effectiveApp.localDrivingLicenseApplicationId}
                  onChange={(e) => {
                    setUserSelectedAppId(Number(e.target.value))
                    setSelectedDate('')
                    setSelectedSlot('')
                    setSubmitSuccess(false)
                  }}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-xs text-white focus:outline-none focus:border-cyan-500 cursor-pointer"
                >
                  {applications.map((app) => (
                    <option key={app.localDrivingLicenseApplicationId} value={app.localDrivingLicenseApplicationId}>
                      App #{app.localDrivingLicenseApplicationId} — {app.fullName || 'Candidate'} ({app.licenseClassName})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">
                  Test Stage
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {[
                    { id: 1 as const, name: 'Vision' },
                    { id: 2 as const, name: 'Written' },
                    { id: 3 as const, name: 'Street' },
                  ].map((t) => (
                    <button
                      key={t.id}
                      type="button"
                      onClick={() => {
                        setUserSelectedTestTypeId(t.id)
                        setSelectedDate('')
                        setSelectedSlot('')
                        setSubmitSuccess(false)
                      }}
                      className={`px-3 py-2 rounded-lg text-xs font-semibold border transition-all cursor-pointer text-center ${
                        effectiveTestTypeId === t.id
                          ? 'bg-cyan-600 text-white border-cyan-400 shadow-sm'
                          : 'bg-slate-950 text-slate-300 border-slate-700 hover:bg-slate-800'
                      }`}
                    >
                      {t.name}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Applicant Summary Banner */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4 flex flex-wrap items-center justify-between gap-3">
          <div className="space-y-1">
            <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block">
              Applicant & Driving Class
            </span>
            <div className="text-sm font-bold text-white flex items-center gap-2">
              <span>{effectiveApp.fullName}</span>
              <span className="text-slate-400 font-normal">({effectiveApp.nationalNo})</span>
            </div>
            <div className="text-xs text-cyan-300 font-medium">
              {effectiveApp.licenseClassName || `Class ${effectiveApp.licenseClassId}`}
            </div>
          </div>

          <div className="flex items-center gap-3">
            {isPassed ? (
              <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-emerald-500/30 bg-emerald-950/30 text-xs font-bold text-emerald-300">
                <CheckCircle2 className="size-4" />
                <span>Test Already Passed</span>
              </div>
            ) : hasActiveAppointment ? (
              <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-cyan-500/30 bg-cyan-950/30 text-xs font-bold text-cyan-300">
                <Clock className="size-4" />
                <span>Appointment Currently Scheduled</span>
              </div>
            ) : canScheduleNew ? (
              <Button
                type="button"
                size="sm"
                className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold text-xs gap-1.5 shadow-sm"
                onClick={() => {
                  setShowScheduleForm((prev) => {
                    const next = !prev
                    if (next) {
                      setLoadingAvailability(true)
                      setAvailabilityError(null)
                    }
                    return next
                  })
                }}
              >
                <PlusCircle className="size-3.5" />
                <span>{showScheduleForm ? 'Close Form' : 'Schedule Appointment'}</span>
              </Button>
            ) : null}
          </div>
        </div>

        {error && (
          <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3.5 flex items-center gap-2.5 text-xs text-rose-300">
            <AlertCircle className="size-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {submitSuccess && (
          <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3.5 flex items-center gap-2.5 text-xs text-emerald-300">
            <CheckCircle2 className="size-4 shrink-0" />
            <span>Appointment successfully scheduled!</span>
          </div>
        )}

        {/* Schedule Form */}
        {showScheduleForm && canScheduleNew && (
          <form
            onSubmit={handleScheduleSubmit}
            className="rounded-xl border border-cyan-500/30 bg-slate-900/90 p-4 space-y-4 shadow-lg animate-in fade-in"
          >
            <div className="flex items-center justify-between border-b border-slate-800 pb-2">
              <span className="text-xs font-bold uppercase tracking-wider text-cyan-300 flex items-center gap-1.5">
                <Calendar className="size-4" />
                <span>Book New {meta.name}</span>
              </span>
              {schedulePrep && (
                <span className="text-xs font-mono font-medium text-slate-300">
                  Trial #{schedulePrep.trial}
                </span>
              )}
            </div>

            {availabilityError && (
              <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 flex items-center justify-between text-xs text-rose-300">
                <div className="flex items-center gap-2">
                  <AlertCircle className="size-4 shrink-0" />
                  <span>{availabilityError}</span>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={loadAvailability}
                  className="h-7 px-2.5 text-xs border-rose-500/30 text-rose-300 hover:bg-rose-500/20"
                >
                  Retry
                </Button>
              </div>
            )}

            {loadingAvailability ? (
              <div className="flex flex-col items-center justify-center py-8 gap-2.5 text-xs text-cyan-300">
                <LoadingSpinner size="sm" />
                <span>Checking available appointment dates and 40-minute slots…</span>
              </div>
            ) : availability && availability.availableDays.length === 0 ? (
              <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-4 text-center space-y-1">
                <div className="flex items-center justify-center gap-2 text-xs font-bold text-amber-300">
                  <AlertCircle className="size-4 shrink-0" />
                  <span>No Appointment Slots Available</span>
                </div>
                <p className="text-[11px] text-slate-300">
                  There are no open 40-minute slots returned by the DVLD system for this test type in the upcoming 4 weeks.
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Availability Date and Slot Selector */}
                <div className="space-y-3">
                  <div>
                    <label
                      htmlFor="appointment-date-select"
                      className="block text-xs font-medium text-slate-300 mb-1"
                    >
                      Available Date
                    </label>
                    <select
                      id="appointment-date-select"
                      value={selectedDate}
                      onChange={(e) => {
                        const newDate = e.target.value
                        setSelectedDate(newDate)
                        const day = availability?.availableDays.find((d) => d.date === newDate)
                        setSelectedSlot(day?.slots[0]?.startTime ?? '')
                      }}
                      className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-xs text-white focus:outline-none focus:border-cyan-500 cursor-pointer"
                    >
                      {availability?.availableDays.map((d) => (
                        <option key={d.date} value={d.date} className="bg-slate-900 text-white">
                          {formatAvailableDateLabel(d.date)} ({d.slots.length}{' '}
                          {d.slots.length === 1 ? 'slot' : 'slots'} available)
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <div className="flex items-center justify-between mb-1.5">
                      <label className="text-xs font-medium text-slate-300">
                        Available 40-Minute Time Slots
                      </label>
                      {currentDaySlots.length > 0 && (
                        <span className="text-[11px] font-mono text-slate-400">
                          {currentDaySlots.length} open
                        </span>
                      )}
                    </div>

                    {currentDaySlots.length === 0 ? (
                      <div className="text-xs text-slate-400 italic py-2">
                        No slots available on selected date.
                      </div>
                    ) : (
                      <div className="grid grid-cols-3 gap-1.5 max-h-40 overflow-y-auto pr-1">
                        {currentDaySlots.map((slot) => {
                          const isSelected = selectedSlot === slot.startTime
                          return (
                            <button
                              key={slot.startTime}
                              type="button"
                              onClick={() => setSelectedSlot(slot.startTime)}
                              className={`px-2.5 py-1.5 rounded-lg text-xs font-mono font-medium border transition-all text-center cursor-pointer ${
                                isSelected
                                  ? 'bg-cyan-600 text-white border-cyan-400 font-bold shadow-sm ring-1 ring-cyan-400/40'
                                  : 'bg-slate-950/80 hover:bg-slate-800 text-slate-300 border-slate-700 hover:border-slate-600'
                              }`}
                              title={`${slot.startTime} - ${slot.endTime}`}
                            >
                              {slot.startTime}
                            </button>
                          )
                        })}
                      </div>
                    )}

                    {selectedSlot && (
                      <div className="mt-2 text-[11px] text-cyan-300 flex items-center gap-1 font-medium bg-cyan-950/30 border border-cyan-500/20 rounded-md px-2 py-1">
                        <Clock className="size-3 shrink-0" />
                        <span>
                          Selected Slot: {selectedSlot} –{' '}
                          {currentDaySlots.find((s) => s.startTime === selectedSlot)?.endTime ?? ''}{' '}
                          (Asia/Amman)
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Fee Breakdown & Summary */}
                <div className="flex flex-col justify-between">
                  <div className="rounded-lg border border-slate-800 bg-slate-950 p-3 space-y-2 text-xs">
                    <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 block border-b border-slate-800/80 pb-1">
                      Applicable Fee Summary
                    </span>
                    <div className="flex justify-between text-slate-400">
                      <span>Exam Fee Snapshot:</span>
                      <span className="font-mono text-slate-200">
                        {(schedulePrep?.fees ?? 0).toFixed(2)} JOD
                      </span>
                    </div>
                    {Boolean(schedulePrep?.retakerFees) && (
                      <div className="flex justify-between text-slate-400">
                        <span>Retake Application Fee:</span>
                        <span className="font-mono text-amber-300">
                          {(schedulePrep?.retakerFees ?? 0).toFixed(2)} JOD
                        </span>
                      </div>
                    )}
                    <div className="flex justify-between font-bold text-white border-t border-slate-800 pt-1.5">
                      <span>Total Applicable Fee:</span>
                      <span className="font-mono text-cyan-300">
                        {totalFees.toFixed(2)} JOD
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div className="flex justify-end gap-2.5 pt-1 border-t border-slate-800">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setShowScheduleForm(false)}
                disabled={submitting}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                size="sm"
                disabled={
                  submitting ||
                  loadingAvailability ||
                  !selectedDate ||
                  !selectedSlot ||
                  Boolean(availability && availability.availableDays.length === 0)
                }
                className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold text-xs"
              >
                {submitting ? 'Booking…' : 'Confirm & Schedule'}
              </Button>
            </div>
          </form>
        )}

        {/* Existing Appointments Table */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3">
          <div className="flex items-center justify-between border-b border-[#1e3458] pb-2.5">
            <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400">
              <Clock className="size-4 text-cyan-400" />
              <span>Recorded Appointments ({appointments.length})</span>
            </div>
            {loading && <LoadingSpinner size="sm" />}
          </div>

          {appointments.length === 0 ? (
            <div className="p-6 text-center text-xs text-slate-400">
              No appointments scheduled for this test type yet.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs border-collapse">
                <thead>
                  <tr className="border-b border-slate-800 text-slate-400 font-semibold uppercase tracking-wider text-[11px]">
                    <th className="py-2.5 px-3">Appointment ID</th>
                    <th className="py-2.5 px-3">Date & Time</th>
                    <th className="py-2.5 px-3">Applicable Fee</th>
                    <th className="py-2.5 px-3">Lifecycle Status</th>
                    <th className="py-2.5 px-3">Result</th>
                    <th className="py-2.5 px-3">Recorded By</th>
                    <th className="py-2.5 px-3 text-right">Details</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/60 font-mono">
                  {appointments.map((apt) => {
                    const lifecycle = resolveAppointmentLifecycle(apt)
                    const isExpanded = expandedAppointmentId === apt.testAppointmentId
                    const testDetails = testDetailsMap[apt.testAppointmentId]
                    const isLoadingTest = loadingTestId === apt.testAppointmentId
                    const isCompleted = lifecycle.state.startsWith('Completed')

                    return (
                      <Fragment key={apt.testAppointmentId}>
                        <tr className="hover:bg-slate-900/50 transition-colors">
                          <td className="py-2.5 px-3">
                            <div className="flex items-center gap-1.5">
                              {onShowAppointmentDetails ? (
                                <button
                                  type="button"
                                  onClick={() => {
                                    onClose()
                                    onShowAppointmentDetails(apt.testAppointmentId)
                                  }}
                                  className="font-bold text-cyan-400 hover:text-cyan-300 hover:underline cursor-pointer transition-all"
                                  title="View dedicated appointment details page"
                                >
                                  #{apt.testAppointmentId}
                                </button>
                              ) : (
                                <span className="font-bold text-cyan-300">
                                  #{apt.testAppointmentId}
                                </span>
                              )}
                              {Boolean(apt.retakeTestApplicationId) && (
                                <span
                                  className="px-1.5 py-0.5 rounded text-[10px] font-mono font-semibold bg-amber-500/10 text-amber-300 border border-amber-500/30"
                                  title={`Retake Examination (Application #${apt.retakeTestApplicationId})`}
                                >
                                  Retake
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="py-2.5 px-3 font-sans text-slate-200">
                            {formatJordanAppointmentDateTime(
                              apt.appointmentDate,
                              apt.appointmentDateFormatted,
                            )}
                          </td>
                          <td className="py-2.5 px-3 text-slate-300">
                            {apt.paidFees.toFixed(2)} JOD
                          </td>
                          <td className="py-2.5 px-3 font-sans">
                            <StatusBadge
                              variant={lifecycle.statusVariant}
                              label={lifecycle.statusLabel}
                              title={lifecycle.description}
                            />
                          </td>
                          <td className="py-2.5 px-3 font-sans">
                            {lifecycle.resultLabel === 'Passed' ? (
                              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                                <CheckCircle2 className="size-3" />
                                <span>Passed</span>
                              </span>
                            ) : lifecycle.resultLabel === 'Failed' ? (
                              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">
                                <XCircle className="size-3" />
                                <span>Failed</span>
                              </span>
                            ) : lifecycle.resultLabel === 'No-Show' ? (
                              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-500/10 text-amber-300 border border-amber-500/20">
                                <AlertCircle className="size-3" />
                                <span>No-Show</span>
                              </span>
                            ) : lifecycle.resultLabel === 'Cancelled' ? (
                              <span className="inline-flex items-center gap-1 text-[11px] text-slate-400 font-medium">
                                <XCircle className="size-3 text-slate-500" />
                                <span>Cancelled</span>
                              </span>
                            ) : (
                              <span className="text-slate-400 font-medium text-[11px]">
                                Not Taken
                              </span>
                            )}
                          </td>
                          <td className="py-2.5 px-3 font-sans text-slate-400">
                            {apt.createdByUserName || 'Staff'}
                          </td>
                          <td className="py-2.5 px-3 font-sans text-right">
                            <div className="inline-flex items-center gap-1.5 justify-end">
                              {onShowAppointmentDetails && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    onClose()
                                    onShowAppointmentDetails(apt.testAppointmentId)
                                  }}
                                  className="inline-flex items-center gap-1 text-[11px] font-medium text-cyan-400 hover:text-cyan-300 py-1 px-2 rounded hover:bg-slate-800/80 transition-all cursor-pointer"
                                  title="View dedicated appointment details page"
                                >
                                  <Eye className="size-3" />
                                  <span>Details</span>
                                </button>
                              )}
                              {isCompleted ? (
                                <button
                                  type="button"
                                  onClick={() => handleToggleTestDetails(apt.testAppointmentId)}
                                  className="inline-flex items-center gap-1 text-[11px] font-medium text-cyan-400 hover:text-cyan-300 py-1 px-2 rounded hover:bg-slate-800/80 transition-all cursor-pointer"
                                  title="View examination record and examiner notes"
                                >
                                  <FileText className="size-3" />
                                  <span>{isExpanded ? 'Hide' : 'Notes'}</span>
                                  {isExpanded ? (
                                    <ChevronDown className="size-3" />
                                  ) : (
                                    <ChevronRight className="size-3" />
                                  )}
                                </button>
                              ) : !apt.isLocked && (apt.status === 'Scheduled' || apt.status === 'Open') ? (
                                <button
                                  type="button"
                                  onClick={() => setSelectedAppointmentForTakeTest(apt)}
                                  className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-400 hover:text-emerald-300 py-1 px-2.5 rounded-lg bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/30 transition-all cursor-pointer"
                                  title="Record official examination outcome"
                                >
                                  <Award className="size-3.5" />
                                  <span>Take Test</span>
                                </button>
                              ) : (
                                <span className="text-slate-600 text-xs">—</span>
                              )}
                            </div>
                          </td>
                        </tr>

                        {isExpanded && (
                          <tr className="bg-slate-950/80 border-b border-slate-800">
                            <td colSpan={7} className="py-2.5 px-4 font-sans">
                              <div className="rounded-lg border border-[#1e3458] bg-[#0c1830] p-3 space-y-1.5 shadow-inner">
                                <div className="flex items-center justify-between text-[11px] text-slate-400 border-b border-slate-800/80 pb-1.5">
                                  <span className="font-semibold text-cyan-300 flex items-center gap-1.5">
                                    <FileText className="size-3.5 text-cyan-400" />
                                    <span>
                                      Official Examination Record{' '}
                                      {testDetails?.testId
                                        ? `(Test Record #${testDetails.testId})`
                                        : ''}
                                    </span>
                                  </span>
                                  {testDetails?.createdByUserName && (
                                    <span className="text-slate-300">
                                      Examiner:{' '}
                                      <strong className="text-white">
                                        {testDetails.createdByUserName}
                                      </strong>
                                    </span>
                                  )}
                                </div>
                                {isLoadingTest ? (
                                  <div className="flex items-center gap-2 py-2 text-xs text-slate-400">
                                    <LoadingSpinner size="sm" />
                                    <span>Loading examination notes…</span>
                                  </div>
                                ) : (
                                  <div className="text-xs pt-1">
                                    <span className="text-slate-400 font-medium">
                                      Examiner Notes:{' '}
                                    </span>
                                    {testDetails?.notes ? (
                                      <span className="text-slate-200 font-sans italic">
                                        "{testDetails.notes}"
                                      </span>
                                    ) : (
                                      <span className="text-slate-500 italic">
                                        No notes were recorded for this test examination.
                                      </span>
                                    )}
                                  </div>
                                )}
                              </div>
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </ModalShell>

    {/* Take Test Modal */}
    {selectedAppointmentForTakeTest && (
      <TakeTestModal
        isOpen={selectedAppointmentForTakeTest !== null}
        onClose={() => setSelectedAppointmentForTakeTest(null)}
        appointment={selectedAppointmentForTakeTest}
        application={effectiveApp}
        accessToken={accessToken}
        onSuccess={async () => {
          setSelectedAppointmentForTakeTest(null)
          await loadData()
          onAppointmentScheduled?.()
        }}
      />
    )}
  </>
  )
}
