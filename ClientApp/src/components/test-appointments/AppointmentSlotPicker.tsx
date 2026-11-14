import { useMemo } from 'react'
import { Calendar, Clock, ShieldAlert, AlertCircle, Check } from 'lucide-react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../ui/card'
import { Button } from '../ui/button'
import EmptyState from '../common/EmptyState'
import LoadingSpinner from '../common/LoadingSpinner'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type {
  AppointmentAvailabilityResponse,
  TestAppointment,
} from '../../types/TestAppointment'
import {
  APPROVED_APPOINTMENT_SLOTS,
  formatAvailableDateLabel,
  formatJordanAppointmentDateTime,
} from '../../utils/jordanTime'

export interface AppointmentSlotPickerProps {
  application: LocalDrivingLicenseApplication | null
  isEligibleToSchedule: boolean
  isNewStatus: boolean
  isStageAlreadyPassed: boolean
  isPrerequisiteMet: boolean
  hasActiveAppointment: boolean
  activeAppointment?: TestAppointment | null
  currentStageName: string
  availability: AppointmentAvailabilityResponse | null
  loadingAvailability: boolean
  availabilityError: string | null
  selectedDate: string
  selectedSlot: string
  onSelectDate: (date: string) => void
  onSelectSlot: (slot: string) => void
  onRetryAvailability: () => void
}

