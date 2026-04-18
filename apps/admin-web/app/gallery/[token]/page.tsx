import { notFound } from 'next/navigation';

import { Badge, Card } from '@photobooth/ui';

import { getPublicGalleryByToken } from '@/lib/data';

export default async function PublicGalleryPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  const payload = await getPublicGalleryByToken(token);

  if (!payload) {
    notFound();
  }

  return (
    <main className="min-h-screen px-6 py-8">
      <div className="mx-auto max-w-6xl space-y-6">
        <div>
          <Badge className="border-emerald-400/20 bg-emerald-400/10 text-emerald-200">Public Gallery</Badge>
          <h1 className="mt-3 text-4xl font-semibold text-white">{payload.gallery.title}</h1>
          {payload.gallery.description ? <p className="mt-2 text-sm text-slate-400">{payload.gallery.description}</p> : null}
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {payload.photos.map((photo, index) => (
            <Card key={photo.id} className="overflow-hidden p-0">
              <div className="aspect-square bg-slate-950/70">
                {photo.previewUrl ? <img src={photo.previewUrl} alt={`Gallery photo ${index + 1}`} className="h-full w-full object-cover" /> : null}
              </div>
              <div className="px-4 py-4 text-sm text-slate-300">Capture {index + 1}</div>
            </Card>
          ))}
        </div>
      </div>
    </main>
  );
}
