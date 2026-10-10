const statusElement = document.querySelector("#status");
const gridElement = document.querySelector("#listing-grid");
const template = document.querySelector("#listing-card-template");
const loadButton = document.querySelector("#load-listings");
const retryButton = document.querySelector("#retry-listings");

loadButton.addEventListener("click", async () => {
  await loadPublicListings();
});

retryButton.addEventListener("click", async () => {
  await loadPublicListings();
});

await loadPublicListings();

async function loadPublicListings() {
  setLoadingState();
  gridElement.replaceChildren();

  try {
    const response = await fetch("/api/market/public/listings");

    if (!response.ok) {
      setErrorState(`Unable to load listings. HTTP ${response.status}.`);
      return;
    }

    const payload = await response.json();
    const items = Array.isArray(payload.items) ? payload.items : [];

    if (items.length === 0) {
      setReadyState("No public listings found.");
      return;
    }

    for (const listing of items) {
      gridElement.append(createListingCard(listing));
    }

    setReadyState(payload.hasMore ? "Loaded listings. More pages are available." : "Loaded listings.");
  } catch {
    setErrorState("Unable to reach the API. Check that Diyarak.Api is running.");
  }
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
  return parts.length === 0 ? "Location unknown" : parts.join(" ");
}

function formatPrice(price) {
  if (!price || price.isOnRequest) {
    return "Price on request";
  }

  if (price.amount === null || price.amount === undefined || !price.currency) {
    return "Price unavailable";
  }

  return `${price.amount} ${price.currency}`;
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
