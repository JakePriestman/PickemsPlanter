const VIEW_STORAGE_KEY = 'pickemsViewPreference';
const VIEW_INTRO_SEEN_KEY = 'pickemsBracketViewIntroSeen';
const VIEW_MOBILE_BREAKPOINT = 768;

let bracketViewLoaded = false;

function isMobileViewport() {
    return window.innerWidth <= VIEW_MOBILE_BREAKPOINT;
}

function getStoredView() {
    return localStorage.getItem(VIEW_STORAGE_KEY) === 'bracket' ? 'bracket' : 'simple';
}

function getEffectiveView() {
    return isMobileViewport() ? 'simple' : getStoredView();
}

async function applyView(view) {
    const pickemLayout = document.querySelector('.pickem-layout');
    if (!pickemLayout) return;

    pickemLayout.classList.toggle('view-bracket', view === 'bracket');
    pickemLayout.classList.toggle('view-simple', view !== 'bracket');

    const toggleInput = document.getElementById('viewToggleInput');
    if (toggleInput) toggleInput.checked = view === 'bracket';

    if (view === 'bracket' && !bracketViewLoaded) {
        bracketViewLoaded = true;
        await LoadAsync();
    }
}

function updateToggleRowVisibility() {
    const row = document.getElementById('viewToggleRow');
    if (row) row.hidden = isMobileViewport();
}

function dismissViewToggleIntro() {
    localStorage.setItem(VIEW_INTRO_SEEN_KEY, 'true');
    const intro = document.getElementById('viewToggleIntro');
    if (intro) intro.hidden = true;
}

function showViewToggleIntroIfNeeded() {
    if (isMobileViewport()) return;
    if (localStorage.getItem(VIEW_INTRO_SEEN_KEY) === 'true') return;

    const intro = document.getElementById('viewToggleIntro');
    if (intro) intro.hidden = false;
}

async function handleViewToggleChange(event) {
    const view = event.target.checked ? 'bracket' : 'simple';
    localStorage.setItem(VIEW_STORAGE_KEY, view);
    dismissViewToggleIntro();
    await applyView(view);
}

document.addEventListener('DOMContentLoaded', async () => {
    updateToggleRowVisibility();
    await applyView(getEffectiveView());
    showViewToggleIntroIfNeeded();

    const toggleInput = document.getElementById('viewToggleInput');
    if (toggleInput) toggleInput.addEventListener('change', handleViewToggleChange);

    const introDismiss = document.getElementById('viewToggleIntroDismiss');
    if (introDismiss) introDismiss.addEventListener('click', dismissViewToggleIntro);

    let resizeTimeout;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(async () => {
            updateToggleRowVisibility();
            await applyView(getEffectiveView());
        }, 150);
    });
});
