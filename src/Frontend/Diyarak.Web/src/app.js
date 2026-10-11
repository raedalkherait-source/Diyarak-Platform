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
const pageElement = document.querySelector("#current-page");
const paginationElement = document.querySelector("#listing-pagination");
const headingElement = document.querySelector("#listing-heading");
const backLink = document.querySelector("#back-to-listings");
const filterForm = document.querySelector("#listing-filters");
const filterFields = document.querySelector("#listing-filter-fields");
const cityInput = document.querySelector("#filter-city");
const intentInput = document.querySelector("#filter-intent");
const categoryInput = document.querySelector("#filter-category");
const priceMinInput = document.querySelector("#filter-price-min");
const priceMaxInput = document.querySelector("#filter-price-max");
const priceCurrencyInput = document.querySelector("#filter-price-currency");
const areaMinInput = document.querySelector("#filter-area-min");
const areaMaxInput = document.querySelector("#filter-area-max");
const areaUnitInput = document.querySelector("#filter-area-unit");
const filterError = document.querySelector("#filter-error");
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

loadButton.addEventListener("click", async () => {
  await loadSelectedView();
});

retryButton.addEventListener("click", async () => {
  await loadSelectedView(requestedUrl);
});

previousButton.addEventListener("click", async () => {
  if (previousUrl) {
    await loadPublicListings(previousUrl);
  }
});

nextButton.addEventListener("click", async () => {
  if (nextUrl) {
    await loadPublicListings(nextUrl);
  }
});

filterForm.addEventListener("submit", async event => {
  event.preventDefault();
  if (isLoading || isDetailView) {
    return;
  }

  const validationError = validatePriceFilters() ?? validateAreaFilters();
  filterError.textContent = validationError ?? "";
  filterError.hidden = !validationError;
  if (validationError) {
    return;
  }

  firstPageUrl = buildFilteredCollectionUrl();
  await loadPublicListings(firstPageUrl);
});

clearFiltersButton.addEventListener("click", async () => {
  if (isLoading || isDetailView) {
    return;
  }

  cityInput.value = "";
  intentInput.value = "";
  categoryInput.value = "";
  priceMinInput.value = "";
  priceMaxInput.value = "";
  priceCurrencyInput.value = "";
  areaMinInput.value = "";
  areaMaxInput.value = "";
  areaUnitInput.value = "";
  filterError.textContent = "";
  filterError.hidden = true;
  firstPageUrl = collectionUrl;
  await loadPublicListings(firstPageUrl);
});

cityInput.value = initialSearch.get("city") ?? "";
intentInput.value = initialSearch.get("transactionIntent") ?? "";
categoryInput.value = initialSearch.get("propertyCategory") ?? "";
priceMinInput.value = initialSearch.get("priceMin") ?? "";
priceMaxInput.value = initialSearch.get("priceMax") ?? "";
priceCurrencyInput.value = initialSearch.get("priceCurrency") ?? "";
areaMinInput.value = initialSearch.get("livingAreaMin") ?? "";
areaMaxInput.value = initialSearch.get("livingAreaMax") ?? "";
areaUnitInput.value = initialSearch.get("livingAreaUnit") ?? "";
backLink.href = collectionPageUrl(initialSearch);
filterForm.hidden = isDetailView;
paginationElement.hidden = isDetailView;
backLink.hidden = !isDetailView;
headingElement.textContent = isDetailView ? "Listing details" : "Public listings";
loadButton.textContent = isDetailView ? "Refresh listing" : "Refresh listings";
await loadSelectedView(initialCollectionUrl);

async function loadSelectedView(url = firstPageUrl) {
  if (isDetailView) {
    await loadPublicListing();
  } else {
    await loadPublicListings(url);
  }
}

function getSupportedSearch(query) {
  const supported = new URLSearchParams();
  for (const [key, value] of query) {
    if (["city", "transactionIntent", "propertyCategory", "priceMin", "priceMax", "priceCurrency", "livingAreaMin", "livingAreaMax", "livingAreaUnit", "page", "pageSize"].includes(key)) {
      supported.append(key, value);
    }
  }
  return supported;
}

function collectionRequestUrl(query) {
  const search = query.toString();
  return search ? `${collectionUrl}?${search}` : collectionUrl;
}

function collectionPageUrl(query) {
  const search = query.toString();
  return search ? `./index.html?${search}` : "./index.html";
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
  if (minimum && maximum && Number(minimum) > Number(maximum)) {
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
  if (minimum && maximum && Number(minimum) > Number(maximum)) {
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

function buildFilteredCollectionUrl() {
  const query = new URLSearchParams();
  const city = cityInput.value.trim();
  if (city) {
    query.set("city", city);
  }
  if (intentInput.value) {
    query.set("transactionIntent", intentInput.value);
  }
  if (categoryInput.value) {
    query.set("propertyCategory", categoryInput.value);
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

  const search = query.toString();
  return search ? `${collectionUrl}?${search}` : collectionUrl;
}

function isListingId(value) {
  return typeof value === "string"
    && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
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
    const response = await fetch(`${collectionUrl}/${encodeURIComponent(selectedListingId)}`);
    if (response.status === 404) {
      setReadyState("Listing not found or no longer public.");
      return;
    }
    if (!response.ok) {
      setErrorState(`Unable to load listing. HTTP ${response.status}.`);
      return;
    }

    const listing = await response.json();
    gridElement.append(createListingCard(listing, true));
    setReadyState("Listing details loaded.");
  } catch {
    setErrorState("Unable to reach the API. Check that Diyarak.Api is running.");
  } finally {
    isLoading = false;
  }
}

async function loadPublicListings(url = collectionUrl) {
  if (isLoading) {
    return;
  }

  requestedUrl = url;
  const currentSearch = getSupportedSearch(new URL(url, window.location.origin).searchParams);
  window.history.replaceState(null, "", collectionPageUrl(currentSearch));
  isLoading = true;
  previousUrl = null;
  nextUrl = null;
  pageElement.textContent = "";
  updatePagination();
  setLoadingState();
  gridElement.replaceChildren();

  try {
    const response = await fetch(requestedUrl);

    if (!response.ok) {
      setErrorState(`Unable to load listings. HTTP ${response.status}.`);
      return;
    }

    const payload = await response.json();
    const items = Array.isArray(payload.items) ? payload.items : [];
    const linkHeader = response.headers.get("Link");
    previousUrl = getPageLink(linkHeader, "prev");
    nextUrl = payload.hasMore === true ? getPageLink(linkHeader, "next") : null;
    pageElement.textContent = `Page ${payload.page}`;

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
  } catch {
    previousUrl = null;
    nextUrl = null;
    pageElement.textContent = "";
    setErrorState("Unable to reach the API. Check that Diyarak.Api is running.");
  } finally {
    isLoading = false;
    updatePagination();
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
  loadButton.disabled = true;
  retryButton.hidden = true;
  statusElement.dataset.state = "loading";
  statusElement.textContent = "Loading public listings...";
}

function setReadyState(message) {
  loadButton.disabled = false;
  retryButton.hidden = true;
  statusElement.dataset.state = "ready";
  statusElement.textContent = message;
}

function setErrorState(message) {
  loadButton.disabled = false;
  retryButton.hidden = false;
  statusElement.dataset.state = "error";
  statusElement.textContent = message;
}
