import { z } from 'zod';

export const syncStateSchema = z.enum(['pending', 'synced', 'failed', 'disabled']);
export const printStateSchema = z.enum(['queued', 'printing', 'printed', 'error', 'cancelled']);
export const captureModeSchema = z.enum(['single', 'multi', 'burst']);
export const cameraTransportSchema = z.enum(['webcam', 'tethered', 'sdk-bridge', 'scaffolded']);
export const cameraConnectionStateSchema = z.enum([
  'disconnected',
  'discovering',
  'connecting',
  'connected',
  'previewing',
  'triggering',
  'transferring',
  'error'
]);
export const uploadJobKindSchema = z.enum(['session', 'photo', 'rendered-output', 'analytics']);
export const outputKindSchema = z.enum(['print-layout', 'gallery-share', 'archive']);
export const roleSchema = z.enum(['admin', 'operator']);

export type SyncState = z.infer<typeof syncStateSchema>;
export type PrintState = z.infer<typeof printStateSchema>;
export type CaptureMode = z.infer<typeof captureModeSchema>;
export type CameraTransport = z.infer<typeof cameraTransportSchema>;
export type CameraConnectionState = z.infer<typeof cameraConnectionStateSchema>;
export type UploadJobKind = z.infer<typeof uploadJobKindSchema>;
export type OutputKind = z.infer<typeof outputKindSchema>;
export type UserRole = z.infer<typeof roleSchema>;

export const photoAdjustmentSchema = z.object({
  crop: z
    .object({
      x: z.number().min(0).max(1),
      y: z.number().min(0).max(1),
      width: z.number().min(0).max(1),
      height: z.number().min(0).max(1)
    })
    .nullable()
    .default(null),
  rotation: z.number().default(0),
  brightness: z.number().min(-1).max(1).default(0),
  contrast: z.number().min(-1).max(1).default(0),
  saturation: z.number().min(-1).max(1).default(0),
  warmth: z.number().min(-1).max(1).default(0),
  sharpen: z.number().min(0).max(1).default(0),
  beauty: z.number().min(0).max(1).default(0),
  blemishSoftening: z.number().min(0).max(1).default(0),
  filterPreset: z.string().default('natural'),
  mirrored: z.boolean().default(false)
});

export type PhotoAdjustments = z.infer<typeof photoAdjustmentSchema>;

export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  passwordHint?: string;
  createdAt: string;
}

export interface Booth {
  id: string;
  name: string;
  slug: string;
  location: string;
  lastSeenAt?: string;
}

export interface Device {
  id: string;
  boothId: string;
  name: string;
  model?: string;
  serialNumber?: string;
  transport: CameraTransport;
  status: CameraConnectionState;
  diagnostics: string[];
  lastSeenAt?: string;
}

export interface EventRecord {
  id: string;
  name: string;
  slug: string;
  venue: string;
  startsAt: string;
  endsAt: string;
  boothIds: string[];
  description?: string;
}

export interface Session {
  id: string;
  boothId: string;
  eventId?: string;
  operatorId?: string;
  templateId?: string;
  status: 'draft' | 'capturing' | 'reviewing' | 'ready_to_print' | 'completed' | 'cancelled';
  captureMode: CaptureMode;
  shotCount: number;
  countdownSeconds: number;
  folderPath: string;
  syncState: SyncState;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
}

export interface SessionPhoto {
  id: string;
  sessionId: string;
  localOriginalPath: string;
  localProcessedPath?: string;
  cloudObjectKey?: string;
  previewUrl?: string;
  transferredAt?: string;
  importedFromDeviceId?: string;
  duplicateOfPhotoId?: string;
  metadata: Record<string, string | number | boolean | null>;
  adjustments: PhotoAdjustments;
  syncState: SyncState;
  createdAt: string;
}

export interface TemplatePhotoSlot {
  id: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation?: number;
  borderRadius?: number;
  bleed?: number;
  fit?: 'cover' | 'contain';
}

export interface TemplateTextBlock {
  id: string;
  x: number;
  y: number;
  width: number;
  text: string;
  fontSize: number;
  fontFamily: string;
  fontWeight?: number;
  color: string;
  letterSpacing?: number;
  align?: 'left' | 'center' | 'right';
}

