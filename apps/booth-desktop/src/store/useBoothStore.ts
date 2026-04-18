import { create } from 'zustand';

import type {
  CameraDeviceDescriptor,
  CameraParameterSupport,
  CameraProvider,
  CameraStatus,
  PreviewSession
} from '@photobooth/camera-core';
import {
  buildSessionFolderName,
  createId,
  createSessionDraft,
  defaultTemplates,
  photoAdjustmentSchema,
  type PrintJob,
  type RenderedOutput,
  type Session,
  type SessionPhoto,
  type TemplateDefinition,
  type UploadJob
} from '@photobooth/core';
import { buildPrintArtifact, openBrowserPrintPreview } from '@photobooth/print-engine';
import { renderTemplateToDataUrl as renderTemplatePreview } from '@photobooth/image-engine';

import { formatError, sleep } from '@/lib/runtime';
import { getCameraRegistry } from '@/services/camera/provider-manager';
import { enqueueSessionSync, flushPendingSync } from '@/services/sync/sync-engine';
import { boothDb } from '@/services/storage/local-db';
import { ensureSessionPaths, persistRenderedDataUrl, persistWebcamBlob, type SessionPaths } from '@/services/storage/session-files';

const registry = getCameraRegistry();

interface CaptureDefaults {
  boothId: string;
  captureMode: 'single' | 'multi' | 'burst';
  shotCount: number;
  countdownSeconds: number;
  templateId: string;
}

interface BoothState {
  bootstrapped: boolean;
  providers: CameraProvider[];
  devices: CameraDeviceDescriptor[];
  selectedProviderId?: string;
  selectedDeviceId?: string;
  cameraStatus: CameraStatus;
  preview?: PreviewSession;
  supportedParameters: CameraParameterSupport[];
  captureDefaults: CaptureDefaults;
  currentSession?: Session;
  currentSessionPaths?: SessionPaths;
  photos: SessionPhoto[];
  templates: TemplateDefinition[];
  selectedTemplateId?: string;
  renderedOutput?: RenderedOutput;
  renderedPreviewUrl?: string;
  recentSessions: Session[];
  uploadJobs: UploadJob[];
  printJobs: PrintJob[];
  activeStep: 'setup' | 'capture' | 'edit' | 'template' | 'output';
  capturePhase: 'idle' | 'counting-down' | 'triggering' | 'waiting-transfer' | 'imported' | 'error';
  countdownRemaining: number;
  error?: string;
  bootstrap(): Promise<void>;
  refreshDevices(): Promise<void>;
  connectCamera(providerId: string, deviceId: string): Promise<void>;
  startPreview(): Promise<void>;
  stopPreview(): Promise<void>;
  setCaptureDefaults(next: Partial<CaptureDefaults>): void;
  startSession(): Promise<void>;
  capture(previewElement?: HTMLVideoElement | null): Promise<void>;
  updatePhotoAdjustment(photoId: string, patch: Partial<SessionPhoto['adjustments']>): Promise<void>;
  selectTemplate(templateId: string): void;
  renderOutput(): Promise<void>;
  openPrintPreview(): Promise<void>;
  queueSync(): Promise<void>;
  flushSync(): Promise<void>;
  resetSession(): Promise<void>;
}

function getProvider(providerId?: string): CameraProvider | undefined {
  return providerId ? registry.getProvider(providerId) : undefined;
}

async function refreshTimeline(set: (partial: Partial<BoothState>) => void): Promise<void> {
  const recentSessions = await boothDb.listRecentSessions();
  const uploadJobs = await boothDb.listUploadJobs();
  const printJobs = await boothDb.listPrintJobs();
  set({ recentSessions, uploadJobs, printJobs });
}

