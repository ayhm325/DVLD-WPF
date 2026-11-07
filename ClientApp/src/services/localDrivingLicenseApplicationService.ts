import type {
  ApplicationBasicInfo,
  LocalDrivingLicenseApplication,
  LocalDrivingLicenseApplicationsPage,
} from '../types/LocalDrivingLicenseApplication'

import { apiRequest } from './apiClient'

export function getLocalDrivingLicenseApplications(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
  searchQuery?: string,
  searchBy?: string,
  statusFilter?: string,
  licenseClassId?: number,
): Promise<LocalDrivingLicenseApplicationsPage> {
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
  if (statusFilter && statusFilter !== 'All') {
    params.set('statusFilter', statusFilter)
  }
  if (licenseClassId && licenseClassId > 0) {
    params.set('licenseClassId', String(licenseClassId))
  }

  return apiRequest<LocalDrivingLicenseApplicationsPage>(
    `/LocalDrivingLicenseApplications?${params.toString()}`,
    accessToken,
  )
}

export function getLocalApplicationCreateInfo(
  accessToken: string,
): Promise<{ applicationFees: number }> {
  return apiRequest<{ applicationFees: number }>(
    '/LocalDrivingLicenseApplications/create-info',
    accessToken,
  )
}

export function createLocalDrivingLicenseApplication(
  accessToken: string,
  payload: { applicantPersonId: number; licenseClassId: number },
): Promise<{ localDrivingLicenseApplicationId: number }> {
  return apiRequest<{ localDrivingLicenseApplicationId: number }>(
    '/LocalDrivingLicenseApplications',
    accessToken,
    {
      method: 'POST',
      body: JSON.stringify(payload),
    },
  )
}

export function updateLocalDrivingLicenseApplication(
  accessToken: string,
  id: number,
  payload: { licenseClassId: number },
): Promise<void> {
  return apiRequest<void>(
    `/LocalDrivingLicenseApplications/${id}`,
    accessToken,
    {
      method: 'PUT',
      body: JSON.stringify(payload),
    },
  )
}

export function deleteLocalDrivingLicenseApplication(
  accessToken: string,
  id: number,
): Promise<void> {
  return apiRequest<void>(
    `/LocalDrivingLicenseApplications/${id}`,
    accessToken,
    {
      method: 'DELETE',
    },
  )
}

export function cancelLocalDrivingLicenseApplication(
  accessToken: string,
  id: number,
): Promise<void> {
  return apiRequest<void>(
    `/LocalDrivingLicenseApplications/${id}/cancel`,
    accessToken,
    {
      method: 'POST',
    },
  )
}

export function getApplicationBasicInfo(
  accessToken: string,
  localId: number,
): Promise<ApplicationBasicInfo> {
  return apiRequest<ApplicationBasicInfo>(
    `/LocalDrivingLicenseApplications/${localId}/application-basic-info`,
    accessToken,
  )
}

export function getLocalDrivingLicenseApplicationById(
  accessToken: string,
  id: number,
): Promise<LocalDrivingLicenseApplication> {
  return apiRequest<LocalDrivingLicenseApplication>(
    `/LocalDrivingLicenseApplications/${id}`,
    accessToken,
  )
}
