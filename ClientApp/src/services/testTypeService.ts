
export type TestType = {
  testTypeId: number
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}

export type UpdateTestTypeRequest = {
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}

const API_BASE_URL = 'http://localhost:5260/api'

async function getErrorMessage(response: Response): Promise<string> {
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

export async function getTestTypes(
  accessToken: string,
): Promise<TestType[]> {
  const response = await fetch(`${API_BASE_URL}/TestTypes`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  return response.json() as Promise<TestType[]>
}

export async function updateTestType(
  accessToken: string,
  id: number,
  request: UpdateTestTypeRequest,
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/TestTypes/${id}`, {
    method: 'PUT',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }
}
