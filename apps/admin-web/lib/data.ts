import type { DashboardSnapshot, PublicGallery, ShareToken } from '@photobooth/core';
import { demoSnapshot, seedPublicGalleries, seedSessionPhotos, seedShareTokens } from '@photobooth/db';

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;

async function fetchJson<T>(path: string, fallback: T): Promise<T> {
  if (!apiBaseUrl) {
    return fallback;
  }

  try {
    const response = await fetch(`${apiBaseUrl}${path}`, { cache: 'no-store' });
    if (!response.ok) {
      return fallback;
    }
    return (await response.json()) as T;
  } catch {
    return fallback;
  }
}

export async function getDashboardSnapshot(): Promise<DashboardSnapshot> {
  return fetchJson('/admin/overview', demoSnapshot);
}

export async function getPublicGalleryByToken(token: string): Promise<
  | {
      gallery: PublicGallery;
      shareToken: ShareToken;
      photos: typeof seedSessionPhotos;
    }
  | null
> {
  const shareToken = seedShareTokens.find((entry) => entry.token === token);
  if (!shareToken) {
    return null;
  }
  const gallery = seedPublicGalleries.find((entry) => entry.id === shareToken.galleryId);
  if (!gallery) {
    return null;
  }
  const photos = seedSessionPhotos.filter((photo) => photo.sessionId === gallery.sessionId);
  return { gallery, shareToken, photos };
}
