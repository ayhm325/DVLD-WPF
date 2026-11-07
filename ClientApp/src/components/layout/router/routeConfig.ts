export type RouteGroup =
  | "dashboard"
  | "users"
  | "people"
  | "licenses"
  | "appointments"
  | "reference"
  | "account";

export const ROUTE_GROUP_MAP: Record<string, RouteGroup> = {
  // Dashboard
  Dashboard: "dashboard",

  // User Management (Admin only)
  "User Management": "users",

  // People & Drivers
  People: "people",
  "Add Person": "people",
  "Edit Person": "people",
  "Person Details": "people",
  Drivers: "people",
  "Person License History": "people",

  // Licenses & Permits
  Licenses: "licenses",
  "International Licenses": "licenses",
  "Renew License": "licenses",
  "Replace License": "licenses",
  "Detain License": "licenses",
  "Detained Licenses": "licenses",

  // Applications & Test Appointments
  "Local Applications": "appointments",
  "Test Appointments": "appointments",
  "Appointment Details": "appointments",
  "Manage Test Appointments": "appointments",
  "Written Test Appointments": "appointments",
  "Take Examination": "appointments",
  "Schedule Appointment": "appointments",

  // Reference Data
  Countries: "reference",
  "Application Types": "reference",
  "License Classes": "reference",
  "Test Types": "reference",

  // Account Management
  "My Profile": "account",
  "Change Password": "account",
};

export function resolveRouteGroup(page: string): RouteGroup | null {
  return ROUTE_GROUP_MAP[page] ?? null;
}
