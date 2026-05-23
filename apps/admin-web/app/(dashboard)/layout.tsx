import type { ReactNode } from 'react';

import { Button, Card } from '@photobooth/ui';

import { signOut, requireUser } from '@/lib/auth';
import { Sidebar } from '@/components/layout/sidebar';

export default async function DashboardLayout({ children }: { children: ReactNode }) {
  const user = await requireUser();

  return (
    <main className="min-h-screen px-6 py-6">
      <div className="mx-auto grid max-w-[1800px] gap-6 xl:grid-cols-[280px_1fr]">
        <div className="space-y-4">
          <Sidebar role={user.role} />
          <Card className="space-y-3">
            <div className="text-sm text-slate-400">Signed in as</div>
            <div className="text-lg font-semibold text-white">{user.name}</div>
            <div className="text-sm text-slate-400">{user.email}</div>
            <form action={signOut}>
              <Button variant="secondary" className="w-full">Sign Out</Button>
            </form>
          </Card>
        </div>
        <section className="space-y-6">{children}</section>
      </div>
    </main>
  );
}
