"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";

const links = [
  { href: "/search?transactionIntent=Sell", label: "شراء" },
  { href: "/search?transactionIntent=Rent", label: "إيجار" },
  { href: "/search?propertyCategory=CommercialProperty", label: "تجاري" },
  { href: "/projects", label: "مشاريع" },
  { href: "/prices", label: "أسعار" },
  { href: "/contractors", label: "مقاولون" },
];

export function MobileNav({ signedIn }: { signedIn: boolean }) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const panelRef = useRef<HTMLElement>(null);
  const toggleRef = useRef<HTMLButtonElement>(null);

  const close = useCallback(() => {
    setOpen(false);
    toggleRef.current?.focus();
  }, []);

  useEffect(() => { setOpen(false); }, [pathname]);

  useEffect(() => {
    if (open) document.body.style.overflow = "hidden";
    else document.body.style.overflow = "";
    return () => { document.body.style.overflow = ""; };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") { close(); return; }
      if (e.key !== "Tab" || !panelRef.current) return;
      const focusable = panelRef.current.querySelectorAll<HTMLElement>("a, button");
      if (focusable.length === 0) return;
      const first = focusable[0]!;
      const last = focusable[focusable.length - 1]!;
      if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }
    document.addEventListener("keydown", onKeyDown);
    panelRef.current?.querySelector<HTMLElement>("a")?.focus();
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, close]);

  return (
    <>
      <button
        ref={toggleRef}
        type="button"
        className="mobile-nav-toggle"
        onClick={() => setOpen(!open)}
        aria-expanded={open}
        aria-label={open ? "إغلاق القائمة" : "فتح القائمة"}
      >
        <span className="hamburger-line" />
        <span className="hamburger-line" />
        <span className="hamburger-line" />
      </button>
      {open && (
        <div className="mobile-nav-overlay" onClick={close}>
          <nav
            ref={panelRef}
            className="mobile-nav-panel"
            aria-label="القائمة الرئيسية"
            onClick={(e) => e.stopPropagation()}
          >
            {links.map((link) => {
              const target = link.href.split("?")[0]!;
              const isCurrent = pathname === target || pathname.startsWith(`${target}/`);
              return (
                <Link key={link.href} href={link.href} className="mobile-nav-link" aria-current={isCurrent ? "page" : undefined}>
                  {link.label}
                </Link>
              );
            })}
            <hr className="mobile-nav-divider" />
            <Link href={signedIn ? "/my" : "/login"} className="mobile-nav-link mobile-nav-cta">
              {signedIn ? "إعلاناتي" : "أضف إعلانك"}
            </Link>
          </nav>
        </div>
      )}
    </>
  );
}
