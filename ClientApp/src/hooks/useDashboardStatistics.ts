import { useCallback } from 'react'
import { getDashboardStatistics } from '../services/dashboardService'
import type { DashboardStatistics } from '../types/Dashboard'
import { useAsyncData } from './useAsyncData'

export type UseDashboardStatisticsResult = {
  statistics: DashboardStatistics | null
  loading: boolean
  error: string
  retry: () => void
}

/**
 * Custom hook to load and refresh dashboard statistics.
 * Encapsulates the lifecycle, loading state, error handling,
 * and asynchronous cancellation for the dashboard counters.
 */
export function useDashboardStatistics(
  accessToken: string | null,
  externalRefreshKey = 0,
): UseDashboardStatisticsResult {
  const fetcher = useCallback(() => {
    return getDashboardStatistics(accessToken!)
  }, [accessToken])

  const {
    data,
    loading,
    error,
    refresh,
  } = useAsyncData<DashboardStatistics | null>(fetcher, {
    enabled: Boolean(accessToken),
    initialData: null,
    dependencies: [externalRefreshKey],
    defaultErrorMessage: 'Failed to load dashboard statistics.',
  })

  return {
    statistics: data,
    loading,
    error,
    retry: refresh,
  }
}
