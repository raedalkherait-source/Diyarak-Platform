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
    return { disabled: false, hidden: false, value: "", textContent: "", dataset: {},
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
    "listing-pagination", "listing-heading", "back-to-listings",
    "listing-filters", "listing-filter-fields", "filter-city", "filter-intent", "filter-category", "clear-filters", "filter-price-min", "filter-price-max", "filter-price-currency", "filter-error", "filter-area-min", "filter-area-max", "filter-area-unit", "filter-rooms-min", "filter-rooms-max", "filter-bedrooms-min", "filter-bedrooms-max", "filter-bathrooms-min", "filter-bathrooms-max", "filter-year-min", "filter-year-max", "filter-parking-min", "filter-parking-max", "filter-furnishing", "filter-commercial"]) {
    elements.set(`#${id}`, element());
  }
  elements.get("#listing-card-template").content = { cloneNode: element };
  const requests = [];
  const historyUrls = [];
  const context = vm.createContext({ URL, URLSearchParams, Intl, Date,
    window: { location: { origin: "https://diyarak.example", search },
      history: { replaceState: (_state, _title, url) => historyUrls.push(url) } },
    document: { querySelector: id => elements.get(id) },
    fetch: async url => {
      requests.push(url);
      const result = responses.shift();
      if (result instanceof Error) throw result;
      return typeof result === "function" ? result() : result;
    } });
  await vm.runInContext(`(async () => { ${source}\n globalThis.formatters = {formatLabel, formatLivingArea, formatNumber, formatYear, formatFeatures, formatPrice, formatDate}; })()`, context);
  return { requests, historyUrls, formatters: context.formatters, get: id => elements.get(`#${id}`),
    submit: () => elements.get("#listing-filters").listeners.submit({ preventDefault() {} }),
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
  assert.equal(card.querySelector('[data-detail="transactionIntent"]').querySelector("dd").textContent, "For rent");
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

test("public enum labels cover all currently supported domain values", async () => {
  const app = await createApp([response(1, false, null, [])]);
  const expected = {
    category: { Apartment: "Apartment", House: "House", Plot: "Plot",
      InvestmentProperty: "Investment property", GarageOrParkingSpace: "Garage or parking space",
      CommercialProperty: "Commercial property" },
    features: { FittedKitchen: "Fitted kitchen", Elevator: "Elevator", BalconyOrTerrace: "Balcony or terrace",
      GuestToilet: "Guest toilet", GardenOrSharedGardenUse: "Garden or shared garden use", Basement: "Basement",
      StepFreeAccess: "Step-free access", SuitableForVacationRental: "Suitable for vacation rental",
      HistoricMonument: "Historic monument", GarageOrParkingSpace: "Garage or parking space" },
    publishingRole: { Owner: "Owner", Tenant: "Tenant", ProfessionalOrAgent: "Professional or agent" },
    transactionIntent: { Rent: "For rent", Sell: "For sale", RentForLimitedPeriod: "For rent for a limited period" },
    furnishingQuality: { Simple: "Simple", Normal: "Normal", Upscale: "Upscale", Luxury: "Luxury" },
    commercialSubtype: { OfficeOrPractice: "Office or practice", RestaurantOrHotel: "Restaurant or hotel",
      HallOrProduction: "Hall or production", Retail: "Retail", CommercialPlot: "Commercial plot" },
    areaUnit: { SquareMeter: "m²", SquareFoot: "ft²", Hectare: "ha", Dunum: "dunum" },
  };
  for (const [field, values] of Object.entries(expected)) {
    for (const [value, label] of Object.entries(values)) {
      assert.equal(app.formatters.formatLabel(field, value), label, `${field}/${value}`);
    }
  }
});

test("unknown enums and inherited property names never become display labels", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  for (const value of [null, undefined, "", "FutureValue", "toString", "constructor", "__proto__", 1]) {
    assert.equal(f.formatLabel("features", value), "Unknown");
  }
  assert.equal(f.formatLabel("toString", "Owner"), "Unknown");
  assert.equal(f.formatLabel("missing", "Owner"), "Unknown");
});

test("areas keep their values while units become symbols without conversion", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  const number = new Intl.NumberFormat(undefined, {maximumFractionDigits: 20});
  for (const [unit, symbol] of [["SquareMeter", "m²"], ["SquareFoot", "ft²"], ["Hectare", "ha"], ["Dunum", "dunum"]]) {
    assert.equal(f.formatLivingArea({value: 82.5, unit}), `${number.format(82.5)} ${symbol}`);
  }
  assert.equal(f.formatLivingArea({value: 0, unit: "SquareMeter"}), `${number.format(0)} m²`);
  for (const unit of [null, undefined, "FutureUnit"]) {
    assert.equal(f.formatLivingArea({value: 12.25, unit}), `${number.format(12.25)} (unit unknown)`);
  }
  for (const value of [null, undefined, NaN, Infinity, "82.5"]) {
    assert.equal(f.formatLivingArea({value, unit: "SquareMeter"}), "Unknown");
  }
});

test("numeric counts preserve fractions and zero while years remain ungrouped", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  const number = new Intl.NumberFormat(undefined, {maximumFractionDigits: 20});
  for (const value of [0, 3.5, 1234.56789]) assert.equal(f.formatNumber(value), number.format(value));
  for (const value of [null, undefined, Infinity, NaN, "3.5"]) assert.equal(f.formatNumber(value), "Unknown");
  assert.equal(f.formatYear(1998), "1998");
  assert.equal(f.formatYear(null), "Unknown");
  assert.equal(f.formatYear(1998.5), "Unknown");
});

test("feature labels retain order and distinguish empty from unknown", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  assert.equal(f.formatFeatures(["FittedKitchen", "BalconyOrTerrace"]), "Fitted kitchen, Balcony or terrace");
  assert.equal(f.formatFeatures(["FutureFeature"]), "Unknown");
  for (const value of [[], null, undefined]) assert.equal(f.formatFeatures(value), "None listed");
});

test("collection and detail render the same formatted property facts", async () => {
  const listing = {listingId, context:{transactionIntent:"Sell",publishingRole:"ProfessionalOrAgent"},
    property:{category:"InvestmentProperty",livingArea:{value:82.5,unit:"SquareMeter"},
      features:["FittedKitchen","BalconyOrTerrace"],totalRooms:3.5,parkingSpaceCount:0,constructionYear:1998}};
  const collection = await createApp([response(1, false, null, [listing])]);
  const detail = await createApp([detailResponse(listing)], detailQuery);
  for (const app of [collection, detail]) {
    const card = cardOf(app);
    assert.equal(card.querySelector("[data-field='category']").textContent,"Investment property");
    assert.equal(card.querySelector("[data-field='features']").textContent,"Fitted kitchen, Balcony or terrace");
    assert.equal(card.querySelector("[data-field='constructionYear']").textContent,"1998");
    assert.equal(card.querySelector("[data-field='parking']").textContent,new Intl.NumberFormat().format(0));
  }
  assert.equal(cardOf(detail).querySelector('[data-detail="transactionIntent"]').querySelector("dd").textContent,"For sale");
  assert.equal(cardOf(detail).querySelector('[data-detail="publishingRole"]').querySelector("dd").textContent,"Professional or agent");
});

