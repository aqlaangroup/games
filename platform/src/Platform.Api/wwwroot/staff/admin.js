// The staff panel (missing/22): a page over the staff API. Every value from the API goes into the page as text,
// never as HTML, because players write some of it (support messages).
"use strict";

const ROLES = { viewer: 1, support: 2, producer: 3, owner: 4 };
const TABS = [
  ["players", "Players", "viewer"],
  ["support", "Support", "support"],
  ["purchases", "Purchases", "viewer"],
  ["revenue", "Revenue", "viewer"],
  ["config", "Config", "viewer"],
  ["events", "Events", "viewer"],
  ["wall", "Sitti wall", "support"],
  ["deletions", "Deletions", "support"],
  ["metrics", "Metrics", "viewer"],
  ["game", "Game", "viewer"],
  ["staff", "Staff", "owner"],
  ["audit", "Audit", "owner"],
  ["account", "My account", "viewer"],
];

const state = { token: sessionStorage.getItem("staffToken"), admin: null, game: sessionStorage.getItem("staffGame"), tab: "players" };
const view = document.getElementById("view");

// ---------- Small helpers ----------

function el(tag, attrs, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs || {})) {
    if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
    else if (value === true) node.setAttribute(key, "");
    else if (value !== false && value != null) node.setAttribute(key, value);
  }
  for (const child of children.flat()) if (child != null) node.append(child instanceof Node ? child : String(child));
  return node;
}

