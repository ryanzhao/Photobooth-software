import type { HTMLAttributes } from 'react';

import { cn } from '../../lib/utils';

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('rounded-[28px] border border-white/10 bg-slate-950/60 p-5 shadow-[0_24px_80px_-32px_rgba(15,23,42,0.8)] backdrop-blur', className)}
      {...props}
    />
  );
}
