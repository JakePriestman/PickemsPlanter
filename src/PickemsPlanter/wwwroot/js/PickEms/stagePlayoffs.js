let currentDraggedElement = null;
let dragOriginElement = null;
let isDragging = false;
let picksAllowed = false;
let isPlayoffs = false;
let teamNameMap = {};
let { eventId, steamId, stage, picks } = window.pageData;

function openSimulator(url, width, height) {
    const winWidth = window.innerWidth;
    const winHeight = window.innerHeight;

    const winLeft = window.screenX;
    const winTop = window.screenY;

    const left = winLeft + (winWidth - width) / 2;
    const top = winTop + (winHeight - height) / 2;

    window.open(
        url,
        "popupWindow",
        `width=${width},height=${height},left=${left},top=${top}`
    );
}

if (stage) {
    document.getElementById("simulatorButton").addEventListener("click", function (e) {
    e.preventDefault();
    openSimulator(`/Simulator?EventId=${eventId}&Stage=${stage}`, 900, 700);
});

}
document.getElementById('saveForm').addEventListener('submit', getImageNamesAndParseToJson);


document.getElementById("profileImage").addEventListener('click', handleNavBarStyling);

document.addEventListener('pointermove', dragging);

document.addEventListener('pointerup', () => {
    dragEnd(isPlayoffs);
});