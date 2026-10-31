import { apiRequest } from './apiClient'
import type { Driver } from '../types/Driver'

export type { Driver }

export function getDrivers(accessToken: string): Promise<Driver[]> {
  return apiRequest<Driver[]>('/Drivers', accessToken)
}
