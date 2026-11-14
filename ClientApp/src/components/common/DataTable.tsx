import { useState, type CSSProperties, type ReactNode } from 'react'
import DataTableContainer from './DataTableContainer'
import LoadingSpinner from './LoadingSpinner'
import EmptyState from './EmptyState'
import ErrorAlert from './ErrorAlert'
import DataTableRowContextMenu, {
  type DataTableRowAction,
} from './DataTableRowContextMenu'
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export type { DataTableRowAction }
export type SortDirection = 'asc' | 'desc'

const ROW_INTERACTIVE_TARGET_SELECTOR =
  'a, button, input, select, textarea, summary, [role="button"], [role="link"], [role="menuitem"], [role="checkbox"], [role="radio"], [role="switch"], [role="option"], [contenteditable]:not([contenteditable="false"]), [tabindex]:not([tabindex="-1"])'

function isNestedInteractiveTarget(
  target: HTMLElement,
  row: HTMLTableRowElement,
) {
  const interactiveTarget = target.closest(ROW_INTERACTIVE_TARGET_SELECTOR)
  return interactiveTarget !== null && interactiveTarget !== row
}

export interface DataTableColumn<T> {
  /** Unique key identifying the column */
  key: string
  /** Column header title or node */
  header: ReactNode
  /** Simple property accessor function */
  accessor?: (row: T) => ReactNode
  /** Custom render function receiving row data and index */
  render?: (row: T, index: number) => ReactNode
  /** Cell CSS class name or class name resolver */
  className?: string | ((row: T, index: number) => string | undefined)
  /** Header cell CSS class name */
  headerClassName?: string
  /** Content text alignment */
  align?: 'left' | 'center' | 'right'
  /** Explicit column width */
  width?: string | number
  /** Whether the column supports sorting */
  sortable?: boolean
  /** Value extractor for sorting comparisons */
  sortValue?: (row: T) => string | number | Date | null | undefined
}

export interface DataTableProps<T> {
  /** Table row items */
  data: T[]
  /** Column definitions */
  columns: DataTableColumn<T>[]
  /** Unique row key extractor */
  getRowKey: (row: T, index: number) => string | number

  /** Loading indicator flag */
  loading?: boolean
  /** Custom loading element */
  loadingComponent?: ReactNode
  /** Loading text message */
  loadingMessage?: string

  /** Error state node or message */
  error?: ReactNode

  /** Custom empty state node when data is empty */
  emptyState?: ReactNode
  /** Message displayed when table has no data */
  emptyMessage?: string
  /** Optional icon for empty state */
  emptyIcon?: ReactNode

  /** Optional click handler for entire row */
  onRowClick?: (row: T) => void
  /** Row CSS class name or resolver */
  rowClassName?: string | ((row: T, index: number) => string | undefined)

  /** Table element className (defaults to 'people-table') */
  tableClassName?: string
  /** Minimum width for internal scroll container */
  minWidth?: number | string
  /** Wrapper container className */
  containerClassName?: string

  /** Optional sorting state & handler */
  sortColumn?: string
  sortDirection?: SortDirection
  onSort?: (columnKey: string, direction: SortDirection) => void

  /** Optional custom row render override. Note: Custom row renderers own their own row markup, semantics, and interaction behavior. */
  customRowRender?: (row: T, index: number, columns: DataTableColumn<T>[]) => ReactNode

  /** Accessible table label */
  ariaLabel?: string

  /**
   * Optional context menu generator.
   * When provided, right-clicking (or pressing Shift+F10 / ContextMenu key) on a row
   * opens a cursor-anchored context menu displaying the returned actions.
   */
  rowContextMenu?: (row: T) => DataTableRowAction<T>[]
}

/**
 * Standardized generic presentation-only DataTable component.
 * Provides consistent layout, dark theme styling, horizontal overflow containment,
 * configurable columns, custom cells, and loading/empty state handling.
 */
