const publicLabels = {
  category: {
    Apartment: "Apartment",
    House: "House",
    Plot: "Plot",
    InvestmentProperty: "Investment property",
    GarageOrParkingSpace: "Garage or parking space",
    CommercialProperty: "Commercial property",
  },
  features: {
    FittedKitchen: "Fitted kitchen",
    Elevator: "Elevator",
    BalconyOrTerrace: "Balcony or terrace",
    GuestToilet: "Guest toilet",
    GardenOrSharedGardenUse: "Garden or shared garden use",
    Basement: "Basement",
    StepFreeAccess: "Step-free access",
    SuitableForVacationRental: "Suitable for vacation rental",
    HistoricMonument: "Historic monument",
    GarageOrParkingSpace: "Garage or parking space",
  },
  publishingRole: {
    Owner: "Owner",
    Tenant: "Tenant",
    ProfessionalOrAgent: "Professional or agent",
  },
  transactionIntent: {
    Rent: "For rent",
    Sell: "For sale",
    RentForLimitedPeriod: "For rent for a limited period",
  },
  furnishingQuality: {
    Simple: "Simple",
    Normal: "Normal",
    Upscale: "Upscale",
    Luxury: "Luxury",
  },
  commercialSubtype: {
    OfficeOrPractice: "Office or practice",
    RestaurantOrHotel: "Restaurant or hotel",
    HallOrProduction: "Hall or production",
    Retail: "Retail",
    CommercialPlot: "Commercial plot",
  },
  areaUnit: {
    SquareMeter: "m²",
    SquareFoot: "ft²",
    Hectare: "ha",
    Dunum: "dunum",
  },
};
const numberFormatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 20 });

const statusElement = document.querySelector("#status");
const gridElement = document.querySelector("#listing-grid");
const template = document.querySelector("#listing-card-template");
const loadButton = document.querySelector("#load-listings");
const retryButton = document.querySelector("#retry-listings");
const previousButton = document.querySelector("#previous-page");
const nextButton = document.querySelector("#next-page");
const pageJumpForm = document.querySelector("#page-jump-form");
const pageJumpFields = document.querySelector("#page-jump-fields");
const pageJumpInput = document.querySelector("#page-jump-input");
const pageJumpError = document.querySelector("#page-jump-error");
const pageElement = document.querySelector("#current-page");
const paginationElement = document.querySelector("#listing-pagination");
const filterSummary = document.querySelector("#applied-filter-summary");
const filterRemovalActions = document.querySelector("#filter-removal-actions");
const editFiltersButton = document.querySelector("#edit-search-filters");
const copySearchButton = document.querySelector("#copy-search-link");
const searchLinkStatus = document.querySelector("#search-link-status");
const headingElement = document.querySelector("#listing-heading");
const backLink = document.querySelector("#back-to-listings");
const filterPanel = document.querySelector("#listing-filter-panel");
const filterForm = document.querySelector("#listing-filters");
const filterFields = document.querySelector("#listing-filter-fields");
const cityInput = document.querySelector("#filter-city");
const pageSizeInput = document.querySelector("#filter-page-size");
const postalInput = document.querySelector("#filter-postal");
const intentInputs = Object.keys(publicLabels.transactionIntent).map(value => ({
  value, input: document.querySelector(`#filter-intent-${value}`),
}));
const categoryInputs = Object.keys(publicLabels.category).map(value => ({
  value, input: document.querySelector(`#filter-category-${value}`),
}));
const commercialInputs = Object.keys(publicLabels.commercialSubtype).map(value => ({
  value, input: document.querySelector(`#filter-commercial-${value}`),
}));
const furnishingInputs = Object.keys(publicLabels.furnishingQuality).map(value => ({
  value, input: document.querySelector(`#filter-furnishing-${value}`),
}));
const featureInputs = Object.keys(publicLabels.features).map(value => ({
  value, input: document.querySelector(`#filter-feature-${value}`),
}));
const priceMinInput = document.querySelector("#filter-price-min");
const priceMaxInput = document.querySelector("#filter-price-max");
const priceCurrencyInput = document.querySelector("#filter-price-currency");
const areaMinInput = document.querySelector("#filter-area-min");
const areaMaxInput = document.querySelector("#filter-area-max");
const areaUnitInput = document.querySelector("#filter-area-unit");
const numericFilterRanges = [
  { key: "totalRooms", label: "total rooms", integer: false,
    minimum: document.querySelector("#filter-rooms-min"),
    maximum: document.querySelector("#filter-rooms-max") },
  { key: "bedroomCount", label: "bedrooms", integer: true,
    minimum: document.querySelector("#filter-bedrooms-min"),
    maximum: document.querySelector("#filter-bedrooms-max") },
  { key: "bathroomCount", label: "bathrooms", integer: true,
    minimum: document.querySelector("#filter-bathrooms-min"),
    maximum: document.querySelector("#filter-bathrooms-max") },
];
numericFilterRanges.push(
  { key: "constructionYear", label: "construction year", integer: true, lowerBound: 1,
    minimum: document.querySelector("#filter-year-min"),
    maximum: document.querySelector("#filter-year-max") },
  { key: "parkingSpaceCount", label: "parking spaces", integer: true,
    minimum: document.querySelector("#filter-parking-min"),
    maximum: document.querySelector("#filter-parking-max") },
);
const filterDraftMessage = document.querySelector("#filter-draft-message");
const filterError = document.querySelector("#filter-error");
const restoreFiltersButton = document.querySelector("#restore-filters");
const clearFiltersButton = document.querySelector("#clear-filters");
const parameters = new URLSearchParams(window.location.search);
const isDetailView = parameters.has("listingId");
const selectedListingId = parameters.get("listingId");
const collectionUrl = "/api/market/public/listings";
const initialSearch = getSupportedSearch(isDetailView
  ? new URLSearchParams(parameters.get("returnSearch") ?? "")
  : parameters);