test("fractional and zero prices retain amounts instead of whole-number rounding", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  for (const currency of ["EUR", "USD", "SYP", "JPY", "KWD", "ZZZ"]) {
    for (const amount of [0, 1200.75, 12.3456]) {
      const expected = new Intl.NumberFormat(undefined, {style:"currency",currency,maximumFractionDigits:20}).format(amount);
      assert.equal(f.formatPrice({isOnRequest:false,amount,currency}), expected);
    }
  }
});

test("on-request prices require an explicit true flag", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  assert.equal(f.formatPrice({isOnRequest:true,amount:null,currency:null}), "Price on request");
  for (const price of [null, undefined, {}, {isOnRequest:null,amount:100,currency:"EUR"},
    {isOnRequest:"true",amount:100,currency:"EUR"}, {amount:100,currency:"EUR"}]) {
    assert.equal(f.formatPrice(price), "Price unavailable");
  }
});

test("incomplete and invalid known prices are unavailable", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  for (const amount of [null, undefined, -1, NaN, Infinity, "100"]) {
    assert.equal(f.formatPrice({isOnRequest:false,amount,currency:"EUR"}), "Price unavailable");
  }
  for (const currency of [null, undefined, "", "EU", "EURO", "eur", "<script>", 123]) {
    assert.equal(f.formatPrice({isOnRequest:false,amount:100,currency}), "Price unavailable");
  }
});

test("valid available dates keep their calendar day across time zones", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  const expected = new Intl.DateTimeFormat(undefined,{year:"numeric",month:"short",day:"numeric",timeZone:"UTC"});
  for (const value of ["2026-01-01", "2026-03-29", "2026-10-25", "2024-02-29", "0001-01-01", "9999-12-31"]) {
    assert.equal(f.formatDate(value), expected.format(new Date(`${value}T00:00:00Z`)), value);
  }
});

test("missing, malformed and impossible calendar dates remain unknown", async () => {
  const {formatters: f} = await createApp([response(1, false, null, [])]);
  for (const value of [null, undefined, "", 20260101, "2026-2-1", "2026-02-29", "2026-04-31",
    "2026-13-01", "2026-00-01", "2026-01-00", "0000-01-01", "10000-01-01", "2026-01-01T00:00:00Z"]) {
    assert.equal(f.formatDate(value), "Unknown", String(value));
  }
});

test("collection and detail show the same fractional price and available day", async () => {
  const listing = {listingId,price:{isOnRequest:false,amount:1200.75,currency:"EUR"},availableFromDate:"2026-01-01"};
  const collection = await createApp([response(1,false,null,[listing])]);
  const detail = await createApp([detailResponse(listing)],detailQuery);
  const expectedPrice = new Intl.NumberFormat(undefined,{style:"currency",currency:"EUR",maximumFractionDigits:20}).format(1200.75);
  const expectedDate = new Intl.DateTimeFormat(undefined,{year:"numeric",month:"short",day:"numeric",timeZone:"UTC"}).format(new Date("2026-01-01T00:00:00Z"));
  for (const app of [collection,detail]) {
    assert.equal(cardOf(app).querySelector(".listing-card__price").textContent,expectedPrice);
    assert.equal(cardOf(app).querySelector("[data-field='available']").textContent,expectedDate);
  }
});

test("filter submit trims and encodes city and sends supported API enum values", async () => {
  const app = await createApp([response(1,false), response(1,false)]);
  app.get("filter-city").value = "  دمشق & حمص  ";
  app.get("filter-intent").value = "Sell";
  app.get("filter-category").value = "Apartment";
  await app.submit();
  const url = new URL(app.requests[1], "https://diyarak.example");
  assert.equal(url.pathname,endpoint);
  assert.equal(url.searchParams.get("city"),"دمشق & حمص");
  assert.equal(url.searchParams.get("transactionIntent"),"Sell");
  assert.equal(url.searchParams.get("propertyCategory"),"Apartment");
  assert.deepEqual([...url.searchParams.keys()],["city","transactionIntent","propertyCategory"]);
  assert.equal(url.searchParams.has("page"),false);
});

test("empty filter values are omitted", async () => {
  const app = await createApp([response(1,false), response(1,false)]);
  app.get("filter-city").value = "   ";
  await app.submit();
  assert.deepEqual(app.requests,[endpoint,endpoint]);
});

test("filtered pagination and refresh retain applied filters while refresh resets the page", async () => {
  const filtered = `${endpoint}?city=Berlin`;
  const filteredPage2 = `${endpoint}?city=Berlin&page=2&pageSize=1`;
  const app = await createApp([response(1,false), response(1,true,link(filteredPage2,"next")),
    response(2,false,link(`${filtered}&page=1&pageSize=1`,"prev")),response(1,false)]);
  app.get("filter-city").value = "Berlin";
  await app.submit();
  await app.click("next-page");
  assert.equal(app.get("current-page").textContent,"Page 2");
  app.get("filter-city").value = "Unsubmitted change";
  await app.click("load-listings");
  assert.deepEqual(app.requests,[endpoint,filtered,filteredPage2,filtered]);
  assert.equal(app.get("current-page").textContent,"Page 1");
});

test("filter errors retry the same submitted request even after form edits", async () => {
  const app = await createApp([response(1,false),{ok:false,status:400},response(1,false)]);
  app.get("filter-city").value="Berlin";
  app.get("filter-intent").value="Rent";
  await app.submit();
  assert.equal(app.get("status").dataset.state,"error");
  assert.equal(app.get("listing-filter-fields").disabled,false);
  app.get("filter-city").value="Paris";
  await app.click("retry-listings");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?city=Berlin&transactionIntent=Rent`,`${endpoint}?city=Berlin&transactionIntent=Rent`]);
});

test("clear filters resets the inputs and reloads the unfiltered first page", async () => {
  const app = await createApp([response(1,false),response(1,false),response(1,false)]);
  app.get("filter-city").value="Berlin";
  app.get("filter-intent").value="Sell";
  app.get("filter-category").value="House";
  await app.submit();
  await app.click("clear-filters");
  assert.equal(app.requests.at(-1),endpoint);
  for(const id of ["filter-city","filter-intent","filter-category"]) assert.equal(app.get(id).value,"");
});

test("loading disables the filter fieldset and ignores apply or clear requests", async () => {
  let resolveResponse;
  const pending = new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-city").value="Berlin";
  const loading=app.submit();
  assert.equal(app.get("listing-filter-fields").disabled,true);
  await app.submit();
  await app.click("clear-filters");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?city=Berlin`]);
  assert.equal(app.get("filter-city").value,"Berlin");
  resolveResponse(response(1,false,null,[]));
  await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
  assert.match(app.get("status").textContent,/No public listings/);
});

