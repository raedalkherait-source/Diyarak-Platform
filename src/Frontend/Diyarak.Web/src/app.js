const statusElement = document.querySelector("#status");
const gridElement = document.querySelector("#listing-grid");
const template = document.querySelector("#listing-card-template");
const loadButton = document.querySelector("#load-listings");
const retryButton = document.querySelector("#retry-listings");
const previousButton = document.querySelector("#previous-page");
const nextButton = document.querySelector("#next-page");
const pageElement = document.querySelector("#current-page");
const collectionUrl = "/api/market/public/listings";
let requestedUrl = collectionUrl;
let previousUrl = null;
let nextUrl = null;
let isLoading = false;

loadButton.addEventListener("click", async () => {
  await loadPublicListings();
});

retryButton.addEventListener("click", async () => {
  await loadPublicListings(requestedUrl);
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

await loadPublicListings();

async function loadPublicListings(url = collectionUrl) {
  if (isLoading) {
    return;
  }

  requestedUrl = url;
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
  previousButton.disabled = isLoading || !previousUrl;
  nextButton.disabled = isLoading || !nextUrl;
}

function createListingCard(listing) {
  const fragment = template.content.cloneNode(true);
  const card = fragment.querySelector(".listing-card");
  const property = listing.property ?? {};
  const location = property.location ?? {};

  card.querySelector(".listing-card__location").textContent = formatLocation(location);
  card.querySelector(".listing-card__headline").textContent = listing.headline ?? "Untitled listing";
  card.querySelector(".listing-card__price").textContent = formatPrice(listing.price);
  card.querySelector("[data-field='category']").textContent = property.category ?? "Unknown";
  card.querySelector("[data-field='livingArea']").textContent = formatLivingArea(property.livingArea);
  card.querySelector("[data-field='rooms']").textContent = formatNumber(property.totalRooms);
  card.querySelector("[data-field='bedrooms']").textContent = formatNumber(property.bedroomCount);
  card.querySelector("[data-field='bathrooms']").textContent = formatNumber(property.bathroomCount);
  card.querySelector("[data-field='features']").textContent = formatFeatures(property.features);
  card.querySelector("[data-field='constructionYear']").textContent = formatNumber(property.constructionYear);
  card.querySelector("[data-field='parking']").textContent = formatNumber(property.parkingSpaceCount);
  card.querySelector("[data-field='available']").textContent = formatDate(listing.availableFromDate);

  return fragment;
}

function formatLocation(location) {
  const parts = [location.city, location.postalCode].filter(Boolean);
  return parts.length === 0 ? "Location unknown" : parts.join(", ");
}

function formatPrice(price) {
  if (!price || price.isOnRequest) {
    return "Price on request";
  }

  if (price.amount === null || price.amount === undefined || !price.currency) {
    return "Price unavailable";
  }

  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: price.currency,
      maximumFractionDigits: 0,
    }).format(price.amount);
  } catch {
    return `${price.amount} ${price.currency}`;
  }
}

function formatLivingArea(livingArea) {
  if (!livingArea || livingArea.value === null || livingArea.value === undefined) {
    return "Unknown";
  }

  return livingArea.unit ? `${livingArea.value} ${livingArea.unit}` : String(livingArea.value);
}

function formatNumber(value) {
  if (value === null || value === undefined) {
    return "Unknown";
  }

  return String(value);
}

function formatFeatures(features) {
  if (!Array.isArray(features) || features.length === 0) {
    return "None listed";
  }

  return features.join(", ");
}

function formatDate(value) {
  if (!value) {
    return "Unknown";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Unknown";
  }

  return date.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
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
