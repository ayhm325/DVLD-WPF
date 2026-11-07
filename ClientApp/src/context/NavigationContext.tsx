import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { Driver } from "../types/Driver";
import {
  NavigationContext,
  buildQueryPath,
  createSerializableNavigationParams,
  extractParamsFromLocation,
  resolvePageFromLocation,
  sanitizeParamsForPage,
  type NavigationContextValue,
  type NavigationParams,
} from "./navigationContextInstance";

export function NavigationProvider({ children }: { children: ReactNode }) {
  const [activePage, setActivePage] = useState<string>(() =>
    resolvePageFromLocation(),
  );

  const [params, setParams] = useState<NavigationParams>(() => {
    if (typeof window === "undefined") return {};
    const page = resolvePageFromLocation();
    const urlParams = extractParamsFromLocation(page, window.location.search);
    const historyParams =
      (window.history.state?.params as NavigationParams) || {};
    const merged = { ...historyParams, ...urlParams };
    return sanitizeParamsForPage(page, merged, urlParams);
  });

  // Synchronize browser popstate (Back/Forward) with activePage and params
  useEffect(() => {
    const handlePopState = (e: PopStateEvent) => {
      const page = resolvePageFromLocation();
      setActivePage(page);

      const urlParams = extractParamsFromLocation(page, window.location.search);
      const historyParams =
        e.state && typeof e.state === "object" && "params" in e.state
          ? ((e.state as { params: NavigationParams }).params || {})
          : {};

      const merged = { ...historyParams, ...urlParams };
      const resolvedParams = sanitizeParamsForPage(page, merged, urlParams);
      setParams(resolvedParams);
    };

    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, []);

  const navigate = useCallback(
    (
      page: string,
      nextParams?: NavigationParams,
      options?: { replace?: boolean },
    ) => {
      setActivePage(page);

      const baseParams: NavigationParams = nextParams ?? {};
      const fullPath = buildQueryPath(page, baseParams);

      const searchIndex = fullPath.indexOf("?");
      const targetSearch =
        searchIndex !== -1 ? fullPath.slice(searchIndex) : "";
      const urlExtracted = extractParamsFromLocation(page, targetSearch);

      const merged: NavigationParams = {
        ...baseParams,
        ...urlExtracted,
      };

      const resolvedParams = sanitizeParamsForPage(page, merged, urlExtracted);
      setParams(resolvedParams);

      if (typeof window !== "undefined") {
        const serializableParams =
          createSerializableNavigationParams(resolvedParams);
        const statePayload = { page, params: serializableParams };
        const currentFullPath =
          window.location.pathname + window.location.search;

        if (options?.replace) {
          window.history.replaceState(statePayload, "", fullPath);
        } else if (currentFullPath !== fullPath || nextParams) {
          window.history.pushState(statePayload, "", fullPath);
        }
      }
    },
    [],
  );

  const goBack = useCallback(
    (fallbackPage = "Dashboard") => {
      const returnPage =
        params.scheduleAppointmentContext?.returnPage ||
        params.manageTestAppointmentsContext?.returnPage ||
        params.takeExaminationContext?.returnPage ||
        params.appointmentDetailsSource ||
        params.personDetailsSource ||
        params.licenseHistorySource ||
        fallbackPage;

      navigate(returnPage);
    },
    [params, navigate],
  );

  const navigateToPersonDetails = useCallback(
    (personId: number, source: "People" | "Drivers" | string = "People") => {
      navigate("Person Details", {
        selectedPersonId: personId,
        personDetailsSource: source,
      });
    },
    [navigate],
  );

  const navigateToEditPerson = useCallback(
    (personId: number) => {
      navigate("Edit Person", {
        selectedPersonId: personId,
      });
    },
    [navigate],
  );

  const navigateToAddPerson = useCallback(() => {
    navigate("Add Person", {
      selectedPersonId: null,
    });
  }, [navigate]);

  const navigateToPersonLicenseHistory = useCallback(
    (
      personId: number,
      driver?: Driver | null,
      source: "Drivers" | "Local Applications" | string = "Drivers",
    ) => {
      navigate("Person License History", {
        selectedPersonId: personId,
        selectedDriverId: driver?.driverId ?? null,
        selectedDriver: driver ?? null,
        licenseHistorySource: source,
      });
    },
    [navigate],
  );

  const navigateToAppointmentDetails = useCallback(
    (appointmentId: number, source = "Test Appointments") => {
      navigate("Appointment Details", {
        selectedAppointmentId: appointmentId,
        appointmentDetailsSource: source,
      });
    },
    [navigate],
  );

  const navigateToScheduleAppointment = useCallback(
    (
      localAppId?: number | null,
      testTypeId: 1 | 2 | 3 = 1,
      source = "Local Applications",
      reopenModalOnReturn = false,
    ) => {
      navigate("Schedule Appointment", {
        scheduleAppointmentContext: {
          localAppId: localAppId ?? null,
          testTypeId,
          returnPage: source,
        },
        reopenModalTarget:
          reopenModalOnReturn && localAppId
            ? { localAppId, testTypeId }
            : null,
      });
    },
    [navigate],
  );

  const navigateToManageTestAppointments = useCallback(
    (
      localAppId: number,
      sourceOrTestType?: 1 | 2 | 3 | string,
      source?: string,
    ) => {
      const resolvedSource =
        typeof sourceOrTestType === "string"
          ? sourceOrTestType
          : source || "Local Applications";
      const resolvedTestType =
        typeof sourceOrTestType === "number"
          ? (sourceOrTestType as 1 | 2 | 3)
          : undefined;

      navigate("Manage Test Appointments", {
        manageTestAppointmentsContext: {
          localAppId,
          testTypeId: resolvedTestType,
          returnPage: resolvedSource,
        },
      });
    },
    [navigate],
  );

  const navigateToTakeExamination = useCallback(
    (appointmentId: number, source = "Manage Test Appointments") => {
      navigate("Take Examination", {
        takeExaminationContext: {
          appointmentId,
          returnPage: source,
        },
      });
    },
    [navigate],
  );

  const setSelectedPersonId = useCallback((id: number | null) => {
    setParams((prev) => ({ ...prev, selectedPersonId: id }));
  }, []);

  const setSelectedDriver = useCallback((driver: Driver | null) => {
    setParams((prev) => ({
      ...prev,
      selectedDriver: driver,
      selectedDriverId: driver?.driverId ?? null,
    }));
  }, []);

  const clearReopenModalTarget = useCallback(() => {
    setParams((prev) => ({ ...prev, reopenModalTarget: null }));
  }, []);

  const value = useMemo<NavigationContextValue>(
    () => ({
      activePage,
      params,
      selectedPersonId: params.selectedPersonId ?? null,
      selectedDriverId:
        params.selectedDriverId ?? params.selectedDriver?.driverId ?? null,
      selectedDriver: params.selectedDriver ?? null,
      personDetailsSource: params.personDetailsSource || "People",
      licenseHistorySource: params.licenseHistorySource || "Drivers",
      selectedAppointmentId: params.selectedAppointmentId ?? null,
      appointmentDetailsSource:
        params.appointmentDetailsSource || "Test Appointments",
      scheduleAppointmentContext: params.scheduleAppointmentContext ?? null,
      manageTestAppointmentsContext:
        params.manageTestAppointmentsContext ?? null,
      takeExaminationContext: params.takeExaminationContext ?? null,
      reopenModalTarget: params.reopenModalTarget ?? null,

      navigate,
      goBack,

      navigateToPersonDetails,
      navigateToEditPerson,
      navigateToAddPerson,
      navigateToPersonLicenseHistory,
      navigateToAppointmentDetails,
      navigateToScheduleAppointment,
      navigateToManageTestAppointments,
      navigateToTakeExamination,

      setSelectedPersonId,
      setSelectedDriver,
      clearReopenModalTarget,
    }),
    [
      activePage,
      params,
      navigate,
      goBack,
      navigateToPersonDetails,
      navigateToEditPerson,
      navigateToAddPerson,
      navigateToPersonLicenseHistory,
      navigateToAppointmentDetails,
      navigateToScheduleAppointment,
      navigateToManageTestAppointments,
      navigateToTakeExamination,
      setSelectedPersonId,
      setSelectedDriver,
      clearReopenModalTarget,
    ],
  );

  return (
    <NavigationContext.Provider value={value}>
      {children}
    </NavigationContext.Provider>
  );
}
