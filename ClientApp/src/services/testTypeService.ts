import { apiRequest } from './apiClient'
import type { TestType, UpdateTestTypeRequest } from '../types/TestType'

export type { TestType, UpdateTestTypeRequest }

export function getTestTypes(accessToken: string): Promise<TestType[]> {
  return apiRequest<TestType[]>('/TestTypes', accessToken)
}

export function updateTestType(
  accessToken: string,
  id: number,
  request: UpdateTestTypeRequest,
): Promise<void> {
  return apiRequest<void>(`/TestTypes/${id}`, accessToken, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