const initialCollectionUrl = collectionRequestUrl(initialSearch);
const firstPageSearch = new URLSearchParams(initialSearch);
firstPageSearch.delete("page");
let firstPageUrl = collectionRequestUrl(firstPageSearch);
let requestedUrl = collectionUrl;
let previousUrl = null;
let nextUrl = null;
let isLoading = false;
let appliedFilterSnapshot = null;
let isCopyingSearchLink = false;
let searchLinkVersion = 0;
let pendingHistorySearch = null;
let cancelPublicRequest = null;
let filterRemovalButtons = [];

loadButton.addEventListener("click", async () => {
  await loadSelectedView(firstPageUrl, "push");
});

retryButton.addEventListener("click", async () => {
  await loadSelectedView(requestedUrl);
});

previousButton.addEventListener("click", async () => {
  if (previousUrl) {
    await loadPublicListings(previousUrl, "push");
  }
});

nextButton.addEventListener("click", async () => {
  if (nextUrl) {
    await loadPublicListings(nextUrl, "push");
  }
});

pageJumpForm.addEventListener("submit", async event => {
  event.preventDefault();
  if (isLoading || isDetailView) {
    return;
  }
  const value = pageJumpInput.value.trim();
  const valid = /^\d+$/.test(value) && Number(value) >= 1 && Number(value) <= 2147483647;
  pageJumpError.textContent = valid ? "" : "Enter a whole-number page between 1 and 2147483647.";
  pageJumpError.hidden = valid;
  if (!valid) {
    pageJumpError.focus();
    return;
  }
  const query = getSupportedSearch(new URL(requestedUrl, window.location.origin).searchParams);
  query.set("page", String(Number(value)));
  await loadPublicListings(collectionRequestUrl(query), "push");
});

copySearchButton.addEventListener("click", async () => {
  if (isLoading || isDetailView || isCopyingSearchLink) {
    return;
  }
  const version = searchLinkVersion;
  const query = getSupportedSearch(new URL(requestedUrl, window.location.origin).searchParams);
  const url = new URL("/app/index.html", window.location.origin);
  url.search = query.toString();
  const link = url.href;
  isCopyingSearchLink = true;
  searchLinkStatus.textContent = "";
  searchLinkStatus.hidden = true;
  updatePagination();
  try {
    if (typeof navigator.clipboard?.writeText !== "function") {
      throw new Error("Clipboard is unavailable.");
    }
    await navigator.clipboard.writeText(link);
    if (version === searchLinkVersion) {
      searchLinkStatus.textContent = "Search link copied.";
      searchLinkStatus.hidden = false;
    }
  } catch {
    if (version === searchLinkVersion) {
      searchLinkStatus.textContent = `Copy this search link manually: ${link}`;
      searchLinkStatus.hidden = false;
    }
  } finally {
    isCopyingSearchLink = false;
    updatePagination();
  }
});

editFiltersButton.addEventListener("click", () => {
  if (isLoading || isDetailView) {
    return;
  }
  filterPanel.open = true;
  cityInput.focus();
});

filterForm.addEventListener("input", updateFilterDraftState);
filterForm.addEventListener("change", updateFilterDraftState);