export const useBoothStore = create<BoothState>((set, get) => ({
  bootstrapped: false,
  providers: [],
  devices: [],
  cameraStatus: { state: 'idle', diagnostics: ['No camera connected yet.'] },
  supportedParameters: [],
  captureDefaults: {
    boothId: 'booth_main',
    captureMode: 'multi',
    shotCount: 3,
    countdownSeconds: 3,
    templateId: defaultTemplates[0]?.id ?? 'template_4x6_single'
  },
  photos: [],
  templates: [],
  recentSessions: [],
  uploadJobs: [],
  printJobs: [],
  activeStep: 'setup',
  capturePhase: 'idle',
  countdownRemaining: 0,

  async bootstrap() {
    await boothDb.bootstrap();
    const templates = await boothDb.listTemplates();
    set({ templates, selectedTemplateId: templates[0]?.id, captureDefaults: { ...get().captureDefaults, templateId: templates[0]?.id ?? get().captureDefaults.templateId } });
    await get().refreshDevices();
    await refreshTimeline(set);
    set({ bootstrapped: true });
  },

  async refreshDevices() {
    const providers = await registry.getAvailableProviders();
    const deviceGroups = await Promise.all(
      providers.map(async (provider) => ({
        provider,
        devices: await provider.listDevices()
      }))
    );
    set({
      providers,
      devices: deviceGroups.flatMap((entry) => entry.devices),
      error: undefined
    });
  },

  async connectCamera(providerId: string, deviceId: string) {
    try {
      const provider = getProvider(providerId);
      if (!provider) {
        throw new Error('Selected camera provider is unavailable.');
      }
      const cameraStatus = await provider.connect(deviceId);
      const supportedParameters = await provider.getSupportedParameters(deviceId);
      set({
        selectedProviderId: providerId,
        selectedDeviceId: deviceId,
        cameraStatus,
        supportedParameters,
        error: undefined
      });
    } catch (error) {
      set({ error: formatError(error), capturePhase: 'error' });
    }
  },

  async startPreview() {
    const provider = getProvider(get().selectedProviderId);
    const deviceId = get().selectedDeviceId;
    if (!provider || !deviceId) {
      set({ error: 'Select and connect a camera before starting preview.' });
      return;
    }
    try {
      const preview = await provider.startLiveView(deviceId);
      const cameraStatus = await provider.getStatus(deviceId);
      set({ preview, cameraStatus, activeStep: 'capture', error: undefined });
    } catch (error) {
      set({ error: formatError(error), capturePhase: 'error' });
    }
  },

  async stopPreview() {
    const provider = getProvider(get().selectedProviderId);
    const deviceId = get().selectedDeviceId;
    if (provider && deviceId) {
      await provider.stopLiveView(deviceId);
    }
    set({ preview: undefined, cameraStatus: { state: 'connected', diagnostics: ['Preview stopped.'] } });
  },

  setCaptureDefaults(next) {
    set({ captureDefaults: { ...get().captureDefaults, ...next } });
  },

  async startSession() {
    const { captureDefaults, selectedTemplateId } = get();
    const folderName = buildSessionFolderName(selectedTemplateId ?? 'session');
    const sessionPaths = await ensureSessionPaths(folderName);
    const session = createSessionDraft({
      boothId: captureDefaults.boothId,
      captureMode: captureDefaults.captureMode,
      shotCount: captureDefaults.shotCount,
      countdownSeconds: captureDefaults.countdownSeconds,
      folderPath: sessionPaths.root,
      templateId: selectedTemplateId
    });
    await boothDb.upsertSession(session);
    await refreshTimeline(set);
    set({ currentSession: session, currentSessionPaths: sessionPaths, activeStep: 'capture', photos: [], renderedOutput: undefined, renderedPreviewUrl: undefined });
  },

  async capture(previewElement) {
    const state = get();
    const provider = getProvider(state.selectedProviderId);
    const deviceId = state.selectedDeviceId;
    if (!provider || !deviceId) {
      set({ error: 'Select a camera before capture.' });
      return;
    }

    let session = state.currentSession;
    let sessionPaths = state.currentSessionPaths;
    if (!session || !sessionPaths) {
      await get().startSession();
      session = get().currentSession;
      sessionPaths = get().currentSessionPaths;
    }
    if (!session || !sessionPaths) {
      set({ error: 'Unable to create local session folders.' });
      return;
    }

    try {
      const countdown = session.countdownSeconds;
      for (let current = countdown; current > 0; current -= 1) {
        set({ capturePhase: 'counting-down', countdownRemaining: current, error: undefined });
        await sleep(1000);
      }

      set({ capturePhase: 'triggering', countdownRemaining: 0 });
      const capturedPhotos: SessionPhoto[] = [];

      if (provider.triggerRemoteShutter) {
        set({ capturePhase: 'waiting-transfer' });
        const transferred = await provider.triggerRemoteShutter(deviceId, {
          countdownSeconds: 0,
          fileNamePrefix: `session-${session.id}`,
          sessionFolder: sessionPaths.originals,
          burstCount: session.shotCount
        });

        for (const image of transferred) {
          const photo: SessionPhoto = {
            id: createId('photo'),
            sessionId: session.id,
            localOriginalPath: image.localPath,
            previewUrl: image.previewUrl,
            transferredAt: new Date().toISOString(),
            importedFromDeviceId: deviceId,
            metadata: image.metadata ?? {},
            adjustments: photoAdjustmentSchema.parse({}),
            syncState: 'pending',
            createdAt: new Date().toISOString()
          };
          await boothDb.upsertPhoto(photo);
          capturedPhotos.push(photo);
        }
      } else if (provider.capturePhoto) {
        for (let index = 0; index < session.shotCount; index += 1) {
          const rawImage = await provider.capturePhoto(deviceId, {
            countdownSeconds: 0,
            fileNamePrefix: `webcam-${session.id}-${index + 1}`,
            sessionFolder: sessionPaths.originals,
            previewElement
          });
          const blob = rawImage.previewUrl ? await fetch(rawImage.previewUrl).then((response) => response.blob()) : new Blob();
          const persisted = await persistWebcamBlob(sessionPaths, blob, `capture-${index + 1}`);
          const photo: SessionPhoto = {
            id: createId('photo'),
            sessionId: session.id,
            localOriginalPath: persisted.localPath,
            previewUrl: persisted.previewUrl,
            transferredAt: new Date().toISOString(),
            importedFromDeviceId: deviceId,
            metadata: rawImage.metadata ?? {},
            adjustments: photoAdjustmentSchema.parse({ mirrored: true }),
            syncState: 'pending',
            createdAt: new Date().toISOString()
          };
          await boothDb.upsertPhoto(photo);
          capturedPhotos.push(photo);
          if (index < session.shotCount - 1) {
            await sleep(800);
          }
        }
      } else {
        throw new Error('The selected provider cannot capture images.');
      }

      const nextSession: Session = {
        ...session,
        status: 'reviewing',
        updatedAt: new Date().toISOString()
      };
      await boothDb.upsertSession(nextSession);
      await refreshTimeline(set);
      set({
        currentSession: nextSession,
        photos: capturedPhotos,
        capturePhase: 'imported',
        activeStep: 'edit',
        error: undefined
      });
    } catch (error) {
      set({ error: formatError(error), capturePhase: 'error' });
    }
  },

  async updatePhotoAdjustment(photoId, patch) {
    const target = get().photos.find((photo) => photo.id === photoId);
    if (!target) {
      return;
    }
    const updated: SessionPhoto = {
      ...target,
      adjustments: photoAdjustmentSchema.parse({
        ...target.adjustments,
        ...patch
      })
    };
    await boothDb.upsertPhoto(updated);
    set({ photos: get().photos.map((photo) => (photo.id === photoId ? updated : photo)) });
  },

  selectTemplate(templateId) {
    set({ selectedTemplateId: templateId, activeStep: 'template' });
  },

  async renderOutput() {
    const state = get();
    const session = state.currentSession;
    const sessionPaths = state.currentSessionPaths;
    const template = state.templates.find((entry) => entry.id === state.selectedTemplateId);
    if (!session || !sessionPaths || !template || state.photos.length === 0) {
      set({ error: 'Capture at least one photo and choose a template before rendering.' });
      return;
    }

    try {
      const dataUrl = await renderTemplatePreview({
        template,
        photos: state.photos
      });
      const localPath = await persistRenderedDataUrl(sessionPaths, dataUrl, template.slug);
      const renderedOutput: RenderedOutput = {
        id: createId('output'),
        sessionId: session.id,
        templateId: template.id,
        kind: 'print-layout',
        localPath,
        width: template.exportWidth,
        height: template.exportHeight,
        syncState: 'pending',
        createdAt: new Date().toISOString()
      };
      await boothDb.upsertOutput(renderedOutput);
      set({ renderedOutput, renderedPreviewUrl: dataUrl, activeStep: 'output', error: undefined });
      await refreshTimeline(set);
    } catch (error) {
      set({ error: formatError(error) });
    }
  },

  async openPrintPreview() {
    const output = get().renderedOutput;
    const previewUrl = get().renderedPreviewUrl;
    const session = get().currentSession;
    if (!output || !previewUrl || !session) {
      set({ error: 'Render the output before printing.' });
      return;
    }

    await buildPrintArtifact({
      dataUrl: previewUrl,
      width: output.width,
      height: output.height
    });
    await openBrowserPrintPreview(previewUrl);

    const printJob: PrintJob = {
      id: createId('print'),
      sessionId: session.id,
      renderedOutputId: output.id,
      printerName: 'System Print Dialog',
      copies: 1,
      state: 'printed',
      submittedAt: new Date().toISOString(),
      completedAt: new Date().toISOString()
    };
    await boothDb.addPrintJob(printJob);
    await refreshTimeline(set);
  },

  async queueSync() {
    const session = get().currentSession;
    if (!session) {
      return;
    }
    await enqueueSessionSync(session.id);
    await refreshTimeline(set);
  },

  async flushSync() {
    await flushPendingSync();
    await refreshTimeline(set);
  },

  async resetSession() {
    set({
      currentSession: undefined,
      currentSessionPaths: undefined,
      photos: [],
      renderedOutput: undefined,
      renderedPreviewUrl: undefined,
      activeStep: 'setup',
      capturePhase: 'idle',
      error: undefined
    });
    await refreshTimeline(set);
  }
}));


