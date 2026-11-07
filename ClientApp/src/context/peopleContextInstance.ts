import { createContext } from "react";
import type { Person } from "../types/Person";

export interface PeopleContextValue {
  people: Person[];
  loading: boolean;
  error: string;
  refreshPeople: () => Promise<void>;
  deletePersonRecord: (personId: number) => Promise<void>;
}

export const PeopleContext = createContext<PeopleContextValue | undefined>(
  undefined,
);
