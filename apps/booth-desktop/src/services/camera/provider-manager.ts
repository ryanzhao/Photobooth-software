import { canonProvider, DigicamControlProvider, fujifilmProvider, nikonProvider, SonyRemoteSdkProvider, WebcamCameraProvider } from '@photobooth/camera-adapters';
import { CameraProviderRegistry } from '@photobooth/camera-core';

import { DigicamControlHttpBridge } from './digicamcontrol-http-bridge';
import { SonySdkHttpBridge } from './sony-http-bridge';

const registry = new CameraProviderRegistry([
  new SonyRemoteSdkProvider(new SonySdkHttpBridge()),
  new DigicamControlProvider(new DigicamControlHttpBridge()),
  new WebcamCameraProvider(),
  canonProvider,
  nikonProvider,
  fujifilmProvider
]);

export function getCameraRegistry(): CameraProviderRegistry {
  return registry;
}
