const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const source = fs.readFileSync(path.join(__dirname,
  "../../src/Frontend/Diyarak.Web/src/app.js"), "utf8");
const endpoint = "/api/market/public/listings";
const page2 = `${endpoint}?page=2&pageSize=1&city=Berlin`;
const page1 = `${endpoint}?page=1&pageSize=1&city=Berlin`;
const link = (url, rel) => `<${url}>; rel="${rel}"`;

function response(page, hasMore, links = null, items = [{}]) {
  return { ok: true, headers: { get: () => links },
    json: async () => ({ page, hasMore, items }) };
}

async function createApp(responses, search = "") {
  const elements = new Map();
  function element() {
    return { disabled: false, hidden: false, textContent: "", dataset: {},
      children: [], listeners: {}, nodes: new Map(),
      addEventListener(event, callback) { this.listeners[event] = callback; },
      replaceChildren() { this.children = []; },
      append(child) { this.children.push(child); },
      querySelector(selector) {
        if (!this.nodes.has(selector)) this.nodes.set(selector, element());
        return this.nodes.get(selector);
      } };
  }
  for (const id of ["status", "listing-grid", "listing-card-template", "load-listings",
    "retry-listings", "previous-page", "next-page", "current-page",
    "listing-pagination", "listing-heading", "back-to-listings"]) {
    elements.set(`#${id}`, element());
  }
  elements.get("#listing-card-template").content = { cloneNode: element };
  const requests = [];
  const context = vm.createContext({ URL, URLSearchParams, Intl, Date,
    window: { location: { origin: "https://diyarak.example", search } },
    document: { querySelector: id => elements.get(id) },
    fetch: async url => {
      requests.push(url);
      const result = responses.shift();
      if (result instanceof Error) throw result;
      return typeof result === "function" ? result() : result;
    } });
  await vm.runInContext(`(async () => { ${source}\n })()`, context);
  return { requests, get: id => elements.get(`#${id}`),
    click: id => elements.get(`#${id}`).listeners.click() };
}

test("first, middle and final pages follow server links and preserve queries", async () => {
  const page3 = `${endpoint}?page=3&pageSize=1&city=Berlin`;
  const app = await createApp([
    response(1, true, link(page2, "next")),
    response(2, true, `${link(page1, "prev")}, ${link(page3, "next")}`),
    response(3, false, link(page2, "prev")),
    response(2, true, `${link(page1, "prev")}, ${link(page3, "next")}`),
  ]);
  assert.equal(app.get("previous-page").disabled, true);
  assert.equal(app.get("next-page").disabled, false);
  await app.click("next-page");
  assert.equal(app.get("current-page").textContent, "Page 2");
  assert.equal(app.get("previous-page").disabled, false);
  await app.click("next-page");
  assert.equal(app.get("next-page").disabled, true);
  await app.click("previous-page");
  assert.deepEqual(app.requests, [endpoint, page2, page3, page2]);
  assert.equal(app.get("listing-grid").children.length, 1);
});

test("HTTP failure disables navigation and retry requests the failed page", async () => {
  const app = await createApp([response(1, true, link(page2, "next")),
    { ok: false, status: 503 }, response(2, false, link(page1, "prev"))]);
  await app.click("next-page");
  assert.equal(app.get("status").dataset.state, "error");
  assert.equal(app.get("retry-listings").hidden, false);
  assert.equal(app.get("next-page").disabled, true);
  assert.equal(app.get("previous-page").disabled, true);
  await app.click("retry-listings");
  assert.deepEqual(app.requests, [endpoint, page2, page2]);
  assert.equal(app.get("current-page").textContent, "Page 2");
  assert.equal(app.get("retry-listings").hidden, true);
});

test("network failure can be retried on the same page", async () => {
  const app = await createApp([response(1, true, link(page2, "next")),
    new Error("offline"), response(2, false)]);
  await app.click("next-page");
  assert.equal(app.get("status").dataset.state, "error");
  await app.click("retry-listings");
  assert.deepEqual(app.requests, [endpoint, page2, page2]);
});

test("empty later page keeps a route back to the previous page", async () => {
  const app = await createApp([response(1, true, link(page2, "next")),
    response(2, false, link(page1, "prev"), [])]);
  await app.click("next-page");
  assert.equal(app.get("previous-page").disabled, false);
  assert.equal(app.get("next-page").disabled, true);
  assert.equal(app.get("listing-grid").children.length, 0);
  assert.match(app.get("status").textContent, /No public listings found/);
});

test("refresh returns to the initial collection", async () => {
  const app = await createApp([response(1, true, link(page2, "next")),
    response(2, false, link(page1, "prev")), response(1, false)]);
  await app.click("next-page");
  await app.click("load-listings");
  assert.deepEqual(app.requests, [endpoint, page2, endpoint]);
  assert.equal(app.get("current-page").textContent, "Page 1");
});

test("missing Link or false hasMore does not invent next-page navigation", async () => {
  for (const result of [response(1, true), response(1, false, link(page2, "next"))]) {
    const app = await createApp([result]);
    assert.equal(app.get("next-page").disabled, true);
    await app.click("next-page");
    assert.deepEqual(app.requests, [endpoint]);
  }
});

test("external, private, credential-bearing and malformed links are ignored", async () => {
  for (const url of ["https://other.example/api/market/public/listings?page=2",
    "/api/market/properties?page=2", "https://user@diyarak.example/api/market/public/listings",
    "http://[", `${page2}#fragment`]) {
    const app = await createApp([response(1, true, link(url, "next"))]);
    assert.equal(app.get("next-page").disabled, true, url);
  }
});

