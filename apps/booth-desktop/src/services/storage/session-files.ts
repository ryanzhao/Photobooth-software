import { createId } from '@photobooth/core';

import { isTauriRuntime } from '@/lib/runtime';

export interface SessionPaths {
  root: string;
  originals: string;
  processed: string;
  outputs: string;
}

const relativeRoot = import.meta.env.VITE_SESSION_ROOT ?? 'photobooth/sessions';

async function getTauriPaths() {
  const pathApi = await import('@tauri-apps/api/path');
  const fsApi = await import('@tauri-apps/plugin-fs');
  return { pathApi, fsApi };
}

export async function resolveSessionRoot(): Promise<string> {
  if (isTauriRuntime()) {
    const { pathApi } = await getTauriPaths();
    const appData = await pathApi.appDataDir();
    return pathApi.join(appData, relativeRoot);
  }
  return relativeRoot;
}

export async function ensureSessionPaths(folderName: string): Promise<SessionPaths> {
  const root = `${await resolveSessionRoot()}/${folderName}`.replace(/\\/g, '/');
  const paths: SessionPaths = {
    root,
    originals: `${root}/originals`,
    processed: `${root}/processed`,
    outputs: `${root}/outputs`
  };

  if (isTauriRuntime()) {
    const { fsApi } = await getTauriPaths();
    await fsApi.mkdir(paths.root, { recursive: true });
    await fsApi.mkdir(paths.originals, { recursive: true });
    await fsApi.mkdir(paths.processed, { recursive: true });
    await fsApi.mkdir(paths.outputs, { recursive: true });
  }

  return paths;
}

export async function persistBlobToPath(targetPath: string, blob: Blob): Promise<void> {
  if (!isTauriRuntime()) {
    return;
  }

  const { fsApi } = await getTauriPaths();
  const bytes = new Uint8Array(await blob.arrayBuffer());
  await fsApi.writeFile(targetPath, bytes);
}

export async function persistWebcamBlob(sessionPaths: SessionPaths, blob: Blob, prefix = 'capture'): Promise<{ localPath: string; previewUrl: string; fileName: string }> {
  const fileName = `${prefix}-${createId('img').slice(-8)}.jpg`;
  const localPath = `${sessionPaths.originals}/${fileName}`;
  await persistBlobToPath(localPath, blob);
  return {
    localPath,
    fileName,
    previewUrl: URL.createObjectURL(blob)
  };
}

export async function persistRenderedDataUrl(sessionPaths: SessionPaths, dataUrl: string, prefix = 'rendered'): Promise<string> {
  const fileName = `${prefix}-${createId('out').slice(-8)}.jpg`;
  const localPath = `${sessionPaths.outputs}/${fileName}`;
  if (isTauriRuntime()) {
    const [_, base64] = dataUrl.split(',');
    const binary = atob(base64 ?? '');
    const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
    const { fsApi } = await getTauriPaths();
    await fsApi.writeFile(localPath, bytes);
  }
  return localPath;
}
