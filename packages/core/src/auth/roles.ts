export const USER_ROLES = ['admin', 'operator'] as const;

export type UserRole = (typeof USER_ROLES)[number];

export function canManageTemplates(role: UserRole): boolean {
  return role === 'admin';
}

export function canOperateBooth(role: UserRole): boolean {
  return role === 'admin' || role === 'operator';
}
