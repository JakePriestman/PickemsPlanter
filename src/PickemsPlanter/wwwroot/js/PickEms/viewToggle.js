const VIEW_STORAGE_KEY = 'pickemsViewPreference';
const VIEW_INTRO_SEEN_KEY = 'pickemsBracketViewIntroSeen';
const VIEW_MOBILE_BREAKPOINT = 768;

let bracketViewLoaded = false;

// pick0-9 physically relocate into the live bracket's own terminal containers (3-0/3-1/
// 3-2/0-3) in Bracket View, then back into Simple View's own layout when switching back —
// same elements moved via appendChild, never cloned, so all existing save/load/drag-drop
// code (which is id-based, not DOM-order-based — see getImageNamesAndParseToJson) keeps
// working unchanged regardless of which parent currently holds them.
const PICK_TERMINAL_GROUPS = {
    '3-0': ['pick0', 'pick1'],
    '3-1': ['pick2', 'pick3', 'pick4'],
    '3-2': ['pick5', 'pick6', 'pick7'],
    '0-3': ['pick8', 'pick9']
};

let pickHomeParents = null;

function relocatePicksForView(view) {
    const allPickIds = Object.values(PICK_TERMINAL_GROUPS).flat();

    if (!pickHomeParents) {
        pickHomeParents = {};

        for (const id of allPickIds) {
            const el = document.getElementById(id);
            if (el) pickHomeParents[id] = el.parentElement;
        }
    }

    if (view === 'bracket') {
        for (const [groupId, pickIds] of Object.entries(PICK_TERMINAL_GROUPS)) {
            const container = document.getElementById(groupId);
            if (!container) continue;

            container.innerHTML = '';

            for (const pickId of pickIds) {
                const el = document.getElementById(pickId);
                if (el) container.appendChild(el);
            }
        }
    } else {
        for (const id of allPickIds) {
            const el = document.getElementById(id);
            const home = pickHomeParents[id];
            if (el && home) home.appendChild(el);
        }
    }
}

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

    relocatePicksForView(view);

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
