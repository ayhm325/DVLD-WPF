import { useState } from 'react'
import {
  Unlock,
  CheckCircle2,
  Info,
  Loader2,
} from 'lucide-react'
import type { DetainedLicense } from '../../types/DetainedLicense'
import { releaseDetainedLicense } from '../../services/detainedLicenseService'
import { formatDate, formatCurrency } from '../../utils/formatters'
import ModalShell from '../common/ModalShell'
import ErrorAlert from '../common/ErrorAlert'
import { Button } from '../ui'

type Props = {
  isOpen: boolean
  detention: DetainedLicense | null
  accessToken: string
  onClose: () => void
  onReleased: () => void
}

const RELEASE_APPLICATION_FEE = 15.0

export default function ReleaseDetainedLicenseModal({
  isOpen,
  detention,
  accessToken,
  onClose,
  onReleased,
}: Props) {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  if (!isOpen || !detention) return null

  const totalPayable = RELEASE_APPLICATION_FEE + detention.fineFees

  async function handleRelease() {
    if (!detention || submitting) return

    setSubmitting(true)
    setError('')

    try {
      await releaseDetainedLicense(accessToken, detention.detainId)
      setSuccess(true)
      setTimeout(() => {
        onReleased()
        onClose()
      }, 1200)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to release the detained license.')
      setSubmitting(false)
    }
  }

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={submitting ? () => {} : onClose}
      title="Release Detained License"
      description={`Process release for Detention Record #${detention.detainId} (License #${detention.licenseId})`}
      size="md"
    >
      <div className="space-y-4">
        {error && <ErrorAlert title="Release Error" message={error} />}

        {success && (
          <div className="flex items-center gap-3 p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-200">
            <CheckCircle2 className="size-5 text-emerald-400 shrink-0" />
            <p className="text-sm font-semibold">License released successfully!</p>
          </div>
        )}

        {/* Detention & Driver Overview */}
        <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-2 text-xs">
          <div className="flex items-center justify-between">
            <span className="text-slate-400">Driver / Applicant:</span>
            <span className="font-bold text-white">{detention.fullName || '—'}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-slate-400">National No:</span>
            <span className="font-mono text-slate-200">{detention.nationalNo || '—'}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-slate-400">Detention Date:</span>
            <span className="text-slate-200">{formatDate(detention.detainDate)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-slate-400">License ID:</span>
            <span className="font-mono font-bold text-cyan-400">#{detention.licenseId}</span>
          </div>
        </div>

        {/* Financial Breakdown */}
        <div className="p-3.5 rounded-xl bg-[#081326] border border-[#1b2f52] space-y-2">
          <span className="text-xs font-bold text-slate-300 block">Required Payments</span>
          <div className="space-y-1.5 text-xs">
            <div className="flex justify-between items-center text-slate-300">
              <span>Release Application Fee:</span>
              <span className="font-semibold text-white">
                {formatCurrency(RELEASE_APPLICATION_FEE)}
              </span>
            </div>
            <div className="flex justify-between items-center text-slate-300">
              <span>Outstanding Fine Fee:</span>
              <span className="font-semibold text-rose-300">
                {formatCurrency(detention.fineFees)}
              </span>
            </div>
            <div className="pt-2 border-t border-[#1b2f52] flex justify-between items-center text-sm font-bold">
              <span className="text-cyan-300">Total Settle Amount:</span>
              <span className="text-emerald-400 font-extrabold">{formatCurrency(totalPayable)}</span>
            </div>
          </div>
        </div>

        {/* Legal & Operational Notice */}
        <div className="flex items-start gap-2.5 p-3 rounded-xl bg-[#0e2447] border border-cyan-500/30 text-xs text-cyan-200">
          <Info className="size-4 text-cyan-400 shrink-0 mt-0.5" />
          <span>
            <strong>Reactivation Policy:</strong> Upon successful payment, if the driving license has
            not expired according to Jordan facility local time, it will be automatically reactivated.
            If the license expired while detained, it will remain inactive and require renewal.
          </span>
        </div>

        {/* Modal Actions */}
        <div className="flex items-center justify-end gap-3 pt-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            disabled={submitting}
            className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:text-white"
          >
            Cancel
          </Button>
          <Button
            type="button"
            onClick={handleRelease}
            disabled={submitting || success || detention.isReleased}
            className="bg-emerald-600 hover:bg-emerald-500 text-white font-semibold rounded-xl h-9 px-5 gap-2 cursor-pointer transition-all shadow-md disabled:opacity-50"
          >
            {submitting ? (
              <>
                <Loader2 className="size-4 animate-spin" />
                <span>Releasing...</span>
              </>
            ) : (
              <>
                <Unlock className="size-4" />
                <span>Confirm & Release</span>
              </>
            )}
          </Button>
        </div>
      </div>
    </ModalShell>
  )
}
