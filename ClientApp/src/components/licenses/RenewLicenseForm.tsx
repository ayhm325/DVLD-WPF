import {
  IdCard,
  ShieldAlert,
  AlertTriangle,
  Info,
  Loader2,
  RotateCw,
} from 'lucide-react'
import type { LocalLicenseInfo } from '../../types/License'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { cn } from '@/lib/utils'
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  Label,
  Textarea,
} from '../ui'

export interface RenewLicenseFormProps {
  license: LocalLicenseInfo
  isExpired: boolean
  isEligible: boolean
  isDetained: boolean
  ineligibilityReason: string | null
  canSubmit: boolean
  applicationFee: number
  totalFees: number
  notes: string
  onNotesChange: (notes: string) => void
  submitting: boolean
  onClearSelection: () => void
  onCancel: () => void
  onSubmit: () => void
}

export default function RenewLicenseForm({
  license,
  isExpired,
  isEligible,
  isDetained,
  ineligibilityReason,
  canSubmit,
  applicationFee,
  totalFees,
  notes,
  onNotesChange,
  submitting,
  onClearSelection,
  onCancel,
  onSubmit,
}: RenewLicenseFormProps) {
  return (
    <Card className="border-[#0090ff]/50 bg-[#0c192e] text-white shadow-2xl rounded-2xl overflow-hidden ring-1 ring-cyan-500/30">
      <CardHeader className="border-b border-[#1b2f52]/80 bg-[#09152b] p-5">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <CardTitle className="text-base font-bold flex items-center gap-2">
              <IdCard className="size-5 text-cyan-400" />
              <span>Selected License for Renewal: #{license.licenseId}</span>
            </CardTitle>
            <CardDescription className="text-xs text-slate-400 mt-0.5">
              Review original license credentials and confirm renewal transaction.
            </CardDescription>
          </div>

          {/* Status Badges */}
          <div className="flex items-center gap-2 shrink-0">
            <span
              className={cn(
                'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                license.isActive
                  ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                  : 'bg-rose-500/15 border-rose-500/30 text-rose-300',
              )}
            >
              <span
                className={cn(
                  'size-1.5 rounded-full',
                  license.isActive ? 'bg-emerald-400' : 'bg-rose-400',
                )}
              />
              <span>{license.isActive ? 'Active' : 'Inactive'}</span>
            </span>

            <span
              className={cn(
                'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                isExpired
                  ? 'bg-amber-500/15 border-amber-500/30 text-amber-300'
                  : 'bg-cyan-500/15 border-cyan-500/30 text-cyan-300',
              )}
            >
              <span>{isExpired ? 'Expired' : 'Valid (Not Expired)'}</span>
            </span>

            <span
              className={cn(
                'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border',
                isEligible
                  ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                  : 'bg-rose-500/15 border-rose-500/30 text-rose-300',
              )}
            >
              <span
                className={cn(
                  'size-1.5 rounded-full',
                  isEligible ? 'bg-emerald-400' : 'bg-rose-400',
                )}
              />
              <span>{isEligible ? 'Eligible for Renewal' : 'Ineligible'}</span>
            </span>

            {isDetained && (
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border bg-rose-500/15 border-rose-500/30 text-rose-300">
                <ShieldAlert className="size-3 text-rose-400" />
                <span>Detained</span>
              </span>
            )}
          </div>
        </div>
      </CardHeader>

      <div className="p-6 space-y-6">
        {/* Ineligibility Warning */}
        {ineligibilityReason && (
          <div className="flex items-start gap-3 p-4 rounded-xl border border-amber-500/30 bg-amber-500/10 text-amber-200">
            <AlertTriangle className="size-5 text-amber-400 shrink-0 mt-0.5" />
            <div className="space-y-1">
              <h4 className="text-sm font-bold text-white">Renewal Ineligible</h4>
              <p className="text-xs text-amber-300/90 leading-relaxed">{ineligibilityReason}</p>
            </div>
          </div>
        )}

        {/* Owner Header Info */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4 p-4 rounded-xl bg-[#081326] border border-[#1b2f52]">
          <div className="size-14 rounded-xl overflow-hidden bg-[#0c192e] border border-[#1b2f52] flex items-center justify-center shrink-0">
            {license.imagePath ? (
              <img
                src={license.imagePath}
                alt={license.fullName}
                className="size-full object-cover"
              />
            ) : (
              <IdCard className="size-8 text-slate-500" />
            )}
          </div>
          <div className="space-y-1 flex-1">
            <h3 className="text-base font-bold text-white">{license.fullName}</h3>
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-300">
              <span>
                National No: <strong className="text-white">{license.nationalNo}</strong>
              </span>
              <span>·</span>
              <span>
                Driver ID: <strong className="text-white font-mono">#{license.driverId}</strong>
              </span>
              <span>·</span>
              <span>
                Class: <strong className="text-cyan-300">{license.licenseClass}</strong>
              </span>
            </div>
          </div>
        </div>

        {/* Attributes Grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
            <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
              License ID
            </span>
            <strong className="text-sm font-bold text-white font-mono block">
              #{license.licenseId}
            </strong>
          </div>

          <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
            <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
              Issue Date
            </span>
            <strong className="text-sm font-bold text-slate-200 block">
              {formatDate(license.issueDate)}
            </strong>
          </div>

          <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
            <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
              Expiration Date
            </span>
            <strong className="text-sm font-bold text-amber-300 block">
              {formatDate(license.expirationDate)}
            </strong>
          </div>

          <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-1">
            <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
              License Class Fee
            </span>
            <strong className="text-sm font-bold text-emerald-400 block">
              {formatCurrency(license.licenseClassFees)}
            </strong>
          </div>
        </div>

        {/* Financial & Fee Breakdown */}
        <div className="p-4 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-3">
          <div className="flex items-center gap-2 text-xs font-bold text-slate-300">
            <Info className="size-4 text-cyan-400" />
            <span>Renewal Fee Breakdown</span>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
            <div className="p-3 rounded-lg bg-[#0c192e] border border-[#1b2f52] flex justify-between items-center">
              <span className="text-slate-400">Application Fee:</span>
              <span className="font-bold text-white">
                {formatCurrency(applicationFee)}
              </span>
            </div>
            <div className="p-3 rounded-lg bg-[#0c192e] border border-[#1b2f52] flex justify-between items-center">
              <span className="text-slate-400">Class Validity Fee:</span>
              <span className="font-bold text-white">
                {formatCurrency(license.licenseClassFees)}
              </span>
            </div>
            <div className="p-3 rounded-lg bg-[#0e2447] border border-cyan-500/30 flex justify-between items-center">
              <span className="text-cyan-300 font-semibold">Total Payable:</span>
              <span className="font-extrabold text-cyan-200 text-sm">
                {formatCurrency(totalFees)}
              </span>
            </div>
          </div>
        </div>

        {/* Optional Notes */}
        <div className="space-y-1.5">
          <Label htmlFor="notes" className="text-xs text-slate-300">
            Renewal Notes (Optional)
          </Label>
          <Textarea
            id="notes"
            placeholder="Enter any administrative notes or remarks regarding this renewal..."
            value={notes}
            onChange={(e) => onNotesChange(e.target.value)}
            disabled={!canSubmit}
            className="bg-[#081326] border-[#1b2f52] text-white min-h-[70px] text-xs focus:border-cyan-500"
          />
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between gap-3 pt-2">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onClearSelection}
            disabled={submitting}
            className="text-slate-400 hover:text-white text-xs cursor-pointer"
          >
            Clear Selection
          </Button>

          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onCancel}
              disabled={submitting}
              className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white"
            >
              Cancel
            </Button>
            <Button
              type="button"
              onClick={onSubmit}
              disabled={!canSubmit}
              className="bg-cyan-600 hover:bg-cyan-500 text-white font-semibold rounded-xl h-10 px-6 gap-2 cursor-pointer transition-all shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {submitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  <span>Processing Renewal...</span>
                </>
              ) : (
                <>
                  <RotateCw className="size-4" />
                  <span>Renew License</span>
                </>
              )}
            </Button>
          </div>
        </div>
      </div>
    </Card>
  )
}
