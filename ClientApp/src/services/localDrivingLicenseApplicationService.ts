import type {
  LocalApplicationPerson,
  LocalDrivingLicenseApplicationsPage,
  LocalLicenseClass,
} from '../types/LocalDrivingLicenseApplication'

import { apiRequest } from './apiClient'

export function getLocalDrivingLicenseApplications(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
): Promise<LocalDrivingLicenseApplicationsPage> {
  return apiRequest<LocalDrivingLicenseApplicationsPage>(
    `/LocalDrivingLicenseApplications?pageNumber=${pageNumber}&pageSize=${pageSize}`,
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

export function getLocalLicenseClasses(
  accessToken: string,
): Promise<LocalLicenseClass[]> {
  return apiRequest<LocalLicenseClass[]>('/LicenseClasses', accessToken)
}

export function getPersonById(
  accessToken: string,
  personId: number,
): Promise<LocalApplicationPerson> {
  return apiRequest<LocalApplicationPerson>(`/People/${personId}`, accessToken)
}

export function getPersonByNationalNo(
  accessToken: string,
  nationalNo: string,
): Promise<LocalApplicationPerson> {
  return apiRequest<LocalApplicationPerson>(
    `/People/national/${encodeURIComponent(nationalNo)}`,
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
