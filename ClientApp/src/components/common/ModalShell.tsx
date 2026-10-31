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
          'border border-[#263750] bg-[#111C30] text-[#F1F5F9] shadow-2xl',
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
          <DialogHeader>
            {title ? (
              typeof title === 'string' ? (
                <DialogTitle className="text-lg font-bold text-white tracking-tight">
                  {title}
                </DialogTitle>
              ) : (
                <DialogTitle asChild className="text-lg font-bold text-white tracking-tight">
                  <div>{title}</div>
                </DialogTitle>
              )
            ) : (
              <DialogTitle className="sr-only">Dialog</DialogTitle>
            )}

            {description && (
              typeof description === 'string' ? (
                <DialogDescription className="text-xs text-slate-400 leading-relaxed pt-1">
                  {description}
                </DialogDescription>
              ) : (
                <DialogDescription asChild className="text-xs text-slate-400 leading-relaxed pt-1">
                  <div>{description}</div>
                </DialogDescription>
              )
            )}
          </DialogHeader>
        ) : (
          <DialogTitle className="sr-only">Dialog</DialogTitle>
        )}

        <div className={cn('py-1 text-sm text-slate-200', contentClassName)}>
          {children}
        </div>

        {footer && (
          <DialogFooter className="gap-2 sm:gap-2 pt-3 border-t border-[#263750]">
            {footer}
          </DialogFooter>
        )}
      </DialogContent>
    </Dialog>
  )
}
