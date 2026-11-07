import { useContext } from "react";
import {
  NavigationContext,
  type NavigationContextValue,
} from "./navigationContextInstance";

export function useNavigation(): NavigationContextValue {
  const context = useContext(NavigationContext);
  if (!context) {
    throw new Error("useNavigation must be used within a NavigationProvider");
  }
  return context;
}
