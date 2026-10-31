import { useCallback, useMemo, useState } from 'react'
import type { Dispatch, SetStateAction } from 'react'

export interface UseTableFiltersOptions<
  T,
  TField extends string = string,
  TFilters extends Record<string, unknown> = Record<string, unknown>,
> {
  /** Array of data items to filter. */
  data: T[]
  /** Default search field identifier. Defaults to 'all'. */
  initialSearchField?: TField
  /** Default dictionary of additional filter values. */
  initialFilters?: TFilters
  /**
   * Filter predicate returning true if the item matches the query and active filters.
   * Query is pre-trimmed and lowercased for convenience.
   */
  filterItem: (
    item: T,
    query: string,
    searchField: TField,
    filters: TFilters,
  ) => boolean
  /** Optional callback fired when filters are reset. */
  onReset?: () => void
}

export interface UseTableFiltersResult<
  T,
  TField extends string = string,
  TFilters extends Record<string, unknown> = Record<string, unknown>,
> {
  search: string
  setSearch: Dispatch<SetStateAction<string>>
  searchField: TField
  setSearchField: Dispatch<SetStateAction<TField>>
  filters: TFilters
  setFilter: <K extends keyof TFilters>(key: K, value: TFilters[K]) => void
  setFilters: Dispatch<SetStateAction<TFilters>>
  filteredData: T[]
  isFiltered: boolean
  resetFilters: () => void
}

/**
 * Reusable table search and filtering hook providing state management,
 * memoized filtered outputs, and reset capabilities while keeping
 * entity-specific predicate logic completely under page control.
 */
export function useTableFilters<
  T,
  TField extends string = string,
  TFilters extends Record<string, unknown> = Record<string, unknown>,
>({
  data,
  initialSearchField = 'all' as TField,
  initialFilters = {} as TFilters,
  filterItem,
  onReset,
}: UseTableFiltersOptions<T, TField, TFilters>): UseTableFiltersResult<
  T,
  TField,
  TFilters
> {
  const [search, setSearch] = useState<string>('')
  const [searchField, setSearchField] = useState<TField>(initialSearchField)
  const [filters, setFilters] = useState<TFilters>(initialFilters)

  const setFilter = useCallback(
    <K extends keyof TFilters>(key: K, value: TFilters[K]) => {
      setFilters((prev) => ({
        ...prev,
        [key]: value,
      }))
    },
    [],
  )

  const resetFilters = useCallback(() => {
    setSearch('')
    setSearchField(initialSearchField)
    setFilters(initialFilters)
    onReset?.()
  }, [initialSearchField, initialFilters, onReset])

  const isFiltered = useMemo(() => {
    if (search.trim() !== '') return true
    if (searchField !== initialSearchField) return true

    for (const key of Object.keys(initialFilters) as Array<keyof TFilters>) {
      if (filters[key] !== initialFilters[key]) {
        return true
      }
    }

    return false
  }, [search, searchField, initialSearchField, filters, initialFilters])

  const filteredData = useMemo(() => {
    const query = search.trim().toLowerCase()

    return data.filter((item) => filterItem(item, query, searchField, filters))
  }, [data, search, searchField, filters, filterItem])

  return {
    search,
    setSearch,
    searchField,
    setSearchField,
    filters,
    setFilter,
    setFilters,
    filteredData,
    isFiltered,
    resetFilters,
  }
}