test("detail views hide filters and do not execute filter requests", async () => {
  const app=await createApp([detailResponse({listingId})],detailQuery);
  assert.equal(app.get("listing-filters").hidden,true);
  app.get("filter-city").value="Berlin";
  await app.submit();
  await app.click("clear-filters");
  assert.deepEqual(app.requests,[detailUrl]);
});

test("bookmarked collection URL restores filters and page coordinates", async () => {
  const search="?city=Luebeck&transactionIntent=Sell&propertyCategory=Apartment&page=3&pageSize=5";
  const app=await createApp([response(3,false)],search);
  assert.deepEqual(app.requests,[`${endpoint}${search}`]);
  assert.equal(app.get("filter-city").value,"Luebeck");
  assert.equal(app.get("filter-intent").value,"Sell");
  assert.equal(app.get("filter-category").value,"Apartment");
  assert.equal(app.get("current-page").textContent,"Page 3");
  assert.deepEqual(app.historyUrls,[`./index.html${search}`]);
});

test("collection URL follows pagination and can restore a browser reload", async () => {
  const next=`${endpoint}?city=Berlin&page=2&pageSize=1`;
  const app=await createApp([response(1,true,link(next,"next")),response(2,false)],"?city=Berlin");
  await app.click("next-page");
  const current=new URL(app.historyUrls.at(-1),"https://diyarak.example/app/");
  assert.equal(current.searchParams.get("page"),"2");
  assert.equal(current.searchParams.get("city"),"Berlin");
  const reloaded=await createApp([response(2,false)],current.search);
  assert.deepEqual(reloaded.requests,[next]);
  assert.equal(reloaded.get("filter-city").value,"Berlin");
});

test("card detail links and Back to listings retain the exact supported search", async () => {
  const query=new URLSearchParams({city:"دمشق & حمص",transactionIntent:"Rent",propertyCategory:"House",page:"2",pageSize:"5"});
  const collection=await createApp([response(2,false,null,[{listingId}])],`?${query}`);
  const anchor=cardOf(collection).querySelector(".listing-card__detail-link");
  const detailLink=new URL(anchor.href,"https://diyarak.example/app/");
  assert.equal(detailLink.searchParams.get("listingId"),listingId);
  assert.equal(detailLink.searchParams.get("returnSearch"),query.toString());
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.deepEqual(detail.requests,[detailUrl]);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.deepEqual(detail.historyUrls,[]);
  const returned=await createApp([response(2,false)],`?${query}`);
  assert.deepEqual(returned.requests,[`${endpoint}?${query}`]);
});

test("untrusted return parameters cannot redirect away from the local collection", async () => {
  for (const returnSearch of ["https://evil.example/", "returnTo=https://evil.example&city=Berlin&page=2", "listingId=private&unknown=value"]) {
    const detailParams=new URLSearchParams({listingId,returnSearch});
    const app=await createApp([detailResponse({listingId})],`?${detailParams}`);
    const back=new URL(app.get("back-to-listings").href,"https://diyarak.example/app/");
    assert.equal(back.origin,"https://diyarak.example");
    assert.equal(back.pathname,"/app/index.html");
    assert.equal(back.searchParams.has("returnTo"),false);
    assert.equal(back.searchParams.has("listingId"),false);
    assert.equal(back.searchParams.has("unknown"),false);
  }
});

test("apply and clear update the URL without retaining old page coordinates", async () => {
  const app=await createApp([response(4,false),response(1,false),response(1,false)],"?city=Berlin&page=4&pageSize=5");
  app.get("filter-city").value="Paris";
  await app.submit();
  assert.equal(app.historyUrls.at(-1),"./index.html?city=Paris");
  await app.click("clear-filters");
  assert.equal(app.historyUrls.at(-1),"./index.html");
  assert.equal(app.requests.at(-1),endpoint);
});

test("Refresh starts on page one with saved filters and size, ignoring unsubmitted edits", async () => {
  const app=await createApp([response(4,false),response(1,false)],"?city=Berlin&page=4&pageSize=5");
  app.get("filter-city").value="Paris";
  assert.equal(app.historyUrls.at(-1),"./index.html?city=Berlin&page=4&pageSize=5");
  await app.click("load-listings");
  assert.equal(app.requests.at(-1),`${endpoint}?city=Berlin&pageSize=5`);
  assert.equal(app.historyUrls.at(-1),"./index.html?city=Berlin&pageSize=5");
});

test("failed bookmark requests remain in the URL and retry preserves them", async () => {
  const search="?city=Berlin&page=0";
  const app=await createApp([{ok:false,status:400},response(1,false)],search);
  assert.equal(app.get("status").dataset.state,"error");
  assert.equal(app.historyUrls.at(-1),`./index.html${search}`);
  await app.click("retry-listings");
  assert.deepEqual(app.requests,[`${endpoint}${search}`,`${endpoint}${search}`]);
});

test("unknown URL parameters are ignored and direct details default to the collection", async () => {
  const collection=await createApp([response(1,false)],"?city=Berlin&returnTo=https://evil.example&unknown=value");
  assert.deepEqual(collection.requests,[`${endpoint}?city=Berlin`]);
  assert.equal(collection.historyUrls.at(-1),"./index.html?city=Berlin");
  const detail=await createApp([detailResponse({listingId})],detailQuery);
  assert.equal(detail.get("back-to-listings").href,"./index.html");
});

test("price range submit retains decimal bounds and normalizes currency", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-price-min").value="0";
  app.get("filter-price-max").value="1200.75";
  app.get("filter-price-currency").value=" eur ";
  await app.submit();
  const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
  assert.equal(query.get("priceMin"),"0");
  assert.equal(query.get("priceMax"),"1200.75");
  assert.equal(query.get("priceCurrency"),"EUR");
  assert.equal(app.get("filter-error").hidden,true);
});

test("single price bound is accepted and omitted bounds are not sent", async () => {
  for(const bound of ["min","max"]) {
    const app=await createApp([response(1,false),response(1,false)]);
    app.get(`filter-price-${bound}`).value="100.25";
    app.get("filter-price-currency").value="SYP";
    await app.submit();
    const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
    assert.equal(query.get(bound==="min"?"priceMin":"priceMax"),"100.25");
    assert.equal(query.has(bound==="min"?"priceMax":"priceMin"),false);
    assert.equal(query.get("priceCurrency"),"SYP");
  }
});

