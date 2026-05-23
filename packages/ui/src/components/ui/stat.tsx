import type { ReactNode } from 'react';

import { Card } from './card';

export function Stat({ label, value, detail, icon }: { label: string; value: string; detail?: string; icon?: ReactNode }) {
  return (
    <Card className="space-y-3 p-4">
      <div className="flex items-center justify-between text-slate-400">
        <span className="text-xs uppercase tracking-[0.24em]">{label}</span>
        {icon}
      </div>
      <div className="text-3xl font-semibold text-white">{value}</div>
      {detail ? <div className="text-sm text-slate-400">{detail}</div> : null}
    </Card>
  );
}
