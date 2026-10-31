import { apiRequest } from './apiClient'
import type {
  ApplicationType,
  UpdateApplicationTypeRequest,
} from '../types/ApplicationType'

export type { ApplicationType, UpdateApplicationTypeRequest }

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
