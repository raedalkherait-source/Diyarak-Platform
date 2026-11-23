/** @type {import('next').NextConfig} */
const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

const nextConfig = {
  poweredByHeader: false,
  // Container builds set NEXT_OUTPUT=standalone for a minimal runtime image; local development
  // keeps the default output so "next start" works.
  output: process.env.NEXT_OUTPUT === "standalone" ? "standalone" : undefined,
  reactStrictMode: true,
  images: {
    // Public listing photos are resized and re-encoded (AVIF/WebP) by the portal, which matters
    // on slow mobile connections. Photo URLs are immutable, so optimized variants can be cached long.
    localPatterns: [{ pathname: "/media/listings/**" }],
    formats: ["image/avif", "image/webp"],
    deviceSizes: [360, 480, 640, 828, 1080, 1280, 1600],
    imageSizes: [96, 160, 256],
    minimumCacheTTL: 31536000,
  },
  experimental: {
    // Photo uploads go through a server action, one file per call (API limit: 10 MB).
    serverActions: { bodySizeLimit: "11mb" },
  },
  async rewrites() {
    // Photos are served through the portal origin so pages need no cross-origin image access.
    return [
      {
        source: "/media/listings/:listingId/photos/:photoId",
        destination: `${apiUrl}/api/market/public/listings/:listingId/photos/:photoId`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
          { key: "X-DNS-Prefetch-Control", value: "on" },
          { key: "Strict-Transport-Security", value: "max-age=63072000; includeSubDomains; preload" },
          { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
          { key: "X-Permitted-Cross-Domain-Policies", value: "none" },
        ],
      },
    ];
  },
};

export default nextConfig;
