import { useEffect, useState } from 'react'
import {
  IdCard,
  Globe,
  Calendar,
  Layers,
  FileText,
  DollarSign,
  ShieldAlert,
  Hash,
  CheckCircle2,
} from 'lucide-react'
import ModalShell from '../common/ModalShell'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import { Button } from '../ui/button'
import { formatDate, formatCurrency } from '../../utils/formatters'
import { getActiveStatusProps } from '../../utils/status'
import { getLicenseDetailsById } from '../../services/licenseService'
import { getInternationalLicenseById } from '../../services/internationalLicenseService'
import type { License, LocalLicenseInfo } from '../../types/License'
import type {
  InternationalLicense,
  InternationalLicenseDetails,
} from '../../types/InternationalLicense'

export type LicenseModalTarget =
  | { type: 'local'; item: License }
  | { type: 'international'; item: InternationalLicense }

export interface LicenseDetailsModalProps {
  isOpen: boolean
  onClose: () => void
  accessToken: string
  target: LicenseModalTarget | null
}

export default function LicenseDetailsModal({
  isOpen,
  onClose,
  accessToken,
  target,
}: LicenseDetailsModalProps) {
  const isLocal = target?.type === 'local'
  const isIntl = target?.type === 'international'

  const [localDetails, setLocalDetails] = useState<LocalLicenseInfo | null>(null)
  const [intlDetails, setIntlDetails] = useState<InternationalLicenseDetails | null>(null)
  const [fetchAttempted, setFetchAttempted] = useState(false)

  const isLoaded = Boolean(
    (isLocal && localDetails) ||
    (isIntl && intlDetails) ||
    fetchAttempted
  )
  const loading = Boolean(isOpen && target && !isLoaded)

  // Fetch enriched details when modal opens
  useEffect(() => {
    if (!isOpen || !target || !accessToken) {
      return
    }

    let isMounted = true

    if (target.type === 'local') {
      getLicenseDetailsById(accessToken, target.item.licenseId)
        .then((data) => {
          if (isMounted) {
            setLocalDetails(data)
            setFetchAttempted(true)
          }
        })
        .catch(() => {
          if (isMounted) setFetchAttempted(true)
        })
    } else {
      getInternationalLicenseById(accessToken, target.item.internationalLicenseId)
        .then((data) => {
          if (isMounted) {
            setIntlDetails(data)
            setFetchAttempted(true)
          }
        })
        .catch(() => {
          if (isMounted) setFetchAttempted(true)
        })
    }

    return () => {
      isMounted = false
      setLocalDetails(null)
      setIntlDetails(null)
      setFetchAttempted(false)
    }
  }, [isOpen, target, accessToken])

  if (!target) return null

  // Extract merged values with priority on full details then row fallback
  const localRow = isLocal ? target.item : null
  const intlRow = isIntl ? target.item : null

  const licenseId = isLocal ? localRow?.licenseId : intlRow?.internationalLicenseId
  const applicationId = isLocal
    ? localRow?.applicationId
    : intlDetails?.applicationId ?? intlRow?.applicationId

  const driverId = isLocal
    ? localDetails?.driverId ?? localRow?.driverId
    : intlDetails?.driverId ?? intlRow?.driverId

  const personId = isLocal ? localDetails?.personId : intlDetails?.personId ?? intlRow?.personId

  const fullName = isLocal
    ? localDetails?.fullName || localRow?.driverName || '—'
    : intlDetails?.fullName || '—'

  const nationalNo = isLocal ? localDetails?.nationalNo : intlDetails?.nationalNo
  const imagePath = isLocal ? localDetails?.imagePath : intlDetails?.imagePath

  const isActive = isLocal
    ? localDetails?.isActive ?? localRow?.isActive ?? false
    : intlDetails?.isActive ?? intlRow?.isActive ?? false

  const issueDate = isLocal
    ? localDetails?.issueDate ?? localRow?.issueDate
    : intlDetails?.issueDate ?? intlRow?.issueDate

  const expirationDate = isLocal
    ? localDetails?.expirationDate ?? localRow?.expirationDate
    : intlDetails?.expirationDate ?? intlRow?.expirationDate

  const fees = isLocal
    ? localDetails?.licenseClassFees ?? localRow?.paidFees
    : intlDetails?.fees

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div className="flex items-center gap-2.5 text-white">
          {isLocal ? (
            <div className="flex size-8 items-center justify-center rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20 shadow-xs">
              <IdCard className="size-4" />
            </div>
          ) : (
            <div className="flex size-8 items-center justify-center rounded-lg bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 shadow-xs">
              <Globe className="size-4" />
            </div>
          )}
          <span className="font-bold text-base tracking-tight">
            {isLocal ? 'Local Driving License Details' : 'International Driving Permit Details'}
          </span>
          <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-800/80 text-cyan-300 border border-slate-700">
            #{licenseId}
          </span>
        </div>
      }
      description={
        isLocal
          ? 'Comprehensive driving credentials and validity records issued by the central licensing authority.'
          : 'Official 1968 Vienna Convention international driving permit linked to a local driving license.'
      }
      size="xl"
    >
      <div className="space-y-5 max-h-[75vh] overflow-y-auto pr-1">
        {/* Top Identity Banner */}
        <div className="relative overflow-hidden rounded-2xl border border-[#1e3458] bg-gradient-to-br from-[#0c1a33] via-[#09152b] to-[#060e1e] p-5 shadow-lg">
          <div className="relative z-10 flex flex-col sm:flex-row items-center sm:items-start gap-5">
            {/* Portrait Photo */}
            <div className="shrink-0 rounded-2xl border-2 border-cyan-400/30 bg-slate-950/70 p-1 shadow-[0_0_20px_rgba(34,211,238,0.15)]">
              <PersonPhoto
                personId={personId}
                hasPhoto={Boolean(imagePath)}
                photoKey={imagePath}
                fullName={fullName}
                size="lg"
                accessToken={accessToken}
                className="rounded-xl border border-slate-700/60 shadow-inner"
              />
            </div>

            {/* Holder Metadata */}
            <div className="flex flex-1 flex-col items-center sm:items-start text-center sm:text-left space-y-2.5 min-w-0">
              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                <span
                  className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider ${
                    isLocal
                      ? 'bg-cyan-950/70 text-cyan-300 border border-cyan-500/30'
                      : 'bg-indigo-950/70 text-indigo-300 border border-indigo-500/30'
                  }`}
                >
                  {isLocal ? <IdCard className="size-3.5" /> : <Globe className="size-3.5" />}
                  <span>{isLocal ? 'Local License' : 'International Permit'}</span>
                </span>

                <StatusBadge {...getActiveStatusProps(isActive)} />

                {isLocal && localDetails?.isDetained && (
                  <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold uppercase bg-rose-500/20 text-rose-300 border border-rose-500/30">
                    <ShieldAlert className="size-3.5" />
                    <span>Detained</span>
                  </span>
                )}
              </div>

              <h3 className="text-xl font-extrabold text-white tracking-tight leading-tight">
                {fullName}
              </h3>

              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3 text-xs text-slate-300 font-medium">
                {nationalNo && (
                  <span className="flex items-center gap-1.5 bg-slate-900/80 px-2.5 py-1 rounded-lg border border-slate-800">
                    <span className="text-slate-400 uppercase tracking-wider text-[11px]">National No:</span>
                    <strong className="text-cyan-300 font-mono text-xs">{nationalNo}</strong>
                  </span>
                )}
                {personId && (
                  <span className="bg-slate-900/60 px-2.5 py-1 rounded-lg border border-slate-800 text-slate-400">
                    Person <strong className="text-slate-200 font-mono">#{personId}</strong>
                  </span>
                )}
                {driverId && (
                  <span className="bg-slate-900/60 px-2.5 py-1 rounded-lg border border-slate-800 text-slate-400">
                    Driver <strong className="text-slate-200 font-mono">#{driverId}</strong>
                  </span>
                )}
              </div>
            </div>
          </div>

          {loading && (
            <div className="absolute top-3 right-3 flex items-center gap-1.5 text-xs text-cyan-400 bg-slate-950/80 px-2.5 py-1 rounded-full border border-cyan-500/30">
              <LoadingSpinner size="sm" />
              <span>Updating...</span>
            </div>
          )}
        </div>

        {/* Section 1: Identification & Classification */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3.5">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400 border-b border-[#1e3458] pb-2.5">
            <Hash className="size-4 text-cyan-400" />
            <span>License Identification & Classification</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                License ID
              </span>
              <span className="text-sm font-bold font-mono text-cyan-300">#{licenseId}</span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Application ID
              </span>
              <span className="text-sm font-bold font-mono text-slate-200">
                {applicationId ? `#${applicationId}` : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Driver ID
              </span>
              <span className="text-sm font-bold font-mono text-slate-200">
                {driverId ? `#${driverId}` : '—'}
              </span>
            </div>

            {isLocal ? (
              <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                  Class ID
                </span>
                <span className="text-sm font-bold font-mono text-slate-200">
                  {localRow?.licenseClassId ? `Class #${localRow.licenseClassId}` : '—'}
                </span>
              </div>
            ) : (
              <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
                <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                  Local License ID
                </span>
                <span className="text-sm font-bold font-mono text-indigo-300">
                  #{intlRow?.issuedUsingLocalLicenseId || intlDetails?.issuedUsingLocalLicenseId || '—'}
                </span>
              </div>
            )}
          </div>

          {isLocal && (
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                <Layers className="size-3.5 text-cyan-400" />
                License Class Name
              </span>
              <span className="text-sm font-bold text-white">
                {localDetails?.licenseClass || localRow?.licenseClassName || 'Ordinary Driving License'}
              </span>
            </div>
          )}
        </div>

        {/* Section 2: Validity, Status & Dates */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3.5">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400 border-b border-[#1e3458] pb-2.5">
            <Calendar className="size-4 text-cyan-400" />
            <span>Validity & Schedule</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Issue Date
              </span>
              <span className="text-sm font-semibold font-mono text-slate-200">
                {issueDate ? formatDate(issueDate) : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Expiration Date
              </span>
              <span className="text-sm font-semibold font-mono text-slate-200">
                {expirationDate ? formatDate(expirationDate) : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Current Status
              </span>
              <span
                className={`text-sm font-bold inline-flex items-center gap-1.5 ${
                  isActive ? 'text-emerald-400' : 'text-slate-400'
                }`}
              >
                <CheckCircle2 className="size-4" />
                <span>{isActive ? 'Active & Valid' : 'Inactive / Expired'}</span>
              </span>
            </div>
          </div>
        </div>

        {/* Section 3: Financial & Administrative Details */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3.5">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400 border-b border-[#1e3458] pb-2.5">
            <DollarSign className="size-4 text-cyan-400" />
            <span>Financial & Administrative Information</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                {isLocal ? 'Paid Fees' : 'Permit Fees'}
              </span>
              <span className="text-sm font-bold font-mono text-emerald-400">
                {fees !== undefined && fees !== null ? formatCurrency(fees, 'JOD') : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                {isLocal ? 'Issue Reason' : 'Created By User'}
              </span>
              <span className="text-sm font-semibold text-white">
                {isLocal
                  ? localDetails?.issueReason || localRow?.issueReasonText || 'First Time'
                  : intlDetails?.createdByUserName || 'Staff'}
              </span>
            </div>
          </div>

          {isLocal && (
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                <FileText className="size-3.5 text-cyan-400" />
                Official Notes
              </span>
              <p className="text-sm text-slate-300 leading-relaxed italic">
                {localDetails?.notes || localRow?.notes || 'No administrative notes recorded for this license.'}
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Modal Actions Footer */}
      <div className="pt-4 border-t border-[#1e3458] flex items-center justify-end">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onClose}
          className="border-[#263e68] bg-[#0e1a33] text-slate-300 hover:text-white hover:bg-[#16274a] rounded-xl cursor-pointer"
        >
          Close
        </Button>
      </div>
    </ModalShell>
  )
}