filterForm.addEventListener("submit", async event => {
  event.preventDefault();
  if (isLoading || isDetailView) {
    return;
  }

  const validationError = validatePageSize() ?? validatePriceFilters() ?? validateAreaFilters() ?? validateNumericFilters();
  filterError.textContent = validationError ?? "";
  filterError.hidden = !validationError;
  if (validationError) {
    filterError.focus();
    return;
  }

  rememberAppliedFilterState();
  firstPageUrl = buildFilteredCollectionUrl();
  await loadPublicListings(firstPageUrl, "push");
});

restoreFiltersButton.addEventListener("click", () => {
  if (isLoading || isDetailView) {
    return;
  }
  restoreFilterForm(getSupportedSearch(new URL(requestedUrl, window.location.origin).searchParams));
});

clearFiltersButton.addEventListener("click", async () => {
  if (isLoading || isDetailView) {
    return;
  }

  restoreFilterForm(new URLSearchParams());
  firstPageUrl = collectionUrl;
  await loadPublicListings(firstPageUrl, "push");
});

if (!isDetailView) {
  window.addEventListener("popstate", async () => {
    pendingHistorySearch = getSupportedSearch(new URLSearchParams(window.location.search));
    searchLinkVersion++;
    searchLinkStatus.textContent = "";
    searchLinkStatus.hidden = true;
    if (isLoading) {
      cancelPublicRequest?.();
      return;
    }
    await loadPendingHistorySearch();
  });
}

async function loadPendingHistorySearch() {
  const search = pendingHistorySearch;
  pendingHistorySearch = null;
  restoreFilterForm(search);
  const firstSearch = new URLSearchParams(search);
  firstSearch.delete("page");
  firstPageUrl = collectionRequestUrl(firstSearch);
  await loadPublicListings(collectionRequestUrl(search));
}

restoreFilterForm(initialSearch);
backLink.href = collectionPageUrl(initialSearch);
filterSummary.hidden = isDetailView;
filterRemovalActions.hidden = true;
editFiltersButton.hidden = isDetailView;
copySearchButton.hidden = isDetailView;
filterPanel.hidden = isDetailView;
filterForm.hidden = isDetailView;
pageJumpForm.hidden = isDetailView;
paginationElement.hidden = isDetailView;
backLink.hidden = !isDetailView;
headingElement.textContent = isDetailView ? "Listing details" : "Public listings";
loadButton.textContent = isDetailView ? "Refresh listing" : "Refresh listings";
await loadSelectedView(initialCollectionUrl);

async function loadSelectedView(url = firstPageUrl, historyMode = "replace") {
  if (isDetailView) {
    await loadPublicListing();
  } else {
    await loadPublicListings(url, historyMode);
  }
}

function filterFormSnapshot() {
  return JSON.stringify({
    fields: [cityInput, postalInput, priceMinInput, priceMaxInput, priceCurrencyInput,
      areaMinInput, areaMaxInput, areaUnitInput, pageSizeInput].map(input => input.value),
    choices: [intentInputs, categoryInputs, commercialInputs, furnishingInputs]
      .map(choices => choices.filter(choice => choice.input.checked).map(choice => choice.value)),
    ranges: numericFilterRanges.map(range => [range.minimum.value, range.maximum.value]),
    features: featureInputs.filter(feature => feature.input.checked).map(feature => feature.value),
  });
}

function rememberAppliedFilterState() {
  appliedFilterSnapshot = filterFormSnapshot();
  filterDraftMessage.hidden = true;
}

function updateFilterDraftState() {
  if (isLoading || isDetailView) {
    return;
  }
  filterDraftMessage.hidden = filterFormSnapshot() === appliedFilterSnapshot;
}

function restoreFilterForm(query) {
  pageSizeInput.value = query.get("pageSize") ?? "";
  cityInput.value = query.get("city") ?? "";
  postalInput.value = query.get("postalCode") ?? "";
  const initialIntents = new Set(query.getAll("transactionIntent"));
  for (const intent of intentInputs) {
    intent.input.checked = initialIntents.has(intent.value);
  }
  const initialCategories = new Set(query.getAll("propertyCategory"));
  for (const choice of categoryInputs) {
    choice.input.checked = initialCategories.has(choice.value);
  }
  const initialFeatures = new Set(query.getAll("propertyFeature"));
  for (const feature of featureInputs) {
    feature.input.checked = initialFeatures.has(feature.value);
  }
  const initialCommercialTypes = new Set(query.getAll("commercialSubtype"));
  for (const choice of commercialInputs) {
    choice.input.checked = initialCommercialTypes.has(choice.value);
  }
  const initialQualities = new Set(query.getAll("furnishingQuality"));
  for (const choice of furnishingInputs) {
    choice.input.checked = initialQualities.has(choice.value);
  }
  priceMinInput.value = query.get("priceMin") ?? "";
  priceMaxInput.value = query.get("priceMax") ?? "";
  priceCurrencyInput.value = query.get("priceCurrency") ?? "";
  areaMinInput.value = query.get("livingAreaMin") ?? "";
  areaMaxInput.value = query.get("livingAreaMax") ?? "";
  areaUnitInput.value = query.get("livingAreaUnit") ?? "";
  for (const range of numericFilterRanges) {
    range.minimum.value = query.get(`${range.key}Min`) ?? "";
    range.maximum.value = query.get(`${range.key}Max`) ?? "";
  }
  filterError.textContent = "";
  filterError.hidden = true;
  rememberAppliedFilterState();
}

