import { seedCredentials, seedUsers } from '@photobooth/db';
import { SignJWT, jwtVerify } from 'jose';
import type { Context } from 'hono';

const encoder = new TextEncoder();

function getSecret(secret?: string) {
  return encoder.encode(secret ?? 'photobooth-local-secret');
}

export async function issueToken(email: string, secret?: string): Promise<string> {
  const user = seedUsers.find((entry) => entry.email === email);
  if (!user) {
    throw new Error('Unknown user');
  }

  return new SignJWT({
    userId: user.id,
    email: user.email,
    role: user.role,
    name: user.name
  })
    .setProtectedHeader({ alg: 'HS256' })
    .setIssuedAt()
    .setExpirationTime('12h')
    .sign(getSecret(secret));
}

export async function verifyToken(token: string, secret?: string) {
  const verification = await jwtVerify(token, getSecret(secret));
  return verification.payload as {
    userId: string;
    email: string;
    role: 'admin' | 'operator';
    name: string;
  };
}

export function validateCredentials(email: string, password: string): boolean {
  const credential = seedCredentials[email as keyof typeof seedCredentials];
  return Boolean(credential && credential.password === password);
}

export async function requireBearerAuth(c: Context, next: () => Promise<void>) {
  const header = c.req.header('authorization');
  if (!header?.startsWith('Bearer ')) {
    return c.json({ error: 'Unauthorized' }, 401);
  }

  try {
    const payload = await verifyToken(header.slice(7), c.env.JWT_SECRET);
    await next();
  } catch {
    return c.json({ error: 'Unauthorized' }, 401);
  }
}

