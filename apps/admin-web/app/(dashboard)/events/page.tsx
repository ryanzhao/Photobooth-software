import { Badge } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function EventsPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge>Events</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Event management</h1>
      </div>
      <TableCard title="Scheduled Events" description="Booth routing and session grouping by event.">
        <div className="space-y-3">
          {snapshot.events.map((event) => (
            <div key={event.id} className="rounded-2xl border border-white/10 px-4 py-4 text-sm text-slate-300">
              <div className="font-medium text-white">{event.name}</div>
              <div className="mt-2 text-xs text-slate-500">{event.venue} · {event.startsAt} → {event.endsAt}</div>
              {event.description ? <div className="mt-2 text-sm text-slate-400">{event.description}</div> : null}
            </div>
          ))}
        </div>
      </TableCard>
    </div>
  );
}
