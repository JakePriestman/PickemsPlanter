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

function openCoinProgress(eventId, steamId) {
    const width = 460;
    const height = 700;

    const winWidth = window.innerWidth;
    const winHeight = window.innerHeight;

    const winLeft = window.screenX;
    const winTop = window.screenY;

    const left = winLeft + (winWidth - width) / 2;
    const top = winTop + (winHeight - height) / 2;

    window.open(
        `/CoinProgress?eventId=${eventId}&steamId=${steamId}`,
        "coinProgressPopup",
        `width=${width},height=${height},left=${left},top=${top}`
    );
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