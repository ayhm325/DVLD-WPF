import * as React from 'react'
import { cn } from '@/lib/utils'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = 'text', error, 'aria-invalid': ariaInvalid, ...props }, ref) => {
    return (
      <input
        type={type}
        ref={ref}
        aria-invalid={ariaInvalid ?? (error || undefined)}
        className={cn(
          'h-11 min-w-0 w-full rounded-xl border border-input bg-surface px-3.5 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-ring/30 read-only:cursor-default read-only:opacity-80 disabled:cursor-not-allowed disabled:opacity-50 transition-colors',
          '[&[aria-invalid=true]]:border-destructive [&[aria-invalid=true]]:focus-visible:border-destructive [&[aria-invalid=true]]:focus-visible:ring-destructive/25',
          className,
        )}
        {...props}
      />
    )
  },
)
Input.displayName = 'Input'

export { Input }
