document.addEventListener("DOMContentLoaded", function () {
    const buttons = document.querySelectorAll(".showAuthCode");

    buttons.forEach(button => {
        button.addEventListener("click", async function (event) {
            event.preventDefault();
            const idSuffix = button.id.split("-")[1];

            const input = document.getElementById(`authCode-${idSuffix}`);

            if (input.type === "text") {
                input.type = "password";
                button.querySelector("img").src = "/Images/showpassword.png";
            } else {
                const authCode = await showAuthCode(idSuffix);
                input.type = "text";
                input.value = authCode === null ? input.value : authCode.authCode;
                button.querySelector("img").src = "/Images/hidepassword.png";
            }
        });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    // Select all AuthCode input fields
    const authCodeInputs = document.querySelectorAll(".AuthCode");

    authCodeInputs.forEach(input => {
        // Get the corresponding event container (the parent .event div)
        const eventContainer = input.closest(".event");

        // Find the two buttons inside that container
        const deleteButton = eventContainer.querySelector(".event-button-delete");
        const selectButton = eventContainer.querySelector(".event-button-select");
        const showButton = eventContainer.querySelector(".showAuthCode");

        // Define a helper function to toggle button state
        const toggleButtons = () => {
            const hasText = input.value.trim() !== "";
            deleteButton.disabled = !hasText;
            selectButton.disabled = !hasText;
            showButton.disabled = !hasText;
        };

        // Initial check (in case the page loads with existing text)
        toggleButtons();

        // Listen for text input changes
        input.addEventListener("input", toggleButtons);
    });
});

async function showAuthCode(eventId) {
    const response = await fetch(`?handler=AuthCode&eventId=${eventId}`);

    if (!response.ok) return null;

    const data = await response.json();

    return data;
}