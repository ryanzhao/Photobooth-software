import type { CameraTransport } from '@photobooth/core';

export interface CameraParameterSupport {
  name: string;
  label: string;
  supported: boolean;
  values?: string[];
  unit?: string;
  reasonIfUnsupported?: string;
}

export interface CameraDeviceDescriptor {
  id: string;
  name: string;
  model?: string;
  transport: CameraTransport;
  providerId: string;
  isConnected: boolean;
  liveViewSupported: boolean;
  remoteTriggerSupported: boolean;
  transferSupported: boolean;
  diagnostics: string[];
}

export interface CameraStatus {
  state:
    | 'idle'
    | 'discovering'
    | 'connecting'
    | 'connected'
    | 'previewing'
    | 'triggering'
    | 'transferring'
    | 'error';
  connectedDeviceId?: string;
  lastError?: string;
  lastImportPath?: string;
  diagnostics: string[];
}

export interface PreviewSession {
  type: 'media-stream' | 'image-url' | 'none';
  mediaStream?: MediaStream;
  imageUrl?: string;
  mirrored?: boolean;
}

export interface CapturePhotoRequest {
  countdownSeconds?: number;
  fileNamePrefix: string;
  sessionFolder: string;
  previewElement?: HTMLVideoElement | null;
}

export interface RemoteShutterRequest {
  countdownSeconds?: number;
  fileNamePrefix: string;
  sessionFolder: string;
  burstCount?: number;
}

export interface TransferredImage {
  localPath: string;
  previewUrl?: string;
  fileName: string;
  metadata?: Record<string, string | number | boolean | null>;
}

export interface CameraProviderEvents {
  connectionChange: (status: CameraStatus) => void;
  imageTransferred: (image: TransferredImage) => void;
  error: (message: string) => void;
}

export interface CameraProvider {
  id: string;
  label: string;
  transport: CameraTransport;
  priority: number;
  isAvailable(): Promise<boolean>;
  listDevices(): Promise<CameraDeviceDescriptor[]>;
  connect(deviceId: string): Promise<CameraStatus>;
  disconnect(deviceId: string): Promise<void>;
  getStatus(deviceId?: string): Promise<CameraStatus>;
  startLiveView(deviceId: string): Promise<PreviewSession>;
  stopLiveView(deviceId: string): Promise<void>;
  capturePhoto?(deviceId: string, request: CapturePhotoRequest): Promise<TransferredImage>;
  triggerRemoteShutter?(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]>;
  setParameter?(deviceId: string, parameter: string, value: string): Promise<void>;
  getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]>;
  subscribe<K extends keyof CameraProviderEvents>(
    event: K,
    callback: CameraProviderEvents[K]
  ): () => void;
}

export abstract class BaseCameraProvider implements CameraProvider {
  abstract id: string;
  abstract label: string;
  abstract transport: CameraTransport;
  abstract priority: number;

  private listeners: {
    [K in keyof CameraProviderEvents]: Set<CameraProviderEvents[K]>;
  } = {
    connectionChange: new Set(),
    imageTransferred: new Set(),
    error: new Set()
  };

  abstract isAvailable(): Promise<boolean>;
  abstract listDevices(): Promise<CameraDeviceDescriptor[]>;
  abstract connect(deviceId: string): Promise<CameraStatus>;
  abstract disconnect(deviceId: string): Promise<void>;
  abstract getStatus(deviceId?: string): Promise<CameraStatus>;
  abstract startLiveView(deviceId: string): Promise<PreviewSession>;
  abstract stopLiveView(deviceId: string): Promise<void>;
  abstract getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]>;

  subscribe<K extends keyof CameraProviderEvents>(
    event: K,
    callback: CameraProviderEvents[K]
  ): () => void {
    this.listeners[event].add(callback);
    return () => this.listeners[event].delete(callback);
  }

  protected emit<K extends keyof CameraProviderEvents>(
    event: K,
    payload: Parameters<CameraProviderEvents[K]>[0]
  ): void {
    this.listeners[event].forEach((listener) => {
      listener(payload as never);
    });
  }
}
