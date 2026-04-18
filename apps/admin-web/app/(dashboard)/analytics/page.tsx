import { Badge, Card } from '@photobooth/ui';

import { MetricGrid } from '@/components/dashboard/metric-grid';
import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function AnalyticsPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge className="border-cyan-400/20 bg-cyan-400/10 text-cyan-200">Analytics</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Operations overview</h1>
        <p className="mt-2 text-sm text-slate-400">Admin metrics across sessions, local booth activity, sync backlog, and print volume.</p>
      </div>
      <MetricGrid snapshot={snapshot} />
      <div className="grid gap-6 xl:grid-cols-2">
        <TableCard title="Storage Usage" description="Rendered outputs and originals waiting for R2 sync.">
          <div className="space-y-3 text-sm text-slate-300">
            <div className="flex items-center justify-between rounded-2xl border border-white/10 px-4 py-3"><span>Original captures</span><span>{snapshot.sessionPhotos.length} files</span></div>
            <div className="flex items-center justify-between rounded-2xl border border-white/10 px-4 py-3"><span>Rendered layouts</span><span>{snapshot.renderedOutputs.length} files</span></div>
            <div className="flex items-center justify-between rounded-2xl border border-white/10 px-4 py-3"><span>Sync backlog</span><span>{snapshot.uploadJobs.filter((job) => job.state !== 'synced').length} jobs</span></div>
          </div>
        </TableCard>
        <TableCard title="Booth Health" description="Current device and booth heartbeat state.">
          <div className="space-y-3 text-sm text-slate-300">
            {snapshot.devices.map((device) => (
              <div key={device.id} className="rounded-2xl border border-white/10 px-4 py-3">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-white">{device.name}</span>
                  <Badge>{device.status}</Badge>
                </div>
                <div className="mt-2 text-xs text-slate-400">{device.diagnostics.join(' · ')}</div>
              </div>
            ))}
          </div>
        </TableCard>
      </div>
    </div>
  );
}
