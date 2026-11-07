import { ChevronLeft, ChevronRight } from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { PAGE_SIZE_OPTIONS, type PageSizeOption } from '@/hooks/usePagination'

export interface PaginationControlsProps {
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  onPreviousPage: () => void
  onNextPage: () => void
  onPageSizeChange: (newPageSize: number) => void
  loading?: boolean
  itemName?: string
  className?: string
}

export default function PaginationControls({
  pageNumber,
  pageSize,
  totalCount,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  onPreviousPage,
  onNextPage,
  onPageSizeChange,
  loading = false,
  itemName = 'records',
  className = '',
}: PaginationControlsProps) {
  const start = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
  const end = Math.min(pageNumber * pageSize, totalCount)
  const safeTotalPages = Math.max(1, totalPages)

  return (
    <div
      className={`panel-footer flex flex-col sm:flex-row items-center justify-between gap-4 p-4 border-t border-[#1e304d] ${className}`}
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 16,
        padding: '16px 24px',
        borderTop: '1px solid #1e304d',
      }}
    >
      <span className="text-xs text-slate-400">
        Showing <strong className="text-white">{start}</strong>–
        <strong className="text-white">{end}</strong> of{' '}
        <strong className="text-white">{totalCount}</strong> {itemName}
      </span>

      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 12,
        }}
      >
        {/* Page Size Selection Dropdown */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            fontSize: 11,
            color: 'var(--dvld-muted)',
          }}
        >
          <span>Page size</span>
          <div style={{ width: 80 }}>
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                const num = Number(val)
                if (num > 0) {
                  onPageSizeChange(num)
                }
              }}
              disabled={loading}
            >
              <SelectTrigger
                aria-label="Select page size"
                className="h-8 min-h-[32px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg px-2.5 py-1"
                style={{
                  minHeight: 32,
                  height: 32,
                  padding: '4px 8px',
                  fontSize: 11,
                }}
              >
                <SelectValue placeholder={String(pageSize)} />
              </SelectTrigger>
              <SelectContent className="bg-[#0e1726] border-[#223656] text-white">
                {PAGE_SIZE_OPTIONS.map((size: PageSizeOption) => (
                  <SelectItem
                    key={size}
                    value={String(size)}
                    className="text-xs hover:bg-blue-600/20 focus:bg-blue-600/30 cursor-pointer"
                  >
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Navigation Buttons and Page Indicator */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <button
            type="button"
            className="secondary-button"
            style={{ minHeight: 32, padding: '5px 12px', fontSize: 11 }}
            disabled={!hasPreviousPage || loading}
            onClick={onPreviousPage}
            aria-label="Previous page"
          >
            <ChevronLeft size={14} />
            <span>Previous</span>
          </button>

          <span style={{ fontSize: 11, color: 'var(--dvld-muted)' }}>
            Page <strong style={{ color: 'var(--dvld-heading)' }}>{pageNumber}</strong> of{' '}
            <strong style={{ color: 'var(--dvld-heading)' }}>{safeTotalPages}</strong>
          </span>

          <button
            type="button"
            className="secondary-button"
            style={{ minHeight: 32, padding: '5px 12px', fontSize: 11 }}
            disabled={!hasNextPage || loading}
            onClick={onNextPage}
            aria-label="Next page"
          >
            <span>Next</span>
            <ChevronRight size={14} />
          </button>
        </div>
      </div>
    </div>
  )
}
