import { useCallback, useMemo, useState } from 'react'

export const PAGE_SIZE_OPTIONS = [10, 20, 60, 80, 100] as const
export type PageSizeOption = (typeof PAGE_SIZE_OPTIONS)[number]

export interface UsePaginationOptions<T = unknown> {
  /** Total count of items across all pages. If data array is provided, defaults to data.length. */
  totalCount?: number
  /** Total number of pages (if known from server response). */
  totalPages?: number
  /** Number of items displayed per page. Defaults to 10. */
  pageSize?: number
  /** Initial page index (1-based). Defaults to 1. */
  initialPage?: number
  /** Optional array of data items to slice for client-side pagination. */
  data?: T[]
  /** Optional callback fired when page changes. */
  onPageChange?: (page: number) => void
  /** Optional callback fired when page size changes. */
  onPageSizeChange?: (pageSize: number) => void
}

export interface UsePaginationResult<T = unknown> {
  page: number
  pageSize: number
  setPage: (page: number | ((prev: number) => number)) => void
  setPageSize: (size: number) => void
  totalPages: number
  totalCount: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  paginatedData?: T[]
  goToPage: (page: number) => void
  nextPage: () => void
  previousPage: () => void
  resetPage: () => void
}

/**
 * Reusable pagination hook supporting both server-coordinated pagination metadata
 * and client-side array slicing with bounds checking.
 */
export function usePagination<T = unknown>(
  options: UsePaginationOptions<T> = {},
): UsePaginationResult<T> {
  const {
    totalCount: explicitTotalCount,
    totalPages: explicitTotalPages,
    pageSize: initialPageSize = 10,
    initialPage = 1,
    data,
    onPageChange,
    onPageSizeChange,
  } = options

  const [page, setPage] = useState<number>(initialPage)
  const [pageSize, setPageSizeState] = useState<number>(initialPageSize)

  const effectiveTotalCount =
    explicitTotalCount !== undefined
      ? explicitTotalCount
      : data
        ? data.length
        : 0

  const calculatedTotalPages = Math.max(1, Math.ceil(effectiveTotalCount / pageSize))
  const totalPages =
    explicitTotalPages !== undefined
      ? Math.max(1, explicitTotalPages)
      : calculatedTotalPages

  const hasPreviousPage = page > 1
  const hasNextPage =
    explicitTotalPages !== undefined
      ? page < explicitTotalPages
      : explicitTotalCount !== undefined || data !== undefined
        ? page < totalPages
        : true

  const goToPage = useCallback(
    (targetPage: number) => {
      setPage((prev) => {
        const max =
          explicitTotalPages !== undefined
            ? explicitTotalPages
            : explicitTotalCount !== undefined || data !== undefined
              ? totalPages
              : Infinity
        const clamped = Math.max(1, Math.min(max, targetPage))
        if (clamped !== prev) {
          onPageChange?.(clamped)
        }
        return clamped
      })
    },
    [explicitTotalPages, explicitTotalCount, data, totalPages, onPageChange],
  )

  const nextPage = useCallback(() => {
    setPage((prev) => {
      const max =
        explicitTotalPages !== undefined
          ? explicitTotalPages
          : explicitTotalCount !== undefined || data !== undefined
            ? totalPages
            : Infinity
      const next = prev + 1
      if (next <= max) {
        onPageChange?.(next)
        return next
      }
      return prev
    })
  }, [explicitTotalPages, explicitTotalCount, data, totalPages, onPageChange])

  const previousPage = useCallback(() => {
    setPage((prev) => {
      const next = Math.max(1, prev - 1)
      if (next !== prev) {
        onPageChange?.(next)
      }
      return next
    })
  }, [onPageChange])

  const resetPage = useCallback(() => {
    setPage(initialPage)
    onPageChange?.(initialPage)
  }, [initialPage, onPageChange])

  const setPageSize = useCallback(
    (newSize: number) => {
      setPageSizeState(newSize)
      setPage(1)
      onPageSizeChange?.(newSize)
      onPageChange?.(1)
    },
    [onPageChange, onPageSizeChange],
  )

  const paginatedData = useMemo(() => {
    if (!data) return undefined
    const start = (page - 1) * pageSize
    return data.slice(start, start + pageSize)
  }, [data, page, pageSize])

  return {
    page,
    pageSize,
    setPage,
    setPageSize,
    totalPages,
    totalCount: effectiveTotalCount,
    hasPreviousPage,
    hasNextPage,
    paginatedData,
    goToPage,
    nextPage,
    previousPage,
    resetPage,
  }
}
