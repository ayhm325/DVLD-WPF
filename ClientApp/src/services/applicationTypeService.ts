
export type ApplicationType = {
  applicationTypeId: number
  applicationTypeTitle: string
  applicationTypeFees: number
}

export type UpdateApplicationTypeRequest = {
  applicationTypeTitle: string
  applicationTypeFees: number
}

const API_BASE_URL = 'http://localhost:5260/api'

async function readError(response: Response): Promise<string> {
  try {
    const body: { detail?: string; title?: string } =
      await response.json()

    return (
      body.detail ||
      body.title ||
      `Request failed (${response.status})`
    )
  } catch {
    return `Request failed (${response.status})`
  }
}

export async function getApplicationTypes(
  accessToken: string,
): Promise<ApplicationType[]> {
  const response = await fetch(`${API_BASE_URL}/ApplicationTypes`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return response.json() as Promise<ApplicationType[]>
}

export async function updateApplicationType(
  accessToken: string,
  id: number,
  request: UpdateApplicationTypeRequest,
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/ApplicationTypes/${id}`,
    {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  )

  if (!response.ok) {
    throw new Error(await readError(response))
  }
}
