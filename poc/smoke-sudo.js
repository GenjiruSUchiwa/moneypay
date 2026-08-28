// Test réel Sudo sandbox : signup (customer) + carte USD, en créditant le solde FCFA
// à la main (le vrai topup MoMo est testé séparément — il envoie un push USSD).
const { signup, issueCard, MOCK_CARD } = require("./server");

(async () => {
  if (MOCK_CARD) throw new Error("SUDO_API_KEY absent — ce test vise le sandbox réel");
  const u = await signup({ name: "Aristide Mbassi", phone: "237670000001", email: "aristide.mbassi28@gmail.com" });
  console.log("✅ customer Sudo créé:", u.customerId);
  u.fcfa = 20000; // crédit manuel, on court-circuite le MoMo pour ce test
  const r = await issueCard(u, 25);
  console.log("✅ carte USD créée:", JSON.stringify(r.card, null, 2));
  console.log("Reste:", u.fcfa, "F");
})().catch((e) => { console.error("❌", e.message); process.exit(1); });
