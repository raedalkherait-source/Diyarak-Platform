"use client";

import { useEffect, useState } from "react";

type Theme = "system" | "light" | "dark";

function getStored(): Theme {
  try {
    const v = localStorage.getItem("diyarak_theme");
    if (v === "light" || v === "dark") return v;
  } catch { /* ignore */ }
  return "system";
}

function apply(theme: Theme) {
  const root = document.documentElement;
  if (theme === "system") {
    root.removeAttribute("data-theme");
    root.style.colorScheme = "";
  } else {
    root.setAttribute("data-theme", theme);
    root.style.colorScheme = theme;
  }
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>("system");

  useEffect(() => {
    const stored = getStored();
    setTheme(stored);
    apply(stored);
  }, []);

  function cycle() {
    const next: Theme = theme === "system" ? "dark" : theme === "dark" ? "light" : "system";
    setTheme(next);
    apply(next);
    try { localStorage.setItem("diyarak_theme", next); } catch { /* ignore */ }
  }

  const labels: Record<Theme, string> = { system: "تلقائي", light: "فاتح", dark: "داكن" };
  const icons: Record<Theme, string> = { system: "◐", light: "☀", dark: "☾" };

  return (
    <button
      type="button"
      onClick={cycle}
      className="theme-toggle"
      title={`الوضع: ${labels[theme]}`}
      aria-label={`تبديل السمة — الحالي: ${labels[theme]}`}
    >
      {icons[theme]}
    </button>
  );
}
