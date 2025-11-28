let currentDraggedElement = null;
let dragOriginElement = null;
let isDragging = false;
let picksAllowed = false;
let isPlayoffs = false;
let { eventId, steamId, stage, picks } = window.pageData;

document.getElementById('saveForm').addEventListener('submit', getImageNamesAndParseToJson);

document.addEventListener("DOMContentLoaded", getEventSpecificStylesheet);

document.getElementById("profileImage").addEventListener('click', handleNavBarStyling);

document.addEventListener('pointermove', dragging);

document.addEventListener('mouseup', () => {
    dragEnd(isPlayoffs);
});