test("invalid price forms do not fetch, clear cards, or change URL", async () => {
  for(const [minimum,maximum,currency] of [["20","10","EUR"],["-1","","EUR"],["oops","","EUR"],
    ["1e3","","EUR"],["10","",""],["10","","EU"],["","","USD"]]) {
    const app=await createApp([response(1,false)]);
    app.get("filter-price-min").value=minimum;
    app.get("filter-price-max").value=maximum;
    app.get("filter-price-currency").value=currency;
    await app.submit();
    assert.deepEqual(app.requests,[endpoint]);
    assert.deepEqual(app.historyUrls,["./index.html"]);
    assert.equal(app.get("listing-grid").children.length,1);
    assert.equal(app.get("filter-error").hidden,false);
    assert.notEqual(app.get("filter-error").textContent,"");
    assert.equal(app.get("retry-listings").hidden,true);
  }
});

test("correcting price validation submits normally and clears the inline error", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-price-min").value="10";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,false);
  app.get("filter-price-currency").value="USD";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.get("filter-error").textContent,"");
  assert.equal(app.requests.at(-1),`${endpoint}?priceMin=10&priceCurrency=USD`);
});

test("clear price fields also dismisses validation and restores unfiltered results", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-price-min").value="20";
  app.get("filter-price-max").value="10";
  app.get("filter-price-currency").value="EUR";
  await app.submit();
  await app.click("clear-filters");
  for(const id of ["filter-price-min","filter-price-max","filter-price-currency"]) assert.equal(app.get(id).value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);
  assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("bookmarked price search and detail return retain price bounds and currency", async () => {
  const query="priceMin=0&priceMax=1200.75&priceCurrency=EUR&page=2&pageSize=5";
  const app=await createApp([response(2,false,null,[{listingId}])],`?${query}`);
  assert.deepEqual(app.requests,[`${endpoint}?${query}`]);
  assert.equal(app.get("filter-price-min").value,"0");
  assert.equal(app.get("filter-price-max").value,"1200.75");
  assert.equal(app.get("filter-price-currency").value,"EUR");
  const detailLink=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
});

test("price pagination failure and Retry preserve the server page request", async () => {
  const next=`${endpoint}?priceMin=10&priceCurrency=EUR&page=2&pageSize=1`;
  const app=await createApp([response(1,false),response(1,true,link(next,"next")),{ok:false,status:503},response(2,false)]);
  app.get("filter-price-min").value="10";
  app.get("filter-price-currency").value="EUR";
  await app.submit();
  await app.click("next-page");
  await app.click("retry-listings");
  assert.equal(app.requests.at(-2),next);
  assert.equal(app.requests.at(-1),next);
  assert.equal(app.historyUrls.at(-1),"./index.html?priceMin=10&priceCurrency=EUR&page=2&pageSize=1");
});

test("equal bounds are valid and blank price controls introduce no price parameters", async () => {
  const app=await createApp([response(1,false),response(1,false),response(1,false)]);
  await app.submit();
  assert.equal(app.requests.at(-1),endpoint);
  app.get("filter-price-min").value="10.5";
  app.get("filter-price-max").value="10.5";
  app.get("filter-price-currency").value="EUR";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),`${endpoint}?priceMin=10.5&priceMax=10.5&priceCurrency=EUR`);
});

test("area range combines with discovery and price filters without conversion", async () => {
  const app=await createApp([response(1,false),response(1,false)],"?page=4");
  app.get("filter-city").value=" دمشق ";
  app.get("filter-intent").value="Sell";
  app.get("filter-category").value="Apartment";
  app.get("filter-price-min").value="100";
  app.get("filter-price-currency").value="USD";
  app.get("filter-area-min").value="0";
  app.get("filter-area-max").value="120.75";
  app.get("filter-area-unit").value="SquareMeter";
  await app.submit();
  const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
  assert.deepEqual(Object.fromEntries(query), {city:"دمشق",transactionIntent:"Sell",propertyCategory:"Apartment",priceMin:"100",priceCurrency:"USD",livingAreaMin:"0",livingAreaMax:"120.75",livingAreaUnit:"SquareMeter"});
  assert.equal(app.get("filter-error").hidden,true);
});

test("single area bounds accept all supported units and retain entered decimals", async () => {
  for (const unit of ["SquareMeter","SquareFoot","Hectare","Dunum"]) {
    for (const bound of ["min","max"]) {
      const app=await createApp([response(1,false),response(1,false)]);
      app.get(`filter-area-${bound}`).value=" 0.125 ";
      app.get("filter-area-unit").value=unit;
      await app.submit();
      const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
      assert.equal(query.get(bound==="min"?"livingAreaMin":"livingAreaMax"),"0.125");
      assert.equal(query.has(bound==="min"?"livingAreaMax":"livingAreaMin"),false);
      assert.equal(query.get("livingAreaUnit"),unit);
    }
  }
});

test("invalid area forms leave requests, cards and current URL intact", async () => {
  for (const [minimum,maximum,unit] of [["20","10","SquareMeter"],["-1","","Dunum"],
    ["oops","","SquareFoot"],["1e3","","Hectare"],["1","",""],
    ["1","","toString"],["1","","Unknown"],["","","SquareMeter"],
    ["","-2","SquareMeter"],["9".repeat(310),"","SquareMeter"]]) {
    const app=await createApp([response(1,false)],"?city=Berlin&page=2");
    app.get("filter-area-min").value=minimum;
    app.get("filter-area-max").value=maximum;
    app.get("filter-area-unit").value=unit;
    await app.submit();
    assert.deepEqual(app.requests,[`${endpoint}?city=Berlin&page=2`]);
    assert.deepEqual(app.historyUrls,["./index.html?city=Berlin&page=2"]);
    assert.equal(app.get("listing-grid").children.length,1);
    assert.equal(app.get("filter-error").hidden,false);
    assert.notEqual(app.get("filter-error").textContent,"");
  }
});

test("correcting an area validation error submits and dismisses the alert", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-area-min").value="10";
  await app.submit();
  assert.match(app.get("filter-error").textContent,/unit/);
  app.get("filter-area-unit").value="Dunum";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.get("filter-error").textContent,"");
  assert.equal(app.requests.at(-1),`${endpoint}?livingAreaMin=10&livingAreaUnit=Dunum`);
});

