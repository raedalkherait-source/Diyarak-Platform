"use client";

export default function GlobalError({ reset }: { error: Error; reset: () => void }) {
  return (
    <html lang="ar" dir="rtl">
      <body style={{ fontFamily: "system-ui, sans-serif", margin: 0, padding: "60px 16px", textAlign: "center" }}>
        <h1 style={{ fontSize: "1.5rem", margin: "0 0 8px" }}>حدث خطأ غير متوقع</h1>
        <p style={{ color: "#666", margin: "0 0 24px" }}>نعتذر عن هذا الخلل. يمكنك إعادة المحاولة.</p>
        <button type="button" onClick={reset} style={{ padding: "10px 24px", fontSize: "1rem", cursor: "pointer" }}>
          إعادة المحاولة
        </button>
      </body>
    </html>
  );
}
