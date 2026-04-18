import { BaseCameraProvider, type CameraDeviceDescriptor, type CameraParameterSupport, type CameraStatus, type CapturePhotoRequest, type PreviewSession, type TransferredImage } from '@photobooth/camera-core';

async function blobToObjectUrl(blob: Blob): Promise<string> {
  return URL.createObjectURL(blob);
}

export class WebcamCameraProvider extends BaseCameraProvider {
  id = 'webcam';
  label = 'Webcam Fallback';
  transport = 'webcam' as const;
  priority = 20;

  private streams = new Map<string, MediaStream>();
  private status: CameraStatus = {
    state: 'idle',
    diagnostics: ['Webcam mode is available for local MVP validation.']
  };

  async isAvailable(): Promise<boolean> {
    return typeof navigator !== 'undefined' && !!navigator.mediaDevices;
  }

  async listDevices(): Promise<CameraDeviceDescriptor[]> {
    if (!(await this.isAvailable())) {
      return [];
    }

    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices
      .filter((device) => device.kind === 'videoinput')
      .map((device, index) => ({
        id: device.deviceId,
        name: device.label || `Webcam ${index + 1}`,
        model: 'MediaDevices',
        transport: 'webcam',
        providerId: this.id,
        isConnected: this.streams.has(device.deviceId),
        liveViewSupported: true,
        remoteTriggerSupported: false,
        transferSupported: false,
        diagnostics: ['Snapshot capture only. This path is not used for tethered remote shutter workflows.']
      }));
  }

  async connect(deviceId: string): Promise<CameraStatus> {
    this.status = {
      state: 'connecting',
      connectedDeviceId: deviceId,
      diagnostics: ['Requesting browser media permissions.']
    };
    this.emit('connectionChange', this.status);
    return this.status;
  }

  async disconnect(deviceId: string): Promise<void> {
    const stream = this.streams.get(deviceId);
    stream?.getTracks().forEach((track) => track.stop());
    this.streams.delete(deviceId);
    this.status = {
      state: 'idle',
      diagnostics: ['Webcam disconnected.']
    };
    this.emit('connectionChange', this.status);
  }

  async getStatus(): Promise<CameraStatus> {
    return this.status;
  }

  async startLiveView(deviceId: string): Promise<PreviewSession> {
    const mediaStream = await navigator.mediaDevices.getUserMedia({
      video: { deviceId: { exact: deviceId } },
      audio: false
    });

    this.streams.set(deviceId, mediaStream);
    this.status = {
      state: 'previewing',
      connectedDeviceId: deviceId,
      diagnostics: ['Webcam live preview active.']
    };
    this.emit('connectionChange', this.status);

    return {
      type: 'media-stream',
      mediaStream,
      mirrored: true
    };
  }

  async stopLiveView(deviceId: string): Promise<void> {
    await this.disconnect(deviceId);
  }

  async capturePhoto(deviceId: string, request: CapturePhotoRequest): Promise<TransferredImage> {
    const stream = this.streams.get(deviceId);
    if (!stream) {
      throw new Error('Webcam preview has not been started.');
    }

    const track = stream.getVideoTracks()[0];
    let blob: Blob | null = null;
    const ImageCaptureCtor = (globalThis as typeof globalThis & {
      ImageCapture?: new (track: MediaStreamTrack) => { takePhoto(): Promise<Blob> };
    }).ImageCapture;

    if (ImageCaptureCtor && track) {
      const imageCapture = new ImageCaptureCtor(track);
      blob = await imageCapture.takePhoto();
    }

    if (!blob) {
      const video = request.previewElement;
      if (!video) {
        throw new Error('A preview element is required when ImageCapture is unavailable.');
      }

      const canvas = document.createElement('canvas');
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const context = canvas.getContext('2d');
      if (!context) {
        throw new Error('Unable to acquire canvas context for webcam capture.');
      }
      context.drawImage(video, 0, 0, canvas.width, canvas.height);
      blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.95));
    }

    if (!blob) {
      throw new Error('Webcam capture did not return an image blob.');
    }

    const fileName = `${request.fileNamePrefix}.jpg`;
    const previewUrl = await blobToObjectUrl(blob);

    const transferred: TransferredImage = {
      fileName,
      localPath: `${request.sessionFolder}/${fileName}`,
      previewUrl,
      metadata: {
        provider: 'webcam',
        source: 'browser-media-devices'
      }
    };

    this.emit('imageTransferred', transferred);
    return transferred;
  }

  async getSupportedParameters(): Promise<CameraParameterSupport[]> {
    return [
      {
        name: 'remoteTrigger',
        label: 'Remote Shutter',
        supported: false,
        reasonIfUnsupported: 'Webcam mode captures frames locally and does not send capture commands to an external camera.'
      }
    ];
  }
}
