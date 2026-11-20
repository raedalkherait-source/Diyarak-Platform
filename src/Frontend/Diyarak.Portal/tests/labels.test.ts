import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import {
  categoryLabels,
  commercialSubtypeLabels,
  featureLabels,
  finishingLevelLabels,
  governorateLabels,
  label,
  legalTitleLabels,
  publishingRoleLabels,
  transactionIntentLabels,
} from "@/lib/labels";
import { reportReasonLabels } from "@/lib/moderation";
import { closureReasonLabels, listingStatusLabels } from "@/lib/status";

const srcRoot = fileURLToPath(new URL("../../../", import.meta.url));

/** Reads member names of a C# enum so labels cannot drift from the backend contract. */
function csharpEnumMembers(relativePath: string): string[] {
  const source = readFileSync(srcRoot + relativePath, "utf8");
  const start = source.indexOf("{", source.indexOf("enum ")) + 1;
  const body = source.slice(start, source.indexOf("}", start));
  return body
    .replace(/\/\/\/.*$/gm, "")
    .split(",")
    .map((member) => member.split("=")[0]!.trim())
    .filter((member) => /^[A-Za-z]+$/.test(member));
}

const cases: Array<[string, Record<string, string>, string]> = [
  ["governorates", governorateLabels, "Modules/Diyarak.Market.Property/SyrianGovernorate.cs"],
  ["categories", categoryLabels, "Modules/Diyarak.Market.Property/PropertyCategory.cs"],
  ["commercial subtypes", commercialSubtypeLabels, "Modules/Diyarak.Market.Property/CommercialPropertySubtype.cs"],
  ["features", featureLabels, "Modules/Diyarak.Market.Property/PropertyFeature.cs"],
  ["legal titles", legalTitleLabels, "Modules/Diyarak.Market.Property/LegalTitleType.cs"],
  ["finishing levels", finishingLevelLabels, "Modules/Diyarak.Market.Property/FinishingLevel.cs"],
  ["transaction intents", transactionIntentLabels, "Modules/Diyarak.Market.Listing/TransactionIntent.cs"],
  ["publishing roles", publishingRoleLabels, "Modules/Diyarak.Market.Listing/PublishingRole.cs"],
  ["listing statuses", listingStatusLabels, "Modules/Diyarak.Market.Listing/ListingStatus.cs"],
  ["closure reasons", closureReasonLabels, "Modules/Diyarak.Market.Listing/ListingClosureReason.cs"],
  ["report reasons", reportReasonLabels, "Modules/Diyarak.Market.Listing/ListingReport.cs"],
];

describe("Arabic labels", () => {
  it.each(cases)("cover every backend value for %s", (_name, labels, file) => {
    const members = csharpEnumMembers(file);
    expect(members.length).toBeGreaterThan(1);
    expect(Object.keys(labels).sort()).toEqual([...members].sort());
    for (const text of Object.values(labels)) expect(text).toMatch(/[؀-ۿ]/);
  });

  it("falls back to the raw value for unknown entries", () => {
    expect(label(categoryLabels, "Castle")).toBe("Castle");
    expect(label(categoryLabels, null)).toBe("");
  });
});
