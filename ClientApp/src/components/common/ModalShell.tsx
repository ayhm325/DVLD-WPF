import type { ReactNode } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { cn } from '@/lib/utils'

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | '2xl'

export interface ModalShellProps {
  /** Controls modal open visibility */
  isOpen?: boolean
  /** Alias for isOpen */
  open?: boolean
  /** Callback triggered when user requests closing the dialog */
  onClose: () => void

  /** Dialog title text or node */
  title?: ReactNode
  /** Accessible dialog description text or node */
  description?: ReactNode
  /** Main dialog body content */
  children: ReactNode
  /** Dialog footer controls */
  footer?: ReactNode

  /** Modal max-width size variant. Defaults to 'md' */
  size?: ModalSize
  /** Whether clicking outside closes the modal. Defaults to true */
  closeOnBackdrop?: boolean
  /** Whether pressing Escape closes the modal. Defaults to true */
  closeOnEscape?: boolean
  /** Whether to show the top-right close (X) button. Defaults to true */
  showCloseButton?: boolean
  /** Prevents closing the modal via backdrop, escape, or close button during active operations */
  preventClose?: boolean

  /** Custom class names for DialogContent */
  className?: string
  /** Custom class names for body wrapper */
  contentClassName?: string
}

const sizeClasses: Record<ModalSize, string> = {
  sm: 'sm:max-w-sm',
  md: 'sm:max-w-md',
  lg: 'sm:max-w-lg',
  xl: 'sm:max-w-2xl',
  '2xl': 'sm:max-w-4xl',
}

/**
 * Standardized presentation-only accessible modal dialog shell.
 * Backed by Radix Dialog primitives to ensure complete WAI-ARIA accessibility,
 * focus trapping, focus restoration, backdrop blur, and dark theme consistency.
 */
export default function ModalShell({
  isOpen,
  open,
  onClose,

  title,
  description,
  children,
  footer,

  size = 'md',
  closeOnBackdrop = true,
  closeOnEscape = true,
  showCloseButton = true,
  preventClose = false,

  className,
  contentClassName,
}: ModalShellProps) {
  const isModalOpen = isOpen ?? open ?? false

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      if (preventClose) return
      onClose()
    }
  }

  return (
    <Dialog open={isModalOpen} onOpenChange={handleOpenChange}>
      <DialogContent
        className={cn(
          sizeClasses[size],
          !showCloseButton && '[&>button:last-child]:hidden',
          preventClose && '[&>button:last-child]:pointer-events-none [&>button:last-child]:opacity-40',
          'w-[calc(100vw-1rem)] min-w-0 border border-border bg-surface text-foreground shadow-2xl',
          className,
        )}
        onPointerDownOutside={(e) => {
          if (!closeOnBackdrop || preventClose) {
            e.preventDefault()
          }
        }}
        onEscapeKeyDown={(e) => {
          if (!closeOnEscape || preventClose) {
            e.preventDefault()
          }
        }}
      >
        {(title || description) ? (
          <DialogHeader className="min-w-0 pr-8 text-left">
            {title ? (
              typeof title === 'string' ? (
                <DialogTitle className="break-words text-lg font-bold tracking-tight">
                  {title}
                </DialogTitle>
              ) : (
                <DialogTitle asChild className="break-words text-lg font-bold tracking-tight">
                  <div>{title}</div>
                </DialogTitle>
              )
            ) : (
              <DialogTitle className="sr-only">Dialog</DialogTitle>
            )}

            {description && (
              typeof description === 'string' ? (
                <DialogDescription className="break-words pt-1 text-xs leading-relaxed">
                  {description}
                </DialogDescription>
              ) : (
                <DialogDescription asChild className="break-words pt-1 text-xs leading-relaxed">
                  <div>{description}</div>
                </DialogDescription>
              )
            )}
          </DialogHeader>
        ) : (
          <DialogTitle className="sr-only">Dialog</DialogTitle>
        )}

        <div className={cn('min-h-0 min-w-0 overflow-x-hidden overflow-y-auto overscroll-contain break-words py-1 text-sm', contentClassName)}>
          {children}
        </div>

        {footer && (
          <DialogFooter className="min-w-0 gap-2 border-t border-border pt-3 sm:gap-2">
            {footer}
          </DialogFooter>
        )}
      </DialogContent>
    </Dialog>
  )
}
