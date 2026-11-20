import "server-only";

export interface MapConfig {
  tileUrl: string;
  attribution: string;
}

/**
 * Tile provider, read at request time so operators can switch provider without rebuilding.
 * The OpenStreetMap default suits development and low traffic only; its usage policy asks
 * heavier users to run or buy their own tiles (see the production runbook).
 */
export function mapConfig(): MapConfig {
  return {
    tileUrl: process.env.MAP_TILE_URL || "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
    attribution: process.env.MAP_ATTRIBUTION || "© OpenStreetMap contributors",
  };
}
