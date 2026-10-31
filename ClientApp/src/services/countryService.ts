import { apiRequest } from './apiClient'
import type { Country } from '../types/Country'

export type { Country }

export function getCountries(accessToken: string): Promise<Country[]> {
  return apiRequest<Country[]>('/Countries', accessToken)
}
