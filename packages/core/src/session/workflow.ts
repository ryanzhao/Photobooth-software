import { addSeconds } from 'date-fns';

import type { CaptureMode, Session } from '../domain';
import { createId } from '../utils/ids';

export interface CapturePlanFrame {
  index: number;
  triggerAt: string;
}

export function buildCapturePlan(
  countdownSeconds: number,
  shotCount: number,
  spacingSeconds = 2
): CapturePlanFrame[] {
  const start = new Date();
  return Array.from({ length: shotCount }, (_, index) => ({
    index,
    triggerAt: addSeconds(start, countdownSeconds + spacingSeconds * index).toISOString()
  }));
}

export function createSessionDraft(input: {
  boothId: string;
  eventId?: string;
  operatorId?: string;
  captureMode: CaptureMode;
  shotCount: number;
  countdownSeconds: number;
  folderPath: string;
  templateId?: string;
}): Session {
  const now = new Date().toISOString();
  return {
    id: createId('session'),
    boothId: input.boothId,
    eventId: input.eventId,
    operatorId: input.operatorId,
    templateId: input.templateId,
    status: 'draft',
    captureMode: input.captureMode,
    shotCount: input.shotCount,
    countdownSeconds: input.countdownSeconds,
    folderPath: input.folderPath,
    syncState: 'pending',
    createdAt: now,
    updatedAt: now
  };
}

export function buildSessionFolderName(nameHint: string): string {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  return `${stamp}_${nameHint.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
}
