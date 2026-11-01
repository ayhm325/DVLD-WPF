import { apiRequest } from './apiClient'
import type {
  Person,
  PersonDetails,
  CreatePersonRequest,
  UpdatePersonRequest,
  CreatePersonResponse,
} from '../types/Person'

export type {
  Person,
  PersonDetails,
  CreatePersonRequest,
  UpdatePersonRequest,
  CreatePersonResponse,
}

export function getPeople(accessToken: string): Promise<Person[]> {
  return apiRequest<Person[]>('/People', accessToken)
}

export function getPersonById(
  accessToken: string,
  personId: number,
): Promise<PersonDetails> {
  return apiRequest<PersonDetails>(`/People/${personId}`, accessToken)
}

export function getPersonByNationalNo(
  accessToken: string,
  nationalNo: string,
): Promise<PersonDetails> {
  return apiRequest<PersonDetails>(
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

export function updatePerson(
  accessToken: string,
  personId: number,
  person: UpdatePersonRequest,
): Promise<void> {
  return apiRequest<void>(`/People/${personId}`, accessToken, {
    method: 'PUT',
    body: JSON.stringify(person),
  })
}

export function deletePerson(
  accessToken: string,
  personId: number,
): Promise<void> {
  return apiRequest<void>(`/People/${personId}`, accessToken, {
    method: 'DELETE',
  })
}