function getSupportedSearch(query) {
  const supported = new URLSearchParams();
  for (const [key, value] of query) {
    if (["city", "postalCode", "transactionIntent", "propertyCategory", "commercialSubtype", "furnishingQuality", "propertyFeature", "priceMin", "priceMax", "priceCurrency", "livingAreaMin", "livingAreaMax", "livingAreaUnit", "totalRoomsMin", "totalRoomsMax", "bedroomCountMin", "bedroomCountMax", "bathroomCountMin", "bathroomCountMax", "constructionYearMin", "constructionYearMax", "parkingSpaceCountMin", "parkingSpaceCountMax", "page", "pageSize"].includes(key)) {
      supported.append(key, value);
    }
  }
  return supported;
}

function describeAppliedFilters(query) {
  const fields = [
    ["city", "City"],
    ["postalCode", "Postal code"],
    ["transactionIntent", "Transaction", "transactionIntent"],
    ["propertyCategory", "Category", "category"],
    ["commercialSubtype", "Commercial type", "commercialSubtype"],
    ["furnishingQuality", "Furnishing", "furnishingQuality"],
    ["priceMin", "Minimum price"],
    ["priceMax", "Maximum price"],
    ["priceCurrency", "Price currency"],
    ["livingAreaMin", "Minimum living area"],
    ["livingAreaMax", "Maximum living area"],
    ["livingAreaUnit", "Area unit", "areaUnit"],
    ...numericFilterRanges.flatMap(range => [
      [`${range.key}Min`, `Minimum ${range.label}`],
      [`${range.key}Max`, `Maximum ${range.label}`],
    ]),
    ["propertyFeature", "Required features", "features"],
  ];
  const parts = [];
  for (const [key, label, enumField] of fields) {
    const values = query.getAll(key);
    if (values.length > 0) {
      const displayValues = values.map(value => enumField ? formatLabel(enumField, value) : value);
      parts.push(`${label}: ${displayValues.join(", ")}`);
    }
  }
  return parts.length ? `Search filters: ${parts.join(" · ")}` : "No filters applied.";
}

function renderFilterRemovalActions(query) {
  filterRemovalActions.replaceChildren();
  filterRemovalButtons = [];
  const groups = [
    { id: "city", label: "city", keys: ["city"] },
    { id: "postal", label: "postal code", keys: ["postalCode"] },
    { id: "intent", label: "transaction intent", keys: ["transactionIntent"] },
    { id: "category", label: "property category", keys: ["propertyCategory"] },
    { id: "commercial", label: "commercial property type", keys: ["commercialSubtype"] },
    { id: "furnishing", label: "furnishing quality", keys: ["furnishingQuality"] },
    { id: "price", label: "price range", keys: ["priceMin", "priceMax", "priceCurrency"] },
    { id: "area", label: "living area range", keys: ["livingAreaMin", "livingAreaMax", "livingAreaUnit"] },
    ...numericFilterRanges.map(range => ({
      id: range.key, label: `${range.label} range`, keys: [`${range.key}Min`, `${range.key}Max`],
    })),
    { id: "features", label: "required features", keys: ["propertyFeature"] },
  ];
  for (const group of groups) {
    if (!group.keys.some(key => query.has(key))) {
      continue;
    }
    const button = document.createElement("button");
    button.type = "button";
    button.className = "secondary-button";
    button.dataset.filterGroup = group.id;
    button.textContent = `Remove ${group.label}`;
    button.addEventListener("click", async () => {
      if (isLoading || isDetailView) {
        return;
      }
      const search = getSupportedSearch(new URL(requestedUrl, window.location.origin).searchParams);
      if (!group.keys.some(key => search.has(key))) {
        return;
      }
      for (const key of group.keys) {
        search.delete(key);
      }
      search.delete("page");
      restoreFilterForm(search);
      firstPageUrl = collectionRequestUrl(search);
      filterSummary.focus();
      await loadPublicListings(firstPageUrl, "push");
    });
    filterRemovalButtons.push(button);
    filterRemovalActions.append(button);
  }
  filterRemovalActions.hidden = isDetailView || filterRemovalButtons.length === 0;
}