test("clear resets area controls, other filters and validation", async () => {
  const app=await createApp([response(2,false),response(1,false)],"?city=Berlin&priceMin=10&priceCurrency=EUR&livingAreaMin=20&livingAreaMax=10&livingAreaUnit=Hectare&page=2");
  await app.submit();
  assert.equal(app.get("filter-error").hidden,false);
  await app.click("clear-filters");
  for(const id of ["filter-city","filter-price-min","filter-price-currency","filter-area-min","filter-area-max","filter-area-unit"]) assert.equal(app.get(id).value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);
  assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("area bookmark reload and detail return restore the same search", async () => {
  const query="livingAreaMin=0&livingAreaMax=120.75&livingAreaUnit=SquareFoot&page=2&pageSize=5";
  const app=await createApp([response(2,false,null,[{listingId}])],`?${query}`);
  assert.equal(app.get("filter-area-min").value,"0");
  assert.equal(app.get("filter-area-max").value,"120.75");
  assert.equal(app.get("filter-area-unit").value,"SquareFoot");
  const detailLink=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  assert.equal(detailLink.searchParams.get("returnSearch"),query);
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.equal(detail.get("listing-filters").hidden,true);
  const reload=await createApp([response(2,false)],new URL(app.historyUrls.at(-1),"https://diyarak.example/app/").search);
  assert.deepEqual(reload.requests,[`${endpoint}?${query}`]);
});

test("area navigation and retry retain server queries, refresh drops only page", async () => {
  const filters="livingAreaMin=10&livingAreaUnit=SquareMeter&pageSize=1";
  const next=`${endpoint}?${filters}&page=2`;
  const previous=`${endpoint}?${filters}&page=1`;
  const app=await createApp([response(1,true,link(next,"next")),{ok:false,status:503},
    response(2,false,link(previous,"prev")),response(1,true,link(next,"next")),response(1,false)],`?${filters}&page=1`);
  await app.click("next-page");
  await app.click("retry-listings");
  await app.click("previous-page");
  app.get("filter-area-min").value="999";
  await app.click("load-listings");
  assert.deepEqual(app.requests,[previous,next,next,previous,`${endpoint}?${filters}`]);
  assert.equal(app.historyUrls.at(-1),`./index.html?${filters}`);
});

test("equal area bounds accept zero and blank controls omit area parameters", async () => {
  const app=await createApp([response(1,false),response(1,false),response(1,false)]);
  await app.submit();
  assert.equal(app.requests.at(-1),endpoint);
  app.get("filter-area-min").value="0";
  app.get("filter-area-max").value="0";
  app.get("filter-area-unit").value="SquareMeter";
  await app.submit();
  assert.equal(app.requests.at(-1),`${endpoint}?livingAreaMin=0&livingAreaMax=0&livingAreaUnit=SquareMeter`);
});

test("loading prevents applying or clearing edited area fields", async () => {
  let resolveResponse;
  const pending=new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-area-min").value="20";
  app.get("filter-area-unit").value="SquareMeter";
  const loading=app.submit();
  assert.equal(app.get("listing-filter-fields").disabled,true);
  app.get("filter-area-min").value="30";
  await app.submit();
  await app.click("clear-filters");
  assert.equal(app.get("filter-area-min").value,"30");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?livingAreaMin=20&livingAreaUnit=SquareMeter`]);
  resolveResponse(response(1,false));
  await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
});

const roomRanges=[["rooms","totalRooms"],["bedrooms","bedroomCount"],["bathrooms","bathroomCount"]];

test("room ranges combine with city, category, price and living area filters", async () => {
  const app=await createApp([response(3,false),response(1,false)],"?page=3");
  app.get("filter-city").value=" دمشق ";
  app.get("filter-intent").value="Rent";
  app.get("filter-category").value="Apartment";
  app.get("filter-price-max").value="500";
  app.get("filter-price-currency").value="USD";
  app.get("filter-area-min").value="50";
  app.get("filter-area-unit").value="SquareMeter";
  for(const [control] of roomRanges) {
    app.get(`filter-${control}-min`).value=control==="rooms"?" 2.5 ":"0";
    app.get(`filter-${control}-max`).value=control==="rooms"?"4.5":"2";
  }
  await app.submit();
  const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
  assert.deepEqual(Object.fromEntries(query),{city:"دمشق",transactionIntent:"Rent",propertyCategory:"Apartment",priceMax:"500",priceCurrency:"USD",livingAreaMin:"50",livingAreaUnit:"SquareMeter",totalRoomsMin:"2.5",totalRoomsMax:"4.5",bedroomCountMin:"0",bedroomCountMax:"2",bathroomCountMin:"0",bathroomCountMax:"2"});
  assert.equal(app.get("filter-error").hidden,true);
});

test("each room range accepts a single bound and omits unused bounds", async () => {
  for(const [control,key] of roomRanges) for(const bound of ["min","max"]) {
    const app=await createApp([response(1,false),response(1,false)]);
    app.get(`filter-${control}-${bound}`).value=control==="rooms"?".5":"2";
    await app.submit();
    const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
    assert.deepEqual(Object.fromEntries(query),{[`${key}${bound==="min"?"Min":"Max"}`]:control==="rooms"?".5":"2"});
  }
});

test("room validation rejects malformed, negative, inverted and fractional integer counts", async () => {
  for(const [control] of roomRanges) {
    const cases=[["4","2"],["-1",""],["","-1"],["oops",""],["1e2",""],["Infinity",""],["9".repeat(310),""]];
    if(control!=="rooms") cases.push(["1.5",""],["","2.5"],["2147483648",""],["","2147483648"],["2.0",""]);
    for(const [min,max] of cases) {
      const app=await createApp([response(2,false)],"?city=Berlin&page=2");
      app.get(`filter-${control}-min`).value=min;
      app.get(`filter-${control}-max`).value=max;
      await app.submit();
      assert.deepEqual(app.requests,[`${endpoint}?city=Berlin&page=2`]);
      assert.deepEqual(app.historyUrls,["./index.html?city=Berlin&page=2"]);
      assert.equal(app.get("listing-grid").children.length,1);
      assert.equal(app.get("filter-error").hidden,false);
      assert.notEqual(app.get("filter-error").textContent,"");
    }
  }
});

test("room validation can be corrected and integer upper boundary is accepted", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-bedrooms-min").value="1.5";
  await app.submit();
  assert.match(app.get("filter-error").textContent,/whole-number/);
  app.get("filter-bedrooms-min").value="1";
  app.get("filter-bedrooms-max").value="2147483647";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.get("filter-error").textContent,"");
  assert.equal(app.requests.at(-1),`${endpoint}?bedroomCountMin=1&bedroomCountMax=2147483647`);
});

test("room controls omit blanks and accept zero and equal bounds", async () => {
  const app=await createApp([response(1,false),response(1,false),response(1,false)]);
  await app.submit();
  assert.equal(app.requests.at(-1),endpoint);
  for(const [control] of roomRanges) for(const bound of ["min","max"]) app.get(`filter-${control}-${bound}`).value="0";
  await app.submit();
  assert.equal(app.requests.at(-1),`${endpoint}?totalRoomsMin=0&totalRoomsMax=0&bedroomCountMin=0&bedroomCountMax=0&bathroomCountMin=0&bathroomCountMax=0`);
});

test("room bookmark reload and detail return preserve all six bounds", async () => {
  const query="totalRoomsMin=2.5&totalRoomsMax=4.5&bedroomCountMin=0&bedroomCountMax=3&bathroomCountMin=1&bathroomCountMax=2&page=3&pageSize=5";
  const app=await createApp([response(3,false,null,[{listingId}])],`?${query}`);
  const params=new URLSearchParams(query);
  for(const [control,key] of roomRanges) for(const bound of ["min","max"]) assert.equal(app.get(`filter-${control}-${bound}`).value,params.get(`${key}${bound==="min"?"Min":"Max"}`));
  const detailLink=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  assert.equal(detailLink.searchParams.get("returnSearch"),query);
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.equal(detail.get("listing-filters").hidden,true);
  const reload=await createApp([response(3,false)],new URL(app.historyUrls.at(-1),"https://diyarak.example/app/").search);
  assert.deepEqual(reload.requests,[`${endpoint}?${query}`]);
});

test("room pagination, Retry and Refresh retain applied bounds", async () => {
  const filters="totalRoomsMin=2.5&bedroomCountMax=3&bathroomCountMin=1&pageSize=1";
  const next=`${endpoint}?${filters}&page=2`;
  const previous=`${endpoint}?${filters}&page=1`;
  const app=await createApp([response(1,true,link(next,"next")),new Error("offline"),
    response(2,false,link(previous,"prev")),response(1,true,link(next,"next")),response(1,false)],`?${filters}&page=1`);
  await app.click("next-page");
  await app.click("retry-listings");
  await app.click("previous-page");
  app.get("filter-rooms-min").value="99";
  await app.click("load-listings");
  assert.deepEqual(app.requests,[previous,next,next,previous,`${endpoint}?${filters}`]);
  assert.equal(app.historyUrls.at(-1),`./index.html?${filters}`);
});

test("clear resets all room controls and dismisses validation", async () => {
  const app=await createApp([response(2,false),response(1,false)],"?totalRoomsMin=4&totalRoomsMax=2&bedroomCountMin=0&bedroomCountMax=3&bathroomCountMin=1&bathroomCountMax=2&page=2");
  await app.submit();
  assert.equal(app.get("filter-error").hidden,false);
  await app.click("clear-filters");
  for(const [control] of roomRanges) for(const bound of ["min","max"]) assert.equal(app.get(`filter-${control}-${bound}`).value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);
  assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("loading blocks submitting or clearing edited room filters", async () => {
  let resolveResponse;
  const pending=new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-rooms-min").value="2.5";
  const loading=app.submit();
  assert.equal(app.get("listing-filter-fields").disabled,true);
  app.get("filter-rooms-min").value="3.5";
  await app.submit();
  await app.click("clear-filters");
  assert.equal(app.get("filter-rooms-min").value,"3.5");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?totalRoomsMin=2.5`]);
  resolveResponse(response(1,false));
  await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
});

