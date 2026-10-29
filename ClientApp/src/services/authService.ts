import { apiRequest } from './apiClient'
import type { LoginRequest, LoginResponse, AuthUser } from '../types/Auth'

export type { LoginRequest, LoginResponse, AuthUser }

export type CurrentUserResponse = {
  userId: number
  username: string
  fullName: string
  role: string
}

export type UserProfileResponse = {
  userId: number
  personId: number
  userName: string
  isActive: boolean
  fullName: string
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
  countryName: string
  imagePath: string | null
}

export type ChangePasswordRequest = {
  currentPassword: string
  newPassword: string
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/Auth/login', undefined, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function getCurrentUser(accessToken: string): Promise<CurrentUserResponse> {
  return apiRequest<CurrentUserResponse>('/Auth/me', accessToken)
}

export function getUserProfile(accessToken: string): Promise<UserProfileResponse> {
  return apiRequest<UserProfileResponse>('/Auth/profile', accessToken)
}

export function changePassword(
  accessToken: string,
  request: ChangePasswordRequest,
): Promise<void> {
  return apiRequest<void>('/Auth/change-password', accessToken, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
