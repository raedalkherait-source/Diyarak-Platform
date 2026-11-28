export default function ContractorsLoading() {
  return (
    <div className="container section" role="status" aria-busy="true" aria-label="جارٍ التحميل">
      <div className="skeleton-page">
        <div className="skeleton skeleton-title" />
        <div className="skeleton skeleton-text short" />
        <div className="skeleton-grid">
          {Array.from({ length: 6 }, (_, i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton skeleton-line" />
              <div className="skeleton skeleton-line short" />
              <div className="skeleton skeleton-line" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
