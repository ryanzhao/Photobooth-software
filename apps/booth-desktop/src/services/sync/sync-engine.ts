import type { UploadJob } from '@photobooth/core';
import { createId } from '@photobooth/core';

import { boothDb } from '../storage/local-db';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:8787';

export async function enqueueSessionSync(sessionId: string): Promise<UploadJob> {
  const now = new Date().toISOString();
  const job: UploadJob = {
    id: createId('upload'),
    entityId: sessionId,
    entityType: 'session',
    state: 'pending',
    attempts: 0,
    createdAt: now,
    updatedAt: now
  };
  await boothDb.addUploadJob(job);
  return job;
}

export async function flushPendingSync(): Promise<UploadJob[]> {
  const pending = await boothDb.listPendingUploadJobs();
  const updated: UploadJob[] = [];

  for (const job of pending) {
    const session = await boothDb.sessions.get(job.entityId);
    if (!session) {
      continue;
    }
    const photos = await boothDb.listPhotosBySession(session.id);
    const outputs = await boothDb.outputs.where('sessionId').equals(session.id).toArray();

    try {
      const response = await fetch(`${apiBaseUrl}/sync/session`, {
        method: 'POST',
        headers: {
          'content-type': 'application/json'
        },
        body: JSON.stringify({ session, photos, outputs })
      });

      if (!response.ok) {
        throw new Error(`Sync API responded with ${response.status}`);
      }

      const nextJob: UploadJob = {
        ...job,
        state: 'synced',
        attempts: job.attempts + 1,
        updatedAt: new Date().toISOString(),
        errorMessage: undefined
      };
      await boothDb.updateUploadJob(nextJob);
      updated.push(nextJob);
    } catch (error) {
      const nextJob: UploadJob = {
        ...job,
        state: 'failed',
        attempts: job.attempts + 1,
        updatedAt: new Date().toISOString(),
        nextAttemptAt: new Date(Date.now() + 1000 * 30).toISOString(),
        errorMessage: error instanceof Error ? error.message : String(error)
      };
      await boothDb.updateUploadJob(nextJob);
      updated.push(nextJob);
    }
  }

  return updated;
}
