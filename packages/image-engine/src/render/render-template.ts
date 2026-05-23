import QRCode from 'qrcode';

import type { PhotoAdjustments, TemplateDefinition, TemplatePhotoSlot, SessionPhoto } from '@photobooth/core';

import { buildCanvasFilter, getPresetOverlay } from '../filters/edit-pipeline';

export interface RenderTemplateInput {
  template: TemplateDefinition;
  photos: Array<Pick<SessionPhoto, 'previewUrl' | 'localOriginalPath' | 'adjustments'> | { previewUrl?: string; localOriginalPath: string; adjustments: PhotoAdjustments }>;
  textOverrides?: Record<string, string>;
  qrOverrides?: Record<string, string>;
}

async function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.crossOrigin = 'anonymous';
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error(`Unable to load image: ${src}`));
    image.src = src;
  });
}

async function drawPhotoSlot(
  ctx: CanvasRenderingContext2D,
  slot: TemplatePhotoSlot,
  source: { previewUrl?: string; localOriginalPath: string; adjustments: PhotoAdjustments }
): Promise<void> {
  const src = source.previewUrl ?? source.localOriginalPath;
  const image = await loadImage(src);
  const crop = source.adjustments.crop;
  const sourceX = crop ? crop.x * image.width : 0;
  const sourceY = crop ? crop.y * image.height : 0;
  const sourceWidth = crop ? crop.width * image.width : image.width;
  const sourceHeight = crop ? crop.height * image.height : image.height;

  ctx.save();
  ctx.translate(slot.x + slot.width / 2, slot.y + slot.height / 2);
  ctx.rotate(((slot.rotation ?? 0) + source.adjustments.rotation) * (Math.PI / 180));
  ctx.filter = buildCanvasFilter(source.adjustments);

  if (slot.borderRadius) {
    const path = new Path2D();
    path.roundRect(-slot.width / 2, -slot.height / 2, slot.width, slot.height, slot.borderRadius);
    ctx.clip(path);
  }

  const fit = slot.fit ?? 'cover';
  const slotRatio = slot.width / slot.height;
  const imageRatio = sourceWidth / sourceHeight;
  let drawWidth = slot.width;
  let drawHeight = slot.height;

  if (fit === 'contain') {
    if (imageRatio > slotRatio) {
      drawHeight = slot.width / imageRatio;
    } else {
      drawWidth = slot.height * imageRatio;
    }
  } else {
    if (imageRatio > slotRatio) {
      const coverWidth = sourceHeight * slotRatio;
      const offset = (sourceWidth - coverWidth) / 2;
      ctx.drawImage(
        image,
        sourceX + offset,
        sourceY,
        coverWidth,
        sourceHeight,
        -slot.width / 2,
        -slot.height / 2,
        slot.width,
        slot.height
      );
      ctx.fillStyle = getPresetOverlay(source.adjustments);
      ctx.fillRect(-slot.width / 2, -slot.height / 2, slot.width, slot.height);
      ctx.restore();
      return;
    }
  }

  ctx.drawImage(
    image,
    sourceX,
    sourceY,
    sourceWidth,
    sourceHeight,
    -drawWidth / 2,
    -drawHeight / 2,
    drawWidth,
    drawHeight
  );
  ctx.fillStyle = getPresetOverlay(source.adjustments);
  ctx.fillRect(-slot.width / 2, -slot.height / 2, slot.width, slot.height);
  ctx.restore();
}

export async function renderTemplateToCanvas(
  canvas: HTMLCanvasElement,
  input: RenderTemplateInput
): Promise<HTMLCanvasElement> {
  canvas.width = input.template.exportWidth;
  canvas.height = input.template.exportHeight;
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Unable to render template without a 2D canvas context.');
  }

  ctx.fillStyle = input.template.backgroundColor;
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  if (input.template.backgroundImage) {
    try {
      const background = await loadImage(input.template.backgroundImage);
      ctx.drawImage(background, 0, 0, canvas.width, canvas.height);
    } catch {
      // Ignore missing background assets during local review.
    }
  }

  for (let index = 0; index < input.template.photoSlots.length; index += 1) {
    const slot = input.template.photoSlots[index];
    const photo = input.photos[index % input.photos.length];
    if (photo) {
      await drawPhotoSlot(ctx, slot, photo);
    }
  }

  for (const block of input.template.assetBlocks) {
    try {
      const asset = await loadImage(block.source);
      ctx.save();
      ctx.globalAlpha = block.opacity ?? 1;
      ctx.translate(block.x + block.width / 2, block.y + block.height / 2);
      ctx.rotate((block.rotation ?? 0) * (Math.PI / 180));
      ctx.drawImage(asset, -block.width / 2, -block.height / 2, block.width, block.height);
      ctx.restore();
    } catch {
      // Ignore missing optional assets.
    }
  }

  for (const block of input.template.textBlocks) {
    ctx.save();
    ctx.fillStyle = block.color;
    ctx.font = `${block.fontWeight ?? 500} ${block.fontSize}px ${block.fontFamily}`;
    ctx.textAlign = block.align ?? 'left';
    ctx.textBaseline = 'top';
    const text = input.textOverrides?.[block.id] ?? block.text;
    const x = block.align === 'center' ? block.x + block.width / 2 : block.x;
    const y = block.y;
    const lines = text.split('\n');
    lines.forEach((line, lineIndex) => {
      ctx.fillText(line, x, y + lineIndex * (block.fontSize * 1.2), block.width);
    });
    ctx.restore();
  }

  for (const block of input.template.qrBlocks) {
    const qrText = input.qrOverrides?.[block.id] ?? block.text;
    const qrDataUrl = await QRCode.toDataURL(qrText, {
      margin: 0,
      color: {
        dark: block.foreground,
        light: block.background
      }
    });
    const qrImage = await loadImage(qrDataUrl);
    ctx.drawImage(qrImage, block.x, block.y, block.size, block.size);
  }

  return canvas;
}

export async function renderTemplateToDataUrl(input: RenderTemplateInput): Promise<string> {
  const canvas = document.createElement('canvas');
  await renderTemplateToCanvas(canvas, input);
  return canvas.toDataURL('image/jpeg', 0.96);
}
