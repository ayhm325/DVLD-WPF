import {
  Users,
  Car,
  IdCard,
  ClipboardList,
  RefreshCw,
  AlertCircle,
  TrendingUp,
} from 'lucide-react'
import DashboardHero from './dashboard/DashboardHero'
import PeopleTable from './PeopleTable'
import StatsGrid from './common/StatsGrid'
import StatCard from './common/StatCard'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { useDashboardStatistics } from '../hooks/useDashboardStatistics'
import type { Person } from '../types/Person'
import { formatNumber } from '../utils/formatters'

export type DashboardPageProps = {
  accessToken: string
  statisticsRefreshKey?: number
  people: Person[]
  filteredPeople?: Person[]
  searchName: string
  onSearchChange: (value: string) => void
  loadingPeople: boolean
  peopleError: string
  onRetryPeople: () => void
  onAddPerson: () => void
}

export default function DashboardPage({
  accessToken,
  statisticsRefreshKey = 0,
  people,
  searchName,
  onSearchChange,
  loadingPeople,
  peopleError,
  onRetryPeople,
  onAddPerson,
}: DashboardPageProps) {
  const { statistics, loading, error, retry } = useDashboardStatistics(
    accessToken,
    statisticsRefreshKey,
  )

  return (
    <section className="page-content space-y-7">
      {/* Premium Neon Dashboard Hero Banner */}
      <DashboardHero />

      {/* Operational Statistics Section */}
      <div className="space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <div className="flex items-center gap-2.5">
              <h2 className="text-xl font-bold text-white tracking-tight">
                Operational Statistics
              </h2>
              <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-blue-500/15 border border-blue-400/30 text-blue-300 text-xs font-semibold">
                <span className="size-1.5 rounded-full bg-cyan-400 shadow-[0_0_6px_#22d3ee]" />
                Live Metrics
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              Key department metrics synchronized with the DVLD central database
            </p>
          </div>

          <div className="flex items-center gap-2 self-start sm:self-auto">
            {loading && (
              <Badge
                variant="secondary"
                className="gap-1.5 py-1 text-xs font-normal bg-blue-500/15 text-blue-300 border-blue-500/30"
              >
                <Spinner size="sm" />
                <span>Updating...</span>
              </Badge>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={retry}
              disabled={loading}
              className="h-9 gap-2 text-xs font-semibold text-white bg-[#0e1b33] hover:bg-[#142647] border border-[#263750] hover:border-blue-500/40 rounded-xl shadow-xs transition-all cursor-pointer"
            >
              <RefreshCw className={cn('size-3.5 text-blue-400', loading && 'animate-spin')} />
              <span>Refresh Counters</span>
            </Button>
          </div>
        </div>

        {/* 4 Neon Statistics Cards Grid */}
        <StatsGrid columns={4} className="mb-0">
          {/* Total People (Electric Blue) */}
          <StatCard
            label="Total People"
            variant="blue"
            icon={<Users className="size-6" />}
            value={
              loading ? (
                '...'
              ) : error ? (
                '—'
              ) : typeof statistics?.totalPeople === 'number' ? (
                formatNumber(statistics.totalPeople)
              ) : (
                statistics?.totalPeople ?? '—'
              )
            }
            loading={loading}
            description="Registered citizens"
            badge={
              <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-blue-200 bg-blue-950/90 px-2.5 py-0.5 rounded-full border border-blue-500/40 shadow-[0_0_10px_rgba(59,130,246,0.25)]">
                <TrendingUp className="size-3" />
                Directory
              </span>
            }
          />

          {/* Total Drivers (Emerald Green) */}
          <StatCard
            label="Total Drivers"
            variant="emerald"
            icon={<Car className="size-6" />}
            value={
              loading ? (
                '...'
              ) : error ? (
                '—'
              ) : typeof statistics?.totalDrivers === 'number' ? (
                formatNumber(statistics.totalDrivers)
              ) : (
                statistics?.totalDrivers ?? '—'
              )
            }
            loading={loading}
            description="Certified drivers"
            badge={
              <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-200 bg-emerald-950/90 px-2.5 py-0.5 rounded-full border border-emerald-500/40 shadow-[0_0_10px_rgba(16,185,129,0.25)]">
                <TrendingUp className="size-3" />
                Licensed
              </span>
            }
          />

          {/* Active Licenses (Neon Purple) */}
          <StatCard
            label="Active Licenses"
            variant="violet"
            icon={<IdCard className="size-6" />}
            value={
              loading ? (
                '...'
              ) : error ? (
                '—'
              ) : typeof statistics?.activeLicenses === 'number' ? (
                formatNumber(statistics.activeLicenses)
              ) : (
                statistics?.activeLicenses ?? '—'
              )
            }
            loading={loading}
            description="Valid driving permits"
            badge={
              <span className="inline-flex items-center text-[11px] font-semibold text-purple-200 bg-purple-950/90 px-2.5 py-0.5 rounded-full border border-purple-500/40 shadow-[0_0_10px_rgba(168,85,247,0.25)]">
                Active
              </span>
            }
          />

          {/* Pending Applications (Amber / Gold) */}
          <StatCard
            label="Pending Applications"
            variant="amber"
            icon={<ClipboardList className="size-6" />}
            value={
              loading ? (
                '...'
              ) : error ? (
                '—'
              ) : typeof statistics?.pendingApplications === 'number' ? (
                formatNumber(statistics.pendingApplications)
              ) : (
                statistics?.pendingApplications ?? '—'
              )
            }
            loading={loading}
            description="Awaiting review"
            badge={
              <span className="inline-flex items-center text-[11px] font-semibold text-amber-200 bg-amber-950/90 px-2.5 py-0.5 rounded-full border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.25)]">
                Queued
              </span>
            }
          />
        </StatsGrid>
      </div>

      {/* Error Alerts */}
      {error && (
        <div
          role="alert"
          className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-xl border border-red-200 bg-red-50/70 p-4 text-sm text-red-700 shadow-2xs"
        >
          <div className="flex items-start sm:items-center gap-3">
            <AlertCircle className="size-5 shrink-0 text-red-600 mt-0.5 sm:mt-0" />
            <div>
              <p className="font-semibold text-red-900">Statistics Loading Error</p>
              <p className="text-xs text-red-600/90 mt-0.5">{error}</p>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={retry}
            className="border-red-300 text-red-700 hover:bg-red-100/70 shrink-0 self-start sm:self-auto bg-white"
          >
            Retry Statistics
          </Button>
        </div>
      )}

      {peopleError && (
        <div
          role="alert"
          className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-xl border border-red-200 bg-red-50/70 p-4 text-sm text-red-700 shadow-2xs"
        >
          <div className="flex items-start sm:items-center gap-3">
            <AlertCircle className="size-5 shrink-0 text-red-600 mt-0.5 sm:mt-0" />
            <div>
              <p className="font-semibold text-red-900">People List Loading Error</p>
              <p className="text-xs text-red-600/90 mt-0.5">{peopleError}</p>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onRetryPeople}
            className="border-red-300 text-red-700 hover:bg-red-100/70 shrink-0 self-start sm:self-auto bg-white"
          >
            Retry People List
          </Button>
        </div>
      )}

      {/* People Directory Table */}
      <PeopleTable
        people={people}
        searchName={searchName}
        onSearchChange={onSearchChange}
        onAddPerson={onAddPerson}
        onRefresh={onRetryPeople}
        loading={loadingPeople}
      />
    </section>
  )
}
