import { useCallback, useEffect, useMemo, useState } from 'react'
import type { Dispatch, SetStateAction } from 'react'
import { getPeople } from '../services/peopleService'
import type { Person } from '../types/Person'
import { isPagePermitted } from '../utils/permissions'

export type UsePeopleResult = {
  people: Person[]
  filteredPeople: Person[]
  searchName: string
  setSearchName: (name: string) => void
  loadingPeople: boolean
  peopleError: string
  refreshPeople: () => void
  setPeople: Dispatch<SetStateAction<Person[]>>
  setPeopleError: (error: string) => void
}

/**
 * Custom hook to manage People state, search filtering,
 * and authorization-guarded data-fetching lifecycle.
 */
export function usePeople(
  accessToken: string | null,
  userRole: string | null | undefined,
  activePage: string,
): UsePeopleResult {
  const [searchName, setSearchName] = useState('')
  const [people, setPeople] = useState<Person[]>([])
  const [loadingPeople, setLoadingPeople] = useState(false)
  const [peopleError, setPeopleError] = useState('')
  const [peopleRefreshKey, setPeopleRefreshKey] = useState(0)

  // Load people: only when authenticated, permitted for role, and viewing Dashboard or People
  useEffect(() => {
    if (!accessToken || !isPagePermitted('People', userRole)) return
    if (activePage !== 'Dashboard' && activePage !== 'People') return

    let cancelled = false

    async function loadPeople() {
      setLoadingPeople(true)
      setPeopleError('')

      try {
        const result = await getPeople(accessToken!)
        if (!cancelled) {
          setPeople(result)
        }
      } catch (error) {
        if (!cancelled) {
          setPeopleError(
            error instanceof Error ? error.message : 'Failed to load people.',
          )
        }
      } finally {
        if (!cancelled) {
          setLoadingPeople(false)
        }
      }
    }

    void loadPeople()

    return () => {
      cancelled = true
    }
  }, [accessToken, userRole, activePage, peopleRefreshKey])

  const refreshPeople = useCallback(() => {
    setPeopleRefreshKey((current) => current + 1)
  }, [])

  // Case-insensitive, null-safe search filter
  const filteredPeople = useMemo(() => {
    const query = searchName.trim().toLowerCase()
    if (!query) return people

    return people.filter(
      (person) =>
        (person.fullName?.toLowerCase() || '').includes(query) ||
        (person.nationalNo?.toLowerCase() || '').includes(query),
    )
  }, [people, searchName])

  return {
    people,
    filteredPeople,
    searchName,
    setSearchName,
    loadingPeople,
    peopleError,
    refreshPeople,
    setPeople,
    setPeopleError,
  }
}
