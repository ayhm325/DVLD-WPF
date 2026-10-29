import { apiRequest } from './apiClient'

export type DashboardStatistics = {
  totalPeople: number
  totalDrivers: number
  activeLicenses: number
  pendingApplications: number
  localDrivingLicenseApplications: number
  internationalLicenses: number
  detainedLicenses: number
  upcomingTests: number
}

export function getDashboardStatistics(
  accessToken: string,
): Promise<DashboardStatistics> {
  return apiRequest<DashboardStatistics>(
    '/Dashboard/statistics',
    accessToken,
  )
}
