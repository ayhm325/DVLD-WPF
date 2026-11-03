import { useCallback, useMemo, useState } from 'react'
import {
  Lock,
  Unlock,
  ShieldAlert,
  CheckCircle2,
  Plus,
  Coins,
} from 'lucide-react'
import type { DetainedLicense } from '../../types/DetainedLicense'
import { getDetainedLicenses } from '../../services/detainedLicenseService'
import { formatDate, formatCurrency, formatNumber } from '../../utils/formatters'
import { useAsyncData, useTableFilters } from '../../hooks'
import PageHero from '../common/PageHero'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import TableToolbar, { type SearchFieldOption } from '../common/TableToolbar'
import StatusBadge from '../common/StatusBadge'
import ErrorAlert from '../common/ErrorAlert'
import EmptyState from '../common/EmptyState'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import ReleaseDetainedLicenseModal from './ReleaseDetainedLicenseModal'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
import { Button } from '../ui'

type Props = {
  accessToken: string
  onNavigateToDetain: () => void
}

type SearchBy = 'all' | 'detainId' | 'licenseId' | 'nationalNo' | 'fullName'

interface DetainedFilters extends Record<string, unknown> {
  statusFilter: 'all' | 'active' | 'released'
}

const SEARCH_OPTIONS: SearchFieldOption<SearchBy>[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'detainId', label: 'Detain ID' },
  { value: 'licenseId', label: 'License ID' },
  { value: 'nationalNo', label: 'National No' },
  { value: 'fullName', label: 'Driver Name' },
]

