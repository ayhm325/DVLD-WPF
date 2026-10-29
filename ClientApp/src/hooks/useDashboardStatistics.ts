import { useCallback, useEffect, useState } from 'react'
import { getDashboardStatistics } from '../services/dashboardService'
import type { DashboardStatistics } from '../services/dashboardService'

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
  const [statistics, setStatistics] = useState<DashboardStatistics | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [internalRefreshKey, setInternalRefreshKey] = useState(0)

  useEffect(() => {
    if (!accessToken) return
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')

      try {
        const result = await getDashboardStatistics(accessToken!)
        if (!cancelled) {
          setStatistics(result)
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Failed to load dashboard statistics.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [accessToken, externalRefreshKey, internalRefreshKey])

  const retry = useCallback(() => {
    setInternalRefreshKey((current) => current + 1)
  }, [])

  return {
    statistics,
    loading,
    error,
    retry,
  }
}
