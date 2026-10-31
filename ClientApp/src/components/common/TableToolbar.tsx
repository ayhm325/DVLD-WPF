import type { ReactNode } from 'react'
import SearchInput from './SearchInput'
import { Filter as FilterIcon, RefreshCw as RefreshIcon } from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select'

const EMPTY_SENTINEL = '__EMPTY_SENTINEL__'

export interface SearchFieldOption<TField extends string = string> {
  value: TField
  label: string
}

export interface TableToolbarProps<TField extends string = string> {
  /** Current search input value */
  searchValue?: string
  /** Search value change callback */
  onSearchChange?: (value: string) => void
  /** Search input placeholder text */
  searchPlaceholder?: string
  /** Accessible label for the search input */
  searchAriaLabel?: string

  /** Active search field identifier (e.g. 'all', 'name', 'id') */
  searchField?: TField
  /** Dropdown options for search field selector */
  searchFieldOptions?: SearchFieldOption<TField>[]
  /** Search field change callback */
  onSearchFieldChange?: (value: TField) => void
  /** Accessible label for search field select */
  searchFieldAriaLabel?: string
  /** Custom min-width for the search field selector */
  searchFieldMinWidth?: number | string

  /** Callback to reset all search/filter criteria */
  onReset?: () => void
  /** Whether any filters or search inputs are currently active */
  isFiltered?: boolean
  /** Custom label for the reset button. Defaults to 'Reset'. */
  resetLabel?: string

  /** Callback to refresh table data */
  onRefresh?: () => void
  /** Whether a data refresh is in progress */
  refreshing?: boolean
  /** Label for refresh button while idle. Defaults to 'Refresh'. */
  refreshLabel?: string
  /** Label for refresh button while loading. Defaults to 'Loading...'. */
  refreshingLabel?: string

  /** Custom filter controls rendered after search input (e.g. status/age/gender dropdowns) */
  filters?: ReactNode
  /** Additional action buttons rendered in the toolbar */
  actions?: ReactNode
  /** Extra child elements */
  children?: ReactNode
  /** Additional class names for toolbar container */
  className?: string
}

/**
 * Standardized presentation-only table toolbar component.
 * Provides consistent layout for search field selection, text search,
 * custom filter dropdowns, filter reset, and data refresh controls.
 */
export default function TableToolbar<TField extends string = string>({
  searchValue,
  onSearchChange,
  searchPlaceholder,
  searchAriaLabel,

  searchField,
  searchFieldOptions,
  onSearchFieldChange,
  searchFieldAriaLabel = 'Filter criterion',
  searchFieldMinWidth = 140,

  onReset,
  isFiltered = false,
  resetLabel = 'Reset',

  onRefresh,
  refreshing = false,
  refreshLabel = 'Refresh',
  refreshingLabel = 'Loading...',

  filters,
  actions,
  children,
  className = '',
}: TableToolbarProps<TField>) {
  const containerClasses = ['table-toolbar', className].filter(Boolean).join(' ')

  return (
    <div className={containerClasses}>
      {searchFieldOptions && onSearchFieldChange && (
        <div style={{ minWidth: searchFieldMinWidth }} className="shrink-0">
          <Select
            value={searchField === '' ? EMPTY_SENTINEL : searchField}
            onValueChange={(val) =>
              onSearchFieldChange((val === EMPTY_SENTINEL ? '' : val) as TField)
            }
          >
            <SelectTrigger
              className="h-10 min-h-[40px] w-full bg-[#111e33] border-[#223656] text-xs text-white hover:border-blue-500/50 hover:bg-[#14233c] focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 rounded-lg"
              aria-label={searchFieldAriaLabel}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {searchFieldOptions.map((opt) => (
                <SelectItem
                  key={opt.value || EMPTY_SENTINEL}
                  value={opt.value === '' ? EMPTY_SENTINEL : opt.value}
                >
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {onSearchChange && (
        <SearchInput
          value={searchValue ?? ''}
          onChange={onSearchChange}
          placeholder={searchPlaceholder}
          ariaLabel={searchAriaLabel ?? 'Search records'}
        />
      )}

      {filters}

      {isFiltered && onReset && (
        <button
          type="button"
          className="secondary-button"
          onClick={onReset}
          title="Reset all filters"
        >
          <FilterIcon size={14} />
          <span>{resetLabel}</span>
        </button>
      )}

      {onRefresh && (
        <button
          type="button"
          className="secondary-button"
          disabled={refreshing}
          onClick={onRefresh}
          title="Refresh data"
        >
          <RefreshIcon
            size={14}
            className={refreshing ? 'animate-spin' : undefined}
          />
          <span>{refreshing ? refreshingLabel : refreshLabel}</span>
        </button>
      )}

      {actions}
      {children}
    </div>
  )
}
