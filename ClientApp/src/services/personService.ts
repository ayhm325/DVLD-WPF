
const API_BASE_URL = 'http://localhost:5260/api'

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

export async function createPerson(
  accessToken: string,
  person: CreatePersonRequest,
): Promise<CreatePersonResponse> {
  const response = await fetch(`${API_BASE_URL}/People`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify(person),
  })

  if (!response.ok) {
    const responseText = await response.text()
    let message = `Failed to create person (${response.status})`

    if (responseText) {
      try {
        const body: unknown = JSON.parse(responseText)

        if (
          typeof body === 'object' &&
          body !== null &&
          'detail' in body &&
          typeof body.detail === 'string'
        ) {
          message = body.detail
        } else if (
          typeof body === 'object' &&
          body !== null &&
          'title' in body &&
          typeof body.title === 'string'
        ) {
          message = body.title
        } else {
          message = responseText
        }
      } catch {
        message = responseText
      }
    }

    throw new Error(message)
  }

  return response.json() as Promise<CreatePersonResponse>
}