const yearParkingRanges=[["year","constructionYear",1],["parking","parkingSpaceCount",0]];

test("year and parking ranges combine with all existing filter groups", async () => {
  const app=await createApp([response(3,false),response(1,false)],"?page=3");
  for(const [id,value] of [["city","Berlin"],["intent","Sell"],["category","House"],["price-min","100"],["price-currency","EUR"],["area-min","50"],["area-unit","SquareMeter"],["rooms-min","2.5"],["bedrooms-min","1"],["bathrooms-min","1"],["year-min"," 2000 "],["year-max","2020"],["parking-min","0"],["parking-max","2"]]) app.get(`filter-${id}`).value=value;
  await app.submit();
  const params=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
  assert.deepEqual(Object.fromEntries(params),{city:"Berlin",transactionIntent:"Sell",propertyCategory:"House",priceMin:"100",priceCurrency:"EUR",livingAreaMin:"50",livingAreaUnit:"SquareMeter",totalRoomsMin:"2.5",bedroomCountMin:"1",bathroomCountMin:"1",constructionYearMin:"2000",constructionYearMax:"2020",parkingSpaceCountMin:"0",parkingSpaceCountMax:"2"});
  assert.equal(app.get("filter-error").hidden,true);
});

test("year and parking single bounds, equal bounds and integer boundaries are accepted", async () => {
  for(const [control,key,lower] of yearParkingRanges) {
    for(const bound of ["min","max"]) for(const value of [String(lower),"2147483647"]) {
      const app=await createApp([response(1,false),response(1,false)]);
      app.get(`filter-${control}-${bound}`).value=value;
      await app.submit();
      assert.equal(app.requests.at(-1),`${endpoint}?${key}${bound==="min"?"Min":"Max"}=${value}`);
    }
    const app=await createApp([response(1,false),response(1,false)]);
    app.get(`filter-${control}-min`).value=String(lower);
    app.get(`filter-${control}-max`).value=String(lower);
    await app.submit();
    assert.equal(app.requests.at(-1),`${endpoint}?${key}Min=${lower}&${key}Max=${lower}`);
  }
});

test("invalid year and parking forms retain cards, requests and URL", async () => {
  for(const [control] of yearParkingRanges) {
    const cases=[["4","2"],["-1",""],["","-1"],["1.5",""],["","2.5"],["1e3",""],["oops",""],["2147483648",""],["","2147483648"],["9".repeat(310),""]];
    if(control==="year") cases.push(["0",""],["","0"]);
    for(const [min,max] of cases) {
      const app=await createApp([response(2,false)],"?city=Berlin&page=2");
      app.get(`filter-${control}-min`).value=min;
      app.get(`filter-${control}-max`).value=max;
      await app.submit();
      assert.deepEqual(app.requests,[`${endpoint}?city=Berlin&page=2`]);
      assert.deepEqual(app.historyUrls,["./index.html?city=Berlin&page=2"]);
      assert.equal(app.get("listing-grid").children.length,1);
      assert.equal(app.get("filter-error").hidden,false);
    }
  }
});

test("correcting year validation submits and clears the inline alert", async () => {
  const app=await createApp([response(1,false),response(1,false)]);
  app.get("filter-year-min").value="0";
  await app.submit();
  assert.match(app.get("filter-error").textContent,/between 1/);
  app.get("filter-year-min").value="2000";
  app.get("filter-parking-max").value="0";
  await app.submit();
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.get("filter-error").textContent,"");
  assert.equal(app.requests.at(-1),`${endpoint}?constructionYearMin=2000&parkingSpaceCountMax=0`);
});

