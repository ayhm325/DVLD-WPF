import { Fragment, useCallback, useRef, type ReactNode } from 'react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu'
import { cn } from '../../lib/utils'

export interface DataTableRowAction<T> {
  key: string
  label: string
  icon?: ReactNode
  danger?: boolean
  disabled?: boolean | ((row: T) => boolean)
  separatorAfter?: boolean
  onClick: (row: T) => void
  children?: DataTableRowAction<T>[]
}

export interface DataTableRowContextMenuProps<T> {
  open: boolean
  onOpenChange: (open: boolean) => void
  position: { x: number; y: number }
  row: T | null
  actions: DataTableRowAction<T>[]
  restoreFocusElement?: HTMLElement | null
}

interface ActionListProps<T> {
  actions: DataTableRowAction<T>[]
  row: T
  onActionClick: () => void
}

function ActionList<T>({ actions, row, onActionClick }: ActionListProps<T>) {
  return (
    <>
      {actions.map((action) => {
        const isDisabled =
          typeof action.disabled === 'function'
            ? action.disabled(row)
            : Boolean(action.disabled)

        if (action.children && action.children.length > 0) {
          return (
            <Fragment key={action.key}>
              <DropdownMenuSub>
                <DropdownMenuSubTrigger
                  disabled={isDisabled}
                  className={cn(
                    'gap-2 text-xs font-medium cursor-pointer px-2.5 py-1.5 rounded-lg transition-colors',
                    action.danger &&
                      'text-red-400 focus:text-red-300 focus:bg-red-950/40 hover:bg-red-950/40',
                  )}
                >
                  {action.icon && (
                    <span className="size-4 shrink-0 text-slate-400">
                      {action.icon}
                    </span>
                  )}
                  <span>{action.label}</span>
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent className="min-w-48 bg-[#101a2d] border border-[#263750] shadow-2xl rounded-xl z-50 p-1 text-slate-200 ring-1 ring-white/10">
                  <ActionList
                    actions={action.children}
                    row={row}
                    onActionClick={onActionClick}
                  />
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              {action.separatorAfter && (
                <DropdownMenuSeparator className="my-1 border-t border-[#263750]" />
              )}
            </Fragment>
          )
        }

        return (
          <Fragment key={action.key}>
            <DropdownMenuItem
              disabled={isDisabled}
              onClick={() => {
                if (!isDisabled) {
                  onActionClick()
                  action.onClick(row)
                }
              }}
              className={cn(
                'gap-2 text-xs font-medium cursor-pointer rounded-lg px-2.5 py-1.5 transition-colors',
                action.danger
                  ? 'text-red-400 focus:text-red-300 focus:bg-red-950/40 hover:bg-red-950/40'
                  : 'text-slate-200 focus:text-white focus:bg-surface-soft hover:bg-surface-soft',
              )}
            >
              {action.icon && (
                <span className="size-4 shrink-0 text-slate-400">
                  {action.icon}
                </span>
              )}
              <span>{action.label}</span>
            </DropdownMenuItem>
            {action.separatorAfter && (
              <DropdownMenuSeparator className="my-1 border-t border-[#263750]" />
            )}
          </Fragment>
        )
      })}
    </>
  )
}

/**
 * Reusable row-specific Context Menu powered by Radix dropdown/popper primitives.
 * Positions at pointer coordinates with viewport collision boundary detection,
 * supports submenus, destructive styling, keyboard navigation, and focus restoration.
 */
export default function DataTableRowContextMenu<T>({
  open,
  onOpenChange,
  position,
  row,
  actions,
  restoreFocusElement,
}: DataTableRowContextMenuProps<T>) {
  const actionExecutedRef = useRef(false)

  const handleActionClick = useCallback(() => {
    actionExecutedRef.current = true
  }, [])

  if (!row || actions.length === 0) return null

  return (
    <DropdownMenu open={open} onOpenChange={onOpenChange}>
      <DropdownMenuTrigger asChild>
        <span
          style={{
            position: 'fixed',
            left: `${position.x}px`,
            top: `${position.y}px`,
            width: 0,
            height: 0,
            pointerEvents: 'none',
          }}
          aria-hidden="true"
        />
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        side="bottom"
        sideOffset={2}
        className="min-w-48 p-1 bg-[#101a2d] border border-[#263750] shadow-2xl rounded-xl z-50 ring-1 ring-white/10 text-slate-200"
        onCloseAutoFocus={(event) => {
          // Always prevent Radix from automatically focusing the invisible coordinate trigger span
          event.preventDefault()

          // If an action was executed, allow that action (and any modal dialog it opens) to manage focus
          if (actionExecutedRef.current) {
            actionExecutedRef.current = false
            return
          }

          // Keyboard dismissal (Escape without action): restore focus directly to the original keyboard target
          if (
            restoreFocusElement &&
            typeof restoreFocusElement.focus === 'function'
          ) {
            restoreFocusElement.focus()
          }
          // Pointer dismissal: restoreFocusElement is null; focus stays naturally where the user clicked outside
        }}
      >
        <ActionList
          actions={actions}
          row={row}
          onActionClick={handleActionClick}
        />
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