function collectionRequestUrl(query) {
  const search = query.toString();
  return search ? `${collectionUrl}?${search}` : collectionUrl;
}

function collectionPageUrl(query) {
  const search = query.toString();
  return search ? `./index.html?${search}` : "./index.html";
}

function compareNonNegativeDecimals(left, right) {
  const normalize = value => {
    const [whole = "", fraction = ""] = value.split(".");
    return { whole: whole.replace(/^0+/, "") || "0", fraction: fraction.replace(/0+$/, "") };
  };
  const a = normalize(left);
  const b = normalize(right);
  if (a.whole.length !== b.whole.length) {
    return a.whole.length > b.whole.length ? 1 : -1;
  }
  if (a.whole !== b.whole) {
    return a.whole > b.whole ? 1 : -1;
  }
  const width = Math.max(a.fraction.length, b.fraction.length);
  const aFraction = a.fraction.padEnd(width, "0");
  const bFraction = b.fraction.padEnd(width, "0");
  return aFraction === bFraction ? 0 : aFraction > bFraction ? 1 : -1;
}

function validatePriceFilters() {
  const minimum = priceMinInput.value.trim();
  const maximum = priceMaxInput.value.trim();
  const currency = priceCurrencyInput.value.trim().toUpperCase();
  const decimalPattern = /^(?:\d+(?:\.\d*)?|\.\d+)$/;
  for (const [value, label] of [[minimum, "minimum"], [maximum, "maximum"]]) {
    if (value && (!decimalPattern.test(value) || !Number.isFinite(Number(value)))) {
      return `Enter a valid non-negative ${label} price.`;
    }
  }
  if (minimum && maximum && compareNonNegativeDecimals(minimum, maximum) > 0) {
    return "Minimum price must not exceed maximum price.";
  }
  if ((minimum || maximum) && !/^[A-Z]{3}$/.test(currency)) {
    return "Enter a three-letter currency code for the price range.";
  }
  if (!minimum && !maximum && currency) {
    return "Enter a minimum or maximum price to use a currency filter.";
  }
  return null;
}

function validateAreaFilters() {
  const minimum = areaMinInput.value.trim();
  const maximum = areaMaxInput.value.trim();
  const unit = areaUnitInput.value;
  const decimalPattern = /^(?:\d+(?:\.\d*)?|\.\d+)$/;
  for (const [value, label] of [[minimum, "minimum"], [maximum, "maximum"]]) {
    if (value && (!decimalPattern.test(value) || !Number.isFinite(Number(value)))) {
      return `Enter a valid non-negative ${label} living area.`;
    }
  }
  if (minimum && maximum && compareNonNegativeDecimals(minimum, maximum) > 0) {
    return "Minimum living area must not exceed maximum living area.";
  }
  if ((minimum || maximum) && !Object.hasOwn(publicLabels.areaUnit, unit)) {
    return "Choose a living area unit for the area range.";
  }
  if (!minimum && !maximum && unit) {
    return "Enter a minimum or maximum living area to use a unit filter.";
  }
  return null;
}

function validateNumericFilters() {
  for (const range of numericFilterRanges) {
    const minimum = range.minimum.value.trim();
    const maximum = range.maximum.value.trim();
    const pattern = range.integer ? /^\d+$/ : /^(?:\d+(?:\.\d*)?|\.\d+)$/;
    for (const [value, bound] of [[minimum, "minimum"], [maximum, "maximum"]]) {
      if (value && (!pattern.test(value) || !Number.isFinite(Number(value))
          || (range.integer && (Number(value) < (range.lowerBound ?? 0)
            || Number(value) > 2147483647)))) {
        return range.integer
          ? `Enter a whole-number ${bound} ${range.label} between ${range.lowerBound ?? 0} and 2147483647.`
          : `Enter a valid non-negative ${bound} ${range.label} count.`;
      }
    }
    if (minimum && maximum && compareNonNegativeDecimals(minimum, maximum) > 0) {
      return `Minimum ${range.label} must not exceed maximum ${range.label}.`;
    }
  }
  return null;
}

function validatePageSize() {
  const value = pageSizeInput.value.trim();
  if (value && (!/^\d+$/.test(value) || Number(value) < 1 || Number(value) > 100)) {
    return "Enter a whole-number page size between 1 and 100, or leave blank for 20.";
  }
  return null;
}

