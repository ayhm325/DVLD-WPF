import { useMemo } from 'react'
import {
  Clock,
  RotateCcw,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Filter,
  IdCard,
} from 'lucide-react'
import type { License } from '../../types/License'
import { formatDate } from '../../utils/formatters'
import { getActiveStatusProps } from '../../utils/status'
import { cn } from '@/lib/utils'
import StatusBadge from '../common/StatusBadge'
import TableToolbar, { type SearchFieldOption } from '../common/TableToolbar'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'
import { Button, Card, CardHeader, CardTitle, CardDescription } from '../ui'
import { useTableFilters, usePagination } from '../../hooks'

export interface ExpiredLicensesTableProps {
  expiredLicenses: License[]
  selectedLicenseId: number | null
  loading: boolean
  loadingDetails: boolean
  onSelectLicense: (licenseId: number) => void
  onRefresh: () => void
}

type SearchBy = 'all' | 'licenseId' | 'driver' | 'class'

interface ExpiredLicenseFilters extends Record<string, unknown> {
  eligibilityFilter: 'all' | 'eligible' | 'ineligible'
}

const SEARCH_OPTIONS: SearchFieldOption<SearchBy>[] = [
  { value: 'all', label: 'All Fields' },
  { value: 'licenseId', label: 'License ID' },
  { value: 'driver', label: 'Driver Name' },
  { value: 'class', label: 'License Class' },
]

