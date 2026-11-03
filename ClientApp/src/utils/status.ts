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

export type AppointmentLifecycleState =
  | 'Scheduled'
  | 'Completed — Pass'
  | 'Completed — Fail'
  | 'NoShow'
  | 'Cancelled'
  | 'Historical'

export interface AppointmentLifecycleDisplay {
  state: AppointmentLifecycleState
  statusLabel: string
  statusVariant: StatusVariant
  resultLabel: string
  resultVariant: StatusVariant | 'neutral'
  isTerminal: boolean
  description: string
}

/**
 * Distinguishes genuine active Scheduled appointments from Completed, NoShow,
 * Cancelled, and historical unclassified records.
 * Authoritative behavior mirrors backend TestAppointmentRepository.IsAppointmentAlreadyScheduledAsync.
 */
export function isAppointmentActive(apt: {
  isLocked?: boolean | null
  status?: string | null
}): boolean {
  if (apt.isLocked) return false
  const status = (apt.status ?? '').trim().toLowerCase()
  return status === 'scheduled' || status === 'open'
}

/**
 * Authoritative lifecycle resolver mapping backend TestAppointmentResponse DTO
 * to distinct semantic lifecycle states, labels, and badges.
 *
 * Rules:
 * - Uses backend Status property.
 * - Distinguishes Pass from Fail using explicit test result data.
 * - Never infers NoShow from IsLocked.
 * - Never infers Cancelled from IsLocked.
 * - Never confuses NoShow with Failed.
 * - Handles historical NULL statuses safely and transparently.
 */
export function resolveAppointmentLifecycle(apt: {
  status?: string | null
  testResult?: number | null
  testResultText?: string | null
  isLocked?: boolean | null
}): AppointmentLifecycleDisplay {
  const rawStatus = (apt.status ?? '').trim().toLowerCase()
  const rawResultText = (apt.testResultText ?? '').trim().toLowerCase()
  const resultNum = apt.testResult

  // 1. Cancelled
  if (rawStatus === 'cancelled') {
    return {
      state: 'Cancelled',
      statusLabel: 'Cancelled',
      statusVariant: 'danger',
      resultLabel: 'Cancelled',
      resultVariant: 'neutral',
      isTerminal: true,
      description: 'Appointment was soft-cancelled before examination.',
    }
  }

  // 2. NoShow
  if (rawStatus === 'noshow') {
    return {
      state: 'NoShow',
      statusLabel: 'No-Show',
      statusVariant: 'warning',
      resultLabel: 'No-Show',
      resultVariant: 'warning',
      isTerminal: true,
      description: 'Applicant did not attend the scheduled appointment.',
    }
  }

  // 3. Completed (Pass / Fail)
  const isPass = rawResultText === 'pass' || resultNum === 1
  const isFail = rawResultText === 'fail' || resultNum === 0

  if (rawStatus === 'completed' || isPass || isFail) {
    if (isPass) {
      return {
        state: 'Completed — Pass',
        statusLabel: 'Completed',
        statusVariant: 'success',
        resultLabel: 'Passed',
        resultVariant: 'success',
        isTerminal: true,
        description: 'Test examination completed with passing result.',
      }
    }
    if (isFail) {
      return {
        state: 'Completed — Fail',
        statusLabel: 'Completed',
        statusVariant: 'danger',
        resultLabel: 'Failed',
        resultVariant: 'danger',
        isTerminal: true,
        description: 'Test examination completed with failing result.',
      }
    }
    return {
      state: 'Completed — Pass',
      statusLabel: 'Completed',
      statusVariant: 'success',
      resultLabel: 'Completed',
      resultVariant: 'success',
      isTerminal: true,
      description: 'Test examination completed.',
    }
  }

  // 4. Scheduled (Open)
  if ((rawStatus === 'scheduled' || rawStatus === 'open') && !apt.isLocked) {
    return {
      state: 'Scheduled',
      statusLabel: 'Scheduled',
      statusVariant: 'info',
      resultLabel: 'Not Taken',
      resultVariant: 'neutral',
      isTerminal: false,
      description: 'Appointment is active and scheduled for examination.',
    }
  }

  // 5. Historical / NULL status records
  if (apt.isLocked) {
    return {
      state: 'Historical',
      statusLabel: 'Locked (Historical)',
      statusVariant: 'neutral',
      resultLabel: 'Not Taken',
      resultVariant: 'neutral',
      isTerminal: true,
      description: 'Historical appointment record prior to lifecycle tracking.',
    }
  }

  // Fallback for unlocked records
  return {
    state: 'Scheduled',
    statusLabel: 'Scheduled',
    statusVariant: 'info',
    resultLabel: 'Not Taken',
    resultVariant: 'neutral',
    isTerminal: false,
    description: 'Appointment is open.',
  }
}

