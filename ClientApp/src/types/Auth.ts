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

export type AuthUser = {
  userId: number
  userName: string
  personId: number
  fullName: string
  role: string
}

export type AuthSession = {
  accessToken: string
  expiresAtUtc: string
  user: AuthUser
}
