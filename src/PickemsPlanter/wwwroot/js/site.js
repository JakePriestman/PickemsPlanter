function toggleLogoutConfirmation() {
    const logoutConfirmationDiv = document.getElementById('logoutConfirmation');

    if (logoutConfirmationDiv) {
        logoutConfirmationDiv.classList.toggle('show');
    }
}

function togglePicksNotAllowedConfirmation() {
    const picksNotAllowedConfirmation = document.getElementById('picksNotAllowedConfirmation');

    if (picksNotAllowedConfirmation) {
        picksNotAllowedConfirmation.classList.toggle('show');
    }
}