export interface TemplateAssetBlock {
  id: string;
  kind: 'logo' | 'sticker' | 'overlay' | 'background';
  x: number;
  y: number;
  width: number;
  height: number;
  rotation?: number;
  source: string;
  opacity?: number;
}

export interface TemplateQrBlock {
  id: string;
  x: number;
  y: number;
  size: number;
  foreground: string;
  background: string;
  text: string;
}

export interface TemplateDefinition {
  id: string;
  name: string;
  slug: string;
  paperSize: '4x6' | '2x6' | '5x7' | 'square' | 'custom';
  exportWidth: number;
  exportHeight: number;
  dpi: number;
  backgroundColor: string;
  backgroundImage?: string;
  bleed: number;
  photoSlots: TemplatePhotoSlot[];
  textBlocks: TemplateTextBlock[];
  assetBlocks: TemplateAssetBlock[];
  qrBlocks: TemplateQrBlock[];
  isDefault?: boolean;
  updatedAt: string;
}

export interface RenderedOutput {
  id: string;
  sessionId: string;
  templateId: string;
  kind: OutputKind;
  localPath?: string;
  cloudObjectKey?: string;
  width: number;
  height: number;
  syncState: SyncState;
  createdAt: string;
}

export interface PrintJob {
  id: string;
  sessionId: string;
  renderedOutputId: string;
  printerName?: string;
  copies: number;
  state: PrintState;
  submittedAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface UploadJob {
  id: string;
  entityId: string;
  entityType: UploadJobKind;
  state: SyncState;
  attempts: number;
  nextAttemptAt?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PublicGallery {
  id: string;
  sessionId?: string;
  eventId?: string;
  title: string;
  description?: string;
  isDownloadEnabled: boolean;
  createdAt: string;
}

export interface ShareToken {
  id: string;
  galleryId: string;
  token: string;
  expiresAt?: string;
  createdAt: string;
}

export interface AuditLog {
  id: string;
  actorId?: string;
  action: string;
  entityType: string;
  entityId: string;
  payload: Record<string, unknown>;
  createdAt: string;
}

export interface DashboardSnapshot {
  sessions: Session[];
  sessionPhotos: SessionPhoto[];
  renderedOutputs: RenderedOutput[];
  printJobs: PrintJob[];
  uploadJobs: UploadJob[];
  templates: TemplateDefinition[];
  events: EventRecord[];
  booths: Booth[];
  devices: Device[];
}

export const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(4)
});

export const syncSessionSchema = z.object({
  session: z.object({
    id: z.string(),
    boothId: z.string(),
    eventId: z.string().optional(),
    operatorId: z.string().optional(),
    templateId: z.string().optional(),
    status: z.string(),
    captureMode: captureModeSchema,
    shotCount: z.number().int().min(1),
    countdownSeconds: z.number().int().min(0),
    folderPath: z.string(),
    syncState: syncStateSchema,
    createdAt: z.string(),
    updatedAt: z.string(),
    completedAt: z.string().optional()
  }),
  photos: z.array(
    z.object({
      id: z.string(),
      sessionId: z.string(),
      localOriginalPath: z.string(),
      localProcessedPath: z.string().optional(),
      cloudObjectKey: z.string().optional(),
      previewUrl: z.string().optional(),
      transferredAt: z.string().optional(),
      importedFromDeviceId: z.string().optional(),
      duplicateOfPhotoId: z.string().optional(),
      metadata: z.record(z.union([z.string(), z.number(), z.boolean(), z.null()])),
      adjustments: photoAdjustmentSchema,
      syncState: syncStateSchema,
      createdAt: z.string()
    })
  ),
  outputs: z.array(
    z.object({
      id: z.string(),
      sessionId: z.string(),
      templateId: z.string(),
      kind: outputKindSchema,
      localPath: z.string().optional(),
      cloudObjectKey: z.string().optional(),
      width: z.number().int(),
      height: z.number().int(),
      syncState: syncStateSchema,
      createdAt: z.string()
    })
  )
});

export const shareTokenSchema = z.object({
  token: z.string().min(8)
});
