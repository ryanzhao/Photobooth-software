import { BaseCameraProvider, type CameraDeviceDescriptor, type CameraParameterSupport, type CameraStatus, type PreviewSession, type RemoteShutterRequest, type TransferredImage } from '@photobooth/camera-core';

export interface VendorSdkBridge {
  isAvailable(): Promise<boolean>;
  listDevices(): Promise<Array<{ id: string; name: string; model?: string }>>;
  connect(deviceId: string): Promise<void>;
  disconnect(deviceId: string): Promise<void>;
  startLiveView(deviceId: string): Promise<string | null>;
  capture(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]>;
  getStatus(deviceId?: string): Promise<CameraStatus>;
  getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]>;
  setParameter(deviceId: string, parameter: string, value: string): Promise<void>;
}

export class SonyRemoteSdkProvider extends BaseCameraProvider {
  id = 'sony-sdk';
  label = 'Sony Camera Remote SDK';
  transport = 'sdk-bridge' as const;
  priority = 100;

  private activeDeviceId?: string;

  constructor(private readonly bridge: VendorSdkBridge) {
    super();
  }

  async isAvailable(): Promise<boolean> {
    return this.bridge.isAvailable();
  }

  async listDevices(): Promise<CameraDeviceDescriptor[]> {
    const devices = await this.bridge.listDevices();
    return devices.map((device) => ({
      id: device.id,
      name: device.name,
      model: device.model,
      transport: 'sdk-bridge',
      providerId: this.id,
      isConnected: device.id === this.activeDeviceId,
      liveViewSupported: true,
      remoteTriggerSupported: true,
      transferSupported: true,
      diagnostics: ['Sony Camera Remote SDK bridge path.']
    }));
  }

  async connect(deviceId: string): Promise<CameraStatus> {
    await this.bridge.connect(deviceId);
    this.activeDeviceId = deviceId;
    const status = await this.bridge.getStatus(deviceId);
    this.emit('connectionChange', status);
    return status;
  }

  async disconnect(deviceId: string): Promise<void> {
    await this.bridge.disconnect(deviceId);
    if (this.activeDeviceId === deviceId) {
      this.activeDeviceId = undefined;
    }
  }

  async getStatus(deviceId?: string): Promise<CameraStatus> {
    return this.bridge.getStatus(deviceId ?? this.activeDeviceId);
  }

  async startLiveView(deviceId: string): Promise<PreviewSession> {
    const imageUrl = await this.bridge.startLiveView(deviceId);
    return imageUrl ? { type: 'image-url', imageUrl } : { type: 'none' };
  }

  async stopLiveView(): Promise<void> {
    return;
  }

  async triggerRemoteShutter(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]> {
    const images = await this.bridge.capture(deviceId, request);
    images.forEach((image) => this.emit('imageTransferred', image));
    return images;
  }

  async setParameter(deviceId: string, parameter: string, value: string): Promise<void> {
    await this.bridge.setParameter(deviceId, parameter, value);
  }

  async getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]> {
    return this.bridge.getSupportedParameters(deviceId);
  }
}