test("year and parking bookmarks reload and return from details with all four values", async () => {
  const query="constructionYearMin=2000&constructionYearMax=2020&parkingSpaceCountMin=0&parkingSpaceCountMax=2&page=3&pageSize=5";
  const app=await createApp([response(3,false,null,[{listingId}])],`?${query}`);
  const params=new URLSearchParams(query);
  for(const [control,key] of yearParkingRanges) for(const bound of ["min","max"]) assert.equal(app.get(`filter-${control}-${bound}`).value,params.get(`${key}${bound==="min"?"Min":"Max"}`));
  const detailLink=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  assert.equal(detailLink.searchParams.get("returnSearch"),query);
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.equal(detail.get("listing-filters").hidden,true);
  const reload=await createApp([response(3,false)],new URL(app.historyUrls.at(-1),"https://diyarak.example/app/").search);
  assert.deepEqual(reload.requests,[`${endpoint}?${query}`]);
});

test("year and parking pagination Retry and Refresh keep applied values", async () => {
  const filters="constructionYearMin=2000&parkingSpaceCountMax=0&pageSize=1";
  const next=`${endpoint}?${filters}&page=2`;
  const prev=`${endpoint}?${filters}&page=1`;
  const app=await createApp([response(1,true,link(next,"next")),{ok:false,status:503},response(2,false,link(prev,"prev")),response(1,true,link(next,"next")),response(1,false)],`?${filters}&page=1`);
  await app.click("next-page");await app.click("retry-listings");await app.click("previous-page");
  app.get("filter-year-min").value="2025";
  await app.click("load-listings");
  assert.deepEqual(app.requests,[prev,next,next,prev,`${endpoint}?${filters}`]);
  assert.equal(app.historyUrls.at(-1),`./index.html?${filters}`);
});

test("blank year and parking controls omit parameters and Clear resets invalid values", async () => {
  const app=await createApp([response(1,false),response(1,false),response(1,false)]);
  await app.submit();assert.equal(app.requests.at(-1),endpoint);
  app.get("filter-year-min").value="2000";
  app.get("filter-year-max").value="1990";
  app.get("filter-parking-min").value="0";
  app.get("filter-parking-max").value="2";
  await app.submit();assert.equal(app.get("filter-error").hidden,false);
  await app.click("clear-filters");
  for(const [control] of yearParkingRanges) for(const bound of ["min","max"]) assert.equal(app.get(`filter-${control}-${bound}`).value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);
  assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("loading prevents submitting or clearing year and parking edits", async () => {
  let resolveResponse;const pending=new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-year-min").value="2000";app.get("filter-parking-min").value="0";
  const loading=app.submit();assert.equal(app.get("listing-filter-fields").disabled,true);
  app.get("filter-year-min").value="2010";app.get("filter-parking-min").value="1";
  await app.submit();await app.click("clear-filters");
  assert.equal(app.get("filter-year-min").value,"2010");assert.equal(app.get("filter-parking-min").value,"1");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?constructionYearMin=2000&parkingSpaceCountMin=0`]);
  resolveResponse(response(1,false));await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
});

test("furnishing filter sends each supported quality and omits Any quality", async () => {
  for(const quality of ["","Simple","Normal","Upscale","Luxury"]) {
    const app=await createApp([response(1,false),response(1,false)]);
    app.get("filter-furnishing").value=quality;
    await app.submit();
    assert.equal(app.requests.at(-1),quality?`${endpoint}?furnishingQuality=${quality}`:endpoint);
  }
});

test("furnishing combines with existing filters and resets page coordinates", async () => {
  const app=await createApp([response(3,false),response(1,false)],"?page=3&pageSize=5");
  for(const [id,value] of [["city","Berlin"],["intent","Sell"],["category","House"],["furnishing","Luxury"],["price-min","100"],["price-currency","EUR"],["area-min","50"],["area-unit","SquareMeter"],["rooms-min","2.5"],["year-min","2000"],["parking-min","0"]]) app.get(`filter-${id}`).value=value;
  await app.submit();
  const query=new URL(app.requests.at(-1),"https://diyarak.example").searchParams;
  assert.deepEqual(Object.fromEntries(query),{city:"Berlin",transactionIntent:"Sell",propertyCategory:"House",furnishingQuality:"Luxury",priceMin:"100",priceCurrency:"EUR",livingAreaMin:"50",livingAreaUnit:"SquareMeter",totalRoomsMin:"2.5",constructionYearMin:"2000",parkingSpaceCountMin:"0"});
});

test("furnishing bookmark reload and details restore the applied quality", async () => {
  const query="city=Berlin&furnishingQuality=Upscale&page=2&pageSize=5";
  const app=await createApp([response(2,false,null,[{listingId}])],`?${query}`);
  assert.equal(app.get("filter-furnishing").value,"Upscale");
  const detailLink=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  assert.equal(detailLink.searchParams.get("returnSearch"),query);
  const detail=await createApp([detailResponse({listingId})],detailLink.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.equal(detail.get("listing-filters").hidden,true);
  const reload=await createApp([response(2,false)],new URL(app.historyUrls.at(-1),"https://diyarak.example/app/").search);
  assert.deepEqual(reload.requests,[`${endpoint}?${query}`]);
  assert.equal(reload.get("filter-furnishing").value,"Upscale");
});

test("furnishing navigation Retry and Refresh preserve applied choice over edits", async () => {
  const filters="furnishingQuality=Normal&pageSize=1";
  const next=`${endpoint}?${filters}&page=2`,prev=`${endpoint}?${filters}&page=1`;
  const app=await createApp([response(1,true,link(next,"next")),{ok:false,status:503},response(2,false,link(prev,"prev")),response(1,true,link(next,"next")),response(1,false)],`?${filters}&page=1`);
  app.get("filter-furnishing").value="Luxury";
  await app.click("next-page");await app.click("retry-listings");await app.click("previous-page");await app.click("load-listings");
  assert.deepEqual(app.requests,[prev,next,next,prev,`${endpoint}?${filters}`]);
  assert.equal(app.historyUrls.at(-1),`./index.html?${filters}`);
});

test("Clear resets furnishing and other filters and dismisses validation", async () => {
  const app=await createApp([response(1,false),response(1,false)],"?furnishingQuality=Luxury&constructionYearMin=0");
  await app.submit();assert.equal(app.get("filter-error").hidden,false);
  await app.click("clear-filters");
  assert.equal(app.get("filter-furnishing").value,"");
  assert.equal(app.get("filter-year-min").value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);
  assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("loading blocks furnishing submission and Clear", async () => {
  let resolveResponse;const pending=new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-furnishing").value="Simple";
  const loading=app.submit();assert.equal(app.get("listing-filter-fields").disabled,true);
  app.get("filter-furnishing").value="Luxury";
  await app.submit();await app.click("clear-filters");
  assert.equal(app.get("filter-furnishing").value,"Luxury");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?furnishingQuality=Simple`]);
  resolveResponse(response(1,false));await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
});

