import { BaseCameraProvider, type CameraDeviceDescriptor, type CameraParameterSupport, type CameraStatus, type PreviewSession, type RemoteShutterRequest, type TransferredImage } from '@photobooth/camera-core';

export interface DigicamControlDevice {
  id: string;
  name: string;
  model?: string;
  diagnostics?: string[];
}

export interface DigicamControlBridge {
  isAvailable(): Promise<boolean>;
  listDevices(): Promise<DigicamControlDevice[]>;
  connect(deviceId: string): Promise<void>;
  disconnect(deviceId: string): Promise<void>;
  getStatus(deviceId?: string): Promise<CameraStatus>;
  captureToSession(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]>;
  getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]>;
  setParameter(deviceId: string, parameter: string, value: string): Promise<void>;
  getLiveViewUrl?(deviceId: string): Promise<string | null>;
}

export class DigicamControlProvider extends BaseCameraProvider {
  id = 'digicamcontrol';
  label = 'Windows Tethered (digiCamControl)';
  transport = 'tethered' as const;
  priority = 90;

  private activeDeviceId?: string;
  private status: CameraStatus = {
    state: 'idle',
    diagnostics: ['Waiting for the digiCamControl command bridge.']
  };

  constructor(private readonly bridge: DigicamControlBridge) {
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
      transport: 'tethered',
      providerId: this.id,
      isConnected: device.id === this.activeDeviceId,
      liveViewSupported: true,
      remoteTriggerSupported: true,
      transferSupported: true,
      diagnostics: device.diagnostics ?? ['Remote trigger and file transfer route through digiCamControl.']
    }));
  }

  async connect(deviceId: string): Promise<CameraStatus> {
    this.status = {
      state: 'connecting',
      connectedDeviceId: deviceId,
      diagnostics: ['Opening digiCamControl session.']
    };
    this.emit('connectionChange', this.status);
    await this.bridge.connect(deviceId);
    this.activeDeviceId = deviceId;
    this.status = await this.bridge.getStatus(deviceId);
    this.emit('connectionChange', this.status);
    return this.status;
  }

  async disconnect(deviceId: string): Promise<void> {
    await this.bridge.disconnect(deviceId);
    if (this.activeDeviceId === deviceId) {
      this.activeDeviceId = undefined;
    }
    this.status = {
      state: 'idle',
      diagnostics: ['Disconnected from digiCamControl camera.']
    };
    this.emit('connectionChange', this.status);
  }

  async getStatus(deviceId?: string): Promise<CameraStatus> {
    this.status = await this.bridge.getStatus(deviceId ?? this.activeDeviceId);
    return this.status;
  }

  async startLiveView(deviceId: string): Promise<PreviewSession> {
    const imageUrl = (await this.bridge.getLiveViewUrl?.(deviceId)) ?? null;
    this.status = {
      state: imageUrl ? 'previewing' : 'connected',
      connectedDeviceId: deviceId,
      diagnostics: imageUrl
        ? ['Live view URL supplied by digiCamControl bridge.']
        : ['Live view is available when the bridge exposes a preview endpoint.']
    };
    this.emit('connectionChange', this.status);
    return imageUrl ? { type: 'image-url', imageUrl } : { type: 'none' };
  }

  async stopLiveView(): Promise<void> {
    this.status = {
      state: 'connected',
      connectedDeviceId: this.activeDeviceId,
      diagnostics: ['Live view stopped.']
    };
    this.emit('connectionChange', this.status);
  }

  async triggerRemoteShutter(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]> {
    this.status = {
      state: 'triggering',
      connectedDeviceId: deviceId,
      diagnostics: ['Sending remote shutter command over USB tethering.']
    };
    this.emit('connectionChange', this.status);
    const transferred = await this.bridge.captureToSession(deviceId, request);
    transferred.forEach((image) => this.emit('imageTransferred', image));
    this.status = {
      state: 'transferring',
      connectedDeviceId: deviceId,
      lastImportPath: transferred.at(-1)?.localPath,
      diagnostics: ['Image transfer completed through the tethered provider.']
    };
    this.emit('connectionChange', this.status);
    return transferred;
  }

  async setParameter(deviceId: string, parameter: string, value: string): Promise<void> {
    await this.bridge.setParameter(deviceId, parameter, value);
  }

  async getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]> {
    return this.bridge.getSupportedParameters(deviceId);
  }
}
