import { useEffect, useState, useMemo } from 'react'
import { History, IdCard, Globe, AlertCircle } from 'lucide-react'
import ModalShell from '../common/ModalShell'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import LoadingSpinner from '../common/LoadingSpinner'
import EmptyState from '../common/EmptyState'
import { Button } from '../ui/button'
import { formatDate } from '../../utils/formatters'
import { getActiveStatusProps } from '../../utils/status'
import { getLicensesByDriverId } from '../../services/licenseService'
import { getInternationalLicensesByDriverId } from '../../services/internationalLicenseService'
import type { Driver } from '../../types/Driver'
import type { License } from '../../types/License'
import type { InternationalLicense } from '../../types/InternationalLicense'

export interface PersonLicenseHistoryModalProps {
  isOpen: boolean
  onClose: () => void
  accessToken: string
  driver: Driver | null
}

type TabType = 'local' | 'international'

export default function PersonLicenseHistoryModal({
  isOpen,
  onClose,
  accessToken,
  driver,
}: PersonLicenseHistoryModalProps) {
  const [activeTab, setActiveTab] = useState<TabType>('local')
  const [localLicenses, setLocalLicenses] = useState<License[]>([])
  const [internationalLicenses, setInternationalLicenses] = useState<InternationalLicense[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    if (!isOpen || !driver) return

    let cancelled = false

    async function fetchData() {
      setLoading(true)
      setError(null)

      try {
        const [localRes, intlRes] = await Promise.allSettled([
          getLicensesByDriverId(accessToken, driver!.driverId),
          getInternationalLicensesByDriverId(accessToken, driver!.driverId),
        ])

        if (cancelled) return

        if (localRes.status === 'fulfilled') {
          setLocalLicenses(localRes.value || [])
        } else {
          setLocalLicenses([])
        }

        if (intlRes.status === 'fulfilled') {
          setInternationalLicenses(intlRes.value || [])
        } else {
          setInternationalLicenses([])
        }

        if (localRes.status === 'rejected' && intlRes.status === 'rejected') {
          setError('Failed to load license records for this driver.')
        }
      } catch {
        if (!cancelled) {
          setError('An unexpected error occurred while loading license history.')
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void fetchData()

    return () => {
      cancelled = true
    }
  }, [isOpen, driver, accessToken, refreshKey])

  const localColumns = useMemo<DataTableColumn<License>[]>(
    () => [
      {
        key: 'licenseId',
        header: 'License ID',
        render: (item) => (
          <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
            #{item.licenseId}
          </span>
        ),
      },
      {
        key: 'applicationId',
        header: 'App ID',
        render: (item) => (
          <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
            #{item.applicationId}
          </span>
        ),
      },
      {
        key: 'licenseClassName',
        header: 'License Class',
        render: (item) => (
          <strong style={{ color: 'var(--dvld-heading)' }}>
            {item.licenseClassName ?? '—'}
          </strong>
        ),
      },
      {
        key: 'issueDate',
        header: 'Issue Date',
        render: (item) => formatDate(item.issueDate),
      },
      {
        key: 'expirationDate',
        header: 'Expiration Date',
        render: (item) => formatDate(item.expirationDate),
      },
      {
        key: 'status',
        header: 'Status',
        render: (item) => <StatusBadge {...getActiveStatusProps(item.isActive)} />,
      },
    ],
    [],
  )

  const intlColumns = useMemo<DataTableColumn<InternationalLicense>[]>(
    () => [
      {
        key: 'internationalLicenseId',
        header: 'Int. License ID',
        render: (item) => (
          <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
            #{item.internationalLicenseId}
          </span>
        ),
      },
      {
        key: 'applicationId',
        header: 'App ID',
        render: (item) => (
          <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
            #{item.applicationId}
          </span>
        ),
      },
      {
        key: 'issuedUsingLocalLicenseId',
        header: 'Local License ID',
        render: (item) => (
          <span style={{ fontFamily: 'ui-monospace, monospace', color: 'var(--dvld-muted)' }}>
            #{item.issuedUsingLocalLicenseId}
          </span>
        ),
      },
      {
        key: 'issueDate',
        header: 'Issue Date',
        render: (item) => formatDate(item.issueDate),
      },
      {
        key: 'expirationDate',
        header: 'Expiration Date',
        render: (item) => formatDate(item.expirationDate),
      },
      {
        key: 'status',
        header: 'Status',
        render: (item) => <StatusBadge {...getActiveStatusProps(item.isActive)} />,
      },
    ],
    [],
  )

  if (!driver) return null

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onClose}
      size="2xl"
      title={
        <div className="flex items-center gap-2.5 text-lg font-semibold text-white">
          <History className="size-5 text-cyan-400" />
          <span>Person License History</span>
        </div>
      }
      description="Review local and international driving license records."
      footer={
        <div className="flex justify-end w-full">
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            className="border-[#263e68] bg-[#0e1a33] text-slate-300 hover:text-white hover:bg-[#16274a] rounded-xl cursor-pointer"
          >
            Close
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        {/* Driver identity banner */}
        <div className="flex flex-wrap items-center justify-between gap-3 p-4 rounded-xl bg-[#091428] border border-[#1e3458]">
          <div className="space-y-1">
            <h3 className="text-base font-bold text-white tracking-tight">
              {driver.fullName}
            </h3>
            <p className="text-xs text-slate-400">
              National No:{' '}
              <span className="font-mono font-semibold text-cyan-300">
                {driver.nationalNo}
              </span>
            </p>
          </div>
          <div className="flex items-center gap-2">
            <span className="font-mono text-xs font-semibold px-2.5 py-1 rounded-full bg-blue-500/20 text-blue-300 border border-blue-500/30">
              Driver #{driver.driverId}
            </span>
            <span className="font-mono text-xs font-semibold px-2.5 py-1 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
              Person #{driver.personId}
            </span>
          </div>
        </div>

        {/* Tab buttons */}
        <div className="flex border-b border-[#1e3458] gap-2 pt-1">
          <button
            type="button"
            onClick={() => setActiveTab('local')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-medium border-b-2 transition-colors cursor-pointer ${
              activeTab === 'local'
                ? 'border-cyan-400 text-cyan-300 font-semibold'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <IdCard className="size-4" />
            <span>Local Licenses</span>
            <span className="px-1.5 py-0.5 text-[10px] font-mono font-bold rounded-full bg-[#1b2f52] text-cyan-300">
              {localLicenses.length}
            </span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('international')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-medium border-b-2 transition-colors cursor-pointer ${
              activeTab === 'international'
                ? 'border-cyan-400 text-cyan-300 font-semibold'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <Globe className="size-4" />
            <span>International Licenses</span>
            <span className="px-1.5 py-0.5 text-[10px] font-mono font-bold rounded-full bg-[#1b2f52] text-cyan-300">
              {internationalLicenses.length}
            </span>
          </button>
        </div>

        {/* Content Area */}
        {loading ? (
          <div className="py-12 flex justify-center">
            <LoadingSpinner size="md" message="Loading license history..." />
          </div>
        ) : error ? (
          <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-300 flex items-center justify-between text-xs">
            <div className="flex items-center gap-2">
              <AlertCircle className="size-4 shrink-0 text-red-400" />
              <span>{error}</span>
            </div>
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => setRefreshKey((k) => k + 1)}
              className="text-xs h-7 text-red-200 border-red-500/40 hover:bg-red-500/20"
            >
              Retry
            </Button>
          </div>
        ) : activeTab === 'local' ? (
          localLicenses.length === 0 ? (
            <div className="py-8">
              <EmptyState
                icon={<IdCard size={32} />}
                message="No local driving licenses issued for this person."
              />
            </div>
          ) : (
            <div className="max-h-[350px] overflow-y-auto">
              <DataTable<License>
                data={localLicenses}
                columns={localColumns}
                getRowKey={(item) => item.licenseId}
                minWidth={600}
              />
            </div>
          )
        ) : internationalLicenses.length === 0 ? (
          <div className="py-8">
            <EmptyState
              icon={<Globe size={32} />}
              message="No international driving licenses issued for this driver."
            />
          </div>
        ) : (
          <div className="max-h-[350px] overflow-y-auto">
            <DataTable<InternationalLicense>
              data={internationalLicenses}
              columns={intlColumns}
              getRowKey={(item) => item.internationalLicenseId}
              minWidth={600}
            />
          </div>
        )}
      </div>
    </ModalShell>
  )
}
