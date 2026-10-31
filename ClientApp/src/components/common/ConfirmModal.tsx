import type { ReactNode } from 'react'
import ModalShell from './ModalShell'
import { Button } from '@/components/ui/button'
import { AlertCircle, AlertTriangle, Info, Loader2 } from 'lucide-react'

export type ConfirmVariant = 'primary' | 'danger' | 'warning' | 'neutral'

export type ConfirmModalProps = {
  /** Controls open visibility */
  isOpen: boolean
  /** Dialog heading title */
  title: string
  /** Confirmation body message */
  message: string
  /** Text for confirmation action button. Defaults to 'Confirm'. */
  confirmText?: string
  /** Text for cancel action button. Defaults to 'Cancel'. */
  cancelText?: string
  /** Semantic variant determining icon and button styling. Defaults to 'primary'. */
  confirmVariant?: ConfirmVariant
  /** Confirmation callback */
  onConfirm: () => void | Promise<void>
  /** Cancel callback */
  onCancel: () => void

  /** Indicates an asynchronous confirmation action is in progress */
  loading?: boolean
  /** Disables both buttons to prevent duplicate submission */
  disabled?: boolean
}

/**
 * Standardized reusable confirmation dialog component.
 * Provides accessible semantic confirmation with customizable intent variants,
 * loading states during asynchronous submission, and duplicate submission prevention.
 */
export default function ConfirmModal({
  isOpen,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  confirmVariant = 'primary',
  onConfirm,
  onCancel,
  loading = false,
  disabled = false,
}: ConfirmModalProps) {
  const isBusy = loading || disabled

  function getVariantIcon(): ReactNode {
    if (confirmVariant === 'danger') {
      return (
        <span
          className="flex size-9 shrink-0 items-center justify-center rounded-full bg-rose-500/15 border border-rose-500/30 text-rose-400"
          aria-hidden="true"
        >
          <AlertCircle className="size-5" />
        </span>
      )
    }

    if (confirmVariant === 'warning') {
      return (
        <span
          className="flex size-9 shrink-0 items-center justify-center rounded-full bg-amber-500/15 border border-amber-500/30 text-amber-400"
          aria-hidden="true"
        >
          <AlertTriangle className="size-5" />
        </span>
      )
    }

    return (
      <span
        className="flex size-9 shrink-0 items-center justify-center rounded-full bg-blue-500/15 border border-blue-500/30 text-blue-400"
        aria-hidden="true"
      >
        <Info className="size-5" />
      </span>
    )
  }

  function getButtonVariant(): 'destructive' | 'default' | 'secondary' {
    if (confirmVariant === 'danger') return 'destructive'
    if (confirmVariant === 'neutral') return 'secondary'
    return 'default'
  }

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={onCancel}
      size="sm"
      preventClose={isBusy}
      title={
        <div className="flex items-center gap-3">
          {getVariantIcon()}
          <span>{title}</span>
        </div>
      }
      description={message}
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onCancel}
            disabled={isBusy}
            className="border-[#263750] bg-transparent text-slate-300 hover:bg-[#162238] hover:text-white"
          >
            {cancelText}
          </Button>

          <Button
            type="button"
            variant={getButtonVariant()}
            size="sm"
            onClick={onConfirm}
            disabled={isBusy}
            autoFocus
          >
            {loading && <Loader2 className="size-3.5 animate-spin mr-1.5" aria-hidden="true" />}
            <span>{loading ? 'Processing...' : confirmText}</span>
          </Button>
        </>
      }
    >
      {null}
    </ModalShell>
  )
}
