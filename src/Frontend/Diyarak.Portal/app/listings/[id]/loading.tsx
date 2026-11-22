export default function ListingLoading() {
  return (
    <div className="container section">
      <div className="skeleton-page">
        <div className="skeleton" style={{ height: 320, borderRadius: 10, marginBottom: 16 }} />
        <div className="skeleton skeleton-title" />
        <div className="skeleton skeleton-text" />
        <div className="skeleton skeleton-text" />
        <div className="skeleton skeleton-text short" />
      </div>
    </div>
  );
}
