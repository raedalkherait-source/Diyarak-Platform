export default function ProjectDetailLoading() {
  return (
    <div className="container section" role="status" aria-label="جارٍ التحميل">
      <div className="skeleton-page">
        <div className="skeleton skeleton-text short" />
        <div className="skeleton skeleton-title" />
        <div className="skeleton skeleton-text" />
        <div className="skeleton skeleton-text short" />
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: "16px", marginTop: "24px" }}>
          {Array.from({ length: 4 }, (_, i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton skeleton-line" />
              <div className="skeleton skeleton-line short" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
