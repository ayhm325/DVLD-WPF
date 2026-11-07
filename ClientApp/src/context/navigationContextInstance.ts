import { createContext } from "react";
import type { Driver } from "../types/Driver";

export const PAGE_TO_PATH: Record<string, string> = {
  Dashboard: "/",
  "User Management": "/users",
  People: "/people",
  "Add Person": "/people/add",
  "Edit Person": "/people/edit",
  "Person Details": "/people/details",
  Drivers: "/drivers",
  Licenses: "/licenses",
  "Person License History": "/licenses/history",
  "International Licenses": "/international-licenses",
  "Local Applications": "/local-applications",
  "Test Appointments": "/test-appointments",
  "Manage Test Appointments": "/test-appointments/manage",
  "Written Test Appointments": "/test-appointments/written",
  "Appointment Details": "/test-appointments/details",
  "Schedule Appointment": "/test-appointments/schedule",
  "Take Examination": "/test-appointments/take-exam",
  "Renew License": "/renew-license",
  "Replace License": "/replace-license",
  "Detain License": "/detain-license",
  "Detained Licenses": "/detained-licenses",
  "Application Types": "/application-types",
  "License Classes": "/license-classes",
  "Test Types": "/test-types",
  Countries: "/countries",
  "My Profile": "/profile",
  "Change Password": "/change-password",
};

export const PATH_TO_PAGE: Record<string, string> = Object.entries(
  PAGE_TO_PATH,
).reduce(
  (acc, [page, path]) => {
    acc[path.toLowerCase()] = page;
    return acc;
  },
  {} as Record<string, string>,
);

export function resolvePageFromLocation(): string {
  if (typeof window === "undefined") return "Dashboard";
  const normalizedPath =
    window.location.pathname.replace(/\/+$/, "").toLowerCase() || "/";
  return PATH_TO_PAGE[normalizedPath] ?? "Dashboard";
}

export function getPathForPage(page: string): string {
  return PAGE_TO_PATH[page] ?? "/";
}

export function isEntityDependentPage(page: string): boolean {
  return (
    page === "Person Details" ||
    page === "Edit Person" ||
    page === "Person License History" ||
    page === "Appointment Details" ||
    page === "Manage Test Appointments" ||
    page === "Take Examination" ||
    page === "Schedule Appointment"
  );
}

export function isContextPreservedPage(page: string): boolean {
  return isEntityDependentPage(page);
}

export function parsePositiveInt(
  val: string | null | undefined,
): number | null {
  if (!val) return null;
  const trimmed = val.trim();
  if (!/^\d+$/.test(trimmed)) return null;
  const num = parseInt(trimmed, 10);
  return Number.isSafeInteger(num) && num > 0 ? num : null;
}

export interface ScheduleAppointmentNavContext {
  localAppId?: number | null;
  testTypeId?: 1 | 2 | 3;
  returnPage?: string;
}

export interface ManageTestAppointmentsNavContext {
  localAppId: number;
  testTypeId?: 1 | 2 | 3;
  returnPage?: string;
}

export interface TakeExaminationNavContext {
  appointmentId: number;
  returnPage?: string;
}

export interface ReopenModalNavContext {
  localAppId: number;
  testTypeId: 1 | 2 | 3;
}

export interface NavigationParams {
  selectedPersonId?: number | null;
  selectedDriverId?: number | null;
  selectedDriver?: Driver | null;
  personDetailsSource?: "People" | "Drivers" | string;
  licenseHistorySource?: "Drivers" | "Local Applications" | string;
  selectedAppointmentId?: number | null;
  appointmentDetailsSource?: string;
  scheduleAppointmentContext?: ScheduleAppointmentNavContext | null;
  reopenModalTarget?: ReopenModalNavContext | null;
  manageTestAppointmentsContext?: ManageTestAppointmentsNavContext | null;
  takeExaminationContext?: TakeExaminationNavContext | null;
}

/**
 * Builds the canonical URL path including query parameters for context-dependent pages.
 */