export default function DataTable<T>({
  data,
  columns,
  getRowKey,

  loading = false,
  loadingComponent,
  loadingMessage = 'Loading records...',

  error,

  emptyState,
  emptyMessage = 'No records found.',
  emptyIcon,

  onRowClick,
  rowClassName,

  tableClassName = 'people-table',
  minWidth = 750,
  containerClassName,

  sortColumn,
  sortDirection,
  onSort,

  customRowRender,
  ariaLabel,
  rowContextMenu,
}: DataTableProps<T>) {
  const [contextMenuState, setContextMenuState] = useState<{
    open: boolean
    x: number
    y: number
    row: T | null
    actions: DataTableRowAction<T>[]
    restoreElement: HTMLElement | null
  }>({
    open: false,
    x: 0,
    y: 0,
    row: null,
    actions: [],
    restoreElement: null,
  })

  function handleRowKeyDown(
    event: React.KeyboardEvent<HTMLTableRowElement>,
    row: T,
  ) {
    const target = event.target as HTMLElement | null
    if (!target || isNestedInteractiveTarget(target, event.currentTarget)) return

    // Explicitly detect ContextMenu key or Shift+F10
    const isContextMenuKey =
      event.key === 'ContextMenu' ||
      (event.shiftKey && (event.key === 'F10' || event.code === 'F10'))

    if (rowContextMenu && isContextMenuKey) {
      const actions = rowContextMenu(row)
      if (!actions || actions.length === 0) return

      // Prevent the browser's native context-menu event from opening the menu a second time.
      event.preventDefault()
      event.stopPropagation()

      const rect = event.currentTarget.getBoundingClientRect()
      setContextMenuState({
        open: true,
        x: Math.max(0, rect.left + 16),
        y: Math.max(0, rect.bottom + 4),
        row,
        actions,
        restoreElement: event.currentTarget,
      })
      return
    }

    const activatesRow = event.key === 'Enter' || event.key === ' '
    if (!onRowClick || !activatesRow || event.repeat) return

    event.preventDefault()
    onRowClick(row)
  }

  function handleRowContextMenu(
    event: React.MouseEvent<HTMLTableRowElement>,
    row: T,
  ) {
    if (!rowContextMenu) return

    // Ignore synthetic non-pointer contextmenu events (keyboard contextmenu fallback where clientX/Y are 0 and button !== 2)
    if (event.button !== 2 && event.clientX === 0 && event.clientY === 0) {
      event.preventDefault()
      return
    }

    const target = event.target as HTMLElement | null
    if (!target) return

    // Preserve native context menus for controls and other focusable content in a row.
    if (isNestedInteractiveTarget(target, event.currentTarget)) return

    // Preserve native browser context menu when user has selected text in a cell
    const selection = window.getSelection()
    if (selection && selection.toString().trim().length > 0) {
      return
    }

    // 3. Resolve actions for this specific row
    const actions = rowContextMenu(row)
    if (!actions || actions.length === 0) return

    event.preventDefault()

    // 4. Capture exact pointer/touch/pen coordinates
    const x = event.clientX
    const y = event.clientY

    // 5. Update state. Pointer invocation does not capture an arbitrary activeElement for focus restoration.
    setContextMenuState({
      open: true,
      x,
      y,
      row,
      actions,
      restoreElement: null,
    })
  }
  function handleSortClick(column: DataTableColumn<T>) {
    if (!column.sortable || !onSort) return
    const isCurrent = sortColumn === column.key
    const nextDirection: SortDirection =
      isCurrent && sortDirection === 'asc' ? 'desc' : 'asc'
    onSort(column.key, nextDirection)
  }

  function getAlignmentClass(align?: 'left' | 'center' | 'right') {
    if (align === 'center') return 'text-center'
    if (align === 'right') return 'text-right'
    return 'text-left'
  }

  function getWidthStyle(width?: string | number): CSSProperties | undefined {
    if (width === undefined) return undefined
    return { width: typeof width === 'number' ? `${width}px` : width }
  }

  return (
    <DataTableContainer minWidth={minWidth} containerClassName={containerClassName}>
      <table className={tableClassName} aria-label={ariaLabel}>
        <thead>
          <tr>
            {columns.map((col) => {
              const isSorted = sortColumn === col.key
              const alignClass = getAlignmentClass(col.align)
              const widthStyle = getWidthStyle(col.width)

              return (
                <th
                  key={col.key}
                  className={cn(alignClass, col.headerClassName)}
                  style={widthStyle}
                  aria-sort={
                    isSorted
                      ? sortDirection === 'asc'
                        ? 'ascending'
                        : 'descending'
                      : undefined
                  }
                >
                  {col.sortable && onSort ? (
                    <button
                      type="button"
                      className="inline-flex items-center gap-1.5 font-inherit text-inherit uppercase hover:text-white transition-colors cursor-pointer bg-transparent border-0 p-0"
                      onClick={() => handleSortClick(col)}
                    >
                      <span>{col.header}</span>
                      <span aria-hidden="true" className="text-slate-400">
                        {isSorted ? (
                          sortDirection === 'asc' ? (
                            <ArrowUp size={13} />
                          ) : (
                            <ArrowDown size={13} />
                          )
                        ) : (
                          <ArrowUpDown size={13} />
                        )}
                      </span>
                    </button>
                  ) : (
                    col.header
                  )}
                </th>
              )
            })}
          </tr>
        </thead>

        <tbody>
          {loading ? (
            <tr>
              <td colSpan={columns.length} className="text-center p-8">
                {loadingComponent ?? (
                  <LoadingSpinner message={loadingMessage} />
                )}
              </td>
            </tr>
          ) : error ? (
            <tr>
              <td colSpan={columns.length} className="p-6">
                {typeof error === 'string' ? (
                  <ErrorAlert message={error} />
                ) : (
                  error
                )}
              </td>
            </tr>
          ) : data.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="p-6 text-center">
                {emptyState ?? (
                  <EmptyState message={emptyMessage} icon={emptyIcon} />
                )}
              </td>
            </tr>
          ) : (
            data.map((row, index) => {
              const rowKey = getRowKey(row, index)

              if (customRowRender) {
                return customRowRender(row, index, columns)
              }

              const resolvedRowClass =
                typeof rowClassName === 'function'
                  ? rowClassName(row, index)
                  : rowClassName

              const isClickable = Boolean(onRowClick)

              return (
                <tr
                  key={rowKey}
                  className={cn(
                    resolvedRowClass,
                    isClickable && 'cursor-pointer hover:bg-[#1d3150] transition-colors',
                    (isClickable || rowContextMenu) &&
                      'focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-blue-300',
                  )}
                  tabIndex={isClickable || rowContextMenu ? 0 : undefined}
                  aria-keyshortcuts={rowContextMenu ? 'Shift+F10 ContextMenu' : undefined}
                  onClick={
                    isClickable
                      ? (e) => {
                          const target = e.target as HTMLElement | null
                          if (target && isNestedInteractiveTarget(target, e.currentTarget)) return
                          onRowClick?.(row)
                        }
                      : undefined
                  }
                  onContextMenu={
                    rowContextMenu
                      ? (e) => handleRowContextMenu(e, row)
                      : undefined
                  }
                  onKeyDown={
                    isClickable || rowContextMenu
                      ? (e) => handleRowKeyDown(e, row)
                      : undefined
                  }
                >
                  {columns.map((col) => {
                    const resolvedCellClass =
                      typeof col.className === 'function'
                        ? col.className(row, index)
                        : col.className

                    const alignClass = getAlignmentClass(col.align)
                    const widthStyle = getWidthStyle(col.width)

                    const cellContent = col.render
                      ? col.render(row, index)
                      : col.accessor
                        ? col.accessor(row)
                        : null

                    return (
                      <td
                        key={col.key}
                        className={cn(alignClass, resolvedCellClass)}
                        style={widthStyle}
                      >
                        {cellContent}
                      </td>
                    )
                  })}
                </tr>
              )
            })
          )}
        </tbody>
      </table>

      {rowContextMenu && (
        <DataTableRowContextMenu<T>
          open={contextMenuState.open}
          onOpenChange={(open) =>
            setContextMenuState((prev) => ({ ...prev, open }))
          }
          position={{ x: contextMenuState.x, y: contextMenuState.y }}
          row={contextMenuState.row}
          actions={contextMenuState.actions}
          restoreFocusElement={contextMenuState.restoreElement}
        />
      )}
    </DataTableContainer>
  )
}
