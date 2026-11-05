import { apiRequest } from './apiClient'
import type { User, UserDetails, CreateUserRequest, UpdateUserRequest } from '../types/User'

export type { User, UserDetails, CreateUserRequest, UpdateUserRequest }

export function getUsers(accessToken: string): Promise<User[]> {
  return apiRequest<User[]>('/Users', accessToken)
}

export function getUserById(accessToken: string, userId: number): Promise<User> {
  return apiRequest<User>(`/Users/${userId}`, accessToken)
}

export function getUserDetails(accessToken: string, userId: number): Promise<UserDetails> {
  return apiRequest<UserDetails>(`/Users/${userId}/details`, accessToken)
}

export function getUserByUsername(accessToken: string, username: string): Promise<User> {
  return apiRequest<User>(`/Users/username/${encodeURIComponent(username)}`, accessToken)
}

export function getUserByPersonId(accessToken: string, personId: number): Promise<User> {
  return apiRequest<User>(`/Users/person/${personId}`, accessToken)
}

export function createUser(
  accessToken: string,
  request: CreateUserRequest,
): Promise<number> {
  return apiRequest<number>('/Users', accessToken, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export function updateUser(
  accessToken: string,
  userId: number,
  request: UpdateUserRequest,
): Promise<void> {
  return apiRequest<void>(`/Users/${userId}`, accessToken, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