export default function DetainedLicensesPage({
  accessToken,
  onNavigateToDetain,
}: Props) {
  const [selectedDetentionForRelease, setSelectedDetentionForRelease] =
    useState<DetainedLicense | null>(null)

  const fetcher = useCallback(() => getDetainedLicenses(accessToken), [accessToken])

  const {
    data: detentions,
    loading,
    error,
    refresh,
  } = useAsyncData(fetcher, {
    enabled: Boolean(accessToken),
    initialData: [],
    defaultErrorMessage: 'Failed to load detained licenses.',
  })

  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredDetentions,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<DetainedLicense, SearchBy, DetainedFilters>({
    data: detentions,
    initialSearchField: 'all',
    initialFilters: { statusFilter: 'all' },
    filterItem: (item, query, field, currentFilters) => {
      // 1. Status Filter
      const status = currentFilters.statusFilter
      if (status === 'active' && item.isReleased) return false
      if (status === 'released' && !item.isReleased) return false

      // 2. Search Query
      if (!query) return true

      const q = query.toLowerCase()
      if (field === 'detainId') return String(item.detainId).includes(q)
      if (field === 'licenseId') return String(item.licenseId).includes(q)
      if (field === 'nationalNo') return Boolean(item.nationalNo?.toLowerCase().includes(q))
      if (field === 'fullName') return Boolean(item.fullName?.toLowerCase().includes(q))

      return (
        String(item.detainId).includes(q) ||
        String(item.licenseId).includes(q) ||
        Boolean(item.nationalNo?.toLowerCase().includes(q)) ||
        Boolean(item.fullName?.toLowerCase().includes(q))
      )
    },
  })

  // Statistics
  const stats = useMemo(() => {
    const total = detentions.length
    const active = detentions.filter((d) => !d.isReleased).length
    const released = detentions.filter((d) => d.isReleased).length
    const totalFines = detentions.reduce((sum, d) => sum + (d.fineFees || 0), 0)
    return { total, active, released, totalFines }
  }, [detentions])

  const columns: DataTableColumn<DetainedLicense>[] = useMemo(
    () => [
      {
        key: 'detainId',
        header: 'Detain ID',
        render: (item: DetainedLicense) => (
          <span className="font-mono text-slate-400 font-bold">#{item.detainId}</span>
        ),
      },
      {
        key: 'licenseId',
        header: 'License ID',
        render: (item: DetainedLicense) => (
          <span className="font-mono text-cyan-400 font-bold">#{item.licenseId}</span>
        ),
      },
      {
        key: 'fullName',
        header: 'Driver Name',
        render: (item: DetainedLicense) => (
          <span className="font-semibold text-white">{item.fullName || '—'}</span>
        ),
      },
      {
        key: 'nationalNo',
        header: 'National No',
        render: (item: DetainedLicense) => (
          <span className="font-mono text-slate-300 text-xs">{item.nationalNo || '—'}</span>
        ),
      },
      {
        key: 'detainDate',
        header: 'Detain Date',
        render: (item: DetainedLicense) => (
          <span className="text-slate-300 text-xs">{formatDate(item.detainDate)}</span>
        ),
      },
      {
        key: 'fineFees',
        header: 'Fine Fees',
        render: (item: DetainedLicense) => (
          <span className="font-bold text-amber-300 text-xs">
            {formatCurrency(item.fineFees)}
          </span>
        ),
      },
      {
        key: 'isReleased',
        header: 'Status',
        render: (item: DetainedLicense) => (
          <StatusBadge
            label={item.isReleased ? 'Released' : 'Detained'}
            variant={item.isReleased ? 'success' : 'danger'}
          />
        ),
      },
      {
        key: 'releaseDate',
        header: 'Release Date',
        render: (item: DetainedLicense) => (
          <span className="text-slate-300 text-xs">
            {item.releaseDate ? formatDate(item.releaseDate) : '—'}
          </span>
        ),
      },
      {
        key: 'actions',
        header: 'Actions',
        render: (item: DetainedLicense) =>
          !item.isReleased ? (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={(e) => {
                e.stopPropagation()
                setSelectedDetentionForRelease(item)
              }}
              className="h-7 px-3 text-xs font-semibold text-emerald-300 bg-emerald-500/15 hover:bg-emerald-500/25 border-emerald-500/30 rounded-lg gap-1.5 transition-all cursor-pointer"
            >
              <Unlock className="size-3.5" />
              <span>Release</span>
            </Button>
          ) : (
            <span className="text-xs text-slate-500 italic">Released</span>
          ),
      },
    ],
    [],
  )

  return (
    <section className="page-content space-y-6">
      <PageHero
        pageId="Detained Licenses"
        title="Detained Licenses"
        highlight="Registry"
        description="Manage active and released detained driving licenses, monitor outstanding fine fees, and process license releases."
      />

      {/* KPI Stats Grid */}
      <StatsGrid columns={4}>
        <StatCard
          icon={<Lock className="size-5" />}
          label="Total Records"
          value={formatNumber(stats.total)}
          description="Total recorded detentions"
          variant="blue"
        />
        <StatCard
          icon={<ShieldAlert className="size-5" />}
          label="Active Detentions"
          value={formatNumber(stats.active)}
          description="Currently impounded"
          variant="rose"
        />
        <StatCard
          icon={<CheckCircle2 className="size-5" />}
          label="Released"
          value={formatNumber(stats.released)}
          description="Successfully released"
          variant="emerald"
        />
        <StatCard
          icon={<Coins className="size-5" />}
          label="Total Fines"
          value={formatCurrency(stats.totalFines)}
          description="Cumulative fine fees"
          variant="amber"
        />
      </StatsGrid>

      {error && <ErrorAlert title="Registry Error" message={error} onRetry={refresh} />}

      {/* Content Panel with TableToolbar & DataTable */}
      <div className="rounded-2xl border border-[#1b2f52] bg-[#0c192e] p-6 shadow-xl space-y-4">
        <TableToolbar<SearchBy>
          searchValue={search}
          onSearchChange={setSearch}
          searchField={searchBy}
          onSearchFieldChange={(field: SearchBy) => setSearchBy(field)}
          searchFieldOptions={SEARCH_OPTIONS}
          searchPlaceholder="Search detained licenses..."
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={refresh}
          refreshing={loading}
          filters={
            <Select
              value={filters.statusFilter}
              onValueChange={(val) =>
                setFilter('statusFilter', val as 'all' | 'active' | 'released')
              }
            >
              <SelectTrigger className="w-[140px] h-9 text-xs bg-[#081326] border-[#1b2f52] text-white">
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent className="bg-[#0c192e] border-[#1b2f52] text-white">
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="active">Active Only</SelectItem>
                <SelectItem value="released">Released Only</SelectItem>
              </SelectContent>
            </Select>
          }
          actions={
            <Button
              type="button"
              onClick={onNavigateToDetain}
              className="h-9 gap-2 text-xs font-semibold text-white bg-rose-600 hover:bg-rose-500 rounded-xl shadow-xs transition-all cursor-pointer"
            >
              <Plus className="size-4" />
              <span>Detain New License</span>
            </Button>
          }
        />

        <DataTable<DetainedLicense>
          columns={columns}
          data={filteredDetentions}
          getRowKey={(item: DetainedLicense) => item.detainId}
          loading={loading}
          emptyState={
            <EmptyState
              icon={<Lock className="size-10 text-slate-500" />}
              message="No Detained Licenses Found"
              description="No detained driving license records match the selected criteria."
            />
          }
        />
      </div>

      {/* Release Confirmation Modal */}
      {selectedDetentionForRelease && (
        <ReleaseDetainedLicenseModal
          isOpen={Boolean(selectedDetentionForRelease)}
          detention={selectedDetentionForRelease}
          accessToken={accessToken}
          onClose={() => setSelectedDetentionForRelease(null)}
          onReleased={() => {
            refresh()
            setSelectedDetentionForRelease(null)
          }}
        />
      )}
    </section>
  )
}
