import type { DashboardSnapshot } from '@photobooth/core';

import {
  seedBooths,
  seedDevices,
  seedEvents,
  seedPrintJobs,
  seedRenderedOutputs,
  seedSessionPhotos,
  seedSessions,
  seedTemplates,
  seedUploadJobs
} from './seed';

export const demoSnapshot: DashboardSnapshot = {
  booths: seedBooths,
  devices: seedDevices,
  events: seedEvents,
  sessions: seedSessions,
  sessionPhotos: seedSessionPhotos,
  renderedOutputs: seedRenderedOutputs,
  printJobs: seedPrintJobs,
  uploadJobs: seedUploadJobs,
  templates: seedTemplates
};
