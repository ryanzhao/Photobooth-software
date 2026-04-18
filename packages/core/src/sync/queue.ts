import type { UploadJob } from '../domain';

export function getNextRetryDelayMs(attempts: number): number {
  return Math.min(60000, Math.max(5000, 2000 * 2 ** attempts));
}

export function sortUploadJobs(jobs: UploadJob[]): UploadJob[] {
  return [...jobs].sort((left, right) => {
    if (left.state !== right.state) {
      return left.state === 'failed' ? -1 : 1;
    }
    return Date.parse(left.createdAt) - Date.parse(right.createdAt);
  });
}
