import { apiRequest } from './apiClient'
import type { Driver } from '../types/Driver'

export type { Driver }

export interface DriverPageResponse {
  items: Driver[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export function getDrivers(accessToken: string): Promise<Driver[]> {
  return apiRequest<Driver[]>('/Drivers', accessToken)
}

export function getDriversPage(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
  searchQuery?: string,
  searchBy?: string,
  licenseFilter?: string,
): Promise<DriverPageResponse> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (searchQuery && searchQuery.trim()) {
    params.set('searchQuery', searchQuery.trim())
  }
  if (searchBy && searchBy !== 'all') {
    params.set('searchBy', searchBy)
  }
  if (licenseFilter && licenseFilter !== 'all') {
    params.set('licenseFilter', licenseFilter)
  }

  return apiRequest<DriverPageResponse>(
    `/Drivers?${params.toString()}`,
    accessToken,
  )
}
