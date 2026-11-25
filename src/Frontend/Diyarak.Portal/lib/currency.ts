export type Currency = "USD" | "SYP";

export function convertPrice(
  amount: number | null,
  fromCurrency: string | null,
  toCurrency: Currency,
  rate: number,
): { amount: number | null; currency: string } {
  if (amount === null) return { amount: null, currency: toCurrency };
  if (!fromCurrency || fromCurrency === toCurrency) return { amount, currency: toCurrency };
  if (fromCurrency === "USD" && toCurrency === "SYP") return { amount: Math.round(amount * rate), currency: "SYP" };
  if (fromCurrency === "SYP" && toCurrency === "USD") return { amount: Math.round((amount / rate) * 100) / 100, currency: "USD" };
  return { amount, currency: fromCurrency };
}
