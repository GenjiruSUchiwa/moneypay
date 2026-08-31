# Provider RFI — information to request before signing

Providers in scope (from `docs/handoff/`): Campay (MoMo collection, primary), Notch Pay (plan B),
pawaPay (multi-country later), Sudo Africa (USD virtual card issuing).

## 1. Checklist of what to ask

### A. Onboarding & compliance (both kinds of provider)
- KYB documents required for a newly incorporated startup (RCCM, NIU, statutes, director IDs, proof of address)
- Is a BEAC / COBAC licence or a partnership with a licensed EMI required, or does the provider carry the licence on our behalf?
- Onboarding timeline: sandbox → KYB approval → production keys
- Countries supported today for collection / issuing, and which are on the roadmap (Cameroon first, then CEMAC)
- Restrictions on our business model (wallet holding customer funds, FX to USD, card top-ups)

### B. Pricing
- Per-transaction fees (percentage + fixed), by operator (MTN vs Orange) and by channel (collection, payout, card load)
- Setup fee, monthly minimum, deposit or pre-funding requirement
- Volume tiers and a startup / early-stage plan
- FX: rate source, spread/margin, who bears it, is it locked at authorization or settlement
- Chargeback, refund, failed-transaction and reversal fees
- Card-specific: card creation fee, monthly fee per card, funding fee, decline fee, cross-border fee, dormant-card fee

### C. Limits
- Min / max per transaction, per day, per month — per customer and at account level
- Production limits at launch and how they scale with volume or KYC tier
- Card-specific: max cards per customer, max balance per card, max card load per day

### D. Settlement & funds
- Settlement cycle (T+0 / T+1 / T+n), currency, and bank / MoMo destination
- Pre-funded model vs post-paid; float requirements in XAF and in USD
- Reconciliation exports (CSV / API), statement frequency
- Where are funds held, and under which entity / regulator

### E. Technical
- Webhooks: events, signature scheme, retry policy, idempotency guarantees
- Timeouts and pending-state semantics (USSD push not answered, operator downtime)
- Sandbox limits (Campay demo caps at 25 XAF — is the production sandbox different?)
- API stability / versioning / deprecation policy, SLA and uptime history
- Rate limits, IP allow-listing, key rotation
- Card-specific: 3-D Secure support, authorization webhooks (real-time vs batch), merchant restrictions (MCC), Apple Pay tokenization, BIN country, card scheme (Visa / Mastercard)

### F. Operations & support
- Support channels, hours, escalation path, dedicated account manager
- Dispute and chargeback process, evidence window
- Reporting obligations we inherit (AML/CFT, suspicious transaction reports)
- Data residency and PCI-DSS scope (do we ever touch PAN/CVV?)

### G. Commercial
- Contract term, exclusivity, termination notice
- References of startups already live with them in Cameroon / CEMAC

## 2. Email — French (Campay, Notch Pay)

Objet : Demande d'informations — intégration [collecte Mobile Money] pour MoniPay

Bonjour,

Je suis [Prénom Nom], cofondateur de MoniPay, une startup camerounaise en cours de lancement. Nous développons une application mobile qui permet à un utilisateur de recharger un portefeuille en FCFA via MTN Mobile Money et Orange Money, puis de convertir ce solde en dollars pour obtenir une carte virtuelle USD utilisable en ligne.

Nous avons déjà réalisé un prototype fonctionnel sur votre environnement sandbox et souhaitons préparer le passage en production. Afin d'évaluer précisément votre offre, pourriez-vous nous communiquer les éléments suivants ?

1. **Onboarding et conformité** : documents KYB requis pour une société nouvellement immatriculée, exigences réglementaires (BEAC / COBAC) et délai moyen jusqu'à l'obtention des clés de production.
2. **Tarification** : frais par transaction (MTN et Orange), frais de mise en service, minimum mensuel, paliers de volume, et l'existence d'une offre adaptée aux jeunes startups.
3. **Plafonds** : montants minimum et maximum par transaction, par jour et par mois, au niveau client et au niveau compte, et les conditions pour les faire évoluer.
4. **Règlement des fonds** : cycle de règlement, modèle préfinancé ou différé, exports de réconciliation.
5. **Technique** : webhooks (événements, signature, politique de retry), gestion des transactions en attente, limites de l'environnement de test, SLA et disponibilité.
6. **Support** : canaux, horaires, procédure de litige et d'escalade.
7. **Contrat** : durée, préavis de résiliation, et si possible des références de startups déjà en production avec vous au Cameroun.

