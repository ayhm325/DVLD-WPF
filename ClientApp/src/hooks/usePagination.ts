import { useCallback, useMemo, useState } from 'react'

export interface UsePaginationOptions<T = unknown> {
  /** Total count of items across all pages. If data array is provided, defaults to data.length. */
  totalCount?: number
  /** Number of items displayed per page. Defaults to 10. */
  pageSize?: number
  /** Initial page index (1-based). Defaults to 1. */
  initialPage?: number
  /** Optional array of data items to slice for client-side pagination. */
  data?: T[]
  /** Optional callback fired when page changes. */
  onPageChange?: (page: number) => void
}

export interface UsePaginationResult<T = unknown> {
  page: number
  pageSize: number
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
    pageSize = 10,
    initialPage = 1,
    data,
    onPageChange,
  } = options

  const [page, setPage] = useState<number>(initialPage)

  const effectiveTotalCount =
    explicitTotalCount !== undefined
      ? explicitTotalCount
      : data
        ? data.length
        : 0

  const totalPages = Math.max(1, Math.ceil(effectiveTotalCount / pageSize))

  const hasPreviousPage = page > 1
  const hasNextPage = page < totalPages

  const goToPage = useCallback(
    (targetPage: number) => {
      const clamped = Math.max(1, Math.min(totalPages, targetPage))
      setPage(clamped)
      onPageChange?.(clamped)
    },
    [totalPages, onPageChange],
  )

  const nextPage = useCallback(() => {
    if (hasNextPage) {
      goToPage(page + 1)
    }
  }, [hasNextPage, goToPage, page])

  const previousPage = useCallback(() => {
    if (hasPreviousPage) {
      goToPage(page - 1)
    }
  }, [hasPreviousPage, goToPage, page])

  const resetPage = useCallback(() => {
    setPage(initialPage)
    onPageChange?.(initialPage)
  }, [initialPage, onPageChange])

  const paginatedData = useMemo(() => {
    if (!data) return undefined
    const start = (page - 1) * pageSize
    return data.slice(start, start + pageSize)
  }, [data, page, pageSize])

  return {
    page,
    pageSize,
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
