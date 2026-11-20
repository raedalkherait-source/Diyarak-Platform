export function Options({ labels, placeholder }: { labels: Record<string, string>; placeholder: string }) {
  return (
    <>
      <option value="">{placeholder}</option>
      {Object.entries(labels).map(([value, text]) => (
        <option key={value} value={value}>
          {text}
        </option>
      ))}
    </>
  );
}
