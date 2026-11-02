import { apiRequest } from './apiClient'
import type { License } from '../types/License'

export type { License }

export function getLicenses(accessToken: string): Promise<License[]> {
  return apiRequest<License[]>('/Licenses', accessToken)
}

export function getLicensesByDriverId(accessToken: string, driverId: number): Promise<License[]> {
  return apiRequest<License[]>(`/Licenses/driver/${driverId}`, accessToken)
}

export function getLicensesByPersonId(accessToken: string, personId: number): Promise<License[]> {
  return apiRequest<License[]>(`/Licenses/person/${personId}`, accessToken)
}
