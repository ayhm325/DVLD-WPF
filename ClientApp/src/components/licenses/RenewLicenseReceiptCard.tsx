import { CheckCircle2, ArrowLeft, RotateCcw } from 'lucide-react'
import { formatCurrency } from '../../utils/formatters'
import { Button } from '../ui'

export interface RenewLicenseReceiptCardProps {
  oldLicenseId: number
  newLicenseId: number
  message: string
  driverName?: string
  licenseClassName?: string
  totalFees?: number
  onBack: () => void
  onRenewAnother?: () => void
}

export default function RenewLicenseReceiptCard({
  oldLicenseId,
  newLicenseId,
  message,
  driverName,
  licenseClassName,
  totalFees,
  onBack,
  onRenewAnother,
}: RenewLicenseReceiptCardProps) {
  return (
    <div className="p-5 rounded-2xl border border-emerald-500/40 bg-emerald-950/30 text-emerald-200 shadow-xl space-y-4 animate-in fade-in duration-200">
      <div className="flex items-start gap-3">
        <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
        <div className="space-y-1 flex-1">
          <h4 className="text-sm font-bold text-white">Renewal Completed</h4>
          <p className="text-xs text-emerald-300/90 leading-relaxed">{message}</p>
        </div>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs pt-1">
        <div className="p-3 rounded-xl bg-slate-900/80 border border-slate-800 space-y-0.5">
          <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">
            Previous License
          </span>
          <span className="font-semibold text-white font-mono">#{oldLicenseId}</span>
        </div>

        <div className="p-3 rounded-xl bg-slate-900/80 border border-slate-800 space-y-0.5">
          <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">
            New License ID
          </span>
          <span className="font-semibold text-emerald-300 font-mono">#{newLicenseId}</span>
        </div>

        {driverName && (
          <div className="p-3 rounded-xl bg-slate-900/80 border border-slate-800 space-y-0.5">
            <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">
              Driver
            </span>
            <span className="font-semibold text-white truncate block">{driverName}</span>
          </div>
        )}

        {licenseClassName && (
          <div className="p-3 rounded-xl bg-slate-900/80 border border-slate-800 space-y-0.5">
            <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">
              Class
            </span>
            <span className="font-semibold text-cyan-300 truncate block">{licenseClassName}</span>
          </div>
        )}

        {totalFees !== undefined && totalFees > 0 && (
          <div className="p-3 rounded-xl bg-slate-900/80 border border-slate-800 space-y-0.5">
            <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">
              Total Fees
            </span>
            <span className="font-semibold text-emerald-400 font-mono">
              {formatCurrency(totalFees)}
            </span>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-2.5 pt-1">
        <Button
          type="button"
          size="sm"
          onClick={onBack}
          className="h-8.5 px-4 bg-emerald-600 hover:bg-emerald-500 text-white font-semibold text-xs rounded-xl cursor-pointer transition-all shadow-md gap-1.5"
        >
          <ArrowLeft className="size-3.5" />
          <span>Back to Licenses</span>
        </Button>

        {onRenewAnother && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onRenewAnother}
            className="h-8.5 px-4 text-xs text-slate-300 hover:text-white border-slate-700 hover:bg-slate-800 rounded-xl cursor-pointer transition-all gap-1.5"
          >
            <RotateCcw className="size-3.5" />
            <span>Renew Another License</span>
          </Button>
        )}
      </div>
    </div>
  )
}
