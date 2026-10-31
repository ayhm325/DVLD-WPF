import { apiRequest } from './apiClient'
import type { License } from '../types/License'

export type { License }

export function getLicenses(accessToken: string): Promise<License[]> {
  return apiRequest<License[]>('/Licenses', accessToken)
}
