/* eslint-disable no-misleading-character-class */
/**
 * Person name validation and sanitization utilities for DVLD.
 */

// Arabic letters (standard and extended, excluding Arabic-Indic digits 0660-0669 and Eastern digits 06F0-06F9)
// Arabic diacritics / combining marks (\u064B-\u065F, \u0670) are intentionally included
export const PERSON_NAME_ALLOWED_REGEX =
  /^[a-zA-Z\u0621-\u064A\u0671-\u06D3\u06D5\u064B-\u065F\u0670\s'’-]+$/u

export const PERSON_NAME_HAS_LETTER_REGEX =
  /[a-zA-Z\u0621-\u064A\u0671-\u06D3\u06D5]/u

export const PERSON_NAME_SANITIZE_REGEX =
  /[^a-zA-Z\u0621-\u064A\u0671-\u06D3\u06D5\u064B-\u065F\u0670\s'’-]/gu

export function sanitizePersonName(value: string): string {
  if (!value) return ''
  return value.replace(PERSON_NAME_SANITIZE_REGEX, '')
}

export function validatePersonName(
  value: string,
  fieldLabel: string,
  required: boolean,
): { valid: boolean; error?: string } {
  const trimmed = value.trim()
  if (!trimmed) {
    if (required) {
      return { valid: false, error: `${fieldLabel} is required.` }
    }
    return { valid: true }
  }

  if (!PERSON_NAME_ALLOWED_REGEX.test(trimmed)) {
    return {
      valid: false,
      error: `${fieldLabel} can contain Arabic or English letters, spaces, hyphens, and apostrophes only.`,
    }
  }

  if (!PERSON_NAME_HAS_LETTER_REGEX.test(trimmed)) {
    return {
      valid: false,
      error: `${fieldLabel} can contain Arabic or English letters, spaces, hyphens, and apostrophes only.`,
    }
  }

  return { valid: true }
}