export default function ExpiredLicensesTable({
  expiredLicenses,
  selectedLicenseId,
  loading,
  loadingDetails,
  onSelectLicense,
  onRefresh,
}: ExpiredLicensesTableProps) {
  // Search and Filtering for Expired Licenses
  const {
    search,
    setSearch,
    searchField: searchBy,
    setSearchField: setSearchBy,
    filters,
    setFilter,
    filteredData: filteredExpiredLicenses,
    isFiltered,
    resetFilters: handleResetFilters,
  } = useTableFilters<License, SearchBy, ExpiredLicenseFilters>({
    data: expiredLicenses,
    initialSearchField: 'all',
    initialFilters: { eligibilityFilter: 'all' },
    filterItem: (lic, query, field, currentFilters) => {
      // Authoritative Eligibility filter
      if (currentFilters.eligibilityFilter === 'eligible' && !lic.isEligibleForRenewal) {
        return false
      }
      if (currentFilters.eligibilityFilter === 'ineligible' && lic.isEligibleForRenewal) {
        return false
      }

      // Query check
      if (!query) return true

      if (field === 'licenseId') {
        return String(lic.licenseId).includes(query)
      }
      if (field === 'driver') {
        return (lic.driverName ?? '').toLowerCase().includes(query)
      }
      if (field === 'class') {
        return (lic.licenseClassName ?? '').toLowerCase().includes(query)
      }

      // 'all'
      return (
        String(lic.licenseId).includes(query) ||
        (lic.driverName ?? '').toLowerCase().includes(query) ||
        (lic.licenseClassName ?? '').toLowerCase().includes(query)
      )
    },
  })

  // Client-side Pagination
  const pagination = usePagination<License>({
    data: filteredExpiredLicenses,
    pageSize: 10,
    initialPage: 1,
  })

  // Table Columns
  const columns = useMemo<DataTableColumn<License>[]>(
    () => [
      {
        key: 'licenseId',
        header: 'License ID',
        render: (lic) => (
          <span className="font-mono text-cyan-400 font-bold">
            #{lic.licenseId}
          </span>
        ),
      },
      {
        key: 'driverName',
        header: 'Driver Name',
        render: (lic) => (
          <strong className="text-white font-semibold">
            {lic.driverName ?? '—'}
          </strong>
        ),
      },
      {
        key: 'licenseClassName',
        header: 'License Class',
        render: (lic) => (
          <span className="text-slate-300 text-xs">
            {lic.licenseClassName ?? '—'}
          </span>
        ),
      },
      {
        key: 'expirationDate',
        header: 'Expiration Date',
        render: (lic) => (
          <span className="text-amber-300 font-mono text-xs">
            {formatDate(lic.expirationDate)}
          </span>
        ),
      },
      {
        key: 'status',
        header: 'License Status',
        render: (lic) => <StatusBadge {...getActiveStatusProps(lic.isActive)} />,
      },
      {
        key: 'eligibility',
        header: 'Renewal Eligibility',
        render: (lic) => {
          if (lic.isEligibleForRenewal) {
            return (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
                <span className="size-1.5 rounded-full bg-emerald-400" />
                Eligible
              </span>
            )
          }
          return (
            <span
              className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-500/15 border border-rose-500/30 text-rose-300 cursor-help"
              title={lic.ineligibilityReason ?? 'License is not eligible for renewal.'}
            >
              <span className="size-1.5 rounded-full bg-rose-400" />
              Ineligible
            </span>
          )
        },
      },
      {
        key: 'action',
        header: 'Action',
        align: 'right',
        render: (lic) => {
          const isSelected = selectedLicenseId === lic.licenseId
          return (
            <Button
              type="button"
              variant={isSelected ? 'default' : 'outline'}
              size="sm"
              disabled={loadingDetails || !lic.isEligibleForRenewal}
              onClick={() => onSelectLicense(lic.licenseId)}
              className={cn(
                'h-8 px-3 gap-1.5 text-xs font-semibold rounded-lg cursor-pointer transition-all',
                isSelected
                  ? 'bg-cyan-600 text-white border-cyan-400'
                  : 'bg-[#0e1b33] text-cyan-300 hover:text-white border-[#263750] hover:border-cyan-500/60 hover:bg-[#15284d]',
                !lic.isEligibleForRenewal && 'opacity-40 cursor-not-allowed hover:bg-[#0e1b33]',
              )}
              title={
                !lic.isEligibleForRenewal
                  ? (lic.ineligibilityReason ?? 'Cannot renew this license')
                  : 'Select license for renewal'
              }
            >
              {isSelected ? (
                <>
                  <CheckCircle2 className="size-3.5 text-cyan-300" />
                  <span>Selected</span>
                </>
              ) : (
                <>
                  <RotateCcw className="size-3.5" />
                  <span>Start Renewal</span>
                </>
              )}
            </Button>
          )
        },
      },
    ],
    [selectedLicenseId, loadingDetails, onSelectLicense],
  )

  return (
    <Card className="border-[#1b2f52] bg-[#0c192e] text-white shadow-xl rounded-2xl overflow-hidden space-y-4">
      <CardHeader className="border-b border-[#1b2f52]/60 p-5 pb-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <CardTitle className="text-base font-bold flex items-center gap-2">
              <Clock className="size-4.5 text-cyan-400" />
              <span>Expired Licenses</span>
            </CardTitle>
            <CardDescription className="text-xs text-slate-400 mt-0.5">
              Review expired driving licenses and select an eligible license to begin renewal.
            </CardDescription>
          </div>
          <span className="text-xs font-mono font-semibold px-2.5 py-1 rounded-full bg-cyan-950/80 border border-cyan-500/30 text-cyan-300 w-fit">
            {filteredExpiredLicenses.length} {filteredExpiredLicenses.length === 1 ? 'License' : 'Licenses'} Found
          </span>
        </div>
      </CardHeader>

      <div className="p-5 pt-0 space-y-4">
        {/* Table Toolbar */}
        <TableToolbar<SearchBy>
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder="Search expired licenses by ID, driver, or class..."
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={setSearchBy}
          isFiltered={isFiltered}
          onReset={handleResetFilters}
          onRefresh={onRefresh}
          refreshing={loading}
          filters={
            <div className="flex items-center gap-2">
              <Filter className="size-4 text-slate-400 shrink-0" />
              <Select
                value={filters.eligibilityFilter}
                onValueChange={(val) =>
                  setFilter('eligibilityFilter', val as ExpiredLicenseFilters['eligibilityFilter'])
                }
              >
                <SelectTrigger className="w-[180px] h-9 bg-[#081326] border-[#1b2f52] text-xs text-slate-200">
                  <SelectValue placeholder="Eligibility Filter" />
                </SelectTrigger>
                <SelectContent className="bg-[#0b162c] border-[#1b2f52] text-xs text-slate-200">
                  <SelectItem value="all">All Expired Licenses</SelectItem>
                  <SelectItem value="eligible">Eligible for Renewal</SelectItem>
                  <SelectItem value="ineligible">Ineligible for Renewal</SelectItem>
                </SelectContent>
              </Select>
            </div>
          }
        />

        {/* Expired Licenses DataTable */}
        <DataTable<License>
          data={pagination.paginatedData ?? []}
          columns={columns}
          getRowKey={(lic) => lic.licenseId}
          loading={loading}
          loadingMessage="Loading expired driving licenses from DVLD server..."
          emptyMessage="No expired driving licenses found matching the selected filter."
          emptyIcon={<IdCard className="size-8 text-slate-500" />}
        />

        {/* Table Pagination */}
        {filteredExpiredLicenses.length > 0 && (
          <div className="flex items-center justify-between pt-2 text-xs text-slate-400">
            <span>
              Showing{' '}
              <strong className="text-white">
                {(pagination.page - 1) * pagination.pageSize + 1}
              </strong>{' '}
              to{' '}
              <strong className="text-white">
                {Math.min(
                  pagination.page * pagination.pageSize,
                  filteredExpiredLicenses.length,
                )}
              </strong>{' '}
              of <strong className="text-white">{filteredExpiredLicenses.length}</strong> expired licenses
            </span>

            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={pagination.previousPage}
                disabled={!pagination.hasPreviousPage}
                className="h-8 gap-1 text-xs bg-[#081326] border-[#1b2f52] text-slate-300 hover:text-white"
              >
                <ChevronLeft className="size-3.5" />
                <span>Previous</span>
              </Button>
              <span className="font-mono text-xs px-2 text-slate-300">
                Page {pagination.page} of {pagination.totalPages || 1}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={pagination.nextPage}
                disabled={!pagination.hasNextPage}
                className="h-8 gap-1 text-xs bg-[#081326] border-[#1b2f52] text-slate-300 hover:text-white"
              >
                <span>Next</span>
                <ChevronRight className="size-3.5" />
              </Button>
            </div>
          </div>
        )}
      </div>
    </Card>
  )
}
