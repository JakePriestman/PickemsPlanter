async function getAuthCodeAsync(eventId) {
    const response = await fetch(`?handler=AuthCode&eventId=${eventId}`);

    if (!response.ok) return null;

    const data = await response.json();

    return data;
}

async function showAuthCodeAsync(event, button) {
    event.preventDefault();
    const idSuffix = button.id.split("-")[1];

    const input = document.getElementById(`authCode-${idSuffix}`);

    if (input.type === "text") {
        input.type = "password";
        button.classList.remove("is-visible");
    } else {
        const authCode = await getAuthCodeAsync(idSuffix);
        input.type = "text";
        input.value = authCode === null ? input.value : authCode.authCode;
        button.classList.add("is-visible");
    }
}

document.addEventListener("DOMContentLoaded", function () {
    const buttons = document.querySelectorAll(".show-auth-code");

    buttons.forEach(button => {
        button.addEventListener("click", async function(event) {
            await showAuthCodeAsync(event, button);
        });
    });
});

function toggleEventButtons(input) {
    const eventContainer = input.closest(".event");

    const deleteButton = eventContainer.querySelector(".event-button-delete");
    const selectButton = eventContainer.querySelector(".event-button-select");
    const showButton = eventContainer.querySelector(".show-auth-code");
    const coinButton = eventContainer.querySelector(".event-button-coin");

    const toggleButtons = () => {
        const hasText = input.value.trim() !== "";
        deleteButton.disabled = !hasText;
        selectButton.disabled = !hasText;
        showButton.disabled = !hasText;
        coinButton.disabled = !hasText;
    };

    toggleButtons();

    input.addEventListener("input", toggleButtons);
}

async function toggleCoinProgress(eventId) {
    const existingPanel = document.querySelector(".coin-progress-panel");
    const wasThisEventOpen = existingPanel?.dataset.eventId === eventId;

    existingPanel?.remove();

    if (wasThisEventOpen) return;

    const eventContainer = document.querySelector(`.event[data-event-id="${eventId}"]`);
    if (!eventContainer) return;

    const panel = document.createElement("div");
    panel.className = "coin-progress-panel";
    panel.dataset.eventId = eventId;
    panel.innerHTML = '<div class="coin-progress-container"><p class="coin-progress-count">Loading coin progress...</p></div>';

    eventContainer.insertAdjacentElement("afterend", panel);

    const response = await fetch(`?handler=CoinProgress&eventId=${eventId}`);

    if (!response.ok) {
        panel.innerHTML = '<div class="coin-progress-container"><p class="coin-progress-count">Could not load coin progress.</p></div>';
        return;
    }

    const progress = await response.json();
    const eventName = eventContainer.querySelector("h2")?.textContent ?? "";

    panel.innerHTML = renderCoinProgress(progress, eventName);
}

// Assumes "<Org> City Year <...>" naming (eg. "IEM Cologne 2026 CS2 Major Championship" ->
// "COLOGNE 2026") — the coin's rim only has room for the short form, matching the reference
// coin design. Locates the city by finding the 4-digit year token and taking the word right
// before it, rather than assuming it's the second-to-last word — safe for multi-word city
// names (eg. "Rio de Janeiro") since it doesn't depend on where the year falls in the string.
function cityAndYear(eventName) {
    const words = eventName.trim().split(/\s+/).filter(Boolean);
    const yearIndex = words.findIndex(word => /^\d{4}$/.test(word));

    if (yearIndex > 0) {
        return `${words[yearIndex - 1]} ${words[yearIndex]}`.toUpperCase();
    }

    return words.slice(-2).join(" ").toUpperCase();
}

