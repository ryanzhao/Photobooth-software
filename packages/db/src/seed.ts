import type {
  Booth,
  Device,
  EventRecord,
  PrintJob,
  PublicGallery,
  RenderedOutput,
  Session,
  SessionPhoto,
  ShareToken,
  TemplateDefinition,
  UploadJob,
  User
} from '@photobooth/core';
import { defaultTemplates, photoAdjustmentSchema } from '@photobooth/core';

const now = new Date().toISOString();

export const seedUsers: User[] = [
  {
    id: 'user_admin_1',
    email: 'admin@photobooth.local',
    name: 'Avery Admin',
    role: 'admin',
    passwordHint: 'admin1234',
    createdAt: now
  },
  {
    id: 'user_operator_1',
    email: 'operator@photobooth.local',
    name: 'Owen Operator',
    role: 'operator',
    passwordHint: 'operator1234',
    createdAt: now
  }
];

export const seedBooths: Booth[] = [
  {
    id: 'booth_main',
    name: 'Main Ballroom Booth',
    slug: 'main-ballroom-booth',
    location: 'Grand Ballroom East'
  }
];

export const seedDevices: Device[] = [
  {
    id: 'device_sony_demo',
    boothId: 'booth_main',
    name: 'Sony Alpha Demo',
    model: 'Sony SDK Bridge',
    transport: 'sdk-bridge',
    status: 'connected',
    diagnostics: ['Waiting for Sony bridge binary', 'Live view supported when vendor SDK is installed'],
    lastSeenAt: now
  },
  {
    id: 'device_webcam_demo',
    boothId: 'booth_main',
    name: 'Integrated Webcam',
    model: 'Browser MediaDevices',
    transport: 'webcam',
    status: 'previewing',
    diagnostics: ['Webcam fallback is fully runnable for local MVP validation'],
    lastSeenAt: now
  }
];

export const seedEvents: EventRecord[] = [
  {
    id: 'event_midnight_reception',
    name: 'Midnight Reception',
    slug: 'midnight-reception',
    venue: 'Rooftop Hall',
    startsAt: now,
    endsAt: new Date(Date.now() + 4 * 60 * 60 * 1000).toISOString(),
    boothIds: ['booth_main'],
    description: 'Launch event for the local-first photobooth platform.'
  }
];

export const seedTemplates: TemplateDefinition[] = defaultTemplates;

export const seedSessions: Session[] = [
  {
    id: 'session_demo_1',
    boothId: 'booth_main',
    eventId: 'event_midnight_reception',
    operatorId: 'user_operator_1',
    templateId: 'template_4x6_single',
    status: 'completed',
    captureMode: 'multi',
    shotCount: 3,
    countdownSeconds: 3,
    folderPath: 'booth-data/sessions/session_demo_1',
    syncState: 'pending',
    createdAt: now,
    updatedAt: now,
    completedAt: now
  }
];

const adjustments = photoAdjustmentSchema.parse({
  brightness: 0.12,
  contrast: 0.08,
  saturation: 0.1,
  warmth: 0.04,
  sharpen: 0.18,
  beauty: 0.22,
  blemishSoftening: 0.16
});

export const seedSessionPhotos: SessionPhoto[] = [
  {
    id: 'photo_demo_1',
    sessionId: 'session_demo_1',
    localOriginalPath: 'booth-data/sessions/session_demo_1/originals/demo-1.jpg',
    localProcessedPath: 'booth-data/sessions/session_demo_1/processed/demo-1.jpg',
    previewUrl: '/samples/demo-1.jpg',
    transferredAt: now,
    importedFromDeviceId: 'device_sony_demo',
    metadata: { shutter: '1/125', iso: 320, aperture: 'f/4.0' },
    adjustments,
    syncState: 'pending',
    createdAt: now
  }
];

export const seedRenderedOutputs: RenderedOutput[] = [
  {
    id: 'output_demo_1',
    sessionId: 'session_demo_1',
    templateId: 'template_4x6_single',
    kind: 'print-layout',
    localPath: 'booth-data/sessions/session_demo_1/outputs/print-layout.jpg',
    width: 1800,
    height: 1200,
    syncState: 'pending',
    createdAt: now
  }
];

export const seedPrintJobs: PrintJob[] = [
  {
    id: 'print_demo_1',
    sessionId: 'session_demo_1',
    renderedOutputId: 'output_demo_1',
    printerName: 'DNP DS-RX1HS',
    copies: 2,
    state: 'printed',
    submittedAt: now,
    completedAt: now
  }
];

export const seedUploadJobs: UploadJob[] = [
  {
    id: 'upload_demo_1',
    entityId: 'session_demo_1',
    entityType: 'session',
    state: 'pending',
    attempts: 1,
    createdAt: now,
    updatedAt: now,
    errorMessage: 'Waiting for Cloudflare credentials'
  }
];

export const seedPublicGalleries: PublicGallery[] = [
  {
    id: 'gallery_demo_1',
    sessionId: 'session_demo_1',
    title: 'Midnight Reception Highlights',
    description: 'Guest-ready gallery for one completed session.',
    isDownloadEnabled: true,
    createdAt: now
  }
];

export const seedShareTokens: ShareToken[] = [
  {
    id: 'share_demo_1',
    galleryId: 'gallery_demo_1',
    token: 'demo-gallery-token',
    createdAt: now
  }
];

export const seedCredentials = {
  'admin@photobooth.local': { password: 'admin1234', role: 'admin' as const },
  'operator@photobooth.local': { password: 'operator1234', role: 'operator' as const }
};
