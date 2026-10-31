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

export type CurrentUserResponse = {
  userId: number
  userName?: string
  username?: string
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
