import { ChevronLeft, ChevronRight } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PAGE_SIZE_OPTIONS, type PageSizeOption } from "@/hooks/usePagination";
import "./PaginationControls.css";

export interface PaginationControlsProps {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPreviousPage: () => void;
  onNextPage: () => void;
  onPageSizeChange: (newPageSize: number) => void;
  loading?: boolean;
  itemName?: string;
  className?: string;
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
  itemName = "records",
  className = "",
}: PaginationControlsProps) {
  const start = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const end = Math.min(pageNumber * pageSize, totalCount);
  const safeTotalPages = Math.max(1, totalPages);

  return (
    <div
      className={`panel-footer pagination-controls ${className}`}
    >
      <span className="text-xs text-slate-400">
        Showing <strong className="text-white">{start}</strong>–
        <strong className="text-white">{end}</strong> of{" "}
        <strong className="text-white">{totalCount}</strong> {itemName}
      </span>

      <div className="pagination-controls__settings">
        {/* Page Size Selection Dropdown */}
        <div
          className="pagination-controls__page-size"
        >
          <span>Page size</span>
          <div className="pagination-controls__page-size-select">
            <Select
              value={String(pageSize)}
              onValueChange={(val) => {
                const num = Number(val);
                if (num > 0) {
                  onPageSizeChange(num);
                }
              }}
              disabled={loading}
            >
              <SelectTrigger
                aria-label="Select page size"
                className="h-8 min-h-8 rounded-lg px-2.5 py-1 text-xs"
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
        <div className="pagination-controls__navigation">
          <button
            type="button"
            className="secondary-button pagination-controls__button"
            disabled={!hasPreviousPage || loading}
            onClick={onPreviousPage}
            aria-label="Previous page"
          >
            <ChevronLeft size={14} />
            <span>Previous</span>
          </button>

          <span className="pagination-controls__page-indicator">
            Page{" "}
            <strong>
              {pageNumber}
            </strong>{" "}
            of{" "}
            <strong>
              {safeTotalPages}
            </strong>
          </span>

          <button
            type="button"
            className="secondary-button pagination-controls__button"
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
  );
}