function renderCoinProgress(progress, eventName) {
    const tierClass = progress.tier.toLowerCase();
    const percent = progress.totalChallenges === 0 ? 0 : (100 * progress.completedChallenges / progress.totalChallenges).toFixed(2);

    const challengesHtml = progress.challenges.map(challenge => `
        <li class="coin-challenge ${challenge.completed ? "completed" : ""}">
            <span class="coin-challenge-check">${challenge.completed ? "✓" : ""}</span>
            <span class="coin-challenge-name">${challenge.name}</span>
        </li>
    `).join("");

    return `
        <div class="coin-progress-container">
            <div class="coin-progress-header">
                <div class="coin-badge coin-tier-${tierClass}">
                    <svg viewBox="0 0 100 100" class="coin-svg" aria-hidden="true">
                        <defs>
                            <radialGradient id="coinFace-${tierClass}" cx="35%" cy="30%" r="75%">
                                <stop offset="0%" stop-color="var(--coin-light)" />
                                <stop offset="55%" stop-color="var(--coin-base)" />
                                <stop offset="100%" stop-color="var(--coin-dark)" />
                            </radialGradient>
                            <!-- Both arcs bulge the same visual "smile" direction their side of the coin
                                 needs (top arc over the top, bottom arc under the bottom), but travel in
                                 OPPOSITE directions (left-to-right vs right-to-left, opposite sweep-flag) —
                                 that's what keeps the bottom text reading upright instead of upside-down;
                                 tracing both the same direction mirrors the bottom text. Radius 40 sits in
                                 the middle of the bezel band (33-46) — a dedicated wide ring for the text,
                                 separate from the inner face, rather than text crowded near the edge. -->
                            <path id="coinTopArc-${tierClass}" d="M 10,53 A 40,40 0 0 1 90,53" />
                            <path id="coinBottomArc-${tierClass}" d="M 10,53 A 40,40 0 0 0 90,53" />
                            <path id="coinStar-${tierClass}" d="M50 27 L56.5 43.5 L74 44.5 L60 55 L65 72 L50 62 L35 72 L40 55 L26 44.5 L43.5 43.5 Z" />
                            <!-- Stamped/embossed look for the rim text: a dark shadow offset one way and a
                                 light highlight offset the other, simulating a raised relief under a
                                 top-left light source. -->
                            <filter id="coinEmboss-${tierClass}" x="-30%" y="-30%" width="160%" height="160%">
                                <feDropShadow dx="0.7" dy="0.7" stdDeviation="0.15" flood-color="#000000" flood-opacity="0.6" />
                                <feDropShadow dx="-0.5" dy="-0.5" stdDeviation="0.15" flood-color="var(--coin-light)" flood-opacity="0.9" />
                            </filter>
                        </defs>
                        <circle cx="50" cy="50" r="46" fill="url(#coinFace-${tierClass})" stroke="var(--coin-dark)" stroke-width="3" />
                        <circle cx="50" cy="50" r="33" fill="none" stroke="var(--coin-light)" stroke-width="1.5" opacity="0.55" />
                        <use href="#coinStar-${tierClass}" fill="var(--coin-dark)" opacity="0.85" />
                        <use href="#coinStar-${tierClass}" fill="var(--coin-dark)" opacity="0.7" transform="translate(10,50) scale(0.14) translate(-50,-50)" />
                        <use href="#coinStar-${tierClass}" fill="var(--coin-dark)" opacity="0.7" transform="translate(90,50) scale(0.14) translate(-50,-50)" />
                        <text class="coin-rim-text" font-size="8" filter="url(#coinEmboss-${tierClass})">
                            <textPath href="#coinTopArc-${tierClass}" startOffset="50%" text-anchor="middle">CS2 MAJOR</textPath>
                        </text>
                        <text class="coin-rim-text" font-size="8" filter="url(#coinEmboss-${tierClass})">
                            <textPath href="#coinBottomArc-${tierClass}" startOffset="50%" text-anchor="middle">${cityAndYear(eventName)}</textPath>
                        </text>
                    </svg>
                </div>
                <p class="coin-tier-name coin-tier-${tierClass}">${progress.tier}</p>
                <p class="coin-progress-count">${progress.completedChallenges} / ${progress.totalChallenges} challenges completed</p>
            </div>
            <div class="coin-progress-bar">
                <div class="coin-progress-bar-fill coin-tier-${tierClass}" style="width: ${percent}%;"></div>
            </div>
            <ul class="coin-challenge-list">${challengesHtml}</ul>
        </div>
    `;
}

document.addEventListener("DOMContentLoaded", function () {
    const authCodeInputs = document.querySelectorAll(".auth-code");

    authCodeInputs.forEach(input => {
        toggleEventButtons(input);
    });
});

function toggleExtraInformation() {
    var popup = document.getElementById("extraInformation");
    popup.classList.toggle("show");
}

document.addEventListener("click", function (e) {
    var popup = document.getElementById("extraInformation");
    var btn = document.querySelector(".information-button");
    if (popup && popup.classList.contains("show") &&
        !popup.contains(e.target) && !btn.contains(e.target)) {
        popup.classList.remove("show");
    }
});