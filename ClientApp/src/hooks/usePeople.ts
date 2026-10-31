import { useCallback, useMemo, useState } from 'react'
import type { Dispatch, SetStateAction } from 'react'
import { getPeople } from '../services/peopleService'
import type { Person } from '../types/Person'
import { isPagePermitted } from '../utils/permissions'
import { useAsyncData } from './useAsyncData'

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

  const isPermitted = isPagePermitted('People', userRole)
  const isTargetPage = activePage === 'Dashboard' || activePage === 'People'
  const isEnabled = Boolean(accessToken && isPermitted && isTargetPage)

  const fetcher = useCallback(() => {
    return getPeople(accessToken!)
  }, [accessToken])

  const {
    data: people,
    loading: loadingPeople,
    error: peopleError,
    refresh: refreshPeople,
    setData: setPeople,
    setError: setPeopleError,
  } = useAsyncData<Person[]>(fetcher, {
    enabled: isEnabled,
    initialData: [],
    dependencies: [userRole, activePage],
    defaultErrorMessage: 'Failed to load people.',
  })

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
