/**
 * Role-Based Access Control (RBAC) definitions for the DVLD React Client.
 *
 * Authoritative mapping verified against ASP.NET Core DVLD.Api authorization policies:
 * - AdminOnly: RequireRole("Admin")
 * - StaffOnly: RequireRole("Staff")
 * - StaffOrAdmin: RequireRole("Staff", "Admin")
 * - Default [Authorize]: Any authenticated user with a valid JWT
 */

export type UserRole = 'Admin' | 'Staff'

export const VALID_ROLES: readonly UserRole[] = ['Admin', 'Staff'] as const

export function isValidRole(role: unknown): role is UserRole {
  return typeof role === 'string' && (role === 'Admin' || role === 'Staff')
}

export type PageId =
  | 'Dashboard'
  | 'People'
  | 'Drivers'
  | 'Licenses'
  | 'International Licenses'
  | 'Local Applications'
  | 'Application Types'
  | 'License Classes'
  | 'Test Types'
  | 'Countries'

/**
 * Explicit permission mapping connecting each frontend page
 * to the authoritative backend roles permitted to access it.
 */
export const PAGE_PERMISSIONS: Record<PageId, readonly UserRole[]> = {
  // General Dashboard: Available to both Staff and Admin
  'Dashboard': ['Admin', 'Staff'],

  // People Management: PeopleController [Authorize(Policy = "StaffOrAdmin")]
  'People': ['Admin', 'Staff'],

  // Drivers Directory: DriversController [Authorize(Policy = "StaffOnly")]
  'Drivers': ['Staff'],

  // Issued Licenses: LicensesController [Authorize(Policy = "StaffOnly")]
  'Licenses': ['Staff'],

  // International Licenses: InternationalLicensesController [Authorize(Policy = "StaffOnly")]
  'International Licenses': ['Staff'],

  // Local Driving License Applications: LocalDrivingLicenseApplicationsController [Authorize(Policy = "StaffOnly")]
  'Local Applications': ['Staff'],

  // Application Types Settings: ApplicationTypesController [Authorize(Policy = "AdminOnly")]
  'Application Types': ['Admin'],

  // License Classes: LicenseClassesController [Authorize]
  'License Classes': ['Admin', 'Staff'],

  // Test Types: TestTypesController [Authorize] (Update is AdminOnly)
  'Test Types': ['Admin', 'Staff'],

  // Countries: CountriesController [Authorize]
  'Countries': ['Admin', 'Staff'],
}

/**
 * Verifies whether the specified role is permitted to navigate to or view a page.
 * Fails safely if the role is missing, null, unrecognized, or if the page is unknown.
 */
export function isPagePermitted(page: string, role?: string | null): boolean {
  if (!role || !isValidRole(role)) {
    // Fail safely: if role is missing or invalid, deny all restricted access
    return false
  }

  const allowedRoles = PAGE_PERMISSIONS[page as PageId]
  if (!allowedRoles) {
    // Unknown page: fail safely
    return false
  }

  return allowedRoles.includes(role)
}
