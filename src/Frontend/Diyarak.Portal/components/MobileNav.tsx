"use client";

import { useState, useEffect } from "react";
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

  useEffect(() => { setOpen(false); }, [pathname]);

  useEffect(() => {
    if (open) document.body.style.overflow = "hidden";
    else document.body.style.overflow = "";
    return () => { document.body.style.overflow = ""; };
  }, [open]);

  return (
    <>
      <button
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
        <div className="mobile-nav-overlay" onClick={() => setOpen(false)}>
          <nav
            className="mobile-nav-panel"
            aria-label="القائمة الرئيسية"
            onClick={(e) => e.stopPropagation()}
          >
            {links.map((link) => (
              <Link key={link.href} href={link.href} className="mobile-nav-link">
                {link.label}
              </Link>
            ))}
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
