import { apiRequest } from './apiClient'
import type {
  LoginRequest,
  LoginResponse,
  AuthUser,
  CurrentUserResponse,
  UserProfileResponse,
  ChangePasswordRequest,
} from '../types/Auth'

export type {
  LoginRequest,
  LoginResponse,
  AuthUser,
  CurrentUserResponse,
  UserProfileResponse,
  ChangePasswordRequest,
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