test("malformed bookmarked furnishing value remains subject to API validation", async () => {
  const query="furnishingQuality=Unknown&page=2";
  const app=await createApp([{ok:false,status:400},response(2,false)],`?${query}`);
  assert.equal(app.get("status").dataset.state,"error");
  assert.equal(app.historyUrls.at(-1),`./index.html?${query}`);
  await app.click("retry-listings");
  assert.deepEqual(app.requests,[`${endpoint}?${query}`,`${endpoint}?${query}`]);
});

test("commercial filter sends all supported subtypes and omits the blank choice", async () => {
  for(const subtype of ["","OfficeOrPractice","RestaurantOrHotel","HallOrProduction","Retail","CommercialPlot"]) {
    const app=await createApp([response(1,false),response(1,false)]);
    app.get("filter-commercial").value=subtype;await app.submit();
    assert.equal(app.requests.at(-1),subtype?`${endpoint}?commercialSubtype=${subtype}`:endpoint);
    assert.equal(app.get("filter-category").value,"");
  }
});

test("commercial subtype combines with the chosen category and previous filters", async () => {
  const app=await createApp([response(3,false),response(1,false)],"?page=3&pageSize=5");
  for(const [id,value] of [["city","Berlin"],["intent","Rent"],["category","CommercialProperty"],["commercial","OfficeOrPractice"],["furnishing","Upscale"],["price-min","100"],["price-currency","EUR"],["area-min","50"],["area-unit","SquareMeter"],["rooms-min","2.5"],["year-min","2000"],["parking-min","0"]]) app.get(`filter-${id}`).value=value;
  await app.submit();
  assert.deepEqual(Object.fromEntries(new URL(app.requests.at(-1),"https://diyarak.example").searchParams),{city:"Berlin",transactionIntent:"Rent",propertyCategory:"CommercialProperty",commercialSubtype:"OfficeOrPractice",furnishingQuality:"Upscale",priceMin:"100",priceCurrency:"EUR",livingAreaMin:"50",livingAreaUnit:"SquareMeter",totalRoomsMin:"2.5",constructionYearMin:"2000",parkingSpaceCountMin:"0"});
});

test("commercial subtype respects a different category and handles empty results", async () => {
  const app=await createApp([response(1,false),response(1,false,null,[])]);
  app.get("filter-category").value="Apartment";app.get("filter-commercial").value="Retail";
  await app.submit();
  assert.equal(app.requests.at(-1),`${endpoint}?propertyCategory=Apartment&commercialSubtype=Retail`);
  assert.equal(app.get("filter-category").value,"Apartment");
  assert.equal(app.get("status").dataset.state,"ready");
  assert.match(app.get("status").textContent,/No public listings/);
});

test("commercial bookmarks reload and details return to the same subtype search", async () => {
  const query="city=Berlin&commercialSubtype=CommercialPlot&page=2&pageSize=5";
  const app=await createApp([response(2,false,null,[{listingId}])],`?${query}`);
  assert.equal(app.get("filter-commercial").value,"CommercialPlot");
  const anchor=new URL(cardOf(app).querySelector(".listing-card__detail-link").href,"https://diyarak.example/app/");
  assert.equal(anchor.searchParams.get("returnSearch"),query);
  const detail=await createApp([detailResponse({listingId})],anchor.search);
  assert.equal(detail.get("back-to-listings").href,`./index.html?${query}`);
  assert.equal(detail.get("listing-filters").hidden,true);
  const reload=await createApp([response(2,false)],new URL(app.historyUrls.at(-1),"https://diyarak.example/app/").search);
  assert.deepEqual(reload.requests,[`${endpoint}?${query}`]);
  assert.equal(reload.get("filter-commercial").value,"CommercialPlot");
});

test("commercial pagination Retry and Refresh preserve applied subtype over edits", async () => {
  const filters="commercialSubtype=HallOrProduction&pageSize=1";
  const next=`${endpoint}?${filters}&page=2`,prev=`${endpoint}?${filters}&page=1`;
  const app=await createApp([response(1,true,link(next,"next")),{ok:false,status:503},response(2,false,link(prev,"prev")),response(1,true,link(next,"next")),response(1,false)],`?${filters}&page=1`);
  app.get("filter-commercial").value="Retail";
  await app.click("next-page");await app.click("retry-listings");await app.click("previous-page");await app.click("load-listings");
  assert.deepEqual(app.requests,[prev,next,next,prev,`${endpoint}?${filters}`]);
  assert.equal(app.historyUrls.at(-1),`./index.html?${filters}`);
});

test("Clear resets commercial subtype and dismisses other validation", async () => {
  const app=await createApp([response(1,false),response(1,false)],"?commercialSubtype=Retail&constructionYearMin=0");
  await app.submit();assert.equal(app.get("filter-error").hidden,false);
  await app.click("clear-filters");
  assert.equal(app.get("filter-commercial").value,"");assert.equal(app.get("filter-year-min").value,"");
  assert.equal(app.get("filter-error").hidden,true);
  assert.equal(app.requests.at(-1),endpoint);assert.equal(app.historyUrls.at(-1),"./index.html");
});

test("loading blocks commercial submission and Clear", async () => {
  let resolveResponse;const pending=new Promise(resolve=>{resolveResponse=resolve;});
  const app=await createApp([response(1,false),()=>pending]);
  app.get("filter-commercial").value="RestaurantOrHotel";
  const loading=app.submit();assert.equal(app.get("listing-filter-fields").disabled,true);
  app.get("filter-commercial").value="Retail";await app.submit();await app.click("clear-filters");
  assert.equal(app.get("filter-commercial").value,"Retail");
  assert.deepEqual(app.requests,[endpoint,`${endpoint}?commercialSubtype=RestaurantOrHotel`]);
  resolveResponse(response(1,false));await loading;
  assert.equal(app.get("listing-filter-fields").disabled,false);
});

test("invalid bookmarked commercial subtype remains subject to API validation", async () => {
  const query="commercialSubtype=Unknown&page=2";
  const app=await createApp([{ok:false,status:400},response(2,false)],`?${query}`);
  assert.equal(app.get("status").dataset.state,"error");assert.equal(app.historyUrls.at(-1),`./index.html?${query}`);
  await app.click("retry-listings");
  assert.deepEqual(app.requests,[`${endpoint}?${query}`,`${endpoint}?${query}`]);
});
