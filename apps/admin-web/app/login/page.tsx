import { Badge, Button, Card, Input } from '@photobooth/ui';

import { authenticate } from '@/lib/auth';

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const params = await searchParams;

  return (
    <main className="grid min-h-screen place-items-center px-6 py-12">
      <Card className="w-full max-w-lg space-y-6 border-cyan-400/10 bg-slate-950/70 p-8">
        <div className="space-y-3">
          <Badge className="border-cyan-400/20 bg-cyan-400/10 text-cyan-200">Protected Admin</Badge>
          <h1 className="text-3xl font-semibold text-white">Photobooth dashboard login</h1>
          <p className="text-sm text-slate-400">
            Seed accounts: `admin@photobooth.local / admin1234` and `operator@photobooth.local / operator1234`.
          </p>
        </div>
        <form action={authenticate} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm text-slate-300">Email</label>
            <Input name="email" type="email" defaultValue="admin@photobooth.local" />
          </div>
          <div className="space-y-2">
            <label className="text-sm text-slate-300">Password</label>
            <Input name="password" type="password" defaultValue="admin1234" />
          </div>
          {params.error ? <div className="rounded-2xl border border-rose-400/20 bg-rose-400/10 px-4 py-3 text-sm text-rose-100">Invalid credentials.</div> : null}
          <Button className="w-full" size="lg">Sign In</Button>
        </form>
      </Card>
    </main>
  );
}
