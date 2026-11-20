/** @type {import('next').NextConfig} */
const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

const nextConfig = {
  poweredByHeader: false,
  reactStrictMode: true,
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
        ],
      },
    ];
  },
};

export default nextConfig;
