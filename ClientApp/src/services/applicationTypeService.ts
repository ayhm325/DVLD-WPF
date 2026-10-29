export type ApplicationType = {
  applicationTypeId: number
  applicationTypeTitle: string
  applicationTypeFees: number
}

export type UpdateApplicationTypeRequest = {
  applicationTypeTitle: string
  applicationTypeFees: number
}

import { apiRequest } from './apiClient'

export function getApplicationTypes(accessToken: string): Promise<ApplicationType[]> {
  return apiRequest<ApplicationType[]>('/ApplicationTypes', accessToken)
}

export function updateApplicationType(
  accessToken: string,
  id: number,
  request: UpdateApplicationTypeRequest,
): Promise<void> {
  return apiRequest<void>(`/ApplicationTypes/${id}`, accessToken, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