export default function AppointmentSlotPicker({
  application,
  isEligibleToSchedule,
  isNewStatus,
  isStageAlreadyPassed,
  isPrerequisiteMet,
  hasActiveAppointment,
  activeAppointment,
  currentStageName,
  availability,
  loadingAvailability,
  availabilityError,
  selectedDate,
  selectedSlot,
  onSelectDate,
  onSelectSlot,
  onRetryAvailability,
}: AppointmentSlotPickerProps) {
  const currentDaySlots = useMemo(() => {
    if (!availability || !selectedDate) return []
    const day = availability.availableDays.find((d) => d.date === selectedDate)
    return day?.slots ?? []
  }, [availability, selectedDate])

  return (
    <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
      <CardHeader className="border-b border-[#1e3458] px-4 py-4 sm:px-6 sm:py-5">
        <div className="flex min-w-0 flex-wrap items-center justify-between gap-3">
          <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
            <Calendar className="size-4.5 text-cyan-400" />
            <span>Facility Available Dates & Slots</span>
          </CardTitle>
          {availability && (
            <span className="text-[11px] text-cyan-300 font-mono font-semibold px-2.5 py-1 rounded-full bg-cyan-950/80 border border-cyan-500/30">
              {availability.availableDays.length} working days open
            </span>
          )}
        </div>
        <CardDescription className="text-xs text-slate-400 mt-1">
          Official Jordan facility calendar (Sunday–Thursday, 08:00–16:00). Minimum booking lead time is 2 calendar days.
        </CardDescription>
      </CardHeader>

      <CardContent className="p-4 sm:p-6 space-y-5">
        {!application ? (
          <EmptyState
            message="No Application Selected"
            description="Please select an eligible local driving license application on the left to inspect facility appointment availability."
            icon={<Calendar className="size-8 text-slate-500" />}
          />
        ) : !isEligibleToSchedule ? (
          <div className="rounded-xl border border-slate-800 bg-slate-950/70 p-6 text-center space-y-2.5">
            <div className="flex items-center justify-center gap-2 text-sm font-bold text-amber-300">
              <ShieldAlert className="size-5 shrink-0" />
              <span>Examination Scheduling Locked</span>
            </div>
            <p className="text-xs text-slate-300 max-w-md mx-auto leading-relaxed">
              {!isNewStatus
                ? `Application status is "${application.statusText || application.applicationStatus}". Only New applications may be scheduled.`
                : isStageAlreadyPassed
                  ? `${currentStageName} has already been passed.`
                  : !isPrerequisiteMet
                    ? `Prerequisite examination stage must be completed before ${currentStageName}.`
                    : hasActiveAppointment
                      ? `An active appointment is currently scheduled on ${formatJordanAppointmentDateTime(activeAppointment?.appointmentDate)}.`
                      : 'Please review application requirements.'}
            </p>
          </div>
        ) : availabilityError ? (
          <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-4.5 space-y-2 text-xs text-rose-300">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 font-semibold">
                <AlertCircle className="size-4.5 shrink-0" />
                <span>Availability Query: {availabilityError}</span>
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onRetryAvailability}
                className="h-8 text-xs border-rose-500/40 hover:bg-rose-500/20 rounded-lg cursor-pointer"
              >
                Retry
              </Button>
            </div>
          </div>
        ) : loadingAvailability ? (
          <div className="flex flex-col items-center justify-center py-12 gap-3 text-cyan-300 text-xs">
            <LoadingSpinner size="md" />
            <span>Checking real-time facility availability (Asia/Amman)…</span>
          </div>
        ) : availability && availability.availableDays.length === 0 ? (
          <EmptyState
            message="No Open Slots"
            description="There are currently no available 40-minute slots returned by the licensing department for this test stage in the next 30 calendar days."
            icon={<Calendar className="size-8 text-amber-400" />}
          />
        ) : (
          <>
            {/* Available Dates: Interactive Calendar Cards Grid (DEF-04) */}
            <div className="space-y-2.5">
              <div className="flex items-center justify-between">
                <label className="block text-xs font-semibold text-slate-300">
                  Available Facility Dates ({availability?.availableDays.length ?? 0} Working Days)
                </label>
                {selectedDate && (
                  <span className="text-[11px] font-mono text-cyan-300 font-semibold">
                    Selected: {formatAvailableDateLabel(selectedDate)}
                  </span>
                )}
              </div>

              {/* Interactive Grid of Date Cards */}
              <div className="grid min-w-0 grid-cols-1 min-[420px]:grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2.5 max-h-56 overflow-y-auto pr-1">
                {availability?.availableDays.map((d) => {
                  const isSelected = selectedDate === d.date
                  const [yyyy, mm, dd] = d.date.split('-')
                  const dateObj = new Date(Date.UTC(Number(yyyy), Number(mm) - 1, Number(dd)))
                  const weekdayStr = dateObj.toLocaleDateString('en-US', {
                    weekday: 'short',
                    timeZone: 'UTC',
                  })
                  const monthDayStr = dateObj.toLocaleDateString('en-US', {
                    month: 'short',
                    day: 'numeric',
                    timeZone: 'UTC',
                  })

                  return (
                    <button
                      key={d.date}
                      type="button"
                      onClick={() => {
                        onSelectDate(d.date)
                        const day = availability.availableDays.find(
                          (x) => x.date === d.date,
                        )
                        onSelectSlot(day?.slots[0]?.startTime ?? '')
                      }}
                      className={`min-w-0 p-3 rounded-xl border text-left transition-all cursor-pointer flex flex-col justify-between [overflow-wrap:anywhere] ${
                        isSelected
                          ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/50'
                          : 'bg-slate-900/90 text-slate-200 border-slate-700 hover:border-cyan-500/60 hover:bg-slate-850'
                      }`}
                    >
                      <div className="flex items-center justify-between w-full">
                        <span
                          className={`text-[11px] font-bold uppercase tracking-wider ${
                            isSelected ? 'text-white' : 'text-cyan-400'
                          }`}
                        >
                          {weekdayStr}
                        </span>
                        {isSelected && <Check className="size-3.5" />}
                      </div>
                      <div className="text-xs font-semibold mt-1">
                        {monthDayStr}
                      </div>
                      <div
                        className={`text-[10px] mt-1.5 font-mono ${
                          isSelected ? 'text-cyan-100' : 'text-slate-400'
                        }`}
                      >
                        {d.slots.length} {d.slots.length === 1 ? 'slot' : 'slots'} open
                      </div>
                    </button>
                  )
                })}
              </div>
            </div>

            {/* 12 Predefined 40-minute Time Slots Grid */}
            <div className="space-y-3 pt-3 border-t border-slate-800">
              <div className="flex items-center justify-between">
                <label className="text-xs font-semibold text-slate-300">
                  Available 40-Minute Time Slots for{' '}
                  <span className="text-cyan-300">
                    {formatAvailableDateLabel(selectedDate)}
                  </span>
                </label>
                <span className="text-[11px] text-slate-400 font-mono">
                  {currentDaySlots.length} open
                </span>
              </div>

              <div className="grid min-w-0 grid-cols-1 min-[420px]:grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2.5">
                {APPROVED_APPOINTMENT_SLOTS.map((slotTime) => {
                  const isAvailable = currentDaySlots.some(
                    (s) => s.startTime === slotTime,
                  )
                  const isSelected = selectedSlot === slotTime
                  const slotObj = currentDaySlots.find(
                    (s) => s.startTime === slotTime,
                  )

                  return (
                    <button
                      key={slotTime}
                      type="button"
                      disabled={!isAvailable || !isEligibleToSchedule}
                      onClick={() => onSelectSlot(slotTime)}
                      className={`min-w-0 px-3.5 py-2.5 rounded-xl text-xs font-mono font-semibold border transition-all text-center break-words ${
                        isSelected
                          ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/50'
                          : isAvailable && isEligibleToSchedule
                            ? 'bg-slate-900/90 text-cyan-300 border-slate-700 hover:border-cyan-500 hover:bg-slate-800 cursor-pointer'
                            : 'bg-slate-950/40 text-slate-600 border-slate-850 opacity-40 cursor-not-allowed'
                      }`}
                      title={
                        slotObj
                          ? `${slotObj.startTime} - ${slotObj.endTime} (Available)`
                          : `${slotTime} (Unavailable or Booked)`
                      }
                    >
                      <div>{slotTime}</div>
                      <div className="text-[10px] font-sans font-normal opacity-80 mt-0.5">
                        {isAvailable ? 'Open' : 'Unavailable'}
                      </div>
                    </button>
                  )
                })}
              </div>

              {selectedSlot && (
                <div className="mt-3 p-4 rounded-xl bg-cyan-950/40 border border-cyan-500/30 flex flex-wrap items-center justify-between gap-3 text-xs text-cyan-300">
                  <div className="flex items-center gap-2.5">
                    <Clock className="size-4.5 text-cyan-400 shrink-0" />
                    <span>
                      Selected: <strong>{selectedSlot}</strong> –{' '}
                      {currentDaySlots.find(
                        (s) => s.startTime === selectedSlot,
                      )?.endTime ?? ''}{' '}
                      on {formatAvailableDateLabel(selectedDate)}
                    </span>
                  </div>
                  <span className="font-mono text-[11px] text-cyan-400 uppercase font-semibold">
                    Asia/Amman
                  </span>
                </div>
              )}
            </div>
          </>
        )}
      </CardContent>
    </Card>
  )
}