function buildFilteredCollectionUrl() {
  const query = new URLSearchParams();
  const city = cityInput.value.trim();
  if (city) {
    query.set("city", city);
  }
  const postalCode = postalInput.value.trim();
  if (postalCode) {
    query.set("postalCode", postalCode);
  }
  for (const intent of intentInputs) {
    if (intent.input.checked) {
      query.append("transactionIntent", intent.value);
    }
  }
  for (const choice of categoryInputs) {
    if (choice.input.checked) {
      query.append("propertyCategory", choice.value);
    }
  }

  for (const feature of featureInputs) {
    if (feature.input.checked) {
      query.append("propertyFeature", feature.value);
    }
  }

  for (const choice of commercialInputs) {
    if (choice.input.checked) {
      query.append("commercialSubtype", choice.value);
    }
  }

  for (const choice of furnishingInputs) {
    if (choice.input.checked) {
      query.append("furnishingQuality", choice.value);
    }
  }

  const minimum = priceMinInput.value.trim();
  const maximum = priceMaxInput.value.trim();
  if (minimum) {
    query.set("priceMin", minimum);
  }
  if (maximum) {
    query.set("priceMax", maximum);
  }
  if (minimum || maximum) {
    query.set("priceCurrency", priceCurrencyInput.value.trim().toUpperCase());
  }

  const areaMinimum = areaMinInput.value.trim();
  const areaMaximum = areaMaxInput.value.trim();
  if (areaMinimum) {
    query.set("livingAreaMin", areaMinimum);
  }
  if (areaMaximum) {
    query.set("livingAreaMax", areaMaximum);
  }
  if (areaMinimum || areaMaximum) {
    query.set("livingAreaUnit", areaUnitInput.value);
  }

  for (const range of numericFilterRanges) {
    const minimum = range.minimum.value.trim();
    const maximum = range.maximum.value.trim();
    if (minimum) {
      query.set(`${range.key}Min`, minimum);
    }
    if (maximum) {
      query.set(`${range.key}Max`, maximum);
    }
  }

  const pageSize = pageSizeInput.value.trim();
  if (pageSize) {
    query.set("pageSize", pageSize);
  }

  const search = query.toString();
  return search ? `${collectionUrl}?${search}` : collectionUrl;
}

function isListingId(value) {
  return typeof value === "string"
    && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}

async function fetchPublicResource(url) {
  const controller = new AbortController();
  let timer;
  const cancellation = new Promise((_, reject) => {
    cancelPublicRequest = () => {
      const error = new Error("Public API request canceled for history navigation.");
      error.name = "AbortError";
      reject(error);
      controller.abort();
    };
  });
  const timeout = new Promise((_, reject) => {
    timer = setTimeout(() => {
      const error = new Error("Public API request timed out.");
      error.name = "PublicRequestTimeout";
      reject(error);
      controller.abort();
    }, 15000);
  });
  const request = (async () => {
    const response = await fetch(url, { signal: controller.signal });
    const payload = response.ok ? await response.json() : null;
    return { response, payload };
  })();
  try {
    return await Promise.race([request, timeout, cancellation]);
  } finally {
    clearTimeout(timer);
    cancelPublicRequest = null;
  }
}

function isResponseObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function invalidPublicResponse() {
  const error = new Error("Invalid public API response.");
  error.name = "InvalidPublicResponse";
  return error;
}

function validateCollectionResponse(payload) {
  if (!isResponseObject(payload)
      || !Array.isArray(payload.items) || !payload.items.every(isResponseObject)
      || !Number.isInteger(payload.page) || payload.page < 1 || payload.page > 2147483647
      || !Number.isInteger(payload.pageSize) || payload.pageSize < 1 || payload.pageSize > 100
      || payload.items.length > payload.pageSize || typeof payload.hasMore !== "boolean") {
    throw invalidPublicResponse();
  }
}

function publicRequestError(error) {
  if (error?.name === "InvalidPublicResponse" || error?.name === "SyntaxError") {
    return "The API returned an invalid listing response. Try again.";
  }
  return error?.name === "PublicRequestTimeout"
    ? "The API request took longer than 15 seconds. Try again."
    : "Unable to reach the API. Check that Diyarak.Api is running.";
}

