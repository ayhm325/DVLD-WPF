import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import type { Person } from "../types/Person";
import { getPeople, deletePerson } from "../services/peopleService";
import { useAuth } from "./useAuth";
import {
  PeopleContext,
  type PeopleContextValue,
} from "./peopleContextInstance";

export function PeopleProvider({ children }: { children: ReactNode }) {
  const { accessToken } = useAuth();

  const [people, setPeople] = useState<Person[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  const inFlightPromiseRef = useRef<Promise<void> | null>(null);
  const currentTokenRef = useRef<string | null>(accessToken);

  useEffect(() => {
    currentTokenRef.current = accessToken;
  }, [accessToken]);

  // Unified list refresh: deduplicates overlapping in-flight calls
  const refreshPeople = useCallback((): Promise<void> => {
    if (!accessToken) {
      setPeople([]);
      return Promise.resolve();
    }

    if (inFlightPromiseRef.current) {
      return inFlightPromiseRef.current;
    }

    const tokenAtStart = accessToken;
    setLoading(true);
    setError("");

    const promise = getPeople(tokenAtStart)
      .then((data) => {
        if (currentTokenRef.current === tokenAtStart) {
          setPeople(data);
        }
      })
      .catch((err) => {
        if (currentTokenRef.current === tokenAtStart) {
          setError(
            err instanceof Error
              ? err.message
              : "Failed to load people directory.",
          );
        }
      })
      .finally(() => {
        if (currentTokenRef.current === tokenAtStart) {
          setLoading(false);
        }
        if (inFlightPromiseRef.current === promise) {
          inFlightPromiseRef.current = null;
        }
      });

    inFlightPromiseRef.current = promise;
    return promise;
  }, [accessToken]);

  // Initial fetch and token lifecycle synchronization
  useEffect(() => {
    let active = true;

    if (!accessToken) {
      currentTokenRef.current = null;
      inFlightPromiseRef.current = null;
      void Promise.resolve().then(() => {
        if (!active) return;
        setPeople([]);
        setLoading(false);
        setError("");
      });
      return () => {
        active = false;
        currentTokenRef.current = null;
        inFlightPromiseRef.current = null;
      };
    }

    currentTokenRef.current = accessToken;
    void Promise.resolve().then(() => {
      if (!active) return;
      setPeople([]);
      setLoading(false);
      setError("");
    });

    return () => {
      active = false;
      currentTokenRef.current = null;
      inFlightPromiseRef.current = null;
    };
  }, [accessToken, refreshPeople]);

  // Centralized deletion: calls delete API first, updates state only after confirmed success
  const deletePersonRecord = useCallback(
    async (personId: number): Promise<void> => {
      if (!accessToken) {
        throw new Error("Authentication required to delete person record.");
      }

      await deletePerson(accessToken, personId);
      setPeople((prev) => prev.filter((p) => p.personId !== personId));
    },
    [accessToken],
  );

  const value = useMemo<PeopleContextValue>(
    () => ({
      people,
      loading,
      error,
      refreshPeople,
      deletePersonRecord,
    }),
    [people, loading, error, refreshPeople, deletePersonRecord],
  );

  return (
    <PeopleContext.Provider value={value}>{children}</PeopleContext.Provider>
  );
}
