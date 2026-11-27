export default function SearchLoading() {
  return (
    <div className="container search-layout" role="status" aria-label="جارٍ التحميل">
      <div className="skeleton-page">
        <div className="skeleton skeleton-title" />
        <div className="skeleton-grid">
          {Array.from({ length: 6 }, (_, i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton skeleton-image" />
              <div className="skeleton skeleton-line" />
              <div className="skeleton skeleton-line short" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
