import { Badge } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function DevicesPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge>Devices</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Booth and camera status</h1>
      </div>
      <TableCard title="Detected Devices" description="Tethered cameras, webcam fallback, and bridge diagnostics.">
        <div className="space-y-4">
          {snapshot.devices.map((device) => (
            <div key={device.id} className="rounded-3xl border border-white/10 bg-white/5 p-5">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <div className="text-lg font-semibold text-white">{device.name}</div>
                  <div className="mt-1 text-sm text-slate-400">{device.model} · {device.transport}</div>
                </div>
                <Badge>{device.status}</Badge>
              </div>
              <div className="mt-4 text-sm text-slate-400">{device.diagnostics.join(' · ')}</div>
            </div>
          ))}
        </div>
      </TableCard>
    </div>
  );
}