test("loading disables controls and duplicate clicks do not start another request", async () => {
  let resolveResponse;
  const pending = new Promise(resolve => { resolveResponse = resolve; });
  const app = await createApp([response(1, true, link(page2, "next")), () => pending]);
  const loading = app.click("next-page");
  assert.equal(app.get("next-page").disabled, true);
  assert.equal(app.get("previous-page").disabled, true);
  assert.equal(app.get("load-listings").disabled, true);
  await app.click("load-listings");
  await app.click("next-page");
  assert.deepEqual(app.requests, [endpoint, page2]);
  resolveResponse(response(2, false, link(page1, "prev")));
  await loading;
  assert.equal(app.get("load-listings").disabled, false);
  assert.equal(app.get("previous-page").disabled, false);
});

const listingId = "12345678-1234-1234-1234-123456789abc";
const detailUrl = `${endpoint}/${listingId}`;
const detailQuery = `?listingId=${listingId}`;
const detailResponse = listing => ({ ok: true, status: 200, json: async () => listing });
const cardOf = app => app.get("listing-grid").children[0].querySelector(".listing-card");

test("collection cards link to valid public detail identifiers only", async () => {
  const app = await createApp([response(1, false, null, [{ listingId }])]);
  const anchor = cardOf(app).querySelector(".listing-card__detail-link");
  assert.equal(anchor.hidden, false);
  assert.equal(anchor.href, `./index.html?listingId=${listingId}`);
  const invalid = await createApp([response(1, false, null, [{ listingId: "../private" }])]);
  assert.equal(cardOf(invalid).querySelector(".listing-card__detail-link").hidden, true);
});

test("direct detail URL loads the selected listing and renders public fields as text", async () => {
  const headline = "<img src=x onerror=alert(1)>";
  const app = await createApp([detailResponse({ listingId, headline,
    context: { publishingRole: "Owner", transactionIntent: "Rent" },
    property: { location: { city: "Berlin", postalCode: "10115", street: "PRIVATE" },
      furnishingQuality: "Standard", commercialSubtype: null, ownerUserId: "PRIVATE" } })], detailQuery);
  assert.deepEqual(app.requests, [detailUrl]);
  assert.equal(app.get("listing-pagination").hidden, true);
  assert.equal(app.get("back-to-listings").hidden, false);
  assert.equal(app.get("listing-heading").textContent, "Listing details");
  const card = cardOf(app);
  assert.equal(card.querySelector(".listing-card__headline").textContent, headline);
  assert.equal(card.querySelector(".listing-card__location").textContent, "Berlin, 10115");
  assert.equal(card.querySelector(".listing-card__detail-link").hidden, true);
  assert.equal(card.querySelector('[data-detail="transactionIntent"]').querySelector("dd").textContent, "Rent");
  assert.equal(card.querySelector('[data-detail="commercialSubtype"]').querySelector("dd").textContent, "Unknown");
  assert.equal(card.querySelector('[data-detail="publishingRole"]').hidden, false);
  assert.equal(app.get("status").dataset.state, "ready");
});

test("malformed and empty identifiers never trigger a detail request", async () => {
  for (const query of ["?listingId=", "?listingId=../private", "?listingId=wrong"]) {
    const app = await createApp([], query);
    assert.deepEqual(app.requests, []);
    assert.match(app.get("status").textContent, /Invalid listing link/);
    assert.equal(app.get("load-listings").disabled, true);
    assert.equal(app.get("retry-listings").hidden, true);
  }
});

test("404 detail displays not-found without a card or error retry", async () => {
  const app = await createApp([{ ok: false, status: 404 }], detailQuery);
  assert.equal(app.get("status").textContent, "Listing not found or no longer public.");
  assert.equal(app.get("listing-grid").children.length, 0);
  assert.equal(app.get("retry-listings").hidden, true);
  assert.equal(app.get("back-to-listings").hidden, false);
});

test("detail HTTP and network failures retry the same listing", async () => {
  for (const failure of [{ ok: false, status: 503 }, new Error("offline")]) {
    const app = await createApp([failure, detailResponse({ listingId })], detailQuery);
    assert.equal(app.get("status").dataset.state, "error");
    assert.equal(app.get("retry-listings").hidden, false);
    await app.click("retry-listings");
    assert.deepEqual(app.requests, [detailUrl, detailUrl]);
    assert.equal(app.get("retry-listings").hidden, true);
    assert.equal(app.get("listing-grid").children.length, 1);
  }
});

test("detail refresh removes a stale card when the listing becomes non-public", async () => {
  const app = await createApp([detailResponse({ listingId }), { ok: false, status: 404 }], detailQuery);
  await app.click("load-listings");
  assert.deepEqual(app.requests, [detailUrl, detailUrl]);
  assert.equal(app.get("listing-grid").children.length, 0);
  assert.match(app.get("status").textContent, /no longer public/);
});

test("detail refresh cannot start overlapping requests", async () => {
  let resolveResponse;
  const pending = new Promise(resolve => { resolveResponse = resolve; });
  const app = await createApp([detailResponse({ listingId }), () => pending], detailQuery);
  const loading = app.click("load-listings");
  assert.equal(app.get("load-listings").disabled, true);
  await app.click("load-listings");
  assert.deepEqual(app.requests, [detailUrl, detailUrl]);
  resolveResponse(detailResponse({ listingId }));
  await loading;
  assert.equal(app.get("load-listings").disabled, false);
});
