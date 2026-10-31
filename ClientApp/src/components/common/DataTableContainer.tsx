import type { HTMLAttributes, ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface DataTableContainerProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  minWidth?: number | string
  className?: string
  containerClassName?: string
}

/**
 * Shared horizontal-scrolling table container for all DVLD data tables.
 * Ensures the table scrolls internally when its width exceeds the viewport,
 * preventing page-level horizontal overflow while maintaining complete readability.
 */
export default function DataTableContainer({
  children,
  minWidth = 750,
  className,
  containerClassName,
  ...props
}: DataTableContainerProps) {
  return (
    <div
      className={cn(
        'table-container data-table-container relative w-full max-w-full overflow-x-auto overflow-y-hidden rounded-b-xl border-t border-[#263750] bg-[#111C30] shadow-md focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-blue-500/30',
        containerClassName,
      )}
      tabIndex={0}
      role="region"
      aria-label="Data table container"
      {...props}
    >
      <div
        className={cn('w-full min-w-full', className)}
        style={{ minWidth: typeof minWidth === 'number' ? `${minWidth}px` : minWidth }}
      >
        {children}
      </div>
    </div>
  )
}
