function toggleLogoutConfirmation() {
    const logoutConfirmationDiv = document.getElementById('logoutConfirmation');

    if (logoutConfirmationDiv) {
        //logoutConfirmationDiv.style.display = logoutConfirmationDiv.style.display === 'none' ? 'flex' : 'none';
        logoutConfirmationDiv.classList.toggle('show');
    }
}