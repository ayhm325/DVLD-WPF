import { useContext } from "react";
import {
  PeopleContext,
  type PeopleContextValue,
} from "./peopleContextInstance";

export function usePeopleData(): PeopleContextValue {
  const context = useContext(PeopleContext);
  if (!context) {
    throw new Error("usePeopleData must be used within a PeopleProvider");
  }
  return context;
}
