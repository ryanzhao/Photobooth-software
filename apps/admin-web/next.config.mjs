/** @type {import('next').NextConfig} */
const nextConfig = {
  transpilePackages: ['@photobooth/core', '@photobooth/db', '@photobooth/ui'],
  experimental: {
    typedRoutes: true
  }
};

export default nextConfig;
