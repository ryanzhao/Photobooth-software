import { Hono } from 'hono';

import { syncSessionSchema } from '@photobooth/core';

import type { ApiEnv } from '../types/env';

export const syncRoutes = new Hono<ApiEnv>();

syncRoutes.post('/session', async (c) => {
  const body = await c.req.json();
  const payload = syncSessionSchema.safeParse(body);
  if (!payload.success) {
    return c.json({ error: 'Invalid sync payload', details: payload.error.flatten() }, 400);
  }

  return c.json({
    accepted: true,
    sessionId: payload.data.session.id,
    receivedPhotos: payload.data.photos.length,
    receivedOutputs: payload.data.outputs.length,
    storageMode: c.env.DB ? 'd1' : 'seed-memory'
  });
});
