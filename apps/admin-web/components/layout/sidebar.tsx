'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { BarChart3, Camera, Clock3, GalleryVertical, ImageIcon, LayoutTemplate, Layers3, RadioTower } from 'lucide-react';

import { Badge, cn } from '@photobooth/ui';

const items = [
  { href: '/analytics', label: 'Analytics', icon: BarChart3 },
  { href: '/sessions', label: 'Sessions', icon: Clock3 },
  { href: '/devices', label: 'Devices', icon: Camera },
  { href: '/outputs', label: 'Outputs', icon: ImageIcon },
  { href: '/sync', label: 'Sync Queue', icon: RadioTower },
  { href: '/templates', label: 'Templates', icon: LayoutTemplate },
  { href: '/events', label: 'Events', icon: Layers3 },
  { href: '/galleries', label: 'Galleries', icon: GalleryVertical }
];

export function Sidebar({ role }: { role: string }) {
  const pathname = usePathname();

  return (
    <aside className="rounded-[30px] border border-white/10 bg-slate-950/60 p-5 backdrop-blur">
      <div className="space-y-3 border-b border-white/10 pb-5">
        <Badge className="border-cyan-400/20 bg-cyan-400/10 text-cyan-200">Admin</Badge>
        <div>
          <div className="text-xl font-semibold text-white">Photobooth Control</div>
          <div className="mt-1 text-sm text-slate-400">Role: {role}</div>
        </div>
      </div>
      <nav className="mt-5 space-y-2">
        {items.map((item) => {
          const Icon = item.icon;
          const active = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                'flex items-center gap-3 rounded-2xl px-4 py-3 text-sm transition',
                active ? 'bg-cyan-400/10 text-cyan-200' : 'text-slate-300 hover:bg-white/5 hover:text-white'
              )}
            >
              <Icon className="h-4 w-4" />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
