import { useMemo, useCallback } from 'react'
import {
  Clock,
  CheckCircle2,
  Eye,
  FileText,
  CalendarDays,
  XCircle,
  AlertCircle,
  Award,
} from 'lucide-react'
import TableToolbar from '../common/TableToolbar'
import DataTable, { type DataTableColumn, type DataTableRowAction } from '../common/DataTable'
import StatusBadge from '../common/StatusBadge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
import type { TestAppointment } from '../../types/TestAppointment'
import type { LocalDrivingLicenseApplication } from '../../types/LocalDrivingLicenseApplication'
import type { TabType } from './appointmentStats'
import { resolveAppointmentLifecycle } from '../../utils/status'
import { formatJordanAppointmentDateTime } from '../../utils/jordanTime'

export interface TestAppointmentsTableProps {
  appointments: TestAppointment[]
  applicationMap: Map<number, LocalDrivingLicenseApplication>
  loading: boolean
  searchQuery: string
  onSearchChange: (query: string) => void
  testTypeFilter: string
  onTestTypeFilterChange: (testType: string) => void
  statusFilter: string
  onStatusFilterChange: (status: string) => void
  onResetFilters: () => void
  onRefresh: () => void
  activeTab: TabType
  onShowAppointmentDetails?: (appointmentId: number) => void
  onNavigate?: (page: string) => void
  onTakeExamination?: (appointmentId: number, returnPage?: string) => void
  onCancelAppointment: (appointment: TestAppointment) => void
}

