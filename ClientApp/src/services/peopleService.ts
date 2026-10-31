import { apiRequest } from './apiClient'
import type {
  Person,
  CreatePersonRequest,
  CreatePersonResponse,
} from '../types/Person'

export type { Person, CreatePersonRequest, CreatePersonResponse }

export function getPeople(accessToken: string): Promise<Person[]> {
  return apiRequest<Person[]>('/People', accessToken)
}

export function getPersonById(
  accessToken: string,
  personId: number,
): Promise<Person> {
  return apiRequest<Person>(`/People/${personId}`, accessToken)
}

export function getPersonByNationalNo(
  accessToken: string,
  nationalNo: string,
): Promise<Person> {
  return apiRequest<Person>(
    `/People/national/${encodeURIComponent(nationalNo)}`,
    accessToken,
  )
}

export function createPerson(
  accessToken: string,
  person: CreatePersonRequest,
): Promise<CreatePersonResponse> {
  return apiRequest<CreatePersonResponse>('/People', accessToken, {
    method: 'POST',
    body: JSON.stringify(person),
  })
}
