import { Hono } from 'hono';

import { seedPublicGalleries, seedSessionPhotos, seedShareTokens } from '@photobooth/db';

import type { ApiEnv } from '../types/env';

export const publicRoutes = new Hono<ApiEnv>();

publicRoutes.get('/gallery/:token', (c) => {
  const token = c.req.param('token');
  const shareToken = seedShareTokens.find((entry) => entry.token === token);
  if (!shareToken) {
    return c.json({ error: 'Not found' }, 404);
  }

  const gallery = seedPublicGalleries.find((entry) => entry.id === shareToken.galleryId);
  if (!gallery) {
    return c.json({ error: 'Not found' }, 404);
  }

  const photos = seedSessionPhotos.filter((photo) => photo.sessionId === gallery.sessionId);
  return c.json({ gallery, photos, shareToken });
});
