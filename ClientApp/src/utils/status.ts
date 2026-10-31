import type { StatusVariant } from '../components/common/StatusBadge'

/**
 * Shared semantic mapping helper for active/inactive entity status (e.g. driver licenses).
 * Explicitly associates boolean active state with semantic variant ('success' | 'danger') and label ('Active' | 'Inactive').
 */
export function getActiveStatusProps(isActive: boolean): {
  variant: StatusVariant
  label: string
} {
  return {
    variant: isActive ? 'success' : 'danger',
    label: isActive ? 'Active' : 'Inactive',
  }
}
