CREATE TABLE IF NOT EXISTS users (
  id TEXT PRIMARY KEY NOT NULL,
  email TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  role TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS booths (
  id TEXT PRIMARY KEY NOT NULL,
  name TEXT NOT NULL,
  slug TEXT NOT NULL UNIQUE,
  location TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS devices (
  id TEXT PRIMARY KEY NOT NULL,
  booth_id TEXT REFERENCES booths(id),
  name TEXT NOT NULL,
  model TEXT,
  serial_number TEXT,
  transport TEXT NOT NULL,
  status TEXT NOT NULL,
  diagnostics_json TEXT NOT NULL DEFAULT '[]',
  last_seen_at TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS events (
  id TEXT PRIMARY KEY NOT NULL,
  name TEXT NOT NULL,
  slug TEXT NOT NULL UNIQUE,
  venue TEXT NOT NULL,
  starts_at TEXT NOT NULL,
  ends_at TEXT NOT NULL,
  booth_ids_json TEXT NOT NULL DEFAULT '[]',
  description TEXT,
  created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS templates (
  id TEXT PRIMARY KEY NOT NULL,
  name TEXT NOT NULL,
  slug TEXT NOT NULL UNIQUE,
  definition_json TEXT NOT NULL,
  is_default INTEGER NOT NULL DEFAULT 0,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sessions (
  id TEXT PRIMARY KEY NOT NULL,
  booth_id TEXT NOT NULL REFERENCES booths(id),
  event_id TEXT REFERENCES events(id),
  operator_id TEXT REFERENCES users(id),
  template_id TEXT REFERENCES templates(id),
  status TEXT NOT NULL,
  capture_mode TEXT NOT NULL,
  shot_count INTEGER NOT NULL,
  countdown_seconds INTEGER NOT NULL,
  folder_path TEXT NOT NULL,
  sync_state TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  completed_at TEXT
);

CREATE TABLE IF NOT EXISTS session_photos (
  id TEXT PRIMARY KEY NOT NULL,
  session_id TEXT NOT NULL REFERENCES sessions(id),
  local_original_path TEXT NOT NULL,
  local_processed_path TEXT,
  cloud_object_key TEXT,
  preview_url TEXT,
  transferred_at TEXT,
  imported_from_device_id TEXT REFERENCES devices(id),
  duplicate_of_photo_id TEXT,
  metadata_json TEXT NOT NULL DEFAULT '{}',
  adjustments_json TEXT NOT NULL DEFAULT '{}',
  sync_state TEXT NOT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS rendered_outputs (
  id TEXT PRIMARY KEY NOT NULL,
  session_id TEXT NOT NULL REFERENCES sessions(id),
  template_id TEXT NOT NULL REFERENCES templates(id),
  kind TEXT NOT NULL,
  local_path TEXT,
  cloud_object_key TEXT,
  width INTEGER NOT NULL,
  height INTEGER NOT NULL,
  sync_state TEXT NOT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS print_jobs (
  id TEXT PRIMARY KEY NOT NULL,
  session_id TEXT NOT NULL REFERENCES sessions(id),
  rendered_output_id TEXT NOT NULL REFERENCES rendered_outputs(id),
  printer_name TEXT,
  copies INTEGER NOT NULL DEFAULT 1,
  state TEXT NOT NULL,
  submitted_at TEXT NOT NULL,
  completed_at TEXT,
  error_message TEXT
);

CREATE TABLE IF NOT EXISTS upload_jobs (
  id TEXT PRIMARY KEY NOT NULL,
  entity_id TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  state TEXT NOT NULL,
  attempts INTEGER NOT NULL DEFAULT 0,
  next_attempt_at TEXT,
  error_message TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS public_galleries (
  id TEXT PRIMARY KEY NOT NULL,
  session_id TEXT REFERENCES sessions(id),
  event_id TEXT REFERENCES events(id),
  title TEXT NOT NULL,
  description TEXT,
  is_download_enabled INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS share_tokens (
  id TEXT PRIMARY KEY NOT NULL,
  gallery_id TEXT NOT NULL REFERENCES public_galleries(id),
  token TEXT NOT NULL UNIQUE,
  expires_at TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS audit_logs (
  id TEXT PRIMARY KEY NOT NULL,
  actor_id TEXT REFERENCES users(id),
  action TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  payload_json TEXT NOT NULL DEFAULT '{}',
  created_at TEXT NOT NULL
);
