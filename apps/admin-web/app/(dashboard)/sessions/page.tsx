import { Badge } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function SessionsPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge>Sessions</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Captured sessions</h1>
        <p className="mt-2 text-sm text-slate-400">Search/filter wiring is prepared around date, booth, operator, sync state, and print state.</p>
      </div>
      <TableCard title="Session List" description="Recent sessions stored locally and ready for cloud sync.">
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm text-slate-300">
            <thead className="text-xs uppercase tracking-[0.24em] text-slate-500">
              <tr>
                <th className="pb-3">Session</th>
                <th className="pb-3">Mode</th>
                <th className="pb-3">Shots</th>
                <th className="pb-3">Status</th>
                <th className="pb-3">Sync</th>
              </tr>
            </thead>
            <tbody>
              {snapshot.sessions.map((session) => (
                <tr key={session.id} className="border-t border-white/10">
                  <td className="py-4">
                    <div className="font-medium text-white">{session.id}</div>
                    <div className="text-xs text-slate-500">{session.folderPath}</div>
                  </td>
                  <td className="py-4">{session.captureMode}</td>
                  <td className="py-4">{session.shotCount}</td>
                  <td className="py-4"><Badge>{session.status}</Badge></td>
                  <td className="py-4"><Badge>{session.syncState}</Badge></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </TableCard>
    </div>
  );
}