function text(value) {
  if (value == null) return "";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

// An array of objects as a table; extra(row) can add a last cell with buttons.
function table(rows, extra) {
  if (!rows || rows.length === 0) return el("p", { class: "note" }, "Nothing here.");
  const keys = [...new Set(rows.flatMap((r) => Object.keys(r)))];
  return el("table", {},
    el("thead", {}, el("tr", {}, keys.map((k) => el("th", {}, k)), extra ? el("th", {}) : null)),
    el("tbody", {}, rows.map((r) => el("tr", {}, keys.map((k) => el("td", {}, text(r[k]))), extra ? el("td", {}, extra(r)) : null))));
}

// An object as a two-column table.
function pairs(object) {
  if (!object) return el("p", { class: "note" }, "None.");
  return el("table", {}, el("tbody", {}, Object.entries(object).map(([k, v]) => el("tr", {}, el("th", {}, k), el("td", {}, text(v))))));
}

function card(title, ...children) {
  return el("section", { class: "card" }, el("h2", {}, title), ...children);
}

function message(node, ok, words) {
  node.textContent = words;
  node.className = ok ? "ok" : "error";
}

async function api(method, path, body, raw) {
  const headers = {};
  if (state.token) headers.Authorization = "Bearer " + state.token;
  if (body !== undefined && !raw) headers["Content-Type"] = "application/json";
  const response = await fetch(path, { method, headers, body: body === undefined ? undefined : raw ? body : JSON.stringify(body) });
  if (response.status === 401 && path !== "/admin/sign-in") {
    signOut();
    throw new Error("Signed out. Please sign in again.");
  }
  const type = response.headers.get("Content-Type") || "";
  const data = response.status === 204 ? null : type.includes("json") ? await response.json() : await response.text();
  if (!response.ok) throw new Error((data && data.error) || response.statusText);
  return data;
}

const game = (path) => `/games/${encodeURIComponent(state.game)}/admin${path}`;
const role = () => ROLES[(state.admin && state.admin.roles[state.game]) || ""] || 0;
const can = (needed) => role() >= ROLES[needed];

// ---------- Sign in ----------

function signOut() {
  state.token = null;
  state.admin = null;
  sessionStorage.removeItem("staffToken");
  showSignIn();
}

function showSignIn(needCode) {
  document.getElementById("bar").hidden = true;
  const status = el("p", {});
  const email = el("input", { type: "email", autocomplete: "username", required: true });
  const password = el("input", { type: "password", autocomplete: "current-password", required: true });
  const code = el("input", { inputmode: "numeric", autocomplete: "one-time-code", maxlength: "6" });
  const codeLabel = el("label", { hidden: !needCode }, "Code from your authenticator app", code);
  view.replaceChildren(el("form", {
    class: "card signin", onsubmit: async (e) => {
      e.preventDefault();
      try {
        const reply = await api("POST", "/admin/sign-in", { email: email.value, password: password.value, code: code.value || null });
        state.token = reply.accessToken;
        sessionStorage.setItem("staffToken", state.token);
        await start();
      } catch (error) {
        if (error.message === "code_required") { codeLabel.hidden = false; code.focus(); message(status, false, "Enter the code from your authenticator app."); }
        else message(status, false, error.message === "invalid_credentials" ? "Wrong email or password." : error.message);
      }
    },
  }, el("h2", {}, "Staff sign-in"), el("label", {}, "Email", email), el("label", {}, "Password", password), codeLabel,
    el("button", { class: "primary", type: "submit" }, "Sign in"), status));
}

function showPasswordChange() {
  const status = el("p", {});
  const current = el("input", { type: "password", autocomplete: "current-password" });
  const next = el("input", { type: "password", autocomplete: "new-password", minlength: "12" });
  view.replaceChildren(el("form", {
    class: "card signin", onsubmit: async (e) => {
      e.preventDefault();
      try {
        await api("POST", "/admin/me/password", { currentPassword: current.value, newPassword: next.value });
        await start();
      } catch (error) { message(status, false, error.message === "weak_password" ? "Use 12 or more characters, different from the old one." : error.message); }
    },
  }, el("h2", {}, "Choose your own password"), el("p", { class: "note" }, "Your account has a temporary password."),
    el("label", {}, "Temporary password", current), el("label", {}, "New password (12+ characters)", next),
    el("button", { class: "primary", type: "submit" }, "Save"), status));
}

async function start() {
  if (!state.token) return showSignIn();
  try { state.admin = await api("GET", "/admin/me"); } catch { return; }
  if (state.admin.mustChangePassword) return showPasswordChange();
  const games = Object.keys(state.admin.roles);
  if (games.length === 0) { view.replaceChildren(card("No games", el("p", {}, "Ask an owner to give you a role on a game."))); return; }
  if (!games.includes(state.game)) state.game = games[0];
  const select = document.getElementById("game");
  select.replaceChildren(...games.map((g) => el("option", { value: g, selected: g === state.game }, g)));
  select.onchange = () => { state.game = select.value; sessionStorage.setItem("staffGame", state.game); show(state.tab); };
  document.getElementById("who").textContent = `${state.admin.email} · ${state.admin.roles[state.game]}`;
  document.getElementById("signOut").onclick = signOut;
  document.getElementById("bar").hidden = false;
  show(state.tab);
}

function show(tab) {
  if (!TABS.some(([id, , needed]) => id === tab && can(needed))) tab = "players";
  state.tab = tab;
  document.getElementById("who").textContent = `${state.admin.email} · ${state.admin.roles[state.game]}`;
  document.getElementById("tabs").replaceChildren(...TABS.filter(([, , needed]) => can(needed)).map(([id, name]) =>
    el("button", { type: "button", class: id === tab ? "on" : null, onclick: () => show(id) }, name)));
  view.replaceChildren(el("p", { class: "note" }, "Loading…"));
  PAGES[tab]().catch((error) => view.replaceChildren(card("Couldn't load", el("p", { class: "error" }, error.message))));
}

// ---------- Pages ----------

const PAGES = {
  async players(query) {
    const box = el("input", { type: "search", placeholder: "Player id, or Apple/Google id", value: query || "" });
    const results = el("div", {});
    const find = async () => {
      const rows = await api("GET", game(`/players?query=${encodeURIComponent(box.value.trim())}`));
      results.replaceChildren(table(rows, (r) => el("button", { type: "button", onclick: () => player(r.playerId) }, "Open")));
    };
    view.replaceChildren(card("Players", el("form", { class: "row", onsubmit: (e) => { e.preventDefault(); find(); } }, box,
      el("button", { class: "primary", type: "submit" }, "Find")), results));
    await find();
  },

  async support() {
    const status = el("select", {}, el("option", { value: "open" }, "Open"), el("option", { value: "closed" }, "Closed"));
    const list = el("div", {});
    const load = async () => {
      const tickets = await api("GET", game(`/support?status=${status.value}`));
      list.replaceChildren(...tickets.map((t) => {
        const note = el("input", { placeholder: "Note (what you did)" });
        return el("section", { class: "card" }, pairs(t), status.value === "open" ? el("div", { class: "row" },
          el("button", { type: "button", onclick: () => player(t.playerId) }, "Open player"), note,
          el("button", { type: "button", onclick: async () => { await api("POST", game(`/support/${t.id}/close`), { note: note.value }); load(); } }, "Close")) : null);
      }));
      if (tickets.length === 0) list.replaceChildren(el("p", { class: "note" }, "No tickets."));
    };
    status.onchange = load;
    view.replaceChildren(card("Support", el("div", { class: "row" }, status), list));
    await load();
  },

  async purchases() {
    const query = el("input", { type: "search", placeholder: "Transaction id, player id or sku" });
    const status = el("select", {}, ...["", "verified", "refunded", "revoked"].map((s) => el("option", { value: s }, s || "Any status")));
    const results = el("div", {});
    const find = async () => results.replaceChildren(table(await api("GET",
      game(`/purchases?query=${encodeURIComponent(query.value.trim())}&status=${status.value}`)), (r) => r.playerId ? el("button", { type: "button", onclick: () => player(r.playerId) }, "Player") : null));
    view.replaceChildren(card("Purchases and refunds", el("form", { class: "row", onsubmit: (e) => { e.preventDefault(); find(); } }, query, status,
      el("button", { class: "primary", type: "submit" }, "Find")), results));
    await find();
  },

  async revenue() {
    const days = el("select", {}, ...[7, 30, 90, 365].map((d) => el("option", { value: d, selected: d === 30 }, `${d} days`)));
    const out = el("div", {});
    const load = async () => {
      const r = await api("GET", game(`/revenue?days=${days.value}`));
      out.replaceChildren(el("h3", {}, "By day and currency"), table(r.byDay), el("h3", {}, "By product"), table(r.bySku),
        el("p", { class: "note" }, "Google's purchase records carry no price; Google revenue comes from the Play Console's reports. Ad revenue arrives with the ad network."));
    };
    days.onchange = load;
    view.replaceChildren(card("Store revenue", el("div", { class: "row" }, days), out));
    await load();
  },

  async config() {
    const versions = await api("GET", game("/config"));
    const active = versions.find((v) => v.isActive);
    const parts = [card("Published versions", table(versions, (v) => can("producer") && !v.isActive
      ? el("button", { type: "button", onclick: async () => { if (confirm(`Make version ${v.version} active?`)) { await api("POST", game(`/config/${v.version}/activate`)); show("config"); } } }, "Make active") : null))];
    if (can("producer")) {
      const editor = el("textarea", { spellcheck: "false", placeholder: "Paste the whole balance.json here, or choose the file." });
      const file = el("input", { type: "file", accept: ".json,application/json" });
      file.onchange = async () => { if (file.files[0]) editor.value = await file.files[0].text(); };
      const changes = el("div", {});
      const status = el("p", {});
      const check = async () => {
        let next;
        try { next = JSON.parse(editor.value); } catch (e) { message(status, false, "Not JSON: " + e.message); return null; }
        let current = {};
        try { current = active ? await api("GET", game(`/config/${active.version}`)) : {}; }
        catch (error) { message(status, false, "Couldn't load the active config: " + error.message); return null; }
        const diff = differences(current, next);
        changes.replaceChildren(el("h3", {}, `${diff.length} change(s) from version ${active ? active.version : "none"}`),
          el("ul", { class: "diff" }, diff.slice(0, 300).map((d) => el("li", {}, d))));
        message(status, true, "Looks like JSON. The platform checks its sections when you publish.");
        return next;
      };
      parts.push(card("Publish a new balance.json", el("div", { class: "row" }, file), editor,
        el("div", { class: "row" }, el("button", { type: "button", onclick: check }, "Check and compare"),
          el("button", { class: "primary", type: "button", onclick: async () => {
            if (!(await check()) || !confirm("Publish this config? Players get it on their next launch.")) return;
            try { const r = await api("POST", game("/config"), editor.value, true); message(status, true, `Published as version ${r.version}.`); setTimeout(() => show("config"), 800); }
            catch (error) { message(status, false, error.message); }
          } }, "Publish")), status, changes));
    }
    view.replaceChildren(...parts);
  },

  async events() {
    const events = await api("GET", game("/events"));
    const parts = [card("Events", table(events.map((e) => ({ id: e.id, template: e.template, startsAt: e.startsAt, endsAt: e.endsAt, settings: e.settingsJson })),
      (e) => can("producer") ? el("button", { type: "button", onclick: async () => { if (confirm("Cancel this event?")) { await api("DELETE", game(`/events/${e.id}`)); show("events"); } } }, "Cancel") : null))];
    if (can("producer")) {
      const template = el("select", {}, ...["miniBoard", "frenzy", "pass", "album", "hospitality"].map((t) => el("option", { value: t }, t)));
      const starts = el("input", { type: "datetime-local", required: true });
      const ends = el("input", { type: "datetime-local", required: true });
      const settings = el("textarea", { spellcheck: "false", placeholder: '{ "name": "سوق الجمعة", "nameEn": "Friday market" }' });
      settings.style.minHeight = "100px";
      const status = el("p", {});
      parts.push(card("Schedule an event", el("form", { onsubmit: async (e) => {
        e.preventDefault();
        let parsed = {};
        try { parsed = settings.value.trim() ? JSON.parse(settings.value) : {}; } catch (err) { message(status, false, "Settings aren't JSON: " + err.message); return; }
        try {
          await api("POST", game("/events"), { template: template.value, startsAt: new Date(starts.value).toISOString(), endsAt: new Date(ends.value).toISOString(), settings: parsed });
          show("events");
        } catch (error) { message(status, false, error.message); }
      } }, el("div", { class: "row" }, el("label", {}, "Template", template), el("label", {}, "Starts (your time)", starts), el("label", {}, "Ends", ends)),
        el("label", {}, "Settings (optional JSON; lay over the template in balance.json)", settings),
        el("button", { class: "primary", type: "submit" }, "Schedule"), status)));
    }
    view.replaceChildren(...parts);
  },

  // Posts wait here until a person approves them; reports send approved posts back.
  async wall() {
    const status = el("select", {}, ...["pending", "approved", "rejected"].map((s) => el("option", { value: s }, s)));
    const list = el("div", {});
    const load = async () => {
      const posts = await api("GET", game(`/wall?status=${status.value}`));
      list.replaceChildren(...posts.map((p) => el("section", { class: "card" },
        el("p", { dir: "auto" }, p.text),
        el("p", { class: "note" }, `${p.createdAt} · likes ${p.likes} · reports ${p.reports}${p.autoReason ? " · " + p.autoReason : ""}`),
        el("div", { class: "row" },
          p.status !== "approved" ? el("button", { class: "primary", type: "button", onclick: async () => { await api("POST", game(`/wall/${p.id}/approve`)); load(); } }, "Approve") : null,
          p.status !== "rejected" ? el("button", { type: "button", onclick: async () => { await api("POST", game(`/wall/${p.id}/reject`)); load(); } }, "Reject") : null,
          el("button", { type: "button", onclick: () => player(p.playerId) }, "Player")))));
      if (posts.length === 0) list.replaceChildren(el("p", { class: "note" }, "Nothing waiting."));
    };
    status.onchange = load;
    view.replaceChildren(card("Sitti wall", el("p", { class: "note" }, "Nothing shows to players until it's approved here. Posts carry no names in the game."), el("div", { class: "row" }, status), list));
    await load();
  },

  async deletions() {
    view.replaceChildren(card("Accounts waiting for deletion", table(await api("GET", game("/deletions"))),
      el("p", { class: "note" }, "Players deleted these accounts in the game. The purge removes them by deleteBy; purchases stay for accounting, unlinked.")));
  },

  async metrics() {
    view.replaceChildren(card("Tester gate (30 days)", pairs(await api("GET", game("/metrics?days=30")))));
  },

  async game() {
    const g = await api("GET", game("/game"));
    const products = g.products;
    delete g.products;
    view.replaceChildren(card("Game", pairs(g)), card("Products", table(products)));
  },

  async staff() {
    const email = el("input", { type: "email", placeholder: "email" });
    const pick = el("select", {}, ...Object.keys(ROLES).map((r) => el("option", { value: r }, r)));
    const status = el("p", {});
    view.replaceChildren(card("Staff on this game", table(await api("GET", game("/staff")), (s) =>
      el("button", { type: "button", onclick: async () => { if (confirm(`Remove ${s.email} from this game?`)) { await api("DELETE", game(`/staff/${s.id}`)); show("staff"); } } }, "Remove"))),
      card("Add or change a role", el("form", { class: "row", onsubmit: async (e) => {
        e.preventDefault();
        try {
          const r = await api("PUT", game("/staff"), { email: email.value, role: pick.value });
          message(status, true, r.temporaryPassword ? `Added. Temporary password (shown once): ${r.temporaryPassword}` : "Saved.");
        } catch (error) { message(status, false, error.message); }
      } }, email, pick, el("button", { class: "primary", type: "submit" }, "Save")), status));
  },

  async audit() {
    view.replaceChildren(card("Audit log (newest 200)", table(await api("GET", game("/audit")))));
  },

  async account() {
    const status = el("p", {});
    const code = el("input", { inputmode: "numeric", maxlength: "6", placeholder: "6-digit code" });
    const twoFactor = state.admin.twoFactorEnabled
      ? [el("p", { class: "ok" }, "Two-factor sign-in is on."), el("div", { class: "row" }, code,
          el("button", { type: "button", onclick: async () => { try { await api("POST", "/admin/me/two-factor/disable", { code: code.value }); await start(); } catch (e) { message(status, false, e.message); } } }, "Turn off"))]
      : [el("p", {}, "Two-factor sign-in asks for a code from an authenticator app each time you sign in."),
         el("button", { type: "button", onclick: async () => {
           const setUp = await api("POST", "/admin/me/two-factor");
           status.replaceChildren(el("p", {}, "Add this key to your authenticator app, then enter its code:"),
             el("p", {}, el("code", {}, setUp.secret)), el("p", { class: "note" }, setUp.uri));
         } }, "Set up"),
         el("div", { class: "row" }, code, el("button", { class: "primary", type: "button", onclick: async () => {
           try { await api("POST", "/admin/me/two-factor/enable", { code: code.value }); await start(); show("account"); } catch (e) { message(status, false, e.message); }
         } }, "Turn on"))];
    const current = el("input", { type: "password", autocomplete: "current-password" });
    const next = el("input", { type: "password", autocomplete: "new-password" });
    const pwStatus = el("p", {});
    view.replaceChildren(card("Two-factor sign-in", ...twoFactor, status),
      card("Password", el("form", { class: "row", onsubmit: async (e) => {
        e.preventDefault();
        try { await api("POST", "/admin/me/password", { currentPassword: current.value, newPassword: next.value }); message(pwStatus, true, "Changed."); }
        catch (error) { message(pwStatus, false, error.message); }
      } }, el("label", {}, "Current", current), el("label", {}, "New (12+ characters)", next), el("button", { type: "submit" }, "Change")), pwStatus));
  },
};

// One player: everything the platform knows, and compensation for support.
async function player(id) {
  view.replaceChildren(el("p", { class: "note" }, "Loading…"));
  const p = await api("GET", game(`/players/${id}`));
  const parts = [
    card("Player " + id, el("button", { type: "button", onclick: () => show("players") }, "← Players"), el("h3", {}, "Account"), pairs(p.account),
      el("h3", {}, "Apple / Google"), table(p.identities), el("h3", {}, "Save"), pairs(p.save), el("h3", {}, "Save check flags"), table(p.flags),
      p.deletion ? el("p", { class: "error" }, `Deletion requested ${p.deletion.requestedAt}; purged by ${p.deletion.deleteBy}.`) : null),
    card("Wallet", pairs(p.wallet), el("h3", {}, "Ledger (newest 100)"), table(p.ledger)),
    card("Purchases", table(p.purchases)),
    card("Support messages", table(p.tickets)),
  ];
  if (can("support") && !p.deletion) {
    const currency = el("input", { value: "gems" });
    const amount = el("input", { type: "number", step: "1", value: "10" });
    const reason = el("input", { placeholder: "Reason (shown in the audit log)", required: true });
    const status = el("p", {});
    parts.splice(1, 0, card("Compensation", el("form", { class: "row", onsubmit: async (e) => {
      e.preventDefault();
      if (!confirm(`Give ${amount.value} ${currency.value} to this player?`)) return;
      try { await api("POST", game(`/players/${id}/grants`), { currency: currency.value, amount: Number(amount.value), reason: reason.value }); await player(id); }
      catch (error) { message(status, false, error.message); }
    } }, el("label", {}, "Currency", currency), el("label", {}, "Amount (negative takes back)", amount), el("label", {}, "Reason", reason),
      el("button", { class: "primary", type: "submit" }, "Grant")), status));
  }
  view.replaceChildren(...parts);
}

// Paths that differ between two JSON values, for comparing configs.
function differences(a, b, path = "", out = []) {
  if (typeof a !== "object" || typeof b !== "object" || a === null || b === null || Array.isArray(a) !== Array.isArray(b)) {
    if (JSON.stringify(a) !== JSON.stringify(b)) out.push(`${path || "(root)"}: ${JSON.stringify(a)} → ${JSON.stringify(b)}`);
    return out;
  }
  for (const key of new Set([...Object.keys(a), ...Object.keys(b)])) {
    const next = Array.isArray(a) ? `${path}[${key}]` : path ? `${path}.${key}` : key;
    if (!(key in a)) out.push(`${next}: added`);
    else if (!(key in b)) out.push(`${next}: removed`);
    else differences(a[key], b[key], next, out);
  }
  return out;
}

start();
