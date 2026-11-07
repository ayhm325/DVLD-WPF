import {
  CalendarDays,
  Clock,
  CheckCircle2,
  AlertTriangle,
  Calendar,
  XCircle,
  AlertCircle,
} from 'lucide-react'
import StatsGrid from '../common/StatsGrid'
import StatCard from '../common/StatCard'
import type { TabType, TabStats } from './appointmentStats'

export interface AppointmentSummaryProps {
  activeTab: TabType
  currentStats: TabStats
  loading: boolean
  error: boolean
  todayJordanStr: string
  statusFilter: string
  onKpiClick: (status: string) => void
}

export default function AppointmentSummary({
  activeTab,
  currentStats,
  loading,
  error,
  todayJordanStr,
  statusFilter,
  onKpiClick,
}: AppointmentSummaryProps) {
  return (
    <StatsGrid columns={5} className="lg:grid-cols-5">
      {activeTab === 'history' ? (
        <>
          {/* 1. Historical Total */}
          <StatCard
            label="Historical Total"
            value={loading ? '...' : error ? '—' : currentStats.total}
            icon={<Clock className="size-5" />}
            variant="blue"
            description="Concluded records"
            onClick={() => onKpiClick('all')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'all'
                ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 2. Passed */}
          <StatCard
            label="Passed Tests"
            value={loading ? '...' : error ? '—' : currentStats.passed}
            icon={<CheckCircle2 className="size-5" />}
            variant="emerald"
            description="Successful evaluations"
            onClick={() => onKpiClick('passed')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'passed'
                ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 3. Failed */}
          <StatCard
            label="Failed Tests"
            value={loading ? '...' : error ? '—' : currentStats.failed}
            icon={<XCircle className="size-5" />}
            variant="rose"
            description="Unsuccessful evaluations"
            onClick={() => onKpiClick('failed')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'failed'
                ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 4. No-Show */}
          <StatCard
            label="No-Show Tests"
            value={loading ? '...' : error ? '—' : currentStats.noShow}
            icon={<AlertCircle className="size-5" />}
            variant="amber"
            description="Applicant did not attend"
            onClick={() => onKpiClick('noshow')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'noshow'
                ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 5. Cancelled */}
          <StatCard
            label="Cancelled Records"
            value={loading ? '...' : error ? '—' : currentStats.cancelled}
            icon={<AlertTriangle className="size-5" />}
            variant="slate"
            description="Soft-cancelled prior to test"
            onClick={() => onKpiClick('cancelled')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'cancelled'
                ? 'ring-2 ring-slate-400 shadow-[0_0_12px_rgba(148,163,184,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
        </>
      ) : activeTab === 'today' ? (
        <>
          {/* 1. Today's Total */}
          <StatCard
            label="Today's Total"
            value={loading ? '...' : error ? '—' : currentStats.total}
            icon={<Calendar className="size-5" />}
            variant="blue"
            description={`Jordan facility: ${todayJordanStr}`}
            onClick={() => onKpiClick('all')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'all'
                ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 2. Scheduled Today */}
          <StatCard
            label="Scheduled Today"
            value={loading ? '...' : error ? '—' : currentStats.scheduled}
            icon={<Clock className="size-5" />}
            variant="purple"
            description="Awaiting examination today"
            onClick={() => onKpiClick('scheduled')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'scheduled'
                ? 'ring-2 ring-purple-400 shadow-[0_0_12px_rgba(168,85,247,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 3. Completed Today */}
          <StatCard
            label="Completed Today"
            value={loading ? '...' : error ? '—' : currentStats.completed}
            icon={<CheckCircle2 className="size-5" />}
            variant="emerald"
            description={`${currentStats.passed} Passed • ${currentStats.failed} Failed`}
            onClick={() => onKpiClick('completed')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'completed'
                ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 4. No-Show Today */}
          <StatCard
            label="No-Show Today"
            value={loading ? '...' : error ? '—' : currentStats.noShow}
            icon={<AlertCircle className="size-5" />}
            variant="amber"
            description="Missed slot today"
            onClick={() => onKpiClick('noshow')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'noshow'
                ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 5. Cancelled Today */}
          <StatCard
            label="Cancelled Today"
            value={loading ? '...' : error ? '—' : currentStats.cancelled}
            icon={<AlertTriangle className="size-5" />}
            variant="rose"
            description="Cancelled today"
            onClick={() => onKpiClick('cancelled')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'cancelled'
                ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
        </>
      ) : (
        <>
          {/* 1. Total Appointments */}
          <StatCard
            label="Total Appointments"
            value={loading ? '...' : error ? '—' : currentStats.total}
            icon={<CalendarDays className="size-5" />}
            variant="blue"
            description="All recorded appointments"
            onClick={() => onKpiClick('all')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'all'
                ? 'ring-2 ring-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 2. Scheduled */}
          <StatCard
            label="Scheduled"
            value={loading ? '...' : error ? '—' : currentStats.scheduled}
            icon={<Clock className="size-5" />}
            variant="purple"
            description="Awaiting examination"
            onClick={() => onKpiClick('scheduled')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'scheduled'
                ? 'ring-2 ring-purple-400 shadow-[0_0_12px_rgba(168,85,247,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 3. Completed */}
          <StatCard
            label="Completed"
            value={loading ? '...' : error ? '—' : currentStats.completed}
            icon={<CheckCircle2 className="size-5" />}
            variant="emerald"
            description={`${currentStats.passed} Passed • ${currentStats.failed} Failed`}
            onClick={() => onKpiClick('completed')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'completed'
                ? 'ring-2 ring-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 4. No-Show */}
          <StatCard
            label="No-Show"
            value={loading ? '...' : error ? '—' : currentStats.noShow}
            icon={<AlertCircle className="size-5" />}
            variant="amber"
            description="Applicant did not attend"
            onClick={() => onKpiClick('noshow')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'noshow'
                ? 'ring-2 ring-amber-400 shadow-[0_0_12px_rgba(245,158,11,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
          {/* 5. Cancelled */}
          <StatCard
            label="Cancelled"
            value={loading ? '...' : error ? '—' : currentStats.cancelled}
            icon={<AlertTriangle className="size-5" />}
            variant="rose"
            description="Cancelled appointments"
            onClick={() => onKpiClick('cancelled')}
            className={`cursor-pointer transition-all ${
              statusFilter === 'cancelled'
                ? 'ring-2 ring-rose-400 shadow-[0_0_12px_rgba(244,63,94,0.3)]'
                : 'hover:border-slate-700'
            }`}
          />
        </>
      )}
    </StatsGrid>
  )
}
