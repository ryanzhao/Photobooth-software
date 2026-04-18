import { Camera, CheckCircle2, CloudUpload, ImagePlus, Printer, RefreshCw, Settings2, Sparkles, Usb, Wand2 } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';

import { Badge, Button, Card, Input, Select, Stat, cn } from '@photobooth/ui';

import { useBoothStore } from './store/useBoothStore';

function statusClass(state: string): string {
  switch (state) {
    case 'previewing':
    case 'connected':
    case 'synced':
    case 'printed':
      return 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200';
    case 'failed':
    case 'error':
      return 'border-rose-400/30 bg-rose-400/10 text-rose-200';
    case 'pending':
    case 'triggering':
    case 'transferring':
    case 'counting-down':
      return 'border-amber-400/30 bg-amber-400/10 text-amber-200';
    default:
      return 'border-white/10 bg-white/5 text-slate-300';
  }
}

export default function App() {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [selectedPhotoId, setSelectedPhotoId] = useState<string | undefined>();
  const [liveViewTick, setLiveViewTick] = useState(0);

  const {
    bootstrapped,
    providers,
    devices,
    selectedProviderId,
    selectedDeviceId,
    cameraStatus,
    preview,
    supportedParameters,
    captureDefaults,
    currentSession,
    photos,
    templates,
    selectedTemplateId,
    renderedPreviewUrl,
    renderedOutput,
    recentSessions,
    uploadJobs,
    printJobs,
    activeStep,
    capturePhase,
    countdownRemaining,
    error,
    bootstrap,
    refreshDevices,
    connectCamera,
    startPreview,
    stopPreview,
    setCaptureDefaults,
    capture,
    updatePhotoAdjustment,
    selectTemplate,
    renderOutput,
    openPrintPreview,
    queueSync,
    flushSync,
    resetSession
  } = useBoothStore();

  const selectedPhoto = useMemo(
    () => photos.find((photo) => photo.id === selectedPhotoId) ?? photos[0],
    [photos, selectedPhotoId]
  );

  useEffect(() => {
    if (!bootstrapped) {
      void bootstrap();
    }
  }, [bootstrapped, bootstrap]);

  useEffect(() => {
    if (!selectedPhotoId && photos[0]) {
      setSelectedPhotoId(photos[0].id);
    }
  }, [photos, selectedPhotoId]);

  useEffect(() => {
    if (preview?.type === 'media-stream' && videoRef.current) {
      videoRef.current.srcObject = preview.mediaStream ?? null;
      void videoRef.current.play().catch(() => undefined);
    }
  }, [preview]);

  useEffect(() => {
    if (preview?.type !== 'image-url') {
      return;
    }
    const timer = window.setInterval(() => setLiveViewTick((tick) => tick + 1), 800);
    return () => window.clearInterval(timer);
  }, [preview]);

  const liveViewImageUrl = preview?.type === 'image-url' && preview.imageUrl ? `${preview.imageUrl}?t=${liveViewTick}` : undefined;

  return (
    <main className="min-h-screen px-6 py-6 text-white">
      <div className="mx-auto grid max-w-[1900px] gap-6 xl:grid-cols-[1.45fr_0.9fr]">
        <section className="space-y-6">
          <Card className="overflow-hidden border-cyan-400/10 bg-slate-950/55 p-0">
            <div className="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 px-6 py-5">
              <div>
                <div className="flex items-center gap-3">
                  <Badge className={cn('border-cyan-400/25 bg-cyan-400/10 text-cyan-200')}>Booth Desktop</Badge>
                  <Badge className={statusClass(cameraStatus.state)}>{cameraStatus.state}</Badge>
                  <Badge className={statusClass(capturePhase)}>{capturePhase}</Badge>
                </div>
                <h1 className="mt-3 text-3xl font-semibold tracking-tight">Local-first Photobooth Operator Console</h1>
                <p className="mt-2 max-w-3xl text-sm text-slate-400">
                  Wired tethered capture is handled through dedicated providers. Webcam mode remains available as a fallback, but it is explicitly separated from USB remote-trigger workflows.
                </p>
              </div>
              <div className="flex gap-3">
                <Button variant="secondary" onClick={() => void refreshDevices()}>
                  <RefreshCw className="mr-2 h-4 w-4" /> Refresh Devices
                </Button>
                {preview ? (
                  <Button variant="secondary" onClick={() => void stopPreview()}>
                    Stop Preview
                  </Button>
                ) : (
                  <Button onClick={() => void startPreview()}>
                    <Camera className="mr-2 h-4 w-4" /> Start Preview
                  </Button>
                )}
              </div>
            </div>

            <div className="grid gap-6 p-6 lg:grid-cols-[1.3fr_0.85fr]">
              <div className="space-y-4">
                <div className="preview-grid relative flex aspect-[16/10] items-center justify-center overflow-hidden rounded-[28px] border border-white/10 bg-slate-950">
                  {preview?.type === 'media-stream' ? (
                    <video ref={videoRef} className={cn("h-full w-full object-cover", preview.mirrored ? "-scale-x-100" : "")} muted playsInline autoPlay />
                  ) : liveViewImageUrl ? (
                    <img src={liveViewImageUrl} alt="Tethered live view" className="h-full w-full object-cover" />
                  ) : selectedPhoto?.previewUrl ? (
                    <img src={selectedPhoto.previewUrl} alt="Latest capture" className="h-full w-full object-cover" />
                  ) : (
                    <div className="space-y-4 text-center text-slate-400">
                      <Usb className="mx-auto h-16 w-16 text-cyan-300/70" />
                      <div className="text-lg font-medium text-white">No live preview yet</div>
                      <p className="max-w-md text-sm text-slate-500">
                        Connect a tethered provider or webcam, then start preview. digiCamControl live view uses its local streaming endpoint when enabled.
                      </p>
                    </div>
                  )}
                  {countdownRemaining > 0 ? (
                    <div className="absolute inset-0 grid place-items-center bg-slate-950/55 backdrop-blur-sm">
                      <div className="rounded-full border border-cyan-300/30 bg-cyan-400/10 px-10 py-7 text-7xl font-semibold text-cyan-100">
                        {countdownRemaining}
                      </div>
                    </div>
                  ) : null}
                </div>

                <div className="grid gap-4 md:grid-cols-4">
                  <Stat label="Mode" value={captureDefaults.captureMode.toUpperCase()} detail="Single, multi-shot, or burst" icon={<Sparkles className="h-4 w-4" />} />
                  <Stat label="Shots" value={String(captureDefaults.shotCount)} detail="Frames per session" icon={<ImagePlus className="h-4 w-4" />} />
                  <Stat label="Countdown" value={`${captureDefaults.countdownSeconds}s`} detail="Before remote trigger" icon={<Camera className="h-4 w-4" />} />
                  <Stat label="Sync Queue" value={String(uploadJobs.filter((job) => job.state !== 'synced').length)} detail="Pending or failed uploads" icon={<CloudUpload className="h-4 w-4" />} />
                </div>
              </div>

              <div className="space-y-4">
                <Card className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Connected Providers</div>
                      <div className="mt-1 text-lg font-semibold text-white">Select transport and device</div>
                    </div>
                    <Badge>{providers.length} providers</Badge>
                  </div>
                  <div className="space-y-3">
                    {devices.map((device) => (
                      <button
                        key={`${device.providerId}-${device.id}`}
                        className={cn(
                          'w-full rounded-3xl border p-4 text-left transition',
                          selectedDeviceId === device.id ? 'border-cyan-400/40 bg-cyan-400/10' : 'border-white/10 bg-white/5 hover:bg-white/10'
                        )}
                        onClick={() => void connectCamera(device.providerId, device.id)}
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div>
                            <div className="font-medium text-white">{device.name}</div>
                            <div className="mt-1 text-sm text-slate-400">{device.model ?? 'Unknown model'} · {device.transport}</div>
                          </div>
                          <div className="flex gap-2">
                            <Badge className={device.remoteTriggerSupported ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-200' : ''}>Trigger {device.remoteTriggerSupported ? 'Yes' : 'No'}</Badge>
                            <Badge className={device.transferSupported ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-200' : ''}>Transfer {device.transferSupported ? 'Yes' : 'No'}</Badge>
                          </div>
                        </div>
                        <div className="mt-3 text-xs text-slate-400">{device.diagnostics.join(' · ')}</div>
                      </button>
                    ))}
                    {devices.length === 0 ? <div className="text-sm text-slate-500">No providers are currently available. Enable digiCamControl or use a webcam for local review.</div> : null}
                  </div>
                </Card>

                <Card className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Capture Setup</div>
                      <div className="mt-1 text-lg font-semibold text-white">Session defaults</div>
                    </div>
                    <Badge>{activeStep}</Badge>
                  </div>
                  <div className="grid gap-3 md:grid-cols-2">
                    <div className="space-y-2">
                      <label className="text-xs uppercase tracking-[0.24em] text-slate-400">Capture mode</label>
                      <Select value={captureDefaults.captureMode} onChange={(event) => setCaptureDefaults({ captureMode: event.target.value as typeof captureDefaults.captureMode })}>
                        <option value="single">Single Shot</option>
                        <option value="multi">Multi Shot</option>
                        <option value="burst">Burst</option>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <label className="text-xs uppercase tracking-[0.24em] text-slate-400">Shots</label>
                      <Input type="number" min={1} max={6} value={captureDefaults.shotCount} onChange={(event) => setCaptureDefaults({ shotCount: Number(event.target.value) || 1 })} />
                    </div>
                    <div className="space-y-2">
                      <label className="text-xs uppercase tracking-[0.24em] text-slate-400">Countdown</label>
                      <Input type="number" min={0} max={20} value={captureDefaults.countdownSeconds} onChange={(event) => setCaptureDefaults({ countdownSeconds: Number(event.target.value) || 0 })} />
                    </div>
                    <div className="space-y-2">
                      <label className="text-xs uppercase tracking-[0.24em] text-slate-400">Template</label>
                      <Select value={selectedTemplateId} onChange={(event) => selectTemplate(event.target.value)}>
                        {templates.map((template) => (
                          <option key={template.id} value={template.id}>{template.name}</option>
                        ))}
                      </Select>
                    </div>
                  </div>
                  <div className="grid gap-3 md:grid-cols-2">
                    <Button size="lg" onClick={() => void capture(videoRef.current)}>
                      <Camera className="mr-2 h-5 w-5" /> Trigger Capture
                    </Button>
                    <Button size="lg" variant="secondary" onClick={() => void resetSession()}>
                      Reset Session
                    </Button>
                  </div>
                </Card>
              </div>
            </div>
          </Card>

          <div className="grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
            <Card className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Captured Photos</div>
                  <div className="mt-1 text-lg font-semibold">Edit and review</div>
                </div>
                <Badge>{photos.length} frames</Badge>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                {photos.map((photo, index) => (
                  <button
                    key={photo.id}
                    className={cn(
                      'overflow-hidden rounded-3xl border text-left transition',
                      selectedPhoto?.id === photo.id ? 'border-cyan-400/40' : 'border-white/10'
                    )}
                    onClick={() => setSelectedPhotoId(photo.id)}
                  >
                    <div className="aspect-square bg-slate-900">
                      {photo.previewUrl ? <img src={photo.previewUrl} alt={`Capture ${index + 1}`} className="h-full w-full object-cover" /> : null}
                    </div>
                    <div className="flex items-center justify-between px-4 py-3 text-sm">
                      <span>Capture {index + 1}</span>
                      <Badge className={statusClass(photo.syncState)}>{photo.syncState}</Badge>
                    </div>
                  </button>
                ))}
                {photos.length === 0 ? <div className="rounded-3xl border border-dashed border-white/10 p-6 text-sm text-slate-500">Captured photos will appear here after transfer/import.</div> : null}
              </div>
            </Card>

            <Card className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Edit Controls</div>
                  <div className="mt-1 text-lg font-semibold">Non-destructive adjustments</div>
                </div>
                <Button variant="secondary" onClick={() => void renderOutput()}>
                  <Wand2 className="mr-2 h-4 w-4" /> Render Output
                </Button>
              </div>
              {selectedPhoto ? (
                <div className="space-y-4">
                  <SliderRow label="Brightness" value={selectedPhoto.adjustments.brightness} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { brightness: value })} />
                  <SliderRow label="Contrast" value={selectedPhoto.adjustments.contrast} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { contrast: value })} />
                  <SliderRow label="Saturation" value={selectedPhoto.adjustments.saturation} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { saturation: value })} />
                  <SliderRow label="Warmth" value={selectedPhoto.adjustments.warmth} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { warmth: value })} />
                  <SliderRow label="Sharpen" value={selectedPhoto.adjustments.sharpen} min={0} max={1} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { sharpen: value })} />
                  <SliderRow label="Beauty" value={selectedPhoto.adjustments.beauty} min={0} max={1} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { beauty: value })} />
                  <SliderRow label="Blemish Softening" value={selectedPhoto.adjustments.blemishSoftening} min={0} max={1} onChange={(value) => void updatePhotoAdjustment(selectedPhoto.id, { blemishSoftening: value })} />
                </div>
              ) : (
                <div className="text-sm text-slate-500">Capture a session first to unlock editing controls.</div>
              )}
            </Card>
          </div>
        </section>

        <aside className="space-y-6">
          <Card className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Template Output</div>
                <div className="mt-1 text-lg font-semibold">Render, print, sync</div>
              </div>
              <Badge className={renderedOutput ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-200' : ''}>{renderedOutput ? 'Ready' : 'Draft'}</Badge>
            </div>
            <div className="aspect-[4/3] overflow-hidden rounded-[28px] border border-white/10 bg-slate-950/80">
              {renderedPreviewUrl ? (
                <img src={renderedPreviewUrl} alt="Rendered layout" className="h-full w-full object-cover" />
              ) : (
                <div className="grid h-full place-items-center text-sm text-slate-500">Rendered output preview appears here.</div>
              )}
            </div>
            <div className="grid gap-3 md:grid-cols-2">
              <Button onClick={() => void openPrintPreview()} disabled={!renderedOutput}>
                <Printer className="mr-2 h-4 w-4" /> Print Preview
              </Button>
              <Button variant="secondary" onClick={() => void queueSync()} disabled={!currentSession}>
                <CloudUpload className="mr-2 h-4 w-4" /> Queue Sync
              </Button>
            </div>
          </Card>

          <Card className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Diagnostics</div>
                <div className="mt-1 text-lg font-semibold">Provider status and parameter support</div>
              </div>
              <Settings2 className="h-5 w-5 text-slate-400" />
            </div>
            <div className="space-y-3 text-sm text-slate-300">
              {cameraStatus.diagnostics.map((diagnostic) => (
                <div key={diagnostic} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3">{diagnostic}</div>
              ))}
              {supportedParameters.map((parameter) => (
                <div key={parameter.name} className="flex items-start justify-between gap-3 rounded-2xl border border-white/10 px-4 py-3">
                  <div>
                    <div className="font-medium text-white">{parameter.label}</div>
                    <div className="mt-1 text-xs text-slate-400">{parameter.supported ? (parameter.values?.slice(0, 3).join(', ') || 'Supported') : parameter.reasonIfUnsupported}</div>
                  </div>
                  <Badge className={parameter.supported ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-200' : ''}>{parameter.supported ? 'Supported' : 'Unavailable'}</Badge>
                </div>
              ))}
            </div>
          </Card>

          <Card className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Sync and Print History</div>
                <div className="mt-1 text-lg font-semibold">Offline-first queues</div>
              </div>
              <Button variant="ghost" onClick={() => void flushSync()}>
                Retry Sync
              </Button>
            </div>
            <div className="space-y-3">
              {uploadJobs.slice(0, 4).map((job) => (
                <div key={job.id} className="rounded-2xl border border-white/10 px-4 py-3 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="text-white">{job.entityType} #{job.entityId.slice(-8)}</span>
                    <Badge className={statusClass(job.state)}>{job.state}</Badge>
                  </div>
                  {job.errorMessage ? <div className="mt-2 text-xs text-rose-200">{job.errorMessage}</div> : null}
                </div>
              ))}
              {printJobs.slice(0, 3).map((job) => (
                <div key={job.id} className="rounded-2xl border border-white/10 px-4 py-3 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="text-white">Printer job #{job.id.slice(-6)}</span>
                    <Badge className={statusClass(job.state)}>{job.state}</Badge>
                  </div>
                  <div className="mt-2 text-xs text-slate-400">{job.printerName ?? 'System Print Dialog'} · {job.copies} copies</div>
                </div>
              ))}
            </div>
          </Card>

          <Card className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs uppercase tracking-[0.24em] text-slate-400">Recent Sessions</div>
                <div className="mt-1 text-lg font-semibold">Local history</div>
              </div>
              <CheckCircle2 className="h-5 w-5 text-emerald-300" />
            </div>
            <div className="space-y-3">
              {recentSessions.slice(0, 5).map((session) => (
                <div key={session.id} className="rounded-2xl border border-white/10 px-4 py-3 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="text-white">{session.id.slice(-8)}</span>
                    <Badge className={statusClass(session.syncState)}>{session.status}</Badge>
                  </div>
                  <div className="mt-2 text-xs text-slate-400">{session.captureMode} · {session.shotCount} shots · {session.folderPath}</div>
                </div>
              ))}
              {recentSessions.length === 0 ? <div className="text-sm text-slate-500">Session history is stored locally and will appear after the first capture.</div> : null}
            </div>
          </Card>

          {error ? <Card className="border-rose-400/20 bg-rose-400/10 text-sm text-rose-100">{error}</Card> : null}
        </aside>
      </div>
    </main>
  );
}

function SliderRow({ label, value, min = -1, max = 1, onChange }: { label: string; value: number; min?: number; max?: number; onChange: (value: number) => void }) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between text-sm text-slate-300">
        <span>{label}</span>
        <span>{value.toFixed(2)}</span>
      </div>
      <input
        type="range"
        min={min}
        max={max}
        step={0.02}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="h-2 w-full cursor-pointer appearance-none rounded-full bg-white/10 accent-cyan-400"
      />
    </div>
  );
}

