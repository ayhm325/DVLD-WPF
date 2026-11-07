import { apiRequest, apiBlobRequest } from './apiClient'
import type {
  Person,
  PersonDetails,
  CreatePersonRequest,
  UpdatePersonRequest,
  CreatePersonResponse,
  UploadPersonPhotoResponse,
} from '../types/Person'

export type {
  Person,
  PersonDetails,
  CreatePersonRequest,
  UpdatePersonRequest,
  CreatePersonResponse,
  UploadPersonPhotoResponse,
}

export interface PeoplePageResponse {
  items: Person[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export function getPeople(accessToken: string): Promise<Person[]> {
  return apiRequest<Person[]>('/People', accessToken)
}

export function getPeoplePage(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
  searchQuery?: string,
  searchBy?: string,
  genderFilter?: string,
): Promise<PeoplePageResponse> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (searchQuery && searchQuery.trim()) {
    params.set('searchQuery', searchQuery.trim())
  }
  if (searchBy && searchBy !== 'all') {
    params.set('searchBy', searchBy)
  }
  if (genderFilter && genderFilter !== 'all') {
    params.set('genderFilter', genderFilter)
  }

  return apiRequest<PeoplePageResponse>(
    `/People?${params.toString()}`,
    accessToken,
  )
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

export function uploadPersonPhoto(
  accessToken: string,
  personId: number,
  file: File,
): Promise<UploadPersonPhotoResponse> {
  const formData = new FormData()
  formData.append('file', file)

  return apiRequest<UploadPersonPhotoResponse>(
    `/People/${personId}/photo`,
    accessToken,
    {
      method: 'POST',
      body: formData,
    },
  )
}

export function getPersonPhotoBlob(
  accessToken: string,
  personId: number,
  photoKey?: string | null,
): Promise<Blob> {
  const query = photoKey ? `?v=${encodeURIComponent(photoKey)}` : ''
  return apiBlobRequest(`/People/${personId}/photo${query}`, accessToken)
}

export function deletePersonPhoto(
  accessToken: string,
  personId: number,
): Promise<void> {
  return apiRequest<void>(`/People/${personId}/photo`, accessToken, {
    method: 'DELETE',
  })
}
