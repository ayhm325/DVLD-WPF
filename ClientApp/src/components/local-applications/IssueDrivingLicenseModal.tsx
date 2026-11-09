import { useState } from 'react'
import {
  Award,
  CheckCircle2,
  AlertCircle,
  User,
} from 'lucide-react'
import ModalShell from '../common/ModalShell'
import { Button } from '../ui/button'
import { issueFirstLicense } from '../../services/licenseService'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'

export interface IssueDrivingLicenseModalProps {
  isOpen: boolean
  onClose: () => void
  application: LocalDrivingLicenseApplication | null
  accessToken: string
  onLicenseIssued?: (licenseId: number) => void
}

export default function IssueDrivingLicenseModal({
  isOpen,
  onClose,
  application,
  accessToken,
  onLicenseIssued,
}: IssueDrivingLicenseModalProps) {
  const [notes, setNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [issuedLicenseId, setIssuedLicenseId] = useState<number | null>(null)

  if (!application) return null

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)

    try {
      const res = await issueFirstLicense(accessToken, {
        localApplicationId: application.localDrivingLicenseApplicationId,
        notes: notes.trim() || null,
      })

      setIssuedLicenseId(res.licenseId)
      onLicenseIssued?.(res.licenseId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to issue driving license.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleClose = () => {
    setNotes('')
    setError(null)
    setIssuedLicenseId(null)
    onClose()
  }

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={handleClose}
      title={
        <div className="flex items-center gap-2.5 text-white">
          <div className="flex size-8 items-center justify-center rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 shadow-xs">
            <Award className="size-4" />
          </div>
          <span className="font-bold text-base tracking-tight">Issue First-Time Driving License</span>
          <span className="font-mono text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-800/80 text-emerald-300 border border-slate-700">
            App #{application.localDrivingLicenseApplicationId}
          </span>
        </div>
      }
      description="Authorize and officially issue the driving license following successful completion of all required examinations."
      size="lg"
    >
      <div className="space-y-5 pr-1">
        {issuedLicenseId ? (
          <div className="rounded-2xl border border-emerald-500/30 bg-emerald-950/20 p-6 text-center space-y-4">
            <div className="size-14 mx-auto rounded-full bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shadow-[0_0_20px_rgba(16,185,129,0.2)]">
              <CheckCircle2 className="size-8" />
            </div>

            <div className="space-y-1">
              <h3 className="text-lg font-bold text-white">Driving License Successfully Issued!</h3>
              <p className="text-xs text-slate-300">
                Official license number <strong className="font-mono text-cyan-300">#{issuedLicenseId}</strong> has been generated and activated.
              </p>
            </div>

            <Button
              type="button"
              className="bg-emerald-600 hover:bg-emerald-500 text-white font-semibold text-xs px-6"
              onClick={handleClose}
            >
              Done
            </Button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Application Summary Card */}
            <div className="rounded-xl border border-[#1e3458] bg-[#0c1930] p-4.5 space-y-3">
              <div className="flex items-center justify-between border-b border-[#1e3458] pb-2.5">
                <span className="text-xs font-bold uppercase tracking-wider text-cyan-400 flex items-center gap-1.5">
                  <User className="size-4" />
                  <span>Applicant Information</span>
                </span>
                <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                  <CheckCircle2 className="size-3.5" />
                  <span>All 3 Tests Passed</span>
                </span>
              </div>

              <div className="grid grid-cols-2 gap-3 text-xs">
                <div>
                  <span className="text-slate-400 block text-[11px] mb-0.5">Full Name:</span>
                  <strong className="text-white">{application.fullName}</strong>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px] mb-0.5">National Number:</span>
                  <span className="font-mono text-cyan-300">{application.nationalNo}</span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px] mb-0.5">License Class:</span>
                  <span className="text-slate-200">{application.licenseClassName}</span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px] mb-0.5">License Class Fees:</span>
                  <span className="font-mono text-emerald-300 font-bold">
                    {application.licenseClassFees.toFixed(2)} JOD
                  </span>
                </div>
              </div>
            </div>

            {error && (
              <div className="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3.5 flex items-center gap-2.5 text-xs text-rose-300">
                <AlertCircle className="size-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            {/* Notes Field */}
            <div className="space-y-1.5">
              <label
                htmlFor="license-notes-input"
                className="block text-xs font-medium text-slate-300"
              >
                Administrative Notes (Optional)
              </label>
              <textarea
                id="license-notes-input"
                rows={3}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Enter any official comments, restrictions, or administrative notes..."
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-cyan-500"
              />
            </div>

            <div className="flex justify-end gap-2.5 pt-2 border-t border-slate-800">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleClose}
                disabled={submitting}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                size="sm"
                disabled={submitting}
                className="bg-emerald-600 hover:bg-emerald-500 text-white font-semibold text-xs gap-1.5 shadow-sm"
              >
                <Award className="size-3.5" />
                <span>{submitting ? 'Issuing License…' : 'Issue Driving License'}</span>
              </Button>
            </div>
          </form>
        )}
      </div>
    </ModalShell>
  )
}
