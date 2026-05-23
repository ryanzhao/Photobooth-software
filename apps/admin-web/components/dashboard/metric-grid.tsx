import { Camera, CloudUpload, FileImage, Printer } from 'lucide-react';

import { Stat } from '@photobooth/ui';
import type { DashboardSnapshot } from '@photobooth/core';

export function MetricGrid({ snapshot }: { snapshot: DashboardSnapshot }) {
  const pendingUploads = snapshot.uploadJobs.filter((job) => job.state !== 'synced').length;
  const totalPrints = snapshot.printJobs.reduce((count, job) => count + job.copies, 0);

  return (
    <div className="grid gap-4 md:grid-cols-4">
      <Stat label="Sessions" value={String(snapshot.sessions.length)} detail="Local + cloud visible sessions" icon={<Camera className="h-4 w-4" />} />
      <Stat label="Photos" value={String(snapshot.sessionPhotos.length)} detail="Imported and editable photos" icon={<FileImage className="h-4 w-4" />} />
      <Stat label="Prints" value={String(totalPrints)} detail="Successful print copies" icon={<Printer className="h-4 w-4" />} />
      <Stat label="Upload Queue" value={String(pendingUploads)} detail="Pending or failed sync items" icon={<CloudUpload className="h-4 w-4" />} />
    </div>
  );
}
