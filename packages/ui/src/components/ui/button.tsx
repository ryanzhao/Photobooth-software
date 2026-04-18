import { cva, type VariantProps } from 'class-variance-authority';
import type { ButtonHTMLAttributes, PropsWithChildren } from 'react';

import { cn } from '../../lib/utils';

const buttonVariants = cva(
  'inline-flex items-center justify-center rounded-2xl text-sm font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-400 disabled:cursor-not-allowed disabled:opacity-60',
  {
    variants: {
      variant: {
        primary: 'bg-cyan-400 px-4 py-2.5 text-slate-950 shadow-lg shadow-cyan-500/20 hover:bg-cyan-300',
        secondary: 'bg-white/10 px-4 py-2.5 text-white hover:bg-white/15',
        ghost: 'px-3 py-2 text-slate-300 hover:bg-white/5 hover:text-white',
        danger: 'bg-rose-500 px-4 py-2.5 text-white hover:bg-rose-400'
      },
      size: {
        md: 'h-11',
        lg: 'h-14 px-6 text-base',
        icon: 'h-11 w-11'
      }
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md'
    }
  }
);

export interface ButtonProps
  extends PropsWithChildren,
    ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export function Button({ className, variant, size, children, ...props }: ButtonProps) {
  return (
    <button className={cn(buttonVariants({ variant, size }), className)} {...props}>
      {children}
    </button>
  );
}
