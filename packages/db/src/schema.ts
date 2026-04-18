import { sql } from 'drizzle-orm';
import { integer, sqliteTable, text } from 'drizzle-orm/sqlite-core';

export const users = sqliteTable('users', {
  id: text('id').primaryKey(),
  email: text('email').notNull().unique(),
  name: text('name').notNull(),
  role: text('role', { enum: ['admin', 'operator'] }).notNull(),
  passwordHash: text('password_hash').notNull(),
  createdAt: text('created_at').notNull().default(sql`CURRENT_TIMESTAMP`)
});

export const booths = sqliteTable('booths', {
  id: text('id').primaryKey(),
  name: text('name').notNull(),
  slug: text('slug').notNull().unique(),
  location: text('location').notNull(),
  createdAt: text('created_at').notNull().default(sql`CURRENT_TIMESTAMP`),
  updatedAt: text('updated_at').notNull().default(sql`CURRENT_TIMESTAMP`)
});

export const devices = sqliteTable('devices', {
  id: text('id').primaryKey(),
  boothId: text('booth_id').references(() => booths.id),
  name: text('name').notNull(),
  model: text('model'),
  serialNumber: text('serial_number'),
  transport: text('transport').notNull(),
  status: text('status').notNull(),
  diagnosticsJson: text('diagnostics_json').notNull().default('[]'),
  lastSeenAt: text('last_seen_at'),
  createdAt: text('created_at').notNull().default(sql`CURRENT_TIMESTAMP`)
});

export const events = sqliteTable('events', {
  id: text('id').primaryKey(),
  name: text('name').notNull(),
  slug: text('slug').notNull().unique(),
  venue: text('venue').notNull(),
  startsAt: text('starts_at').notNull(),
  endsAt: text('ends_at').notNull(),
  boothIdsJson: text('booth_ids_json').notNull().default('[]'),
  description: text('description'),
  createdAt: text('created_at').notNull().default(sql`CURRENT_TIMESTAMP`)
});

export const templates = sqliteTable('templates', {
  id: text('id').primaryKey(),
  name: text('name').notNull(),
  slug: text('slug').notNull().unique(),
  definitionJson: text('definition_json').notNull(),
  isDefault: integer('is_default', { mode: 'boolean' }).notNull().default(false),
  updatedAt: text('updated_at').notNull()
});

export const sessions = sqliteTable('sessions', {
  id: text('id').primaryKey(),
  boothId: text('booth_id').notNull().references(() => booths.id),
  eventId: text('event_id').references(() => events.id),
  operatorId: text('operator_id').references(() => users.id),
  templateId: text('template_id').references(() => templates.id),
  status: text('status').notNull(),
  captureMode: text('capture_mode').notNull(),
  shotCount: integer('shot_count').notNull(),
  countdownSeconds: integer('countdown_seconds').notNull(),
  folderPath: text('folder_path').notNull(),
  syncState: text('sync_state').notNull(),
  createdAt: text('created_at').notNull(),
  updatedAt: text('updated_at').notNull(),
  completedAt: text('completed_at')
});

export const sessionPhotos = sqliteTable('session_photos', {
  id: text('id').primaryKey(),
  sessionId: text('session_id').notNull().references(() => sessions.id),
  localOriginalPath: text('local_original_path').notNull(),
  localProcessedPath: text('local_processed_path'),
  cloudObjectKey: text('cloud_object_key'),
  previewUrl: text('preview_url'),
  transferredAt: text('transferred_at'),
  importedFromDeviceId: text('imported_from_device_id').references(() => devices.id),
  duplicateOfPhotoId: text('duplicate_of_photo_id'),
  metadataJson: text('metadata_json').notNull().default('{}'),
  adjustmentsJson: text('adjustments_json').notNull().default('{}'),
  syncState: text('sync_state').notNull(),
  createdAt: text('created_at').notNull()
});

export const renderedOutputs = sqliteTable('rendered_outputs', {
  id: text('id').primaryKey(),
  sessionId: text('session_id').notNull().references(() => sessions.id),
  templateId: text('template_id').notNull().references(() => templates.id),
  kind: text('kind').notNull(),
  localPath: text('local_path'),
  cloudObjectKey: text('cloud_object_key'),
  width: integer('width').notNull(),
  height: integer('height').notNull(),
  syncState: text('sync_state').notNull(),
  createdAt: text('created_at').notNull()
});

export const printJobs = sqliteTable('print_jobs', {
  id: text('id').primaryKey(),
  sessionId: text('session_id').notNull().references(() => sessions.id),
  renderedOutputId: text('rendered_output_id').notNull().references(() => renderedOutputs.id),
  printerName: text('printer_name'),
  copies: integer('copies').notNull().default(1),
  state: text('state').notNull(),
  submittedAt: text('submitted_at').notNull(),
  completedAt: text('completed_at'),
  errorMessage: text('error_message')
});

export const uploadJobs = sqliteTable('upload_jobs', {
  id: text('id').primaryKey(),
  entityId: text('entity_id').notNull(),
  entityType: text('entity_type').notNull(),
  state: text('state').notNull(),
  attempts: integer('attempts').notNull().default(0),
  nextAttemptAt: text('next_attempt_at'),
  errorMessage: text('error_message'),
  createdAt: text('created_at').notNull(),
  updatedAt: text('updated_at').notNull()
});

export const publicGalleries = sqliteTable('public_galleries', {
  id: text('id').primaryKey(),
  sessionId: text('session_id').references(() => sessions.id),
  eventId: text('event_id').references(() => events.id),
  title: text('title').notNull(),
  description: text('description'),
  isDownloadEnabled: integer('is_download_enabled', { mode: 'boolean' }).notNull().default(false),
  createdAt: text('created_at').notNull()
});

export const shareTokens = sqliteTable('share_tokens', {
  id: text('id').primaryKey(),
  galleryId: text('gallery_id').notNull().references(() => publicGalleries.id),
  token: text('token').notNull().unique(),
  expiresAt: text('expires_at'),
  createdAt: text('created_at').notNull()
});

export const auditLogs = sqliteTable('audit_logs', {
  id: text('id').primaryKey(),
  actorId: text('actor_id').references(() => users.id),
  action: text('action').notNull(),
  entityType: text('entity_type').notNull(),
  entityId: text('entity_id').notNull(),
  payloadJson: text('payload_json').notNull().default('{}'),
  createdAt: text('created_at').notNull()
});
