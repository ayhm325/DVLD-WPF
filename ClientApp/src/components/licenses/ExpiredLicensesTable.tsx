import { useMemo } from 'react'
import { Clock, RotateCcw, CheckCircle2, IdCard } from 'lucide-react'
import type { License } from '../../types/License'
import { formatDate } from '../../utils/formatters'
import { getActiveStatusProps } from '../../utils/status'
import { cn } from '@/lib/utils'
import StatusBadge from '../common/StatusBadge'
import TableToolbar, { type SearchFieldOption } from '../common/TableToolbar'
import DataTable, { type DataTableColumn } from '../common/DataTable'
import PaginationControls from '../common/PaginationControls'
import { Button, Card, CardHeader, CardTitle, CardDescription } from '../ui'

export type ExpiredLicensesSearchBy = 'all' | 'licenseId' | 'driver' | 'class'

export interface ExpiredLicensesTableProps {
  expiredLicenses: License[]
  selectedLicenseId: number | null
  loading: boolean
  loadingDetails: boolean
  onSelectLicense: (licenseId: number) => void
  onRefresh: () => void
  search?: string
  onSearchChange?: (val: string) => void
  searchBy?: ExpiredLicensesSearchBy
  onSearchByChange?: (val: ExpiredLicensesSearchBy) => void
  onResetFilters?: () => void
  totalCount?: number
  pageNumber?: number
  pageSize?: number
  totalPages?: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
  onPreviousPage?: () => void
  onNextPage?: () => void
  onPageSizeChange?: (newPageSize: number) => void
}

const SEARCH_OPTIONS: SearchFieldOption<ExpiredLicensesSearchBy>[] = [
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
  search = '',
  onSearchChange,
  searchBy = 'all',
  onSearchByChange,
  onResetFilters,
  totalCount = 0,
  pageNumber = 1,
  pageSize = 10,
  totalPages = 1,
  hasPreviousPage = false,
  hasNextPage = false,
  onPreviousPage = () => {},
  onNextPage = () => {},
  onPageSizeChange = () => {},
}: ExpiredLicensesTableProps) {
  const isFiltered = Boolean(search && search.trim()) || searchBy !== 'all'

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
            {totalCount} {totalCount === 1 ? 'License' : 'Licenses'} Total
          </span>
        </div>
      </CardHeader>

      <div className="p-5 pt-0 space-y-4">
        {/* Table Toolbar */}
        <TableToolbar<ExpiredLicensesSearchBy>
          searchValue={search}
          onSearchChange={onSearchChange ?? (() => {})}
          searchPlaceholder="Search expired licenses by ID, driver, or class..."
          searchField={searchBy}
          searchFieldOptions={SEARCH_OPTIONS}
          onSearchFieldChange={onSearchByChange ?? (() => {})}
          isFiltered={isFiltered}
          onReset={onResetFilters}
          onRefresh={onRefresh}
          refreshing={loading}
        />

        {/* Expired Licenses DataTable */}
        <DataTable<License>
          data={expiredLicenses}
          columns={columns}
          getRowKey={(lic) => lic.licenseId}
          loading={loading}
          loadingMessage="Loading expired driving licenses from DVLD server..."
          emptyMessage="No expired driving licenses found matching the selected filter."
          emptyIcon={<IdCard className="size-8 text-slate-500" />}
        />

        {/* Table Pagination */}
        {totalCount > 0 && (
          <PaginationControls
            pageNumber={pageNumber}
            pageSize={pageSize}
            totalCount={totalCount}
            totalPages={totalPages}
            hasPreviousPage={hasPreviousPage}
            hasNextPage={hasNextPage}
            onPreviousPage={onPreviousPage}
            onNextPage={onNextPage}
            onPageSizeChange={onPageSizeChange}
            loading={loading}
            itemName="expired licenses"
          />
        )}
      </div>
    </Card>
  )
}
