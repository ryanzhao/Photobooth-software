import type { CameraParameterSupport, CameraStatus, RemoteShutterRequest, TransferredImage } from '@photobooth/camera-core';
import type { DigicamControlBridge, DigicamControlDevice } from '@photobooth/camera-adapters';

import { sleep } from '@/lib/runtime';

const baseUrl = import.meta.env.VITE_DIGICAMCONTROL_BASE_URL ?? 'http://127.0.0.1:5513';
const knownParameters = ['shutterspeed', 'iso', 'exposurecompensation', 'aperture', 'focusmode', 'whitebalance'];

async function callSingleCommand(action: 'get' | 'set' | 'list' | 'capture', param1 = '', param2 = ''): Promise<string> {
  const url = new URL(`${baseUrl}/`);
  url.searchParams.set('slc', action);
  url.searchParams.set('param1', param1);
  url.searchParams.set('param2', param2);
  const response = await fetch(url.toString());
  if (!response.ok) {
    throw new Error(`digiCamControl request failed with status ${response.status}`);
  }
  return (await response.text()).trim();
}

async function callCommandWindow(command: string): Promise<void> {
  const url = new URL(`${baseUrl}/`);
  url.searchParams.set('CMD', command);
  const response = await fetch(url.toString());
  if (!response.ok) {
    throw new Error(`digiCamControl command failed with status ${response.status}`);
  }
}

function parseList(value: string): string[] {
  return value
    .split(/[\r\n,]+/)
    .map((item) => item.trim())
    .filter(Boolean);
}

export class DigicamControlHttpBridge implements DigicamControlBridge {
  async isAvailable(): Promise<boolean> {
    try {
      await callSingleCommand('list', 'cameras');
      return true;
    } catch {
      return false;
    }
  }

  async listDevices(): Promise<DigicamControlDevice[]> {
    const devices = parseList(await callSingleCommand('list', 'cameras'));
    return devices.map((name) => ({
      id: name,
      name,
      model: 'digiCamControl',
      diagnostics: ['Detected through the digiCamControl local command server.']
    }));
  }

  async connect(deviceId: string): Promise<void> {
    await callSingleCommand('set', 'camera', deviceId);
  }

  async disconnect(): Promise<void> {
    return;
  }

  async getStatus(deviceId?: string): Promise<CameraStatus> {
    return {
      state: deviceId ? 'connected' : 'idle',
      connectedDeviceId: deviceId,
      diagnostics: ['digiCamControl bridge reachable.', 'Enable its local web server for live view and capture commands.']
    };
  }

  async captureToSession(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]> {
    await this.connect(deviceId);
    await callSingleCommand('set', 'session.folder', request.sessionFolder);
    const images: TransferredImage[] = [];
    const burstCount = request.burstCount ?? 1;

    for (let index = 0; index < burstCount; index += 1) {
      const fileNamePrefix = burstCount > 1 ? `${request.fileNamePrefix}-${index + 1}` : request.fileNamePrefix;
      await callSingleCommand('set', 'session.filenametemplate', fileNamePrefix);
      await callSingleCommand('capture');

      let fileName = '';
      for (let attempt = 0; attempt < 20; attempt += 1) {
        await sleep(500);
        fileName = await callSingleCommand('get', 'lastcaptured');
        if (fileName) {
          break;
        }
      }

      if (!fileName) {
        throw new Error('Timed out waiting for digiCamControl to report the imported image.');
      }

      images.push({
        fileName,
        localPath: `${request.sessionFolder}/${fileName}`.replace(/\\/g, '/'),
        previewUrl: `${baseUrl}/image/${encodeURIComponent(fileName)}`,
        metadata: {
          provider: 'digicamcontrol',
          transport: 'usb-tethered'
        }
      });
    }

    return images;
  }

  async getSupportedParameters(): Promise<CameraParameterSupport[]> {
    const supports = await Promise.all(
      knownParameters.map(async (parameter) => {
        try {
          const values = parseList(await callSingleCommand('list', parameter));
          return {
            name: parameter,
            label: parameter,
            supported: values.length > 0,
            values,
            reasonIfUnsupported: values.length === 0 ? 'No values reported by digiCamControl for the selected camera.' : undefined
          } satisfies CameraParameterSupport;
        } catch {
          return {
            name: parameter,
            label: parameter,
            supported: false,
            reasonIfUnsupported: 'This parameter is not exposed by the active camera through digiCamControl.'
          } satisfies CameraParameterSupport;
        }
      })
    );

    return supports;
  }

  async setParameter(_deviceId: string, parameter: string, value: string): Promise<void> {
    await callSingleCommand('set', parameter, value);
  }

  async getLiveViewUrl(_deviceId: string): Promise<string | null> {
    await callCommandWindow('LiveViewWnd_Show');
    return `${baseUrl}/liveview.jpg`;
  }
}
