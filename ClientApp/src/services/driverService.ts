
export type Driver = {
  driverId: number
  personId: number
  fullName: string
  activeLicenses: number
  nationalNo: string
  dateOfBirth: string
  createdDate: string
}

const API_BASE_URL = 'http://localhost:5260/api'

export async function getDrivers(
  accessToken: string,
): Promise<Driver[]> {
  const response = await fetch(`${API_BASE_URL}/Drivers`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load drivers (${response.status})`)
  }

  return response.json() as Promise<Driver[]>
}