export function buildQueryPath(
  page: string,
  params?: NavigationParams,
): string {
  const basePath = getPathForPage(page);
  if (!params) return basePath;

  switch (page) {
    case "Person Details":
      return params.selectedPersonId && params.selectedPersonId > 0
        ? `${basePath}?id=${params.selectedPersonId}`
        : basePath;

    case "Edit Person":
      return params.selectedPersonId && params.selectedPersonId > 0
        ? `${basePath}?id=${params.selectedPersonId}`
        : basePath;

    case "Person License History":
      return params.selectedPersonId && params.selectedPersonId > 0
        ? `${basePath}?personId=${params.selectedPersonId}`
        : basePath;

    case "Appointment Details":
      return params.selectedAppointmentId && params.selectedAppointmentId > 0
        ? `${basePath}?id=${params.selectedAppointmentId}`
        : basePath;

    case "Manage Test Appointments": {
      const appId = params.manageTestAppointmentsContext?.localAppId;
      return appId && appId > 0 ? `${basePath}?appId=${appId}` : basePath;
    }

    case "Take Examination": {
      const apptId = params.takeExaminationContext?.appointmentId;
      return apptId && apptId > 0
        ? `${basePath}?appointmentId=${apptId}`
        : basePath;
    }

    case "Schedule Appointment": {
      const ctx = params.scheduleAppointmentContext;
      if (ctx?.localAppId && ctx.localAppId > 0) {
        const query = new URLSearchParams();
        query.set("localAppId", String(ctx.localAppId));
        if (ctx.testTypeId && [1, 2, 3].includes(ctx.testTypeId)) {
          query.set("testTypeId", String(ctx.testTypeId));
        }
        return `${basePath}?${query.toString()}`;
      }
      return basePath;
    }

    default:
      return basePath;
  }
}

/**
 * Parses and validates entity identifiers from URL search parameters.
 */
export function extractParamsFromLocation(
  page: string,
  search: string,
): Partial<NavigationParams> {
  if (!search) return {};
  const query = new URLSearchParams(search);

  switch (page) {
    case "Person Details": {
      const id =
        parsePositiveInt(query.get("id")) ??
        parsePositiveInt(query.get("personId"));
      return id ? { selectedPersonId: id } : {};
    }

    case "Edit Person": {
      const id =
        parsePositiveInt(query.get("id")) ??
        parsePositiveInt(query.get("personId"));
      return id ? { selectedPersonId: id } : {};
    }

    case "Person License History": {
      const personId =
        parsePositiveInt(query.get("personId")) ??
        parsePositiveInt(query.get("id"));
      return personId ? { selectedPersonId: personId } : {};
    }

    case "Appointment Details": {
      const id =
        parsePositiveInt(query.get("id")) ??
        parsePositiveInt(query.get("appointmentId"));
      return id ? { selectedAppointmentId: id } : {};
    }

    case "Manage Test Appointments": {
      const appId =
        parsePositiveInt(query.get("appId")) ??
        parsePositiveInt(query.get("localAppId"));
      return appId
        ? {
            manageTestAppointmentsContext: {
              localAppId: appId,
            },
          }
        : {};
    }

    case "Take Examination": {
      const appointmentId =
        parsePositiveInt(query.get("appointmentId")) ??
        parsePositiveInt(query.get("id"));
      return appointmentId
        ? {
            takeExaminationContext: {
              appointmentId,
            },
          }
        : {};
    }

    case "Schedule Appointment": {
      const localAppId = parsePositiveInt(query.get("localAppId"));
      const rawTestType = parsePositiveInt(query.get("testTypeId"));
      const testTypeId =
        rawTestType === 1 || rawTestType === 2 || rawTestType === 3
          ? (rawTestType as 1 | 2 | 3)
          : undefined;

      if (localAppId) {
        return {
          scheduleAppointmentContext: {
            localAppId,
            testTypeId,
          },
        };
      }
      return {};
    }

    default:
      return {};
  }
}

/**
 * Sanitizes parameters for a specific page. Ensures that entity-dependent pages
 * never carry stale IDs from previously visited entities or invalid URL queries.
 */
export function sanitizeParamsForPage(
  page: string,
  merged: NavigationParams,
  urlParams: Partial<NavigationParams>,
): NavigationParams {
  switch (page) {
    case "Person Details":
    case "Edit Person": {
      const validId = urlParams.selectedPersonId ?? merged.selectedPersonId;
      return {
        ...merged,
        selectedPersonId: validId && validId > 0 ? validId : null,
      };
    }

    case "Person License History": {
      const validId = urlParams.selectedPersonId ?? merged.selectedPersonId;
      return {
        ...merged,
        selectedPersonId: validId && validId > 0 ? validId : null,
        selectedDriver: null, // Driver entity is resolved fresh via people/driver service
      };
    }

    case "Appointment Details": {
      const validId =
        urlParams.selectedAppointmentId ?? merged.selectedAppointmentId;
      return {
        ...merged,
        selectedAppointmentId: validId && validId > 0 ? validId : null,
      };
    }

    case "Manage Test Appointments": {
      const validAppId =
        urlParams.manageTestAppointmentsContext?.localAppId ??
        merged.manageTestAppointmentsContext?.localAppId;
      return {
        ...merged,
        manageTestAppointmentsContext:
          validAppId && validAppId > 0
            ? {
                ...merged.manageTestAppointmentsContext,
                localAppId: validAppId,
              }
            : null,
      };
    }

    case "Take Examination": {
      const validApptId =
        urlParams.takeExaminationContext?.appointmentId ??
        merged.takeExaminationContext?.appointmentId;
      return {
        ...merged,
        takeExaminationContext:
          validApptId && validApptId > 0
            ? {
                ...merged.takeExaminationContext,
                appointmentId: validApptId,
              }
            : null,
      };
    }

    case "Schedule Appointment": {
      const validLocalAppId =
        urlParams.scheduleAppointmentContext?.localAppId ??
        merged.scheduleAppointmentContext?.localAppId;
      return {
        ...merged,
        scheduleAppointmentContext:
          validLocalAppId && validLocalAppId > 0
            ? {
                ...merged.scheduleAppointmentContext,
                localAppId: validLocalAppId,
                testTypeId:
                  urlParams.scheduleAppointmentContext?.testTypeId ??
                  merged.scheduleAppointmentContext?.testTypeId ??
                  1,
              }
            : null,
      };
    }

    default:
      return merged;
  }
}

