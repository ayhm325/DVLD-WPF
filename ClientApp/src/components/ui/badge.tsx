import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const badgeVariants = cva(
  'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-[11px] font-bold tracking-tight transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 select-none',
  {
    variants: {
      variant: {
        default:
          'bg-primary text-primary-foreground hover:bg-primary/90',
        secondary:
          'bg-secondary text-secondary-foreground border border-border hover:bg-secondary/80',
        destructive:
          'bg-[#fef2f2] text-[#dc2626] border border-[#fecaca]',
        outline:
          'text-foreground border border-border-strong',
        success:
          'bg-[#ecfdf5] text-[#059669] border border-[#a7f3d0]',
        warning:
          'bg-[#fffbeb] text-[#d97706] border border-[#fde68a]',
        info:
          'bg-[#eff6ff] text-[#2563eb] border border-[#bfdbfe]',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export { Badge, badgeVariants }
