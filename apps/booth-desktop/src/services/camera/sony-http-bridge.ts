import type { CameraParameterSupport, CameraStatus, RemoteShutterRequest, TransferredImage } from '@photobooth/camera-core';
import type { VendorSdkBridge } from '@photobooth/camera-adapters';

const baseUrl = import.meta.env.VITE_SONY_SDK_BRIDGE_URL ?? 'http://127.0.0.1:5641';

async function getJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(init?.headers ?? {})
    }
  });
  if (!response.ok) {
    throw new Error(`Sony bridge request failed with status ${response.status}`);
  }
  return response.json() as Promise<T>;
}

export class SonySdkHttpBridge implements VendorSdkBridge {
  async isAvailable(): Promise<boolean> {
    try {
      await getJson('/health');
      return true;
    } catch {
      return false;
    }
  }

  async listDevices(): Promise<Array<{ id: string; name: string; model?: string }>> {
    return getJson('/devices');
  }

  async connect(deviceId: string): Promise<void> {
    await getJson(`/devices/${encodeURIComponent(deviceId)}/connect`, { method: 'POST' });
  }

  async disconnect(deviceId: string): Promise<void> {
    await getJson(`/devices/${encodeURIComponent(deviceId)}/disconnect`, { method: 'POST' });
  }

  async startLiveView(deviceId: string): Promise<string | null> {
    const result = await getJson<{ url?: string }>(`/devices/${encodeURIComponent(deviceId)}/live-view`, { method: 'POST' });
    return result.url ?? null;
  }

  async capture(deviceId: string, request: RemoteShutterRequest): Promise<TransferredImage[]> {
    return getJson(`/devices/${encodeURIComponent(deviceId)}/capture`, {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async getStatus(deviceId?: string): Promise<CameraStatus> {
    return getJson(`/status${deviceId ? `?deviceId=${encodeURIComponent(deviceId)}` : ''}`);
  }

  async getSupportedParameters(deviceId: string): Promise<CameraParameterSupport[]> {
    return getJson(`/devices/${encodeURIComponent(deviceId)}/parameters`);
  }

  async setParameter(deviceId: string, parameter: string, value: string): Promise<void> {
    await getJson(`/devices/${encodeURIComponent(deviceId)}/parameters/${encodeURIComponent(parameter)}`, {
      method: 'POST',
      body: JSON.stringify({ value })
    });
  }
}
