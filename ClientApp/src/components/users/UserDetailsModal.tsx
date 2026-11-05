import { useEffect, useState } from 'react'
import {
  ShieldCheck,
  User as UserIcon,
  IdCard,
  Calendar,
  Globe,
  Phone,
  Mail,
  MapPin,
  Hash,
  Lock,
} from 'lucide-react'
import ModalShell from '../common/ModalShell'
import PersonPhoto from '../common/PersonPhoto'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import ErrorAlert from '../common/ErrorAlert'
import { Button } from '../ui/button'
import { getUserDetails, type UserDetails } from '../../services/userService'
import { formatDate } from '../../utils/formatters'
import { getRoleLabel, isAdminRole, isPrimaryAdminUser } from './userValidation'

export interface UserDetailsModalProps {
  isOpen: boolean
  userId: number | null
  accessToken: string
  currentUserId?: number
  onClose: () => void
  onEditUser?: (userId: number) => void
}

export default function UserDetailsModal({
  isOpen,
  userId,
  accessToken,
  currentUserId,
  onClose,
  onEditUser,
}: UserDetailsModalProps) {
  const [details, setDetails] = useState<UserDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    queueMicrotask(() => {
      if (!active) return
      if (!isOpen || !userId || !accessToken) {
        setDetails(null)
        setError(null)
      }
    })

    if (!isOpen || !userId || !accessToken) {
      return
    }

    async function load() {
      setLoading(true)
      setError(null)

      try {
        const data = await getUserDetails(accessToken, userId!)
        if (!active) return
        setDetails(data)
      } catch (err) {
        if (!active) return
        setError(
          err instanceof Error ? err.message : 'Failed to load user details.',
        )
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    void load()

    return () => {
      active = false
    }
  }, [isOpen, userId, accessToken])

  const fullName = details
    ? [details.firstName, details.secondName, details.thirdName, details.lastName]
        .filter(Boolean)
        .join(' ')
    : ''

  const isPrimary = details ? isPrimaryAdminUser(details) : false
  const isSelf = details ? details.userId === currentUserId : false

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      size="lg"
      title="User Account Details"
      description="Verified system credentials, role assignment, and linked citizen identity."
      footer={
        <div className="flex items-center justify-between w-full gap-2">
          <div className="text-xs text-slate-400">
            {isPrimary && (
              <span className="inline-flex items-center gap-1.5 text-amber-300 font-medium">
                <Lock className="size-3.5" />
                Protected Primary Administrator
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            {details && onEditUser && !isPrimary && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  const targetId = details.userId
                  onClose()
                  onEditUser(targetId)
                }}
                className="bg-[#14233c] border-[#263750] text-slate-200 hover:bg-[#1a2e4d] hover:text-white cursor-pointer"
              >
                Edit Account
              </Button>
            )}
            <Button
              type="button"
              variant="default"
              size="sm"
              onClick={onClose}
              className="bg-blue-600 hover:bg-blue-500 text-white cursor-pointer"
            >
              Close
            </Button>
          </div>
        </div>
      }
    >
      {loading ? (
        <div className="py-10 flex justify-center">
          <LoadingSpinner message="Loading user account details..." />
        </div>
      ) : error ? (
        <ErrorAlert message={error} />
      ) : details ? (
        <div className="space-y-5">
          {/* Top Identity & Role Card */}
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4 p-4 rounded-xl bg-[#0b1526] border border-[#223656]">
            <PersonPhoto
              personId={details.personId}
              hasPhoto={Boolean(details.imagePath)}
              photoKey={details.imagePath}
              fullName={fullName}
              accessToken={accessToken}
              size="lg"
            />

            <div className="min-w-0 flex-1 space-y-1.5">
              <div className="flex flex-wrap items-center gap-2">
                <h3 className="text-base font-bold text-white truncate">
                  {fullName || 'Unnamed Person'}
                </h3>
                {isSelf && (
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-cyan-500/15 text-cyan-300 border border-cyan-500/30">
                    Your Account
                  </span>
                )}
              </div>

              <div className="flex flex-wrap items-center gap-2 text-xs text-slate-300">
                <span className="font-mono font-semibold text-cyan-300 bg-cyan-950/60 border border-cyan-500/30 px-2 py-0.5 rounded-md">
                  @{details.userName}
                </span>
                <span className="text-slate-500">•</span>
                <span className="font-mono text-slate-400">
                  User #{details.userId}
                </span>
                <span className="text-slate-500">•</span>
                <span className="font-mono text-slate-400">
                  Person #{details.personId}
                </span>
              </div>

              <div className="flex flex-wrap items-center gap-2 pt-1">
                <span
                  className={
                    isAdminRole(details.role)
                      ? 'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-violet-500/20 text-violet-200 border border-violet-400/40'
                      : 'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-blue-500/15 text-blue-200 border border-blue-400/30'
                  }
                >
                  {isAdminRole(details.role) ? (
                    <ShieldCheck className="size-3.5 text-violet-300" />
                  ) : (
                    <UserIcon className="size-3.5 text-blue-300" />
                  )}
                  {getRoleLabel(details.role)}
                </span>

                <StatusBadge
                  variant={details.isActive ? 'success' : 'danger'}
                  label={details.isActive ? 'Active' : 'Inactive'}
                />
              </div>
            </div>
          </div>

          {/* Linked Person Attributes Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <IdCard className="size-3.5 text-cyan-400" />
                National Number
              </span>
              <p className="mt-1 font-mono text-sm font-semibold text-white">
                {details.nationalNo || '—'}
              </p>
            </div>

            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <Calendar className="size-3.5 text-cyan-400" />
                Date of Birth & Gender
              </span>
              <p className="mt-1 text-sm font-semibold text-white">
                {formatDate(details.dateOfBirth)} · {details.gender || '—'}
              </p>
            </div>

            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <Phone className="size-3.5 text-cyan-400" />
                Phone Number
              </span>
              <p className="mt-1 font-mono text-sm font-semibold text-white">
                {details.phone || '—'}
              </p>
            </div>

            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <Mail className="size-3.5 text-cyan-400" />
                Email Address
              </span>
              <p className="mt-1 text-sm font-semibold text-white truncate" title={details.email || 'Not provided'}>
                {details.email || 'Not provided'}
              </p>
            </div>

            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <Globe className="size-3.5 text-cyan-400" />
                Nationality
              </span>
              <p className="mt-1 text-sm font-semibold text-white">
                {details.countryName || '—'}
              </p>
            </div>

            <div className="p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <Hash className="size-3.5 text-cyan-400" />
                Account Governance
              </span>
              <p className="mt-1 text-xs font-medium text-slate-300">
                {isPrimary
                  ? 'System-protected primary administrator'
                  : 'Standard DVLD role-governed account'}
              </p>
            </div>

            <div className="sm:col-span-2 p-3 rounded-xl bg-[#0d182c] border border-[#1e304d]">
              <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                <MapPin className="size-3.5 text-cyan-400" />
                Residential Address
              </span>
              <p className="mt-1 text-sm text-slate-200">
                {details.address || '—'}
              </p>
            </div>
          </div>
        </div>
      ) : null}
    </ModalShell>
  )
}
