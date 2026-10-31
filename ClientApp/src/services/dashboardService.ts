import { apiRequest } from './apiClient'
import type { DashboardStatistics } from '../types/Dashboard'

export type { DashboardStatistics }

export function getDashboardStatistics(
  accessToken: string,
): Promise<DashboardStatistics> {
  return apiRequest<DashboardStatistics>(
    '/Dashboard/statistics',
    accessToken,
  )
}
