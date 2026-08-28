// POC MoniPay bout en bout : recharge MoMo (Campay sandbox) → solde FCFA → carte USD (Sudo Africa sandbox).
// Zéro dépendance (Node 18+). Sans clés dans .env, tourne en mode MOCK (fournisseurs simulés).
//
// Endpoints fournisseurs (à ajuster si les docs bougent) :
//   Campay demo  : https://demo.campay.net/api        (docs: https://documenter.getpostman.com/view/2391374/T1LV8PVA)
//   Sudo sandbox : https://api.sandbox.sudo.africa    (docs: https://docs.sudo.africa/reference — certains exemples
//                  utilisent api.sandbox.sudo.cards, même API)

const http = require("node:http");
const crypto = require("node:crypto");

// ---------- config ----------
const env = loadEnv();
const DEMO = process.argv.includes("--demo"); // la démo auto-vérifiée reste 100 % mock, même avec un .env rempli
const CAMPAY = { base: "https://demo.campay.net/api", user: env.CAMPAY_APP_USERNAME, pass: env.CAMPAY_APP_PASSWORD };
const SUDO = { base: env.SUDO_BASE || "https://api.sandbox.sudo.africa", key: env.SUDO_API_KEY };
const MOCK_MOMO = DEMO || !CAMPAY.user;
const MOCK_CARD = DEMO || !SUDO.key;
const RATE_FCFA_PER_USD = 628; // ponytail: taux fixe codé en dur, brancher un vrai taux quand on sort du POC
const PORT = Number(env.PORT || 8743);

function loadEnv() {
  const out = { ...process.env };
  try {
    for (const line of require("node:fs").readFileSync(__dirname + "/.env", "utf8").split("\n")) {
      const m = line.match(/^\s*([A-Z_]+)\s*=\s*(.+?)\s*$/);
      if (m) out[m[1]] = m[2];
    }
  } catch {}
  return out;
}

// ---------- état en mémoire ----------
const users = new Map(); // id → {id, name, phone, email, customerId, fcfa, card}

// ---------- Campay (collecte MoMo) ----------
let campayToken = null;
async function campayAuth() {
  if (campayToken) return campayToken;
  const r = await post(`${CAMPAY.base}/token/`, { username: CAMPAY.user, password: CAMPAY.pass });
  campayToken = r.token;
  return campayToken;
}

async function momoCollect(phone, amountFcfa, ref) {
  if (MOCK_MOMO) return { reference: "mock-" + ref, status: "SUCCESSFUL", operator: phone.startsWith("23769") ? "ORANGE" : "MTN" };
  const token = await campayAuth();
  const init = await post(`${CAMPAY.base}/collect/`, {
    amount: String(amountFcfa), currency: "XAF", from: phone,
    description: "Recharge MoniPay", external_reference: ref,
  }, { Authorization: `Token ${token}` });
  // Sandbox : on poll le statut (en prod on prendrait le webhook)
  for (let i = 0; i < 30; i++) {
    const tx = await get(`${CAMPAY.base}/transaction/${init.reference}/`, { Authorization: `Token ${token}` });
    if (tx.status !== "PENDING") return tx;
    await sleep(2000);
  }
  throw new Error("Campay: transaction toujours PENDING après 60s");
}

// ---------- Sudo Africa (carte USD) ----------
const sh = () => ({ Authorization: `Bearer ${SUDO.key}` });

let fundingSourceId = null; // la funding source par défaut du compte sandbox
async function sudoFundingSource() {
  if (fundingSourceId) return fundingSourceId;
  const r = await get(`${SUDO.base}/fundingsources`, sh());
  const list = r.data || [];
  const fs = list.find((f) => f.isDefault) || list[0];
  if (!fs) throw new Error("Aucune funding source sur le compte Sudo — en créer une dans le dashboard (Card Programs / Funding Sources)");
  fundingSourceId = fs._id;
  return fundingSourceId;
}

async function cardCustomer(u) {
  if (MOCK_CARD) return "mock-cust-" + u.id;
  const [first, ...rest] = u.name.split(" ");
  const r = await post(`${SUDO.base}/customers`, {
    type: "individual", status: "active", name: u.name,
    phoneNumber: "+" + u.phone.replace(/^\+/, ""), emailAddress: u.email,
    individual: {
      firstName: first, lastName: rest.join(" ") || first, dob: "1995/06/15",
      identity: { type: "BVN", number: "22222222222" }, // identité de test sandbox — vrai KYC en prod
    },
    billingAddress: { line1: "Rue 1.234", city: "Douala", state: "Littoral", country: "CM", postalCode: "00237" },
  }, sh());
  return r.data._id;
}

async function sudoUsdAccount(customerId) {
  // Un wallet USD par client, débité par sa carte
  const r = await post(`${SUDO.base}/accounts`, { type: "wallet", accountType: "Current", currency: "USD", customerId }, sh());
  return r.data._id;
}

async function cardCreate(customerId, amountUsdCents) {
  if (MOCK_CARD) {
    return { id: "mock-card-" + crypto.randomBytes(3).toString("hex"), currency: "USD", brand: "VISA",
             masked_pan: "4111 **** **** " + String(1000 + Math.floor(Math.random() * 9000)),
             balance_cents: amountUsdCents };
  }
  const dollars = Math.max(1, Math.round(amountUsdCents / 100));
  const accountId = await sudoUsdAccount(customerId);
  await sudoSimFund(accountId, dollars + 2); // sandbox : +2 $ de marge, l'émission prélève des frais en plus du montant
  const r = await post(`${SUDO.base}/cards`, {
    customerId, fundingSourceId: await sudoFundingSource(), debitAccountId: accountId,
    brand: "Visa", type: "virtual", currency: "USD", status: "active", issuerCountry: "USA",
    amount: dollars,
  }, sh());
  const c = r.data || r;
  if (!c._id) throw new Error("Réponse /cards inattendue: " + JSON.stringify(r).slice(0, 1500));
  return { id: c._id, account_id: c.account?._id || accountId, currency: c.currency, brand: c.brand, masked_pan: c.maskedPan, balance_cents: amountUsdCents };
}

