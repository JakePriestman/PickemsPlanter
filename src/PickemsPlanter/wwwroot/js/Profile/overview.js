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
        button.querySelector("img").src = "/Images/showpassword.png";
    } else {
        const authCode = await getAuthCodeAsync(idSuffix);
        input.type = "text";
        input.value = authCode === null ? input.value : authCode.authCode;
        button.querySelector("img").src = "/Images/hidepassword.png";
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

    const toggleButtons = () => {
        const hasText = input.value.trim() !== "";
        deleteButton.disabled = !hasText;
        selectButton.disabled = !hasText;
        showButton.disabled = !hasText;
    };

    toggleButtons();

    input.addEventListener("input", toggleButtons);
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