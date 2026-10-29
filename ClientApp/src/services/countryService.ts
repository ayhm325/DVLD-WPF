import { apiRequest } from './apiClient'

export type Country = {
  countryId: number
  countryName: string
}

export function getCountries(accessToken: string): Promise<Country[]> {
  return apiRequest<Country[]>('/Countries', accessToken)
}
