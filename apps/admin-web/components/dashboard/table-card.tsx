import type { ReactNode } from 'react';

import { Card } from '@photobooth/ui';

export function TableCard({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  return (
    <Card className="space-y-4">
      <div>
        <div className="text-xs uppercase tracking-[0.24em] text-slate-400">{title}</div>
        <div className="mt-1 text-sm text-slate-400">{description}</div>
      </div>
      {children}
    </Card>
  );
}
