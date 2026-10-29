import { apiRequest } from './apiClient'

export type LoginRequest = {
  userName: string
  password: string
}

export type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  userId: number
  userName: string
  personId: number
  fullName: string
  role: string
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/Auth/login', undefined, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
