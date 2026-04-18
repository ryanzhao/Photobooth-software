import Link from 'next/link';

import { Badge, Button } from '@photobooth/ui';
import { seedPublicGalleries, seedShareTokens } from '@photobooth/db';

import { TableCard } from '@/components/dashboard/table-card';

export default function GalleriesPage() {
  return (
    <div className="space-y-6">
      <div>
        <Badge>Galleries</Badge>
        <h1 className="mt-3 text-3xl font-semibold text-white">Guest gallery links</h1>
      </div>
      <TableCard title="Public Galleries" description="Shareable galleries backed by public share tokens.">
        <div className="space-y-3">
          {seedPublicGalleries.map((gallery) => {
            const token = seedShareTokens.find((entry) => entry.galleryId === gallery.id);
            return (
              <div key={gallery.id} className="flex items-center justify-between rounded-2xl border border-white/10 px-4 py-4 text-sm text-slate-300">
                <div>
                  <div className="font-medium text-white">{gallery.title}</div>
                  <div className="mt-1 text-xs text-slate-500">Token: {token?.token}</div>
                </div>
                {token ? (
                  <Link href={`/gallery/${token.token}`}>
                    <Button variant="secondary">Open Gallery</Button>
                  </Link>
                ) : null}
              </div>
            );
          })}
        </div>
      </TableCard>
    </div>
  );
}
