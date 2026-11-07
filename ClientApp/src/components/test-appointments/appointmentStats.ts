import type { TestAppointment } from '../../types/TestAppointment'
import { resolveAppointmentLifecycle } from '../../utils/status'

export type TabType = 'all' | 'today' | 'history'

export interface TabStats {
  total: number
  scheduled: number
  completed: number
  passed: number
  failed: number
  noShow: number
  cancelled: number
}

export function computeTabStats(dataset: TestAppointment[]): TabStats {
  let scheduled = 0
  let completed = 0
  let passed = 0
  let failed = 0
  let noShow = 0
  let cancelled = 0

  dataset.forEach((apt) => {
    const lifecycle = resolveAppointmentLifecycle(apt)
    if (lifecycle.statusLabel === 'Cancelled') {
      cancelled++
    } else if (lifecycle.statusLabel === 'No-Show') {
      noShow++
    } else if (lifecycle.statusLabel === 'Completed') {
      completed++
      if (lifecycle.resultLabel === 'Passed') passed++
      else if (lifecycle.resultLabel === 'Failed') failed++
    } else if (lifecycle.statusLabel === 'Scheduled') {
      scheduled++
    }
  })

  return {
    total: dataset.length,
    scheduled,
    completed,
    passed,
    failed,
    noShow,
    cancelled,
  }
}
