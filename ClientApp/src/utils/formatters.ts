/**
 * Centralized formatting utilities for dates, times, currencies, and numbers.
 */

export function formatDate(
  value?: string | Date | null,
  fallback = '—',
  options?: Intl.DateTimeFormatOptions,
): string {
  if (!value) return fallback
  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return fallback
  return date.toLocaleDateString(undefined, options)
}

export function formatDateTime(
  value?: string | Date | null,
  fallback = '—',
  options?: Intl.DateTimeFormatOptions,
): string {
  if (!value) return fallback
  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return fallback
  return date.toLocaleString(undefined, options)
}

export function formatCurrency(
  value?: number | null,
  currency = '$',
  fallback = '—',
): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return fallback
  }

  const formatted = value.toFixed(2)
  if (!currency) {
    return formatted
  }

  const prefixSymbols = ['$', '€', '£', '¥', '₹']
  if (prefixSymbols.includes(currency)) {
    return `${currency}${formatted}`
  }

  return `${formatted} ${currency}`
}

export function formatNumber(
  value?: number | null,
  fallback = '—',
  options?: Intl.NumberFormatOptions,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return fallback
  }
  return new Intl.NumberFormat(undefined, options).format(value)
}
