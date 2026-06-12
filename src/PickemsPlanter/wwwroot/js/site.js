function toggleLogoutConfirmation() {
    const logoutConfirmationDiv = document.getElementById('logoutConfirmation');

    if (logoutConfirmationDiv) {
        logoutConfirmationDiv.classList.toggle('show');
    }
}

function togglePicksNotAllowedConfirmation() {
    const picksNotAllowedConfirmation = document.getElementById('picksNotAllowedConfirmation');

    if (picksNotAllowedConfirmation) {
        picksNotAllowedConfirmation.classList.add('show');
    }
}

function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    requestAnimationFrame(() => {
        requestAnimationFrame(() => toast.classList.add('show'));
    });

    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => toast.remove(), { once: true });
    }, 2500);
}