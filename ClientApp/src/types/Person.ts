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

export type CreatePersonResponse = {
  personId: number
}
