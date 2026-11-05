export interface User {
  userId: number
  personId: number
  userName: string
  isActive: boolean
  personFullName: string | null
  role: number // 1 = Staff, 2 = Admin
  imagePath?: string | null
}

export interface UserDetails {
  userId: number
  personId: number
  userName: string
  isActive: boolean
  role: number
  nationalNo: string
  firstName: string
  secondName: string
  thirdName: string | null
  lastName: string
  dateOfBirth: string
  gender: string
  address: string
  phone: string
  email: string | null
  nationalityCountryID: number
  countryName: string | null
  imagePath: string | null
}

export interface CreateUserRequest {
  personId: number
  userName: string
  password: string
  isActive: boolean
  role: number // 1 = Staff, 2 = Admin
}

export interface UpdateUserRequest {
  personId: number
  userName: string
  isActive: boolean
  role?: number // 1 = Staff, 2 = Admin
}
