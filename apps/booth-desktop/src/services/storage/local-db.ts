import Dexie, { type Table } from 'dexie';

import type { PrintJob, RenderedOutput, Session, SessionPhoto, TemplateDefinition, UploadJob } from '@photobooth/core';
import { defaultTemplates } from '@photobooth/core';

class BoothDexieDatabase extends Dexie {
  sessions!: Table<Session, string>;
  photos!: Table<SessionPhoto, string>;
  outputs!: Table<RenderedOutput, string>;
  templates!: Table<TemplateDefinition, string>;
  printJobs!: Table<PrintJob, string>;
  uploadJobs!: Table<UploadJob, string>;

  constructor() {
    super('photobooth-local-db');
    this.version(1).stores({
      sessions: 'id, boothId, eventId, createdAt, updatedAt, syncState',
      photos: 'id, sessionId, createdAt, syncState',
      outputs: 'id, sessionId, templateId, syncState, createdAt',
      templates: 'id, slug, updatedAt',
      printJobs: 'id, sessionId, state, submittedAt',
      uploadJobs: 'id, entityId, entityType, state, createdAt'
    });
  }

  async bootstrap(): Promise<void> {
    if ((await this.templates.count()) === 0) {
      await this.templates.bulkPut(defaultTemplates);
    }
  }

  async listRecentSessions(limit = 8): Promise<Session[]> {
    const sessions = await this.sessions.orderBy('createdAt').reverse().limit(limit).toArray();
    return sessions;
  }

  async upsertSession(session: Session): Promise<void> {
    await this.sessions.put(session);
  }

  async upsertPhoto(photo: SessionPhoto): Promise<void> {
    await this.photos.put(photo);
  }

  async listPhotosBySession(sessionId: string): Promise<SessionPhoto[]> {
    return this.photos.where('sessionId').equals(sessionId).sortBy('createdAt');
  }

  async upsertOutput(output: RenderedOutput): Promise<void> {
    await this.outputs.put(output);
  }

  async listTemplates(): Promise<TemplateDefinition[]> {
    return this.templates.toArray();
  }

  async addPrintJob(job: PrintJob): Promise<void> {
    await this.printJobs.put(job);
  }

  async addUploadJob(job: UploadJob): Promise<void> {
    await this.uploadJobs.put(job);
  }

  async updateUploadJob(job: UploadJob): Promise<void> {
    await this.uploadJobs.put(job);
  }

  async listPendingUploadJobs(): Promise<UploadJob[]> {
    return this.uploadJobs.filter((job) => job.state === 'pending' || job.state === 'failed').toArray();
  }

  async listPrintJobs(limit = 10): Promise<PrintJob[]> {
    return this.printJobs.orderBy('submittedAt').reverse().limit(limit).toArray();
  }

  async listUploadJobs(limit = 10): Promise<UploadJob[]> {
    return this.uploadJobs.orderBy('createdAt').reverse().limit(limit).toArray();
  }
}

export const boothDb = new BoothDexieDatabase();
