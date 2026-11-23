import { ImageResponse } from "next/og";

export const runtime = "edge";
export const alt = "ديارك — عقارات سوريا للبيع والإيجار";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OgImage() {
  return new ImageResponse(
    (
      <div
        dir="rtl"
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          background: "linear-gradient(135deg, #0f6b5c 0%, #0a4d42 100%)",
          color: "#fff",
          fontFamily: "sans-serif",
        }}
      >
        <div style={{ fontSize: 72, fontWeight: 700, marginBottom: 16, display: "flex" }}>ديارك</div>
        <div style={{ fontSize: 32, opacity: 0.9, display: "flex" }}>عقارات سوريا للبيع والإيجار</div>
        <div
          style={{
            marginTop: 40,
            fontSize: 20,
            opacity: 0.7,
            display: "flex",
          }}
        >
          diyarak.app
        </div>
      </div>
    ),
    { ...size },
  );
}