export default function TestAppointmentsTable({
  appointments,
  applicationMap,
  loading,
  searchQuery,
  onSearchChange,
  testTypeFilter,
  onTestTypeFilterChange,
  statusFilter,
  onStatusFilterChange,
  onResetFilters,
  onRefresh,
  activeTab,
  onShowAppointmentDetails,
  onNavigate,
  onTakeExamination,
  onCancelAppointment,
}: TestAppointmentsTableProps) {
  // Data Table Columns
  const columns = useMemo<DataTableColumn<TestAppointment>[]>(() => [
    {
      key: 'testAppointmentId',
      header: 'Appt ID',
      sortable: true,
      sortValue: (r) => r.testAppointmentId,
      render: (r) => (
        <div className="flex items-center gap-1.5 font-mono">
          <span className="font-bold text-cyan-300">#{r.testAppointmentId}</span>
          {Boolean(r.retakeTestApplicationId) && (
            <span
              className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30"
              title={`Retake Examination for Application #${r.retakeTestApplicationId}`}
            >
              Retake
            </span>
          )}
        </div>
      ),
    },
    {
      key: 'appointmentDate',
      header: 'Appointment Date & Slot',
      sortable: true,
      sortValue: (r) => new Date(r.appointmentDate).getTime(),
      render: (r) => (
        <div className="space-y-0.5">
          <div className="font-medium text-white flex items-center gap-1.5">
            <Clock className="size-3 text-cyan-400 shrink-0" />
            <span>
              {formatJordanAppointmentDateTime(r.appointmentDate, r.appointmentDateFormatted)}
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-sans">Asia/Amman facility time</div>
        </div>
      ),
    },
    {
      key: 'applicant',
      header: 'Applicant / Candidate',
      render: (r) => {
        const app = applicationMap.get(r.localDrivingLicenseApplicationId)
        return (
          <div className="space-y-0.5">
            <div className="font-semibold text-slate-200">
              {app?.fullName ?? `Applicant (App #${r.localDrivingLicenseApplicationId})`}
            </div>
            {app?.nationalNo && (
              <div className="text-[10px] font-mono text-cyan-300/80">
                Nat ID: {app.nationalNo}
              </div>
            )}
          </div>
        )
      },
    },
    {
      key: 'localApplication',
      header: 'Local Application',
      render: (r) => {
        const app = applicationMap.get(r.localDrivingLicenseApplicationId)
        return (
          <div className="space-y-0.5">
            <div className="font-mono text-xs text-white">
              App #{r.localDrivingLicenseApplicationId}
            </div>
            <div className="text-[10px] text-slate-400 truncate max-w-[160px]">
              {app?.licenseClassName ?? 'Driving License'}
            </div>
          </div>
        )
      },
    },
    {
      key: 'testTypeName',
      header: 'Test Type',
      sortable: true,
      sortValue: (r) => r.testTypeName,
      render: (r) => (
        <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-semibold bg-slate-900/80 border border-slate-700/60 text-slate-200">
          <Award className="size-3.5 text-cyan-400" />
          <span>{r.testTypeName}</span>
        </div>
      ),
    },
    {
      key: 'lifecycleStatus',
      header: 'Lifecycle Status',
      render: (r) => {
        const lifecycle = resolveAppointmentLifecycle(r)
        return (
          <StatusBadge
            variant={lifecycle.statusVariant}
            label={lifecycle.statusLabel}
            title={lifecycle.description}
          />
        )
      },
    },
    {
      key: 'result',
      header: 'Exam Result',
      render: (r) => {
        const lifecycle = resolveAppointmentLifecycle(r)
        if (lifecycle.resultLabel === 'Passed') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
              <CheckCircle2 className="size-3" />
              <span>Passed</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'Failed') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">
              <XCircle className="size-3" />
              <span>Failed</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'No-Show') {
          return (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-500/10 text-amber-300 border border-amber-500/20">
              <AlertCircle className="size-3" />
              <span>No-Show</span>
            </span>
          )
        }
        if (lifecycle.resultLabel === 'Cancelled') {
          return (
            <span className="inline-flex items-center gap-1 text-[11px] text-slate-400 font-medium">
              <XCircle className="size-3 text-slate-500" />
              <span>Cancelled</span>
            </span>
          )
        }
        return <span className="text-slate-400 font-medium text-[11px]">Not Taken</span>
      },
    },
    {
      key: 'paidFees',
      header: 'Applicable Fee',
      sortable: true,
      sortValue: (r) => r.paidFees,
      render: (r) => (
        <span className="font-mono text-xs font-semibold text-emerald-400">
          {r.paidFees.toFixed(2)} JOD
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      align: 'right',
      render: (r) => (
        <div className="flex items-center justify-end gap-1.5">
          {!r.isLocked && (r.status === 'Scheduled' || r.status === 'Open') && (
            <button
              type="button"
              onClick={() => onCancelAppointment(r)}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-xs font-medium text-rose-300 bg-rose-950/40 hover:bg-rose-900/60 border border-rose-500/30 hover:border-rose-400 transition-all cursor-pointer shadow-sm"
              title="Cancel scheduled appointment"
            >
              <XCircle className="size-3.5" />
              <span>Cancel</span>
            </button>
          )}
          <button
            type="button"
            onClick={() => {
              if (onShowAppointmentDetails) {
                onShowAppointmentDetails(r.testAppointmentId)
              } else {
                onNavigate?.('Appointment Details')
              }
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium text-cyan-300 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-500/30 hover:border-cyan-400 transition-all cursor-pointer shadow-sm"
            title="View comprehensive examination record and audit details"
          >
            <Eye className="size-3.5" />
            <span>Details</span>
          </button>
        </div>
      ),
    },
  ], [applicationMap, onNavigate, onShowAppointmentDetails, onCancelAppointment])

  // Context Menu Actions (Right-Click)
  const rowContextMenu = useCallback(
    (apt: TestAppointment): DataTableRowAction<TestAppointment>[] => {
      const actions: DataTableRowAction<TestAppointment>[] = [
        {
          key: 'details',
          label: 'View Appointment Details',
          icon: <Eye className="size-4" />,
          onClick: () => {
            if (onShowAppointmentDetails) {
              onShowAppointmentDetails(apt.testAppointmentId)
            } else {
              onNavigate?.('Appointment Details')
            }
          },
        },
      ]

      if (!apt.isLocked && (apt.status === 'Scheduled' || apt.status === 'Open')) {
        actions.push({
          key: 'take-test',
          label: 'Take Examination',
          icon: <Award className="size-4 text-emerald-400" />,
          onClick: () => {
            if (onTakeExamination) {
              onTakeExamination(apt.testAppointmentId, 'Test Appointments')
            } else {
              onNavigate?.('Take Examination')
            }
          },
        })
        actions.push({
          key: 'cancel',
          label: 'Cancel Appointment',
          icon: <XCircle className="size-4 text-rose-400" />,
          onClick: () => onCancelAppointment(apt),
        })
      }

      if (onNavigate) {
        actions.push({
          key: 'goto-app',
          label: 'Go to Local Application',
          icon: <FileText className="size-4" />,
          onClick: () => onNavigate('Local Applications'),
        })
      }

      return actions
    },
    [onNavigate, onShowAppointmentDetails, onTakeExamination, onCancelAppointment],
  )

  return (
    <>
      {/* TableToolbar with EXACT order:
          1. Test Type filter (on the LEFT of search)
          2. Search input
          3. Appointment Status filter
          4. Refresh action */}
      <TableToolbar
        searchField={testTypeFilter}
        searchFieldOptions={[
          { value: 'all', label: 'All Test Types' },
          { value: '1', label: 'Vision Test' },
          { value: '2', label: 'Written Test' },
          { value: '3', label: 'Practical (Street) Test' },
        ]}
        onSearchFieldChange={onTestTypeFilterChange}
        searchFieldMinWidth={170}
        searchFieldAriaLabel="Filter by test type"
        searchValue={searchQuery}
        onSearchChange={onSearchChange}
        searchPlaceholder="Search by ID, candidate name, National ID..."
        searchAriaLabel="Search test appointments"
        filters={
          <div style={{ minWidth: 170 }} className="shrink-0">
            <Select value={statusFilter} onValueChange={onStatusFilterChange}>
              <SelectTrigger
                className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg cursor-pointer"
                aria-label="Filter by appointment status"
              >
                <SelectValue placeholder="All Statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="scheduled">Scheduled (Active)</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
                <SelectItem value="passed">Completed — Passed</SelectItem>
                <SelectItem value="failed">Completed — Failed</SelectItem>
                <SelectItem value="noshow">No-Show</SelectItem>
                <SelectItem value="cancelled">Cancelled</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
        isFiltered={Boolean(searchQuery || testTypeFilter !== 'all' || statusFilter !== 'all')}
        onReset={onResetFilters}
        onRefresh={onRefresh}
        refreshing={loading}
      />

      {/* Main Appointments Table */}
      <DataTable
        data={appointments}
        columns={columns}
        getRowKey={(r) => r.testAppointmentId}
        loading={loading}
        loadingMessage="Loading test appointments from DVLD database..."
        emptyMessage={
          searchQuery || testTypeFilter !== 'all' || statusFilter !== 'all'
            ? 'No appointment records matched your search and filter criteria.'
            : activeTab === 'today'
              ? 'No test appointments scheduled for today in Jordan facility time.'
              : activeTab === 'history'
                ? 'No past examination history records found.'
                : 'No test appointments recorded in the system yet.'
        }
        emptyIcon={<CalendarDays className="size-8 text-slate-600" />}
        minWidth={1000}
        rowContextMenu={rowContextMenu}
        onRowClick={(r) => {
          if (onShowAppointmentDetails) {
            onShowAppointmentDetails(r.testAppointmentId)
          } else {
            onNavigate?.('Appointment Details')
          }
        }}
        ariaLabel="Driving test appointments table"
      />
    </>
  )
}
