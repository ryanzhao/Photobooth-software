import type { PhotoAdjustments } from '@photobooth/core';

export function buildCanvasFilter(adjustments: PhotoAdjustments): string {
  const brightness = 100 + adjustments.brightness * 40;
  const contrast = 100 + adjustments.contrast * 40;
  const saturation = 100 + adjustments.saturation * 50;
  const warmthHue = adjustments.warmth * 10;
  const blur = adjustments.beauty * 0.8 + adjustments.blemishSoftening * 0.6;

  return [
    `brightness(${brightness}%)`,
    `contrast(${contrast}%)`,
    `saturate(${saturation}%)`,
    `hue-rotate(${warmthHue}deg)`,
    `blur(${blur.toFixed(2)}px)`
  ].join(' ');
}

export function getPresetOverlay(adjustments: PhotoAdjustments): string {
  switch (adjustments.filterPreset) {
    case 'mono':
      return 'rgba(18, 18, 18, 0.12)';
    case 'warm':
      return 'rgba(255, 164, 91, 0.08)';
    case 'cool':
      return 'rgba(123, 176, 255, 0.07)';
    case 'flash':
      return 'rgba(255, 255, 255, 0.06)';
    default:
      return 'rgba(0, 0, 0, 0)';
  }
}
