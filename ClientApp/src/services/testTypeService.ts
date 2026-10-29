export type TestType = {
  testTypeId: number
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}

export type UpdateTestTypeRequest = {
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}

import { apiRequest } from './apiClient'

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
