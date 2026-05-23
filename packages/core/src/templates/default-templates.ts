import type { TemplateDefinition } from '../domain';

const now = new Date().toISOString();

export const defaultTemplates: TemplateDefinition[] = [
  {
    id: 'template_4x6_single',
    name: '4x6 Single Hero',
    slug: '4x6-single-hero',
    paperSize: '4x6',
    exportWidth: 1800,
    exportHeight: 1200,
    dpi: 300,
    backgroundColor: '#0d1117',
    bleed: 24,
    photoSlots: [{ id: 'slot-1', x: 120, y: 120, width: 960, height: 960, borderRadius: 36, fit: 'cover' }],
    textBlocks: [
      { id: 'title', x: 1120, y: 180, width: 520, text: 'Photobooth', fontSize: 60, fontFamily: 'ui-sans-serif', fontWeight: 700, color: '#f8fafc' },
      { id: 'subtitle', x: 1120, y: 280, width: 520, text: 'Print-ready 4x6 portrait', fontSize: 26, fontFamily: 'ui-sans-serif', color: '#cbd5e1' }
    ],
    assetBlocks: [],
    qrBlocks: [{ id: 'qr', x: 1310, y: 820, size: 220, foreground: '#f8fafc', background: '#0d1117', text: 'https://gallery.example/session/demo' }],
    isDefault: true,
    updatedAt: now
  },
  {
    id: 'template_4x6_grid',
    name: '4x6 Grid',
    slug: '4x6-grid',
    paperSize: '4x6',
    exportWidth: 1800,
    exportHeight: 1200,
    dpi: 300,
    backgroundColor: '#111827',
    bleed: 24,
    photoSlots: [
      { id: 'slot-1', x: 96, y: 96, width: 768, height: 480, borderRadius: 24, fit: 'cover' },
      { id: 'slot-2', x: 936, y: 96, width: 768, height: 480, borderRadius: 24, fit: 'cover' },
      { id: 'slot-3', x: 96, y: 624, width: 768, height: 480, borderRadius: 24, fit: 'cover' },
      { id: 'slot-4', x: 936, y: 624, width: 768, height: 480, borderRadius: 24, fit: 'cover' }
    ],
    textBlocks: [],
    assetBlocks: [],
    qrBlocks: [],
    isDefault: true,
    updatedAt: now
  },
  {
    id: 'template_2x6_strip',
    name: '2x6 Classic Strip',
    slug: '2x6-classic-strip',
    paperSize: '2x6',
    exportWidth: 600,
    exportHeight: 1800,
    dpi: 300,
    backgroundColor: '#fafaf9',
    bleed: 18,
    photoSlots: [
      { id: 'slot-1', x: 60, y: 80, width: 480, height: 360, borderRadius: 16, fit: 'cover' },
      { id: 'slot-2', x: 60, y: 480, width: 480, height: 360, borderRadius: 16, fit: 'cover' },
      { id: 'slot-3', x: 60, y: 880, width: 480, height: 360, borderRadius: 16, fit: 'cover' },
      { id: 'slot-4', x: 60, y: 1280, width: 480, height: 360, borderRadius: 16, fit: 'cover' }
    ],
    textBlocks: [
      { id: 'footer', x: 60, y: 1680, width: 480, text: 'Scan for gallery', fontSize: 28, fontFamily: 'ui-sans-serif', fontWeight: 600, color: '#18181b', align: 'center' }
    ],
    assetBlocks: [],
    qrBlocks: [{ id: 'qr', x: 210, y: 1500, size: 180, foreground: '#18181b', background: '#fafaf9', text: 'https://gallery.example/session/demo' }],
    isDefault: true,
    updatedAt: now
  },
  {
    id: 'template_square_collage',
    name: 'Square Collage',
    slug: 'square-collage',
    paperSize: 'square',
    exportWidth: 1600,
    exportHeight: 1600,
    dpi: 300,
    backgroundColor: '#f8fafc',
    bleed: 20,
    photoSlots: [
      { id: 'slot-1', x: 80, y: 80, width: 690, height: 690, borderRadius: 24, fit: 'cover' },
      { id: 'slot-2', x: 830, y: 80, width: 690, height: 690, borderRadius: 24, fit: 'cover' },
      { id: 'slot-3', x: 80, y: 830, width: 1440, height: 690, borderRadius: 24, fit: 'cover' }
    ],
    textBlocks: [],
    assetBlocks: [],
    qrBlocks: [],
    isDefault: true,
    updatedAt: now
  },
  {
    id: 'template_freeform_demo',
    name: 'Freeform Event Hero',
    slug: 'freeform-event-hero',
    paperSize: 'custom',
    exportWidth: 2000,
    exportHeight: 1400,
    dpi: 300,
    backgroundColor: '#111827',
    backgroundImage: '',
    bleed: 24,
    photoSlots: [
      { id: 'slot-1', x: 120, y: 160, width: 820, height: 980, borderRadius: 40, rotation: -4, fit: 'cover' },
      { id: 'slot-2', x: 1040, y: 180, width: 760, height: 560, borderRadius: 32, rotation: 3, fit: 'cover' },
      { id: 'slot-3', x: 1120, y: 820, width: 600, height: 420, borderRadius: 32, rotation: -2, fit: 'cover' }
    ],
    textBlocks: [
      { id: 'headline', x: 1040, y: 60, width: 760, text: 'Midnight Reception', fontSize: 78, fontFamily: 'ui-serif', fontWeight: 700, color: '#f8fafc' },
      { id: 'tagline', x: 1040, y: 720, width: 760, text: 'Cloud-synced output with local-first capture', fontSize: 30, fontFamily: 'ui-sans-serif', color: '#cbd5e1' }
    ],
    assetBlocks: [],
    qrBlocks: [{ id: 'qr', x: 1760, y: 1120, size: 160, foreground: '#f8fafc', background: '#111827', text: 'https://gallery.example/session/demo' }],
    isDefault: true,
    updatedAt: now
  }
];