async function sudoSimFund(accountId, dollars) {
  // Endpoint sandbox uniquement — en prod le compte se crédite par virement réel
  return post(`${SUDO.base}/accounts/simulator/fund`, { accountId, amount: dollars }, sh());
}

async function cardFund(card, amountUsdCents) {
  if (MOCK_CARD) return { funded: amountUsdCents };
  return sudoSimFund(card.account_id, Math.max(1, Math.round(amountUsdCents / 100)));
}

// ---------- logique métier ----------
async function signup({ name, phone, email }) {
  if (!name || !phone || !email) throw new Error("name, phone, email requis");
  const u = { id: crypto.randomBytes(4).toString("hex"), name, phone, email, fcfa: 0, card: null };
  u.customerId = await cardCustomer(u);
  users.set(u.id, u);
  return u;
}

async function topup(u, amountFcfa) {
  if (!(amountFcfa > 0)) throw new Error("montant invalide");
  const tx = await momoCollect(u.phone, amountFcfa, u.id + "-" + Date.now());
  if (tx.status !== "SUCCESSFUL") throw new Error("Recharge MoMo échouée: " + tx.status);
  u.fcfa += amountFcfa;
  return { tx, fcfa: u.fcfa };
}

async function issueCard(u, amountUsd) {
  const cents = Math.round(amountUsd * 100);
  const costFcfa = Math.ceil(amountUsd * RATE_FCFA_PER_USD);
  if (costFcfa > u.fcfa) throw new Error(`Solde insuffisant: il faut ${costFcfa} F, solde ${u.fcfa} F`);
  u.fcfa -= costFcfa;
  if (u.card) {
    await cardFund(u.card, cents);
    u.card.balance_cents = (u.card.balance_cents || 0) + cents;
  } else {
    u.card = await cardCreate(u.customerId, cents);
  }
  return { card: u.card, debited_fcfa: costFcfa, fcfa: u.fcfa };
}

// ---------- HTTP ----------
const routes = {
  "POST /signup": (body) => signup(body),
  "POST /topup": (body) => topup(mustUser(body.user_id), Number(body.amount_fcfa)),
  "POST /card": (body) => issueCard(mustUser(body.user_id), Number(body.amount_usd)),
  "GET /user": (_, q) => mustUser(q.get("id")),
};
const mustUser = (id) => users.get(id) ?? (() => { throw new Error("utilisateur inconnu"); })();

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, "http://x");
  const handler = routes[`${req.method} ${url.pathname}`];
  try {
    if (!handler) throw new Error("route inconnue");
    const body = req.method === "POST" ? JSON.parse((await readBody(req)) || "{}") : null;
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(await handler(body, url.searchParams), null, 2));
  } catch (e) {
    res.writeHead(400, { "content-type": "application/json" });
    res.end(JSON.stringify({ error: e.message }));
  }
});

// ---------- utilitaires ----------
async function post(url, body, headers = {}) { return call("POST", url, body, headers); }
async function get(url, headers = {}) { return call("GET", url, null, headers); }
async function call(method, url, body, headers) {
  const r = await fetch(url, {
    method, headers: { "content-type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  if (!r.ok) throw new Error(`${method} ${url} → ${r.status}: ${text.slice(0, 2000)}`);
  return text ? JSON.parse(text) : {};
}
const readBody = (req) => new Promise((ok) => { let d = ""; req.on("data", (c) => (d += c)); req.on("end", () => ok(d)); });
const sleep = (ms) => new Promise((ok) => setTimeout(ok, ms));

// ---------- démo auto-vérifiée (mode mock) ----------
async function demo() {
  const assert = require("node:assert");
  const u = await signup({ name: "Awa Ndongo", phone: "237670000001", email: "awa@example.com" });
  await topup(u, 20000);
  assert.equal(u.fcfa, 20000);
  const r = await issueCard(u, 25); // 25 $ = 15 700 F
  assert.equal(u.fcfa, 20000 - 25 * RATE_FCFA_PER_USD);
  assert.ok(r.card.id && r.card.masked_pan);
  await issueCard(u, 5); // recharge de la carte existante
  assert.equal(u.card.balance_cents, 3000);
  await assert.rejects(() => issueCard(u, 999), /Solde insuffisant/);
  console.log("✅ Démo OK — utilisateur:", u.name, "| carte:", u.card.masked_pan, "| solde carte:", u.card.balance_cents / 100, "$ | reste:", u.fcfa, "F");
}

module.exports = { signup, topup, issueCard, users, MOCK_MOMO, MOCK_CARD };

if (require.main === module) {
  if (process.argv.includes("--demo")) {
    demo().then(() => process.exit(0), (e) => { console.error("❌", e.message); process.exit(1); });
  } else {
    server.listen(PORT, () => console.log(`POC MoniPay sur http://localhost:${PORT} — MoMo: ${MOCK_MOMO ? "MOCK" : "Campay sandbox"} | Carte: ${MOCK_CARD ? "MOCK" : "Sudo sandbox"}`));
  }
}
