import { useState } from 'react'
import {
  Globe,
  Calendar,
  Clock,
  ChevronDown,
  ChevronUp,
  FileText,
  User,
  Hash,
  IdCard,
} from 'lucide-react'
import type { InternationalLicense } from '../../types/InternationalLicense'
import type { Person, PersonDetails } from '../../types/Person'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import { getActiveStatusProps } from '../../utils/status'
import { formatDate } from '../../utils/formatters'
import { Button } from '../ui/button'

export interface InternationalLicenseCardProps {
  license: InternationalLicense
  person: PersonDetails | Person
  accessToken?: string
}

export default function InternationalLicenseCard({
  license,
  person,
  accessToken,
}: InternationalLicenseCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)

  const isExpired = !license.isActive
  const statusProps = getActiveStatusProps(license.isActive)

  return (
    <div
      className={`group relative flex flex-col rounded-2xl bg-gradient-to-br from-[#0b172d] via-[#101b33] to-[#071120] border transition-all duration-300 shadow-xl overflow-hidden ${
        license.isActive
          ? 'border-[#203a66] hover:border-blue-500/50 hover:shadow-[0_8px_30px_rgba(59,130,246,0.15)]'
          : 'border-[#233148] hover:border-slate-500/40 opacity-90'
      }`}
    >
      {/* Decorative top security stripe inspired by 1968 Vienna Convention International Driving Permits */}
      <div className="relative px-5 py-3 border-b border-[#1b3154] bg-[#081224]/80 backdrop-blur-sm flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 min-w-0">
          <div className="flex size-7 items-center justify-center rounded-lg bg-blue-500/10 text-blue-400 border border-blue-500/20">
            <Globe className="size-4" />
          </div>
          <div className="min-w-0">
            <span className="block text-[10px] font-extrabold uppercase tracking-widest text-blue-400 truncate">
              International Driving Permit
            </span>
            <span className="block text-[11px] font-semibold text-slate-300 truncate">
              Convention on Road Traffic (1968)
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
      <div className="min-w-0 p-4 sm:p-5 flex-1 space-y-4">
        {/* Header number & basis */}
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#1a2d4c] pb-3">
          <div className="min-w-0">
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">
              Issued On Basis Of Local License
            </span>
            <div className="flex items-center gap-1.5 text-sm font-bold text-white tracking-tight">
              <IdCard className="size-3.5 text-blue-400" />
              <span className="break-all">Local License #{license.issuedUsingLocalLicenseId}</span>
            </div>
          </div>

          <div className="min-w-0 text-right">
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">
              Permit ID
            </span>
            <span className="font-mono text-base font-extrabold text-blue-300 drop-shadow-[0_0_8px_rgba(59,130,246,0.3)] break-all">
              #{license.internationalLicenseId}
            </span>
          </div>
        </div>

        {/* Card profile section */}
        <div className="flex min-w-0 items-start gap-4">
          <div className="relative shrink-0">
            <PersonPhoto
              personId={person.personId}
              photoKey={person.imagePath}
              hasPhoto={Boolean(person.imagePath)}
              fullName={person.fullName}
              size="lg"
              accessToken={accessToken}
              className="border-2 border-blue-400/40 shadow-[0_0_15px_rgba(59,130,246,0.2)] rounded-xl"
            />
            <span className="mt-1.5 block text-center font-mono text-[10px] font-semibold px-1.5 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
              Driver #{license.driverId}
            </span>
          </div>

          <div className="flex-1 min-w-0 space-y-2 text-xs [overflow-wrap:anywhere]">
            <div>
              <span className="text-[10px] text-slate-400 block">Permit Holder</span>
              <strong className="text-white text-sm font-bold truncate block">
                {person.fullName}
              </strong>
            </div>

            <div className="grid min-w-0 grid-cols-1 sm:grid-cols-2 gap-2 pt-1 border-t border-[#1a2d4c] [overflow-wrap:anywhere]">
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

            <div className="grid min-w-0 grid-cols-1 sm:grid-cols-2 gap-2 [overflow-wrap:anywhere]">
              <div>
                <span className="text-[10px] text-slate-400 flex items-center gap-1">
                  <Calendar className="size-3 text-blue-400" />
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

        {/* Pill tags */}
        <div className="flex flex-wrap items-center gap-2 pt-1 text-[11px]">
          <span className="px-2 py-0.5 rounded-md bg-[#111e36] text-slate-300 border border-[#1e3458]">
            Local License: <strong className="text-cyan-300">#{license.issuedUsingLocalLicenseId}</strong>
          </span>
          <span className="px-2 py-0.5 rounded-md bg-[#111e36] text-slate-300 border border-[#1e3458]">
            Valid Region: <strong className="text-white">International</strong>
          </span>
        </div>
      </div>

      {/* Expandable details drawer */}
      {isExpanded && (
        <div className="border-t border-[#1b3154] bg-[#071120]/95 p-4 sm:p-5 space-y-3.5 text-xs animate-in fade-in slide-in-from-top-2 duration-200">
          <div className="flex items-center gap-2 pb-2 border-b border-[#182944] text-blue-400 font-semibold uppercase tracking-wider text-[11px]">
            <FileText className="size-3.5" />
            <span>Complete Permit Record</span>
          </div>

          <div className="grid min-w-0 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 [overflow-wrap:anywhere]">
            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <Hash className="size-3 text-blue-400" />
                Int. License ID
              </span>
              <span className="font-mono font-bold text-white text-sm">#{license.internationalLicenseId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <IdCard className="size-3 text-blue-400" />
                Local License ID
              </span>
              <span className="font-mono font-bold text-cyan-300 text-sm">#{license.issuedUsingLocalLicenseId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <FileText className="size-3 text-blue-400" />
                Application ID
              </span>
              <span className="font-mono text-slate-200">#{license.applicationId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <User className="size-3 text-blue-400" />
                Driver ID
              </span>
              <span className="font-mono text-slate-200">#{license.driverId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 flex items-center gap-1">
                <User className="size-3 text-blue-400" />
                Person ID
              </span>
              <span className="font-mono text-slate-200">#{license.personId}</span>
            </div>

            <div>
              <span className="text-[10px] text-slate-400 block mb-0.5">Status</span>
              <span className={`font-semibold ${isExpired ? 'text-rose-400' : 'text-emerald-400'}`}>
                {isExpired ? 'Permit Expired' : 'Active Permit'}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Card action footer */}
      <div className="px-5 py-3 border-t border-[#1a2d4c] bg-[#091428] flex items-center justify-between gap-3">
        <span className="text-[11px] text-slate-400">
          {license.isActive ? 'Valid Worldwide' : 'Inactive Record'}
        </span>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setIsExpanded(!isExpanded)}
          className="h-8 gap-1.5 text-xs text-blue-300 hover:text-white border-[#263e68] bg-[#0e1a33] hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          <span>{isExpanded ? 'Hide Details' : 'View Details'}</span>
          {isExpanded ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
        </Button>
      </div>
    </div>
  )
}
