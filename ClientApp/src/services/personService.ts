
import { apiRequest } from './apiClient'

export type CreatePersonRequest = {
  nationalNo: string
  firstName: string
  secondName: string
  thirdName: string | null
  lastName: string
  dateOfBirth: string
  gender: number
  address: string
  phone: string
  email: string | null
  nationalityCountryID: number
  imagePath: string | null
}

export type CreatePersonResponse = {
  personId: number
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
