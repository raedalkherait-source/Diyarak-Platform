/** Geographic helpers shared by the server and the map components. */

export interface LatLng {
  latitude: number;
  longitude: number;
}

/** Rough bounding box of Syria; pins outside it are rejected. */
export const syriaBounds = { south: 32.3, west: 35.6, north: 37.4, east: 42.4 } as const;

export const syriaCenter: LatLng = { latitude: 34.8, longitude: 38.5 };

/** Approximate centre of each governorate, used to centre the location picker. */
export const governorateCenters: Record<string, LatLng> = {
  Damascus: { latitude: 33.513, longitude: 36.292 },
  RifDimashq: { latitude: 33.57, longitude: 36.4 },
  Aleppo: { latitude: 36.202, longitude: 37.134 },
  Homs: { latitude: 34.731, longitude: 36.709 },
  Hama: { latitude: 35.131, longitude: 36.754 },
  Latakia: { latitude: 35.523, longitude: 35.791 },
  Tartus: { latitude: 34.889, longitude: 35.886 },
  Idlib: { latitude: 35.931, longitude: 36.633 },
  Daraa: { latitude: 32.625, longitude: 36.106 },
  AsSuwayda: { latitude: 32.709, longitude: 36.569 },
  Quneitra: { latitude: 33.126, longitude: 35.824 },
  DeirEzZor: { latitude: 35.336, longitude: 40.14 },
  Raqqa: { latitude: 35.95, longitude: 39.01 },
  AlHasakah: { latitude: 36.502, longitude: 40.747 },
};

export function insideSyria({ latitude, longitude }: LatLng): boolean {
  return (
    latitude >= syriaBounds.south && latitude <= syriaBounds.north &&
    longitude >= syriaBounds.west && longitude <= syriaBounds.east
  );
}

/** Parses the picker's hidden inputs: both empty → null; both valid inside Syria → coordinates. */
export function parseCoordinates(latitude: string, longitude: string): LatLng | null | "invalid" {
  if (latitude === "" && longitude === "") return null;
  if (!/^-?\d{1,3}(\.\d{1,8})?$/.test(latitude) || !/^-?\d{1,3}(\.\d{1,8})?$/.test(longitude)) return "invalid";
  const point = { latitude: Number(latitude), longitude: Number(longitude) };
  return insideSyria(point) ? { latitude: round6(point.latitude), longitude: round6(point.longitude) } : "invalid";
}

function round6(value: number): number {
  return Math.round(value * 1e6) / 1e6;
}
