

type LoginRequest = {
  userName: string
  password: string
}


type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  userId: number
  userName: string
  personId: number
  fullName: string
  role: string
}


const API_BASE_URL = 'http://localhost:5260/api'

export async function login(
  request: LoginRequest
): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/Auth/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('Login failed')
  }

  return response.json()
}