async function loadPublicListing() {
  if (isLoading) {
    return;
  }

  gridElement.replaceChildren();
  if (!isListingId(selectedListingId)) {
    setReadyState("Invalid listing link. Return to public listings.");
    loadButton.disabled = true;
    return;
  }

  isLoading = true;
  setLoadingState();
  statusElement.textContent = "Loading listing details...";
  try {
    const { response, payload: listing } = await fetchPublicResource(`${collectionUrl}/${encodeURIComponent(selectedListingId)}`);
    if (response.status === 404) {
      setReadyState("Listing not found or no longer public.");
      return;
    }
    if (!response.ok) {
      setErrorState(`Unable to load listing. HTTP ${response.status}.`);
      return;
    }

    if (!isResponseObject(listing) || !isListingId(listing.listingId)
        || listing.listingId.toLowerCase() !== selectedListingId.toLowerCase()) {
      throw invalidPublicResponse();
    }
    gridElement.append(createListingCard(listing, true));
    setReadyState("Listing details loaded.");
  } catch (error) {
    setErrorState(publicRequestError(error));
  } finally {
    isLoading = false;
  }
}

async function loadPublicListings(url = collectionUrl, historyMode = "replace") {
  if (isLoading) {
    return;
  }

  requestedUrl = url;
  searchLinkVersion++;
  searchLinkStatus.textContent = "";
  searchLinkStatus.hidden = true;
  const currentSearch = getSupportedSearch(new URL(url, window.location.origin).searchParams);
  pageJumpError.textContent = "";
  pageJumpError.hidden = true;
  pageJumpInput.value = currentSearch.get("page") ?? "1";
  filterSummary.textContent = describeAppliedFilters(currentSearch);
  renderFilterRemovalActions(currentSearch);
  const currentUrlSearch = getSupportedSearch(new URLSearchParams(window.location.search));
  if (historyMode === "push" && currentSearch.toString() !== currentUrlSearch.toString()) {
    window.history.pushState(null, "", collectionPageUrl(currentSearch));
  } else {
    window.history.replaceState(null, "", collectionPageUrl(currentSearch));
  }
  isLoading = true;
  previousUrl = null;
  nextUrl = null;
  pageElement.textContent = "";
  updatePagination();
  setLoadingState();
  gridElement.replaceChildren();

  try {
    const { response, payload } = await fetchPublicResource(requestedUrl);
    if (pendingHistorySearch !== null) {
      return;
    }

    if (!response.ok) {
      setErrorState(`Unable to load listings. HTTP ${response.status}.`);
      return;
    }

    validateCollectionResponse(payload);
    const items = payload.items;
    const linkHeader = response.headers.get("Link");
    previousUrl = getPageLink(linkHeader, "prev");
    nextUrl = payload.hasMore === true ? getPageLink(linkHeader, "next") : null;
    pageElement.textContent = `Page ${payload.page}`;
    pageJumpInput.value = String(payload.page);

    if (items.length === 0) {
      setReadyState("No public listings found. Try again later or adjust your filters.");
      return;
    }

    for (const listing of items) {
      gridElement.append(createListingCard(listing));
    }

    const listingCount = items.length;
    const listingLabel = listingCount === 1 ? "listing" : "listings";
    setReadyState(
      payload.hasMore
        ? `Loaded ${listingCount} ${listingLabel}. More pages are available.`
        : `Loaded ${listingCount} ${listingLabel}.`,
    );
  } catch (error) {
    if (pendingHistorySearch !== null) {
      return;
    }
    previousUrl = null;
    nextUrl = null;
    pageElement.textContent = "";
    setErrorState(publicRequestError(error));
  } finally {
    isLoading = false;
    updatePagination();
    if (pendingHistorySearch !== null) {
      await loadPendingHistorySearch();
    }
  }
}

function getPageLink(header, relation) {
  for (const match of (header ?? "").matchAll(/<([^>]+)>;\s*rel="(prev|next)"/g)) {
    if (match[2] !== relation) {
      continue;
    }

    try {
      const url = new URL(match[1], window.location.origin);
      if (url.origin === window.location.origin
          && url.pathname === collectionUrl
          && !url.username && !url.password && !url.hash) {
        return url.pathname + url.search;
      }
    } catch {
      // Ignore malformed navigation links.
    }
  }

  return null;
}

function updatePagination() {
  for (const button of filterRemovalButtons) {
    button.disabled = isLoading;
  }
  editFiltersButton.disabled = isLoading;
  copySearchButton.disabled = isLoading || isCopyingSearchLink;
  pageJumpFields.disabled = isLoading;
  filterFields.disabled = isLoading;
  previousButton.disabled = isLoading || !previousUrl;
  nextButton.disabled = isLoading || !nextUrl;
}

