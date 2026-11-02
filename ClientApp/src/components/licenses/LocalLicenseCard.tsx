import { useState } from 'react'
import {
  IdCard,
  Calendar,
  Clock,
  ShieldCheck,
  ChevronDown,
  ChevronUp,
  FileText,
  User,
  CreditCard,
  Hash,
  AlertCircle,
} from 'lucide-react'
import type { License } from '../../types/License'
import type { Person, PersonDetails } from '../../types/Person'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import { getActiveStatusProps } from '../../utils/status'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { Button } from '../ui/button'

export interface LocalLicenseCardProps {
  license: License
  person: PersonDetails | Person
  accessToken?: string
}

export default function LocalLicenseCard({
  license,
  person,
  accessToken,
}: LocalLicenseCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)

  const isExpired = !license.isActive
  const statusProps = getActiveStatusProps(license.isActive)

  const issueReasonLabel =
    license.issueReasonText ||
    (license.issueReason === 1
      ? 'First Time'
      : license.issueReason === 2
        ? 'Renew'
        : license.issueReason === 3
          ? 'Replacement for Damaged'
          : license.issueReason === 4
            ? 'Replacement for Lost'
            : `Reason #${license.issueReason}`)

  return (
    <div
      className={`group relative flex flex-col rounded-2xl bg-gradient-to-br from-[#0c1830] via-[#0f2142] to-[#0a152b] border transition-all duration-300 shadow-xl overflow-hidden ${
        license.isActive
          ? 'border-[#203a66] hover:border-cyan-500/50 hover:shadow-[0_8px_30px_rgba(34,211,238,0.15)]'
          : 'border-[#233148] hover:border-slate-500/40 opacity-90'
      }`}
    >
      {/* Decorative top security stripe inspired by official biometric driver license cards */}
      <div className="relative px-5 py-3 border-b border-[#1b3154] bg-[#081224]/80 backdrop-blur-sm flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 min-w-0">
          <div className="flex size-7 items-center justify-center rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <ShieldCheck className="size-4" />
          </div>
          <div className="min-w-0">
            <span className="block text-[10px] font-extrabold uppercase tracking-widest text-cyan-400 truncate">
              Hashemite Kingdom of Jordan
            </span>
            <span className="block text-[11px] font-semibold text-slate-300 truncate">
              Official Driving License
            </span>
          </div>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <StatusBadge
            variant={statusProps.variant}
            label={statusProps.label}
          />
        </div>
      </div>

      {/* Main card body */}
      <div className="p-5 flex-1 space-y-4">
        {/* License Number & Class Heading */}
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#1a2d4c] pb-3">
          <div>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">
              License Class
            </span>
            <h4 className="text-sm font-bold text-white tracking-tight">
              {license.licenseClassName ?? '—'}
            </h4>
          </div>

          <div className="text-right">
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">
              License ID
            </span>
            <span className="font-mono text-base font-extrabold text-cyan-300 drop-shadow-[0_0_8px_rgba(34,211,238,0.3)]">
              #{license.licenseId}
            </span>
          </div>
        </div>

        {/* Card profile section */}
        <div className="flex items-start gap-4">
          <div className="relative shrink-0">
            <PersonPhoto
              personId={person.personId}
              photoKey={person.imagePath}
              hasPhoto={Boolean(person.imagePath)}
              fullName={person.fullName}
              size="lg"
              accessToken={accessToken}
              className="border-2 border-cyan-400/40 shadow-[0_0_15px_rgba(34,211,238,0.2)] rounded-xl"
            />
            <span className="mt-1.5 block text-center font-mono text-[10px] font-semibold px-1.5 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
              Driver #{license.driverId}
            </span>
          </div>

          <div className="flex-1 min-w-0 space-y-2 text-xs">
            <div>
              <span className="text-[10px] text-slate-400 block">Driver Name</span>
              <strong className="text-white text-sm font-bold truncate block">
                {license.driverName || person.fullName}
              </strong>
            </div>

            <div className="grid grid-cols-2 gap-2 pt-1 border-t border-[#1a2d4c]">
              <div>
                <span className="text-[10px] text-slate-400 block">National No.</span>
                <span className="font-mono font-semibold text-slate-200">
                  {person.nationalNo}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-slate-400 block">Application ID</span>
                <span className="font-mono text-slate-300">
                  #{license.applicationId}
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <span className="text-[10px] text-slate-400 flex items-center gap-1">
                  <Calendar className="size-3 text-cyan-400" />
                  Issue Date
                </span>
                <span className="font-mono text-slate-200">
                  {formatDate(license.issueDate)}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-slate-400 flex items-center gap-1">
                  <Clock className={`size-3 ${isExpired ? 'text-rose-400' : 'text-emerald-400'}`} />
                  Expiration
                </span>
                <span className={`font-mono font-semibold ${isExpired ? 'text-rose-300' : 'text-slate-200'}`}>
                  {formatDate(license.expirationDate)}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Quick pill metadata */}
        <div className="flex flex-wrap items-center gap-2 pt-1 text-[11px]">
          <span className="px-2 py-0.5 rounded-md bg-[#13233e] text-slate-300 border border-[#203658]">
            Reason: <strong className="text-white">{issueReasonLabel}</strong>
          </span>

          {license.paidFees > 0 && (
            <span className="px-2 py-0.5 rounded-md bg-[#13233e] text-slate-300 border border-[#203658]">
              Fees: <strong className="text-emerald-400">{formatCurrency(license.paidFees, 'JOD')}</strong>
            </span>
          )}

          {license.notes && (
            <span className="px-2 py-0.5 rounded-md bg-[#13233e] text-slate-300 border border-[#203658] truncate max-w-[200px]" title={license.notes}>
              Notes: {license.notes}
            </span>
          )}
        </div>
      </div>

      {/* Expandable full details drawer */}
      {isExpanded && (
        <div className="border-t border-[#1b3154] bg-[#071120]/95 p-5 space-y-3.5 text-xs animate-in fade-in slide-in-from-top-2 duration-200">
          <div className="flex items-center gap-2 pb-2 border-b border-[#182944] text-cyan-400 font-semibold uppercase tracking-wider text-[11px]">
            <FileText className="size-3.5" />
            <span>Complete License Metadata</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <Hash className="size-3 text-cyan-400" />
                License ID
              </span>
              <span className="font-mono font-bold text-white text-sm">#{license.licenseId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <FileText className="size-3 text-cyan-400" />
                Application ID
              </span>
              <span className="font-mono text-slate-200">#{license.applicationId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <User className="size-3 text-cyan-400" />
                Driver ID
              </span>
              <span className="font-mono text-slate-200">#{license.driverId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <IdCard className="size-3 text-cyan-400" />
                License Class ID
              </span>
              <span className="font-mono text-slate-200">#{license.licenseClassId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <CreditCard className="size-3 text-cyan-400" />
                Paid Fees
              </span>
              <span className="font-mono font-semibold text-emerald-400">
                {formatCurrency(license.paidFees, 'JOD')}
              </span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <AlertCircle className="size-3 text-cyan-400" />
                Issue Reason
              </span>
              <span className="text-slate-200 font-medium">{issueReasonLabel}</span>
            </div>
          </div>

          <div className="pt-2 border-t border-[#182944] grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <span className="text-[10px] text-slate-400 block mb-0.5">Created By User</span>
              <span className="text-slate-200 font-medium">
                {license.createdByUserName || `User ID #${license.createdByUserId}`}
              </span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 block mb-0.5">Validity Status</span>
              <span className={`font-semibold ${isExpired ? 'text-rose-400' : 'text-emerald-400'}`}>
                {isExpired ? 'License Expired' : 'Active and Valid'}
              </span>
            </div>
          </div>

          {license.notes && (
            <div className="pt-2 border-t border-[#182944]">
              <span className="text-[10px] text-slate-400 block mb-0.5">Administrative Notes</span>
              <p className="text-slate-300 bg-[#0c1830] p-2.5 rounded-lg border border-[#1a2d4c] leading-relaxed">
                {license.notes}
              </p>
            </div>
          )}
        </div>
      )}

      {/* Card action footer */}
      <div className="px-5 py-3 border-t border-[#1a2d4c] bg-[#091428] flex items-center justify-between gap-3">
        <span className="text-[11px] text-slate-400">
          {license.isActive ? 'Officially Valid' : 'Inactive Record'}
        </span>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setIsExpanded(!isExpanded)}
          className="h-8 gap-1.5 text-xs text-cyan-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <span>{isExpanded ? 'Hide Details' : 'View Details'}</span>
          {isExpanded ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
        </Button>
      </div>
    </div>
  )
}
