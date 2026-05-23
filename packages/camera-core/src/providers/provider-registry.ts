import type { CameraProvider } from '../contracts/camera-provider';

export class CameraProviderRegistry {
  constructor(private readonly providers: CameraProvider[]) {}

  async getAvailableProviders(): Promise<CameraProvider[]> {
    const checks = await Promise.all(
      this.providers.map(async (provider) => ({
        provider,
        available: await provider.isAvailable()
      }))
    );

    return checks
      .filter((entry) => entry.available)
      .map((entry) => entry.provider)
      .sort((left, right) => right.priority - left.priority);
  }

  getProvider(providerId: string): CameraProvider | undefined {
    return this.providers.find((provider) => provider.id === providerId);
  }
}
