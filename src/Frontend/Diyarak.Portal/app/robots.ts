import type { MetadataRoute } from "next";
import { siteUrl } from "@/lib/site";

// Rendered per request so the sitemap URL follows PORTAL_PUBLIC_URL at runtime, not build time.
export const dynamic = "force-dynamic";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        disallow: ["/my", "/admin", "/login", "/api/"],
      },
    ],
    sitemap: `${siteUrl()}/sitemap.xml`,
  };
}
