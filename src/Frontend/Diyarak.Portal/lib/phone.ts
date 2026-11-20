const arabicIndicDigits = /[٠-٩۰-۹]/g;

function toWesternDigits(value: string): string {
  return value.replace(arabicIndicDigits, (digit) => {
    const code = digit.charCodeAt(0);
    return String(code >= 0x06f0 ? code - 0x06f0 : code - 0x0660);
  });
}

/**
 * Normalises what Syrian users typically type into E.164. Accepts local mobile numbers
 * (09xxxxxxxx, 9xxxxxxxx), 00963/963/+963 prefixes, Arabic-Indic digits and separators, and any
 * other international number written with a leading + or 00.
 */
export function normalizePhone(input: string): string | null {
  const compact = toWesternDigits(input).replace(/[\s\-().]/g, "");

  let digits: string;
  if (compact.startsWith("+")) digits = compact.slice(1);
  else if (compact.startsWith("00")) digits = compact.slice(2);
  else if (/^09\d{8}$/.test(compact)) digits = `963${compact.slice(1)}`;
  else if (/^9\d{8}$/.test(compact)) digits = `963${compact}`;
  else if (/^963\d{9}$/.test(compact)) digits = compact;
  else return null;

  if (!/^[1-9]\d{7,14}$/.test(digits)) return null;
  if (digits.startsWith("963") && !/^9639\d{8}$/.test(digits)) return null;

  return `+${digits}`;
}
