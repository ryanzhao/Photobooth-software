import { BaseCameraProvider, type CameraDeviceDescriptor, type CameraParameterSupport, type CameraStatus, type PreviewSession } from '@photobooth/camera-core';
import type { CameraTransport } from '@photobooth/core';

export function createScaffoldedProvider(config: {
  id: string;
  label: string;
  transport?: CameraTransport;
  diagnostics: string[];
}): BaseCameraProvider {
  return new (class extends BaseCameraProvider {
    id = config.id;
    label = config.label;
    transport = config.transport ?? ('scaffolded' as const);
    priority = 10;

    async isAvailable(): Promise<boolean> {
      return true;
    }

    async listDevices(): Promise<CameraDeviceDescriptor[]> {
      return [
        {
          id: `${config.id}-placeholder`,
          name: `${config.label} Placeholder`,
          model: 'Provider scaffold',
          transport: this.transport,
          providerId: this.id,
          isConnected: false,
          liveViewSupported: false,
          remoteTriggerSupported: false,
          transferSupported: false,
          diagnostics: config.diagnostics
        }
      ];
    }

    async connect(): Promise<CameraStatus> {
      const status: CameraStatus = {
        state: 'error',
        diagnostics: config.diagnostics,
        lastError: `${config.label} requires vendor-specific integration work before use.`
      };
      this.emit('error', status.lastError);
      return status;
    }

    async disconnect(): Promise<void> {
      return;
    }

    async getStatus(): Promise<CameraStatus> {
      return {
        state: 'idle',
        diagnostics: config.diagnostics
      };
    }

    async startLiveView(): Promise<PreviewSession> {
      return { type: 'none' };
    }

    async stopLiveView(): Promise<void> {
      return;
    }

    async getSupportedParameters(): Promise<CameraParameterSupport[]> {
      return [
        {
          name: 'vendor-sdk',
          label: 'Vendor SDK Required',
          supported: false,
          reasonIfUnsupported: config.diagnostics.join(' ')
        }
      ];
    }
  })();
}
