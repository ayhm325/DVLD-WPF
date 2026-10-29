import { useEffect } from 'react'
import { CloseIcon, AlertCircleIcon } from './Icons'

type ConfirmModalProps = {
  isOpen: boolean
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  confirmVariant?: 'primary' | 'danger'
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmModal({
  isOpen,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  confirmVariant = 'primary',
  onConfirm,
  onCancel,
}: ConfirmModalProps) {
  useEffect(() => {
    if (!isOpen) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onCancel()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onCancel])

  if (!isOpen) return null

  return (
    <div
      className="modal-overlay"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-modal-title"
      onClick={onCancel}
    >
      <div
        className="modal-card confirm-modal-card"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <div className="confirm-modal-title-row">
            {confirmVariant === 'danger' && (
              <span className="confirm-modal-alert-icon" aria-hidden="true">
                <AlertCircleIcon size={20} />
              </span>
            )}
            <h3 id="confirm-modal-title">{title}</h3>
          </div>
          <button
            type="button"
            className="modal-close-btn"
            onClick={onCancel}
            aria-label="Close dialog"
          >
            <CloseIcon size={16} />
          </button>
        </div>

        <div className="modal-body">
          <p className="confirm-modal-message">{message}</p>
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="secondary-button"
            onClick={onCancel}
          >
            {cancelText}
          </button>
          <button
            type="button"
            className={
              confirmVariant === 'danger' ? 'danger-button' : 'primary-button'
            }
            onClick={onConfirm}
            autoFocus
          >
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}