function createListingCard(listing, showDetails = false) {
  const fragment = template.content.cloneNode(true);
  const card = fragment.querySelector(".listing-card");
  const property = listing.property ?? {};
  const location = property.location ?? {};
  const detailLink = card.querySelector(".listing-card__detail-link");
  detailLink.hidden = showDetails || !isListingId(listing.listingId);
  if (!detailLink.hidden) {
    const detailParameters = new URLSearchParams({ listingId: listing.listingId });
    const returnSearch = getSupportedSearch(new URL(requestedUrl, window.location.origin).searchParams).toString();
    if (returnSearch) {
      detailParameters.set("returnSearch", returnSearch);
    }
    detailLink.href = `./index.html?${detailParameters}`;
  }

  for (const [field, value] of [
    ["publishingRole", listing.context?.publishingRole],
    ["transactionIntent", listing.context?.transactionIntent],
    ["furnishingQuality", property.furnishingQuality],
    ["commercialSubtype", property.commercialSubtype],
  ]) {
    const fact = card.querySelector(`[data-detail="${field}"]`);
    fact.hidden = !showDetails;
    fact.querySelector("dd").textContent = formatLabel(field, value);
  }

  card.querySelector(".listing-card__location").textContent = formatLocation(location);
  card.querySelector(".listing-card__headline").textContent = listing.headline ?? "Untitled listing";
  card.querySelector(".listing-card__price").textContent = formatPrice(listing.price);
  card.querySelector("[data-field='category']").textContent = formatLabel("category", property.category);
  card.querySelector("[data-field='livingArea']").textContent = formatLivingArea(property.livingArea);
  card.querySelector("[data-field='rooms']").textContent = formatNumber(property.totalRooms);
  card.querySelector("[data-field='bedrooms']").textContent = formatNumber(property.bedroomCount);
  card.querySelector("[data-field='bathrooms']").textContent = formatNumber(property.bathroomCount);
  card.querySelector("[data-field='features']").textContent = formatFeatures(property.features);
  card.querySelector("[data-field='constructionYear']").textContent = formatYear(property.constructionYear);
  card.querySelector("[data-field='parking']").textContent = formatNumber(property.parkingSpaceCount);
  card.querySelector("[data-field='available']").textContent = formatDate(listing.availableFromDate);

  return fragment;
}

function formatLocation(location) {
  const parts = [location.city, location.postalCode].filter(Boolean);
  return parts.length === 0 ? "Location unknown" : parts.join(", ");
}

function formatPrice(price) {
  if (!price) {
    return "Price unavailable";
  }

  if (price.isOnRequest === true) {
    return "Price on request";
  }

  if (price.isOnRequest !== false
      || !Number.isFinite(price.amount) || price.amount < 0
      || typeof price.currency !== "string" || !/^[A-Z]{3}$/.test(price.currency)) {
    return "Price unavailable";
  }

  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: price.currency,
      maximumFractionDigits: 20,
    }).format(price.amount);
  } catch {
    return `${formatNumber(price.amount)} ${price.currency}`;
  }
}

function formatLabel(field, value) {
  if (!Object.hasOwn(publicLabels, field)) {
    return "Unknown";
  }
  const labels = publicLabels[field];
  return typeof value === "string" && Object.hasOwn(labels, value)
    ? labels[value]
    : "Unknown";
}

function formatLivingArea(livingArea) {
  if (!livingArea || !Number.isFinite(livingArea.value)) {
    return "Unknown";
  }

  const amount = formatNumber(livingArea.value);
  const unit = formatLabel("areaUnit", livingArea.unit);
  return unit === "Unknown" ? `${amount} (unit unknown)` : `${amount} ${unit}`;
}

function formatNumber(value) {
  return Number.isFinite(value) ? numberFormatter.format(value) : "Unknown";
}

function formatYear(value) {
  return Number.isInteger(value) ? String(value) : "Unknown";
}

function formatFeatures(features) {
  if (!Array.isArray(features) || features.length === 0) {
    return "None listed";
  }

  return features.map(value => formatLabel("features", value)).join(", ");
}

function formatDate(value) {
  if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)
      || value.startsWith("0000")) {
    return "Unknown";
  }

  const date = new Date(`${value}T00:00:00Z`);
  if (Number.isNaN(date.getTime()) || date.toISOString().slice(0, 10) !== value) {
    return "Unknown";
  }

  return date.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  });
}

function setLoadingState() {
  gridElement.setAttribute("aria-busy", "true");
  loadButton.disabled = true;
  retryButton.hidden = true;
  statusElement.dataset.state = "loading";
  statusElement.textContent = "Loading public listings...";
}

function setReadyState(message) {
  gridElement.setAttribute("aria-busy", "false");
  loadButton.disabled = false;
  retryButton.hidden = true;
  statusElement.dataset.state = "ready";
  statusElement.textContent = message;
}

function setErrorState(message) {
  gridElement.setAttribute("aria-busy", "false");
  loadButton.disabled = false;
  retryButton.hidden = false;
  statusElement.dataset.state = "error";
  statusElement.textContent = message;
}
