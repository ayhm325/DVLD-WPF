
export type Country = {
  countryId: number
  countryName: string
}

const API_BASE_URL = 'http://localhost:5260/api'

export async function getCountries(
  accessToken: string,
): Promise<Country[]> {
  const response = await fetch(`${API_BASE_URL}/Countries`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load countries (${response.status})`)
  }

  return response.json() as Promise<Country[]>
}
