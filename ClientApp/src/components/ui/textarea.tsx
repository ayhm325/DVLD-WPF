import * as React from 'react'
import { cn } from '@/lib/utils'

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, error, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        className={cn(
          'w-full min-h-[80px] px-3.5 py-2.5 rounded-xl bg-[#0e1a33] border border-[#263e68] text-white text-sm placeholder:text-slate-500 focus:outline-none focus:border-cyan-400 focus:ring-2 focus:ring-cyan-500/25 transition-all disabled:opacity-50 disabled:cursor-not-allowed resize-y',
          error && 'border-rose-500/70 focus:border-rose-400 focus:ring-rose-500/25',
          className,
        )}
        {...props}
      />
    )
  },
)
Textarea.displayName = 'Textarea'

export { Textarea }
