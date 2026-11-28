import { describe, it, expect } from "vitest";
import {
  label,
  governorateLabels,
  categoryLabels,
  commercialSubtypeLabels,
  transactionIntentLabels,
  publishingRoleLabels,
  legalTitleLabels,
  finishingLevelLabels,
  featureLabels,
  contractorSpecialtyLabels,
  constructionStatusLabels,
  areaUnitLabels,
} from "../lib/labels";
import { listingStatusLabels, closureReasonLabels } from "../lib/status";
import { reportReasonLabels } from "../lib/moderation";

describe("label()", () => {
  it("returns Arabic label for known key", () => {
    expect(label(governorateLabels, "Damascus")).toBe("دمشق");
  });

  it("falls back to raw value for unknown key", () => {
    expect(label(governorateLabels, "UnknownPlace")).toBe("UnknownPlace");
  });

  it("returns empty string for null", () => {
    expect(label(governorateLabels, null)).toBe("");
  });

  it("returns empty string for undefined", () => {
    expect(label(governorateLabels, undefined)).toBe("");
  });

  it("returns empty string for empty string", () => {
    expect(label(governorateLabels, "")).toBe("");
  });
});

const allLabelMaps: [string, Record<string, string>][] = [
  ["governorateLabels", governorateLabels],
  ["categoryLabels", categoryLabels],
  ["commercialSubtypeLabels", commercialSubtypeLabels],
  ["transactionIntentLabels", transactionIntentLabels],
  ["publishingRoleLabels", publishingRoleLabels],
  ["legalTitleLabels", legalTitleLabels],
  ["finishingLevelLabels", finishingLevelLabels],
  ["featureLabels", featureLabels],
  ["contractorSpecialtyLabels", contractorSpecialtyLabels],
  ["constructionStatusLabels", constructionStatusLabels],
  ["areaUnitLabels", areaUnitLabels],
  ["listingStatusLabels", listingStatusLabels],
  ["closureReasonLabels", closureReasonLabels],
  ["reportReasonLabels", reportReasonLabels],
];

describe("label records integrity", () => {
  it.each(allLabelMaps)("%s has no empty values", (_name, map) => {
    for (const [key, val] of Object.entries(map)) {
      expect(val, `empty value for key "${key}"`).not.toBe("");
    }
  });

  it.each(allLabelMaps)("%s has at least one entry", (_name, map) => {
    expect(Object.keys(map).length).toBeGreaterThan(0);
  });

  it("governorateLabels covers all 14 Syrian governorates", () => {
    expect(Object.keys(governorateLabels)).toHaveLength(14);
  });
});
