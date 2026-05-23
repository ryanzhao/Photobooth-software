import { Hono } from 'hono';

import { demoSnapshot } from '@photobooth/db';

import { requireBearerAuth } from '../lib/auth';
import type { ApiEnv } from '../types/env';

export const adminRoutes = new Hono<ApiEnv>();

adminRoutes.use('*', requireBearerAuth);

adminRoutes.get('/overview', (c) => c.json(demoSnapshot));
adminRoutes.get('/sessions', (c) => c.json(demoSnapshot.sessions));
adminRoutes.get('/templates', (c) => c.json(demoSnapshot.templates));
adminRoutes.get('/sync', (c) => c.json(demoSnapshot.uploadJobs));
