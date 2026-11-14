import type { CSSProperties, ReactNode } from "react";
import { AlertCircle, AlertTriangle, Info, CheckCircle2 } from "lucide-react";
import { cn } from "@/lib/utils";

export type AlertVariant = "error" | "warning" | "info" | "success";

export interface ErrorAlertProps {
  title?: string;
  message?: string | null;
  children?: ReactNode;
  onRetry?: () => void;
  retryLabel?: string;
  onDismiss?: () => void;
  dismissLabel?: string;
  variant?: AlertVariant;
  className?: string;
  style?: CSSProperties;
}

const variantStyles: Record<
  AlertVariant,
  {
    container: string;
    iconColor: string;
    titleColor: string;
    textColor: string;
    retryButton: string;
    dismissButton: string;
  }
> = {
  error: {
    container: "border-destructive/40 bg-destructive/10 text-destructive",
    iconColor: "text-destructive",
    titleColor: "text-destructive",
    textColor: "text-destructive/90",
    retryButton: "bg-destructive/15 hover:bg-destructive/25 border-destructive/40 text-foreground focus-visible:ring-destructive",
    dismissButton: "text-destructive hover:text-foreground hover:bg-destructive/15 focus-visible:ring-destructive",
  },
  warning: {
    container: "border-warning/40 bg-warning-bg text-warning",
    iconColor: "text-warning",
    titleColor: "text-warning",
    textColor: "text-warning/90",
    retryButton: "bg-warning/15 hover:bg-warning/25 border-warning/40 text-foreground focus-visible:ring-warning",
    dismissButton: "text-warning hover:text-foreground hover:bg-warning/15 focus-visible:ring-warning",
  },
  info: {
    container: "border-info/40 bg-info-bg text-info",
    iconColor: "text-info",
    titleColor: "text-info",
    textColor: "text-info/90",
    retryButton: "bg-info/15 hover:bg-info/25 border-info/40 text-foreground focus-visible:ring-info",
    dismissButton: "text-info hover:text-foreground hover:bg-info/15 focus-visible:ring-info",
  },
  success: {
    container: "border-success/40 bg-success-bg text-success",
    iconColor: "text-success",
    titleColor: "text-success",
    textColor: "text-success/90",
    retryButton: "bg-success/15 hover:bg-success/25 border-success/40 text-foreground focus-visible:ring-success",
    dismissButton: "text-success hover:text-foreground hover:bg-success/15 focus-visible:ring-success",
  },
};

function getVariantIcon(variant: AlertVariant) {
  switch (variant) {
    case "warning":
      return (
        <AlertTriangle
          className={cn("mt-0.5 size-5 shrink-0 sm:mt-0", variantStyles.warning.iconColor)}
          aria-hidden="true"
        />
      );
    case "info":
      return (
        <Info
          className={cn("mt-0.5 size-5 shrink-0 sm:mt-0", variantStyles.info.iconColor)}
          aria-hidden="true"
        />
      );
    case "success":
      return (
        <CheckCircle2
          className={cn("mt-0.5 size-5 shrink-0 sm:mt-0", variantStyles.success.iconColor)}
          aria-hidden="true"
        />
      );
    case "error":
    default:
      return (
        <AlertCircle
          className={cn("mt-0.5 size-5 shrink-0 sm:mt-0", variantStyles.error.iconColor)}
          aria-hidden="true"
        />
      );
  }
}

export default function ErrorAlert({
  title,
  message,
  children,
  onRetry,
  retryLabel = "Retry",
  onDismiss,
  dismissLabel = "Dismiss",
  variant = "error",
  className = "",
  style,
}: ErrorAlertProps) {
  if (!message && !children) return null;

  const styles = variantStyles[variant] || variantStyles.error;

  return (
    <div
      role="alert"
      aria-atomic="true"
      className={cn(
        "flex min-w-0 flex-col gap-3 rounded-xl border p-4 text-sm transition-colors sm:flex-row sm:items-center sm:justify-between",
        styles.container,
        className,
      )}
      style={style}
    >
      <div className="flex min-w-0 flex-1 items-start gap-3 sm:items-center">
        {getVariantIcon(variant)}
        <div className="min-w-0 flex-1 [overflow-wrap:anywhere]">
          {title && (
            <p className={cn("font-semibold text-sm", styles.titleColor)}>
              {title}
            </p>
          )}
          {message && (
            <span
              className={cn(
                title ? "text-xs block mt-0.5" : "font-semibold",
                title ? styles.textColor : styles.titleColor,
              )}
            >
              {message}
            </span>
          )}
          {children}
        </div>
      </div>

      <div className="flex min-w-0 flex-wrap items-center gap-2 self-start sm:shrink-0 sm:self-auto">
        {onRetry && (
          <button
            type="button"
            className={cn(
              "inline-flex min-h-10 items-center justify-center rounded-lg border px-3 py-2 text-xs font-semibold shadow-xs transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-offset-background",
              styles.retryButton,
            )}
            onClick={onRetry}
          >
            {retryLabel}
          </button>
        )}
        {onDismiss && (
          <button
            type="button"
            className={cn(
              "inline-flex min-h-10 items-center justify-center rounded-lg px-2.5 py-2 text-xs font-medium transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-offset-background",
              styles.dismissButton,
            )}
            onClick={onDismiss}
          >
            {dismissLabel}
          </button>
        )}
      </div>
    </div>
  );
}