/**
 * Strips non-serializable objects (such as full Driver entities) before writing
 * to window.history.state, storing only primitive identifiers.
 */
export function createSerializableNavigationParams(
  params: NavigationParams,
): NavigationParams {
  return {
    selectedPersonId: params.selectedPersonId ?? null,
    selectedDriverId:
      params.selectedDriverId ?? params.selectedDriver?.driverId ?? null,
    personDetailsSource: params.personDetailsSource,
    licenseHistorySource: params.licenseHistorySource,
    selectedAppointmentId: params.selectedAppointmentId ?? null,
    appointmentDetailsSource: params.appointmentDetailsSource,
    scheduleAppointmentContext: params.scheduleAppointmentContext
      ? {
          localAppId: params.scheduleAppointmentContext.localAppId ?? null,
          testTypeId: params.scheduleAppointmentContext.testTypeId,
          returnPage: params.scheduleAppointmentContext.returnPage,
        }
      : null,
    manageTestAppointmentsContext: params.manageTestAppointmentsContext
      ? {
          localAppId: params.manageTestAppointmentsContext.localAppId,
          testTypeId: params.manageTestAppointmentsContext.testTypeId,
          returnPage: params.manageTestAppointmentsContext.returnPage,
        }
      : null,
    takeExaminationContext: params.takeExaminationContext
      ? {
          appointmentId: params.takeExaminationContext.appointmentId,
          returnPage: params.takeExaminationContext.returnPage,
        }
      : null,
    reopenModalTarget: params.reopenModalTarget ?? null,
  };
}

export interface NavigationContextValue {
  activePage: string;
  params: NavigationParams;

  // Direct parameter convenience accessors
  selectedPersonId: number | null;
  selectedDriverId: number | null;
  selectedDriver: Driver | null;
  personDetailsSource: string;
  licenseHistorySource: string;
  selectedAppointmentId: number | null;
  appointmentDetailsSource: string;
  scheduleAppointmentContext: ScheduleAppointmentNavContext | null;
  manageTestAppointmentsContext: ManageTestAppointmentsNavContext | null;
  takeExaminationContext: TakeExaminationNavContext | null;
  reopenModalTarget: ReopenModalNavContext | null;

  // Generalized navigation
  navigate: (
    page: string,
    nextParams?: NavigationParams,
    options?: { replace?: boolean },
  ) => void;
  goBack: (fallbackPage?: string) => void;

  // Semantic navigation helpers
  navigateToPersonDetails: (
    personId: number,
    source?: "People" | "Drivers" | string,
  ) => void;
  navigateToEditPerson: (personId: number) => void;
  navigateToAddPerson: () => void;
  navigateToPersonLicenseHistory: (
    personId: number,
    driver?: Driver | null,
    source?: "Drivers" | "Local Applications" | string,
  ) => void;
  navigateToAppointmentDetails: (
    appointmentId: number,
    source?: string,
  ) => void;
  navigateToScheduleAppointment: (
    localAppId?: number | null,
    testTypeId?: 1 | 2 | 3,
    source?: string,
    reopenModalOnReturn?: boolean,
  ) => void;
  navigateToManageTestAppointments: (
    localAppId: number,
    sourceOrTestType?: 1 | 2 | 3 | string,
    source?: string,
  ) => void;
  navigateToTakeExamination: (
    appointmentId: number,
    source?: string,
  ) => void;

  // Parameter modifiers
  setSelectedPersonId: (id: number | null) => void;
  setSelectedDriver: (driver: Driver | null) => void;
  clearReopenModalTarget: () => void;
}

export const NavigationContext = createContext<
  NavigationContextValue | undefined
>(undefined);
