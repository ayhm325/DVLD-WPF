import {
  Award,
  Check,
  AlertCircle,
  CheckCircle2,
  Clock,
} from 'lucide-react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../ui/card'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type { TestAppointment } from '../../types/TestAppointment'
import { formatJordanAppointmentDateTime } from '../../utils/jordanTime'
import { STAGE_CONFIG } from './stageConfig'

export interface AppointmentStageStepperProps {
  selectedTestTypeId: 1 | 2 | 3
  onSelectStage: (stageId: 1 | 2 | 3) => void
  passedCount: number
  trialNumber?: number
  application?: LocalDrivingLicenseApplication | null
  isNewStatus: boolean
  isStageAlreadyPassed: boolean
  isPrerequisiteMet: boolean
  hasActiveAppointment: boolean
  activeAppointment?: TestAppointment | null
  onShowAppointmentDetails?: (appointmentId: number) => void
}

export default function AppointmentStageStepper({
  selectedTestTypeId,
  onSelectStage,
  passedCount,
  trialNumber,
  application,
  isNewStatus,
  isStageAlreadyPassed,
  isPrerequisiteMet,
  hasActiveAppointment,
  activeAppointment,
  onShowAppointmentDetails,
}: AppointmentStageStepperProps) {
  const currentStageConfig = STAGE_CONFIG[selectedTestTypeId]

  return (
    <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
      <CardHeader className="border-b border-[#1e3458] px-6 py-5">
        <div className="flex items-center justify-between gap-3">
          <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
            <Award className="size-4.5 text-cyan-400" />
            <span>Examination Stage</span>
          </CardTitle>
          {trialNumber !== undefined && (
            <span className="px-2.5 py-1 rounded-full text-[11px] font-mono font-semibold bg-cyan-950/80 text-cyan-300 border border-cyan-500/30">
              Trial #{trialNumber}
            </span>
          )}
        </div>
        <CardDescription className="text-xs text-slate-400 mt-1">
          Select the required examination stage in sequential order.
        </CardDescription>
      </CardHeader>

      <CardContent className="p-4 sm:p-6 space-y-4">
        <div className="grid min-w-0 grid-cols-1 min-[420px]:grid-cols-2 sm:grid-cols-3 gap-3">
          {([1, 2, 3] as const).map((stageId) => {
            const cfg = STAGE_CONFIG[stageId]
            const StageIcon = cfg.icon
            const isSelected = selectedTestTypeId === stageId
            const isPassed = passedCount >= stageId
            const isPrereqMet = passedCount >= cfg.requiredPasses

            return (
              <button
                key={stageId}
                type="button"
                onClick={() => onSelectStage(stageId)}
                className={`flex min-w-0 flex-col items-center justify-center p-3 sm:p-3.5 rounded-xl border text-center transition-all cursor-pointer ${
                  isSelected
                    ? 'bg-cyan-600 text-white border-cyan-400 shadow-md ring-2 ring-cyan-400/40'
                    : isPassed
                      ? 'bg-emerald-950/20 text-emerald-300 border-emerald-500/30 hover:bg-emerald-950/40'
                      : isPrereqMet
                        ? 'bg-slate-900/80 text-slate-300 border-slate-700 hover:border-slate-500'
                        : 'bg-slate-950/60 text-slate-500 border-slate-800 opacity-60 cursor-not-allowed'
                }`}
              >
                <StageIcon className="size-5 mb-1.5" />
                <span className="text-xs font-bold leading-tight">
                  {cfg.shortName}
                </span>
                <span className="text-[10px] mt-1">
                  {isPassed ? (
                    <span className="text-emerald-400 font-semibold flex items-center gap-0.5">
                      <Check className="size-3" /> Passed
                    </span>
                  ) : !isPrereqMet ? (
                    'Prereq Req.'
                  ) : (
                    'Ready'
                  )}
                </span>
              </button>
            )
          })}
        </div>

        {/* Eligibility Feedback Alerts */}
        {application && !isNewStatus && (
          <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3.5 text-xs text-rose-300 flex items-center gap-2.5">
            <AlertCircle className="size-4.5 shrink-0" />
            <span>
              Application status is "{application.statusText || application.applicationStatus}". Only applications with status "New" can schedule examinations.
            </span>
          </div>
        )}

        {application && isStageAlreadyPassed && (
          <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3.5 text-xs text-emerald-300 flex items-center gap-2.5">
            <CheckCircle2 className="size-4.5 shrink-0" />
            <span>
              {currentStageConfig.name} has already been passed for this application.
            </span>
          </div>
        )}

        {application && !isPrerequisiteMet && (
          <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3.5 text-xs text-amber-300 flex items-center gap-2.5">
            <AlertCircle className="size-4.5 shrink-0" />
            <span>
              Sequential examination rule: Candidate must pass previous stage(s) before scheduling {currentStageConfig.name}.
            </span>
          </div>
        )}

        {hasActiveAppointment && activeAppointment && (
          <div className="rounded-xl border border-cyan-500/30 bg-cyan-950/40 p-4 text-xs text-cyan-300 space-y-1.5">
            <div className="flex items-center gap-2 font-bold">
              <Clock className="size-4.5 shrink-0 text-cyan-400" />
              <span>Active Appointment Already Scheduled</span>
            </div>
            <p className="text-[11px] text-slate-300 leading-relaxed">
              Appointment #{activeAppointment.testAppointmentId} is scheduled on{' '}
              {formatJordanAppointmentDateTime(
                activeAppointment.appointmentDate,
                activeAppointment.appointmentDateFormatted,
              )}.
            </p>
            {onShowAppointmentDetails && (
              <button
                type="button"
                onClick={() =>
                  onShowAppointmentDetails(activeAppointment.testAppointmentId)
                }
                className="text-[11px] font-semibold text-cyan-400 hover:underline pt-1 block cursor-pointer"
              >
                View Active Appointment Details →
              </button>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
