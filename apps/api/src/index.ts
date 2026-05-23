import { Hono } from 'hono';
import { cors } from 'hono/cors';

import { loginSchema } from '@photobooth/core';

import { issueToken, validateCredentials } from './lib/auth';
import { adminRoutes } from './routes/admin';
import { publicRoutes } from './routes/public';
import { syncRoutes } from './routes/sync';
import type { ApiEnv } from './types/env';

const app = new Hono<ApiEnv>();

app.use('*', cors({ origin: '*' }));

app.get('/health', (c) => c.json({ ok: true, date: '2026-04-08' }));

app.post('/auth/login', async (c) => {
  const body = await c.req.json();
  const payload = loginSchema.safeParse(body);
  if (!payload.success) {
    return c.json({ error: 'Invalid credentials payload' }, 400);
  }

  if (!validateCredentials(payload.data.email, payload.data.password)) {
    return c.json({ error: 'Invalid credentials' }, 401);
  }

  const token = await issueToken(payload.data.email, c.env.JWT_SECRET);
  return c.json({ token });
});

app.route('/admin', adminRoutes);
app.route('/public', publicRoutes);
app.route('/sync', syncRoutes);

export default app;
