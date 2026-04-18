import { Badge } from '@photobooth/ui';

import { TableCard } from '@/components/dashboard/table-card';
import { getDashboardSnapshot } from '@/lib/data';

export default async function TemplatesPage() {
  const snapshot = await getDashboardSnapshot();

  return (
    <div className="space-y-6">
      <div>
        <Badge>Templates</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">JSON layout templates</h1>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {snapshot.templates.map((template) => (
          <TableCard key={template.id} title={template.name} description={`${template.paperSize} · ${template.exportWidth}×${template.exportHeight} @ ${template.dpi}dpi`}>
            <div className="space-y-2 text-sm text-slate-300">
              <div>{template.photoSlots.length} photo slots</div>
              <div>{template.textBlocks.length} text blocks</div>
              <div>{template.qrBlocks.length} QR blocks</div>
              <div className="text-xs text-slate-500">Updated {template.updatedAt}</div>
            </div>
          </TableCard>
        ))}
      </div>
    </div>
  );
}
