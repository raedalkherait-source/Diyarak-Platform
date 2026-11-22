export default function PricesLoading() {
  return (
    <div className="container section">
      <div className="skeleton-page">
        <div className="skeleton skeleton-title" />
        <div className="skeleton skeleton-text" />
        <div className="skeleton" style={{ height: 200, borderRadius: 8, marginTop: 16 }} />
        <div className="skeleton" style={{ height: 200, borderRadius: 8, marginTop: 16 }} />
      </div>
    </div>
  );
}
