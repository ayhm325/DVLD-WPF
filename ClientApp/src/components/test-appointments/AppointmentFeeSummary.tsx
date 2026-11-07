import { DollarSign } from 'lucide-react'
import LoadingSpinner from '../common/LoadingSpinner'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../ui/card'

export interface AppointmentFeeSummaryProps {
  loadingPrep: boolean
  hasApplication: boolean
  standardFee?: number
  retakeFee?: number
  trialNumber?: number
  totalFees: number
}

export default function AppointmentFeeSummary({
  loadingPrep,
  hasApplication,
  standardFee = 0,
  retakeFee = 0,
  trialNumber = 1,
  totalFees,
}: AppointmentFeeSummaryProps) {
  return (
    <Card className="border-[#1e3458] bg-[#0c1830] rounded-2xl shadow-md overflow-hidden">
      <CardHeader className="border-b border-[#1e3458] px-6 py-5">
        <CardTitle className="text-base font-bold text-white flex items-center gap-2.5">
          <DollarSign className="size-4.5 text-emerald-400" />
          <span>Applicable Fee Breakdown</span>
        </CardTitle>
        <CardDescription className="text-xs text-slate-400 mt-1">
          Official fee schedule retrieved directly from DVLD backend.
        </CardDescription>
      </CardHeader>

      <CardContent className="p-6 space-y-3 text-xs">
        {loadingPrep ? (
          <div className="flex items-center gap-2.5 py-4 text-slate-400">
            <LoadingSpinner size="sm" />
            <span>Retrieving official fee quote…</span>
          </div>
        ) : !hasApplication ? (
          <div className="text-xs text-slate-400 py-3 italic">
            Fee quote will appear once an application is selected.
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between text-slate-300 py-1.5">
              <span>Standard Examination Fee:</span>
              <span className="font-mono font-semibold text-slate-100">
                {standardFee.toFixed(2)} JOD
              </span>
            </div>

            {Boolean(retakeFee) && (
              <div className="flex items-center justify-between text-amber-300 py-1.5 border-t border-slate-800">
                <span>Retake Application Fee (Trial #{trialNumber}):</span>
                <span className="font-mono font-semibold">
                  {retakeFee.toFixed(2)} JOD
                </span>
              </div>
            )}

            <div className="flex items-center justify-between py-3 border-t border-slate-700 font-bold text-sm">
              <span className="text-white">Total Applicable Fee:</span>
              <span className="text-cyan-400 font-mono text-base">
                {totalFees.toFixed(2)} JOD
              </span>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  )
}
