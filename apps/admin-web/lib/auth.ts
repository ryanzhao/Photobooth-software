import { seedCredentials, seedUsers } from '@photobooth/db';
import { SignJWT, jwtVerify } from 'jose';
import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';

const COOKIE_NAME = 'photobooth-admin-session';
const secret = new TextEncoder().encode(process.env.JWT_SECRET ?? 'photobooth-local-secret');

export async function createSession(email: string): Promise<void> {
  const user = seedUsers.find((entry) => entry.email === email);
  if (!user) {
    throw new Error('Unknown user');
  }

  const token = await new SignJWT({ role: user.role, email: user.email, name: user.name, userId: user.id })
    .setProtectedHeader({ alg: 'HS256' })
    .setIssuedAt()
    .setExpirationTime('7d')
    .sign(secret);

  const cookieStore = await cookies();
  cookieStore.set(COOKIE_NAME, token, {
    httpOnly: true,
    sameSite: 'lax',
    secure: false,
    path: '/'
  });
}

export async function destroySession(): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.delete(COOKIE_NAME);
}

export async function getCurrentUser() {
  const cookieStore = await cookies();
  const token = cookieStore.get(COOKIE_NAME)?.value;
  if (!token) {
    return null;
  }

  try {
    const verification = await jwtVerify(token, secret);
    return verification.payload as {
      role: 'admin' | 'operator';
      email: string;
      name: string;
      userId: string;
    };
  } catch {
    return null;
  }
}

export async function requireUser() {
  const user = await getCurrentUser();
  if (!user) {
    redirect('/login');
  }
  return user;
}

export async function authenticate(formData: FormData) {
  'use server';

  const email = String(formData.get('email') ?? '');
  const password = String(formData.get('password') ?? '');
  const credential = seedCredentials[email as keyof typeof seedCredentials];

  if (!credential || credential.password !== password) {
    redirect('/login?error=1');
  }

  await createSession(email);
  redirect('/analytics');
}

export async function signOut() {
  'use server';

  await destroySession();
  redirect('/login');
}
