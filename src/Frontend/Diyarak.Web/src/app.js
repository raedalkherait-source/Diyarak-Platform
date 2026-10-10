const statusElement = document.querySelector("#status");
const gridElement = document.querySelector("#listing-grid");
const template = document.querySelector("#listing-card-template");
const loadButton = document.querySelector("#load-listings");

loadButton.addEventListener("click", async () => {
  await loadPublicListings();
});

async function loadPublicListings() {
  setStatus("Loading public listings...");
  gridElement.replaceChildren();

  try {
    const response = await fetch("/api/market/public/listings");

    if (!response.ok) {
      setStatus(`Unable to load listings. HTTP ${response.status}.`);
      return;
    }

    const payload = await response.json();
    const items = Array.isArray(payload.items) ? payload.items : [];

    if (items.length === 0) {
      setStatus("No public listings found.");
      return;
    }

    for (const listing of items) {
      gridElement.append(createListingCard(listing));
    }

    setStatus(payload.hasMore ? "Loaded listings. More pages are available." : "Loaded listings.");
  } catch {
    setStatus("Unable to reach the API.");
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
  card.querySelector("[data-field='rooms']").textContent = formatRooms(property);
  card.querySelector("[data-field='available']").textContent = listing.availableFromDate ?? "Unknown";

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

function formatRooms(property) {
  if (property.totalRooms === null || property.totalRooms === undefined) {
    return "Unknown";
  }

  return String(property.totalRooms);
}

function setStatus(message) {
  statusElement.textContent = message;
}