Nous serions ravis d'échanger lors d'un court appel si cela vous convient. Vous trouverez ci-joint une présentation d'une page de MoniPay.

Bien cordialement,
[Prénom Nom]
[Rôle] — MoniPay
[Téléphone] · [Email] · [Site]

## 3. Email — English (Sudo Africa, pawaPay)

Subject: Information request — [USD virtual card issuing / mobile money collection] for MoniPay

Hello,

I am [First Last], co-founder of MoniPay, a Cameroon-based fintech startup preparing to launch. We are building a mobile app where users top up an XAF wallet through MTN Mobile Money and Orange Money, convert the balance to USD and receive a virtual card for online payments.

We already have a working prototype on your sandbox and are now planning the move to production. To evaluate your offer properly, could you share the following?

1. **Onboarding & compliance**: KYB documents required for a newly incorporated company, licensing requirements (do you carry the issuing / EMI licence on our behalf?), supported countries, and the typical timeline to production keys.
2. **Pricing**: per-transaction fees, card creation and monthly card fees, funding and FX fees (rate source and spread), decline and cross-border fees, setup fee, monthly minimum, and whether an early-stage / startup plan exists.
3. **Limits**: per-transaction, daily and monthly limits, max cards per customer, max card balance, and how limits scale with volume or KYC tier.
4. **Settlement & funding**: pre-funding requirements in USD and XAF, settlement cycle, reconciliation exports.
5. **Technical**: authorization and transaction webhooks (real-time?), signature and retry policy, 3-D Secure support, tokenization (Apple Pay), MCC restrictions, sandbox limitations, SLA and uptime history.
6. **Support & disputes**: support channels and hours, chargeback process and evidence window, escalation path.
7. **Commercial**: contract term, exclusivity, termination notice, and references of startups live with you in Central / West Africa.

We would be glad to schedule a short call. A one-page overview of MoniPay is attached.

Best regards,
[First Last]
[Role] — MoniPay
[Phone] · [Email] · [Website]

## 4. Email — formal English, card issuing (Maplerad, Onafriq, Sudo Africa)

Subject: Request for Information — USD Virtual Card Issuing Partnership for MoniPay (Cameroon / CEMAC)

Dear [Partnerships / Sales] Team,

My name is [First Last], and I am the co-founder of MoniPay, an early-stage fintech company incorporated in Cameroon. I am writing to request detailed information about your card issuing offering, with a view to selecting an issuing partner ahead of our production launch.

**About MoniPay**

MoniPay is a mobile application for consumers in the CEMAC region. Users fund an XAF wallet through MTN Mobile Money and Orange Money, convert their balance to US dollars, and receive a virtual USD card for online purchases — subscriptions, digital services and international e-commerce that remain inaccessible to most local payment instruments. Our target market is Cameroon at launch, with an expansion to the wider CEMAC zone (Gabon, Congo, Chad, Central African Republic, Equatorial Guinea) planned thereafter.

We have already built a functional prototype covering the full flow — mobile money collection, wallet ledger, FX conversion and card issuance — against sandbox environments, and we are now formalising our choice of production partners. Card issuing is the cornerstone of our product; we are therefore seeking a partner able to support us from our first thousand cards through to meaningful scale.

To that end, I would be grateful if you could provide the information below. Where a question does not apply to your model, a brief note to that effect would be equally helpful.

