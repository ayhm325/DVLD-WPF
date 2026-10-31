import { useCallback, useState } from 'react'
import type { Dispatch, SetStateAction } from 'react'

export interface UseModalResult<TPayload = void> {
  /** Whether the modal is currently open */
  isOpen: boolean
  /** Attached payload data for the modal (e.g. selected entity) */
  payload: TPayload | undefined
  /** Open modal and optionally attach payload (cleans stale payload if omitted) */
  open: (payload?: TPayload) => void
  /** Close modal */
  close: () => void
  /** Toggle modal open/close state */
  toggle: () => void
  /** Direct payload state updater */
  setPayload: Dispatch<SetStateAction<TPayload | undefined>>
  /** Reset modal to initial open and payload state */
  reset: () => void
}

/**
 * Reusable modal state hook for managing open/close states,
 * toggling, reset, and optional item/payload association.
 */
export function useModal<TPayload = void>(
  initialOpen = false,
  initialPayload?: TPayload,
): UseModalResult<TPayload> {
  const [isOpen, setIsOpen] = useState<boolean>(initialOpen)
  const [payload, setPayload] = useState<TPayload | undefined>(initialPayload)

  const open = useCallback((newPayload?: TPayload) => {
    setPayload(newPayload !== undefined ? newPayload : initialPayload)
    setIsOpen(true)
  }, [initialPayload])

  const close = useCallback(() => {
    setIsOpen(false)
  }, [])

  const toggle = useCallback(() => {
    setIsOpen((prev) => !prev)
  }, [])

  const reset = useCallback(() => {
    setIsOpen(initialOpen)
    setPayload(initialPayload)
  }, [initialOpen, initialPayload])

  return {
    isOpen,
    payload,
    open,
    close,
    toggle,
    setPayload,
    reset,
  }
}
