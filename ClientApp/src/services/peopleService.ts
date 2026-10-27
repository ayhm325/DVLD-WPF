const API_BASE_URL = 'http://localhost:5260/api'

export async function getPeople(accessToken: string) {
  const response = await fetch(`${API_BASE_URL}/People`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch people: ${response.status}`)
  }

  return response.json()
}