**1. Eligibility, onboarding and regulatory framework**
- The KYB documentation you require from a newly incorporated Cameroonian company (commercial registration, tax identifier, articles of association, directors' identification, proof of address, and any additional items).
- Whether you can issue cards to residents of Cameroon and the other CEMAC member states, and the countries on your issuing roadmap.
- The regulatory structure of the programme: which entity holds the issuing licence and the scheme membership, in which jurisdiction, and whether MoniPay must hold any licence or regulatory authorisation of its own, or engage a sponsoring institution.
- Your expectations regarding end-customer KYC: whether you perform identity verification yourselves, accept our verification, or require a specific vendor; and the identity documents accepted for Cameroonian nationals.
- The average timeline from application to production access, and any pilot or limited-launch phase you offer to early-stage companies.

**2. Card product characteristics**
- Card scheme(s) available (Visa, Mastercard), BIN country and card type (prepaid, debit).
- Whether cards are issued under your BIN or a dedicated BIN, and whether white-labelling (our brand on the card and on 3-D Secure pages) is available.
- Supported card controls: freeze / unfreeze, spending limits per card, per-transaction and per-period, merchant category restrictions, single-use versus multi-use cards, and card expiry.
- 3-D Secure support and the authentication flow offered to the cardholder.
- Digital wallet tokenisation, in particular Apple Pay, and the timeline if not yet available.
- Any known merchant acceptance limitations (merchants or categories that routinely decline your BIN).

**3. Pricing**
- One-time and recurring fees: programme setup, monthly platform or minimum fee, and any annual fee.
- Per-card fees: issuance, monthly maintenance, replacement and closure.
- Transaction fees: purchase authorisation, declined transaction, cross-border and international transaction fees, chargeback and dispute handling.
- Funding fees: cost of loading a card, and any fee on unloading or on residual balances.
- Foreign-exchange terms, if you provide XAF-to-USD conversion: rate source, applied spread, whether the rate is fixed at authorisation or at settlement, and who bears exchange-rate movements.
- Volume-based tiers, and whether you offer preferential terms or a reduced-commitment plan for early-stage companies.

**4. Limits**
- Minimum and maximum balance per card, per-transaction limit, and daily / monthly spending limits.
- Maximum number of cards per cardholder and per programme.
- Card loading limits per transaction, per day and per month.
- The initial limits applied to a new programme and the criteria under which they are raised.

**5. Funding, settlement and reconciliation**
- The funding model: pre-funded float in USD, credit line, or on-demand funding; the minimum float required and the currencies in which it may be held.
- Accepted methods for funding the programme account from Cameroon, and the associated lead times and fees.
- The authorisation and settlement mechanics: whether card balances are held with you or in a ledger we control (just-in-time funding), and the timing of debits to our float.
- Reconciliation and reporting: statement frequency, export formats, and API access to settlement and transaction data.
- Custody of funds: which entity holds cardholder and programme balances, in which institution, and under which safeguarding arrangements.

**6. Technical integration**
- API documentation, SDKs, and the current API version and deprecation policy.
- Webhook coverage — authorisation, clearing, decline, refund, chargeback, card status changes — including delivery guarantees, signature scheme, retry policy and idempotency.
- Whether real-time authorisation decisioning is available (i.e. you call our endpoint to approve or decline an authorisation), and the latency budget you impose.
- Sandbox capabilities and limitations, and how closely the sandbox mirrors production behaviour.
- PCI-DSS scope: whether MoniPay handles card numbers or CVVs at any point, or whether you provide hosted or tokenised card display (secure iframe, mobile SDK).
- Service levels: uptime commitment, historical availability, incident communication, and support response times.

**7. Disputes, risk and compliance obligations**
- The chargeback and dispute process, evidence requirements and time windows.
- Fraud monitoring provided by you, and the tools available to us for velocity checks and rule configuration.
- Compliance obligations that MoniPay inherits under the programme (transaction monitoring, suspicious activity reporting, sanctions screening, record retention).
- Data residency and data protection arrangements.

**8. Commercial terms**
- Standard contract term, exclusivity provisions, termination notice and exit conditions, including the migration of active cards.
- References of comparable companies already live with you in Central or West Africa.

I would welcome the opportunity to discuss our project in more detail on a call at your convenience, and I have attached a short overview of MoniPay for your reference. Should you require any further information to assess our application, please do not hesitate to let me know.

Thank you for your time and consideration. I look forward to hearing from you.

Yours sincerely,

[First Last]
Co-founder — MoniPay
[Phone] · [Email] · [Website]
[Company registration number, if available]
