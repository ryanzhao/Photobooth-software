import { Badge } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function OutputsPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge>Outputs</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Rendered layouts and print results</h1>
      </div>
      <TableCard title="Rendered Outputs" description="High-resolution print layouts and gallery assets.">
        <div className="space-y-3">
          {snapshot.renderedOutputs.map((output) => (
            <div key={output.id} className="flex items-center justify-between rounded-2xl border border-white/10 px-4 py-4 text-sm text-slate-300">
              <div>
                <div className="font-medium text-white">{output.id}</div>
                <div className="mt-1 text-xs text-slate-500">{output.width}×{output.height} · {output.localPath ?? 'Pending local export'}</div>
              </div>
              <Badge>{output.syncState}</Badge>
            </div>
          ))}
        </div>
      </TableCard>
    </div>
  );
}
