import { useEffect, useState } from 'react'
import {
  FileText,
  User,
  Calendar,
  Layers,
  Clock,
  CheckCircle2,
  XCircle,
  AlertCircle,
  CreditCard,
  Hash,
  ShieldCheck,
} from 'lucide-react'
import ModalShell from '../common/ModalShell'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge, { type StatusVariant } from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import { formatDate } from '../../utils/formatters'
import { getApplicationBasicInfo } from '../../services/localDrivingLicenseApplicationService'
import type {
  ApplicationBasicInfo,
  LocalDrivingLicenseApplication,
} from '../../types/LocalDrivingLicenseApplication'

export interface ApplicationDetailsModalProps {
  isOpen: boolean
  onClose: () => void
  application: LocalDrivingLicenseApplication | null
  accessToken: string
}

function getStatusVariant(status: string): StatusVariant {
  switch (status.trim().toLowerCase()) {
    case 'completed':
      return 'success'
    case 'cancelled':
    case 'canceled':
      return 'danger'
    case 'new':
      return 'warning'
    default:
      return 'neutral'
  }
}

export default function ApplicationDetailsModal({
  isOpen,
  onClose,
  application,
  accessToken,
}: ApplicationDetailsModalProps) {
  const [basicInfo, setBasicInfo] = useState<ApplicationBasicInfo | null>(null)
  const [fetchAttempted, setFetchAttempted] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loading = Boolean(isOpen && application && !basicInfo && !fetchAttempted)

  useEffect(() => {
    if (!isOpen || !application) {
      return
    }

    let isMounted = true

    getApplicationBasicInfo(accessToken, application.localDrivingLicenseApplicationId)
      .then((data) => {
        if (isMounted) {
          setBasicInfo(data)
          setFetchAttempted(true)
        }
      })
      .catch((err) => {
        if (isMounted) {
          setError(err instanceof Error ? err.message : 'Could not fetch full application info.')
          setFetchAttempted(true)
        }
      })

    return () => {
      isMounted = false
      setBasicInfo(null)
      setError(null)
      setFetchAttempted(false)
    }
  }, [isOpen, application, accessToken])

  if (!application) return null

  const status = application.statusText || application.applicationStatus || 'New'
  const applicantName = basicInfo?.applicantFullName || application.fullName || '—'
  const nationalNo = application.nationalNo || '—'
  const personId = application.applicantPersonId || basicInfo?.applicantPersonId

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div className="flex items-center gap-2.5 text-white">
          <div className="flex size-8 items-center justify-center rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20 shadow-xs">
            <FileText className="size-4" />
          </div>
          <span className="font-bold text-base tracking-tight">
            Local Driving License Application Details
          </span>
          <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-800/80 text-cyan-300 border border-slate-700">
            #{application.localDrivingLicenseApplicationId}
          </span>
        </div>
      }
      description="Full official application registry record, test progression lifecycle, and credential status."
      size="xl"
    >
      <div className="space-y-5 max-h-[75vh] overflow-y-auto pr-1">
        {/* Applicant Identity Card */}
        <div className="relative overflow-hidden rounded-2xl border border-[#1e3458] bg-gradient-to-br from-[#0c1a33] via-[#09152b] to-[#060e1e] p-5 shadow-lg">
          <div className="relative z-10 flex flex-col sm:flex-row items-center sm:items-start gap-5">
            <div className="shrink-0 rounded-2xl border-2 border-cyan-400/30 bg-slate-950/70 p-1 shadow-[0_0_20px_rgba(34,211,238,0.15)]">
              <PersonPhoto
                personId={personId}
                fullName={applicantName}
                size="lg"
                accessToken={accessToken}
                className="rounded-xl border border-slate-700/60 shadow-inner"
              />
            </div>

            <div className="flex flex-1 flex-col items-center sm:items-start text-center sm:text-left space-y-2.5 min-w-0">
              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                <StatusBadge variant={getStatusVariant(status)} label={status} />
                <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider bg-cyan-950/70 text-cyan-300 border border-cyan-500/30">
                  <Layers className="size-3.5" />
                  <span>{application.licenseClassName || `Class ${application.licenseClassId}`}</span>
                </span>
                {application.hasLicense && (
                  <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold uppercase bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    <ShieldCheck className="size-3.5" />
                    <span>License Issued</span>
                  </span>
                )}
              </div>

              <h3 className="text-xl font-extrabold text-white tracking-tight leading-tight">
                {applicantName}
              </h3>

              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3 text-xs text-slate-300 font-medium">
                <span className="flex items-center gap-1.5 bg-slate-900/80 px-2.5 py-1 rounded-lg border border-slate-800">
                  <span className="text-slate-400 uppercase tracking-wider text-[11px]">National No:</span>
                  <strong className="text-cyan-300 font-mono text-xs">{nationalNo}</strong>
                </span>
                {personId && (
                  <span className="bg-slate-900/60 px-2.5 py-1 rounded-lg border border-slate-800 text-slate-400">
                    Person <strong className="text-slate-200 font-mono">#{personId}</strong>
                  </span>
                )}
                {basicInfo?.applicationId && (
                  <span className="bg-slate-900/60 px-2.5 py-1 rounded-lg border border-slate-800 text-slate-400">
                    Main App <strong className="text-slate-200 font-mono">#{basicInfo.applicationId}</strong>
                  </span>
                )}
              </div>
            </div>
          </div>

          {loading && (
            <div className="absolute top-3 right-3 flex items-center gap-1.5 text-xs text-cyan-400 bg-slate-950/80 px-2.5 py-1 rounded-full border border-cyan-500/30">
              <LoadingSpinner size="sm" />
              <span>Fetching details...</span>
            </div>
          )}
        </div>

        {error && (
          <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3.5 flex items-center gap-2.5 text-xs text-amber-300">
            <AlertCircle className="size-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {/* Section 1: Application Specifications */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3.5">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400 border-b border-[#1e3458] pb-2.5">
            <Hash className="size-4 text-cyan-400" />
            <span>Application Metadata & Fees</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Local App ID
              </span>
              <span className="text-sm font-bold font-mono text-cyan-300">
                #{application.localDrivingLicenseApplicationId}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Base Application ID
              </span>
              <span className="text-sm font-bold font-mono text-slate-200">
                {basicInfo?.applicationId ? `#${basicInfo.applicationId}` : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                Application Fees
              </span>
              <span className="text-sm font-bold font-mono text-slate-200">
                {(basicInfo?.paidFees ?? application.applicationFees).toFixed(2)} JOD
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 block mb-0.5">
                License Class Fees
              </span>
              <span className="text-sm font-bold font-mono text-slate-200">
                {application.licenseClassFees.toFixed(2)} JOD
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                <Calendar className="size-3.5 text-cyan-400" />
                Application Date
              </span>
              <span className="text-sm font-medium text-white">
                {formatDate(application.applicationDate)}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                <Clock className="size-3.5 text-cyan-400" />
                Last Status Date
              </span>
              <span className="text-sm font-medium text-white">
                {basicInfo?.lastStatusDate ? formatDate(basicInfo.lastStatusDate) : '—'}
              </span>
            </div>

            <div className="rounded-xl border border-slate-800/80 bg-slate-900/50 p-3">
              <span className="text-[11px] font-medium uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-1">
                <User className="size-3.5 text-cyan-400" />
                Created By User
              </span>
              <span className="text-sm font-medium text-white font-mono">
                {basicInfo?.createdByUserName || 'Staff'}
              </span>
            </div>
          </div>
        </div>

        {/* Section 2: Examination Progress Tracker */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3.5">
          <div className="flex items-center justify-between border-b border-[#1e3458] pb-2.5">
            <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400">
              <ShieldCheck className="size-4 text-cyan-400" />
              <span>Required Examination Progress</span>
            </div>
            <span
              className={`text-xs font-bold font-mono px-2.5 py-0.5 rounded-full ${
                application.passedTest === 3
                  ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                  : 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30'
              }`}
            >
              {application.passedTest} / 3 Tests Passed
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {/* Step 1: Vision */}
            <div
              className={`rounded-xl border p-3 flex items-start gap-3 ${
                application.passedTest >= 1
                  ? 'border-emerald-500/40 bg-emerald-950/20'
                  : 'border-slate-800 bg-slate-900/40 opacity-70'
              }`}
            >
              {application.passedTest >= 1 ? (
                <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
              ) : (
                <XCircle className="size-5 text-slate-500 shrink-0 mt-0.5" />
              )}
              <div>
                <span className="text-xs font-bold text-white block">1. Vision Test</span>
                <span className="text-[11px] text-slate-400">
                  {application.passedTest >= 1 ? 'Passed & Certified' : 'Pending Examination'}
                </span>
              </div>
            </div>

            {/* Step 2: Written */}
            <div
              className={`rounded-xl border p-3 flex items-start gap-3 ${
                application.passedTest >= 2
                  ? 'border-emerald-500/40 bg-emerald-950/20'
                  : 'border-slate-800 bg-slate-900/40 opacity-70'
              }`}
            >
              {application.passedTest >= 2 ? (
                <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
              ) : (
                <XCircle className="size-5 text-slate-500 shrink-0 mt-0.5" />
              )}
              <div>
                <span className="text-xs font-bold text-white block">2. Written Test</span>
                <span className="text-[11px] text-slate-400">
                  {application.passedTest >= 2
                    ? 'Passed & Certified'
                    : application.passedTest >= 1
                      ? 'Eligible to Schedule'
                      : 'Locked (Requires Vision)'}
                </span>
              </div>
            </div>

            {/* Step 3: Street */}
            <div
              className={`rounded-xl border p-3 flex items-start gap-3 ${
                application.passedTest >= 3
                  ? 'border-emerald-500/40 bg-emerald-950/20'
                  : 'border-slate-800 bg-slate-900/40 opacity-70'
              }`}
            >
              {application.passedTest >= 3 ? (
                <CheckCircle2 className="size-5 text-emerald-400 shrink-0 mt-0.5" />
              ) : (
                <XCircle className="size-5 text-slate-500 shrink-0 mt-0.5" />
              )}
              <div>
                <span className="text-xs font-bold text-white block">3. Street Test</span>
                <span className="text-[11px] text-slate-400">
                  {application.passedTest >= 3
                    ? 'Passed & Certified'
                    : application.passedTest >= 2
                      ? 'Eligible to Schedule'
                      : 'Locked (Requires Written)'}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Section 3: Issued License Status */}
        <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-cyan-400 border-b border-[#1e3458] pb-2.5">
            <CreditCard className="size-4 text-cyan-400" />
            <span>Driver License Issuance Status</span>
          </div>

          {application.hasLicense ? (
            <div className="flex items-center justify-between p-3 rounded-xl border border-emerald-500/30 bg-emerald-950/30">
              <div className="flex items-center gap-2.5 text-xs text-emerald-300 font-medium">
                <CheckCircle2 className="size-5 text-emerald-400" />
                <span>An official driving license has been successfully issued for this application.</span>
              </div>
            </div>
          ) : application.passedTest === 3 && status.toLowerCase() === 'new' ? (
            <div className="flex items-center justify-between p-3 rounded-xl border border-cyan-500/30 bg-cyan-950/30">
              <div className="flex items-center gap-2.5 text-xs text-cyan-300 font-medium">
                <AlertCircle className="size-5 text-cyan-400" />
                <span>All tests passed! This application is ready for initial license issuance.</span>
              </div>
            </div>
          ) : (
            <div className="p-3 rounded-xl border border-slate-800 bg-slate-900/40 text-xs text-slate-400">
              {status.toLowerCase() === 'completed'
                ? 'Application completed.'
                : status.toLowerCase() === 'cancelled'
                  ? 'Application was cancelled. No license can be issued.'
                  : `License issuance will become available after passing all 3 tests (${3 - application.passedTest} remaining).`}
            </div>
          )}
        </div>
      </div>
    </ModalShell>
  )
}
