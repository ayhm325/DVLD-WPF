import { apiRequest } from './apiClient'
import type { Person } from '../types/Person'

export function getPeople(accessToken: string): Promise<Person[]> {
  return apiRequest<Person[]>('/People', accessToken)
}
