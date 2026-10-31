import { useEffect, useState } from 'react'

/**
 * Custom hook to debounce rapidly changing values (such as search inputs or resize listeners).
 *
 * @param value The value to debounce.
 * @param delayMs The delay in milliseconds. Defaults to 300ms.
 */
export function useDebounce<T>(value: T, delayMs = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value)

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value)
    }, delayMs)

    return () => {
      clearTimeout(handler)
    }
  }, [value, delayMs])

  return debouncedValue
}
