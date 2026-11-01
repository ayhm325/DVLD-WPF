export type Person = {
  personId: number
  nationalNo: string
  fullName: string
  dateOfBirth: string
  gender: number
  address: string
  phone: string
  email: string | null
  countryName: string
}

export type PersonDetails = {
  personId: number
  nationalNo: string
  firstName: string
  secondName: string
  thirdName: string | null
  lastName: string
  fullName: string
  dateOfBirth: string
  gender: number
  address: string
  phone: string
  email: string | null
  nationalityCountryID: number
  countryName: string | null
  imagePath: string | null
}

export type CreatePersonRequest = {
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
  imagePath: string | null
}

export type UpdatePersonRequest = CreatePersonRequest

export type CreatePersonResponse = {
  personId: number
}
