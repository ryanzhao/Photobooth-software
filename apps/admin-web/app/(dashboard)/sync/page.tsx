import { Badge, Button } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function SyncPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <Badge>Sync</Badge>
          <h1 className="mt-3 text-3xl font-semibold text-white">Upload queue</h1>
          <p className="mt-2 text-sm text-slate-400">Background sync is non-blocking; booth operations continue even when uploads fail.</p>
        </div>
        <Button variant="secondary">Retry Failed Jobs</Button>
      </div>
      <TableCard title="Queue State" description="Pending, synced, and failed upload jobs.">
        <div className="space-y-3">
          {snapshot.uploadJobs.map((job) => (
            <div key={job.id} className="rounded-2xl border border-white/10 px-4 py-4 text-sm text-slate-300">
              <div className="flex items-center justify-between">
                <div className="font-medium text-white">{job.entityType} · {job.entityId}</div>
                <Badge>{job.state}</Badge>
              </div>
              <div className="mt-2 text-xs text-slate-500">Attempts: {job.attempts} {job.errorMessage ? `· ${job.errorMessage}` : ''}</div>
            </div>
          ))}
        </div>
      </TableCard>
    </div>
  );
}
