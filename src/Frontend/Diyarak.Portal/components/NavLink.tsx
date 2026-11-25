"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ComponentProps } from "react";

export function NavLink({ href, children, ...props }: ComponentProps<typeof Link>) {
  const pathname = usePathname();
  const target = typeof href === "string" ? href : href.pathname ?? "";
  const isCurrent = pathname === target || pathname.startsWith(`${target}/`) || pathname.startsWith(`${target}?`);

  return (
    <Link href={href} aria-current={isCurrent ? "page" : undefined} {...props}>
      {children}
    </Link>
  );
}
