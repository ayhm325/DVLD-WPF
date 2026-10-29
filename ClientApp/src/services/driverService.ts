import { apiRequest } from './apiClient'

export type Driver = {
  driverId: number
  personId: number
  fullName: string
  activeLicenses: number
  nationalNo: string
  dateOfBirth: string
  createdDate: string
}

export function getDrivers(accessToken: string): Promise<Driver[]> {
  return apiRequest<Driver[]>('/Drivers', accessToken)
}
