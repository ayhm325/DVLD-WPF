import { CheckCircle2 } from 'lucide-react'
import { Button } from '../ui/button'
import { formatAvailableDateLabel } from '../../utils/jordanTime'

export interface BookedAppointmentDetails {
  date: string
  time: string
  fee: number
  testName: string
  appId: number
  candidateName: string
}

export interface AppointmentReceiptCardProps {
  bookedDetails: BookedAppointmentDetails
  returnPage: string
  onBack: () => void
  onScheduleAnother: () => void
  onNavigate?: (page: string) => void
}

export default function AppointmentReceiptCard({
  bookedDetails,
  returnPage,
  onBack,
  onScheduleAnother,
  onNavigate,
}: AppointmentReceiptCardProps) {
  return (
    <div className="rounded-2xl border border-emerald-500/40 bg-emerald-950/30 p-5 space-y-3 shadow-lg animate-in fade-in">
      <div className="flex items-center gap-3 text-emerald-300 font-bold text-sm">
        <CheckCircle2 className="size-5 text-emerald-400 shrink-0" />
        <span>
          Appointment Successfully Scheduled for {bookedDetails.candidateName}!
        </span>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs text-slate-300 pt-1">
        <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
          <span className="text-slate-400 block text-[10px] uppercase">
            Examination Stage
          </span>
          <span className="font-semibold text-white">
            {bookedDetails.testName}
          </span>
        </div>
        <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
          <span className="text-slate-400 block text-[10px] uppercase">
            Date & Time (Asia/Amman)
          </span>
          <span className="font-semibold text-cyan-300 font-mono">
            {formatAvailableDateLabel(bookedDetails.date)} at {bookedDetails.time}
          </span>
        </div>
        <div className="bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
          <span className="text-slate-400 block text-[10px] uppercase">
            Applicable Fee
          </span>
          <span className="font-semibold text-emerald-400 font-mono">
            {bookedDetails.fee.toFixed(2)} JOD
          </span>
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2.5 pt-2">
        <Button
          type="button"
          size="sm"
          onClick={onBack}
          className="bg-emerald-600 hover:bg-emerald-500 text-white font-semibold text-xs rounded-xl cursor-pointer"
        >
          Return to {returnPage}
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onScheduleAnother}
          className="text-xs text-slate-300 hover:text-white border-slate-700 hover:bg-slate-800 rounded-xl cursor-pointer"
        >
          Schedule Another Slot
        </Button>
        {onNavigate && returnPage !== 'Test Appointments' && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onNavigate('Test Appointments')}
            className="text-xs text-cyan-300 hover:text-white border-cyan-700/60 hover:bg-cyan-950/40 rounded-xl cursor-pointer"
          >
            Go to Test Appointments
          </Button>
        )}
      </div>
    </div>
  )
}
