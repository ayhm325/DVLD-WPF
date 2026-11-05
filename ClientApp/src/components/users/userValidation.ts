import type { User } from '../../types/User'

export const RESERVED_USERNAMES = ['admin', 'root', 'system', 'null'] as const
export const USERNAME_REGEX = /^[a-zA-Z][a-zA-Z0-9_]*$/

/**
 * Validates username using the exact rules enforced by Application.Validators.UserValidator.
 */
export function validateUsername(rawUsername: string): string | undefined {
  if (!rawUsername || !rawUsername.trim()) {
    return 'Username is required.'
  }

  const trimmed = rawUsername.trim()

  if (trimmed.includes(' ')) {
    return 'Username cannot contain spaces.'
  }

  if (trimmed.length < 3 || trimmed.length > 20) {
    return 'Username must be 3-20 characters.'
  }

  if (!USERNAME_REGEX.test(trimmed)) {
    return 'Username must start with a letter and contain only letters, numbers, or underscores.'
  }

  if (
    RESERVED_USERNAMES.some(
      (reserved) => reserved.toLowerCase() === trimmed.toLowerCase(),
    )
  ) {
    return 'This username is reserved.'
  }

  return undefined
}

/**
 * Validates password using the exact rules enforced by Application.Validators.UserValidator.
 */
export function validatePassword(password: string): string | undefined {
  if (!password || !password.trim()) {
    return 'Password is required.'
  }

  if (password.length < 8) {
    return 'Password must be at least 8 characters.'
  }

  if (password.length > 100) {
    return 'Password cannot exceed 100 characters.'
  }

  return undefined
}

export function isAdminRole(role: number): boolean {
  return role === 2
}

export function getRoleLabel(role: number): 'Admin' | 'Staff' {
  return role === 2 ? 'Admin' : 'Staff'
}

/**
 * Identifies the protected primary administrator account (UserID: 2, Role: Admin).
 */
export function isPrimaryAdminUser(user: Pick<User, 'userId' | 'role'>): boolean {
  return user.userId === 2 && user.role === 2
}

/**
 * Returns a human-readable reason if deactivating this user is blocked by safety policy,
 * or null if the account can safely be deactivated.
 */
export function getDeactivationBlockReason(
  target: Pick<User, 'userId' | 'role' | 'isActive'>,
  currentUserId: number | undefined,
  activeAdminCount: number,
): string | null {
  if (!target.isActive) {
    return null
  }

  if (isPrimaryAdminUser(target)) {
    return 'The primary system administrator account cannot be deactivated.'
  }

  if (currentUserId !== undefined && target.userId === currentUserId) {
    return 'You cannot deactivate your own currently signed-in account.'
  }

  if (isAdminRole(target.role) && activeAdminCount <= 1) {
    return 'Cannot deactivate the last active administrator account.'
  }

  return null
}

/**
 * Returns a human-readable reason if modifying this user's role is blocked by safety policy,
 * or null if the role can safely be updated.
 */
export function getRoleChangeBlockReason(
  target: Pick<User, 'userId' | 'role' | 'isActive'>,
  currentUserId: number | undefined,
  activeAdminCount: number,
  nextRole: number,
): string | null {
  if (target.role === nextRole) {
    return null
  }

  if (isPrimaryAdminUser(target)) {
    return 'The primary system administrator account role cannot be changed.'
  }

  if (currentUserId !== undefined && target.userId === currentUserId) {
    return 'You cannot change your own role.'
  }

  if (isAdminRole(target.role) && target.isActive && activeAdminCount <= 1 && !isAdminRole(nextRole)) {
    return 'Cannot change the role of the last active administrator.'
  }

  return null
}
