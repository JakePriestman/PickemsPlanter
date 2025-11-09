function allowDrop(ev) {
    ev.preventDefault();
}

function enableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "true");
    item.setAttribute("ondragstart", "drag(event)");
}

function disableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "false");
}

function drag(ev) {
    if (ev.target.src.includes("unknown"))
        return;
    ev.dataTransfer.setData("sourceId", ev.currentTarget.id);
}
function checkDropzonesFilled() {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    const allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('img.dropped-img') !== null
    );

    const saveButton = document.getElementById('saveButton');
    const { picksAllowed } = window.pageData;

    if (saveButton) {
        saveButton.disabled = !allFilled || !picksAllowed;
        saveButton.textContent = allFilled ? "Plant Picks" : "All picks need to be within the dropzones to plant your picks";
    } else {
        console.warn("Save button with id 'saveButton' not found.");
    }
}

function placeImageInDropzone(imageSrc, dropzone, isPlayoffs) {
    if (!imageSrc || imageSrc.includes("unknown") || !dropzone) return;

    const filename = imageSrc.split('/').pop();
    const imageInDropzone = dropzone.querySelector('img');

    if (!isPlayoffs) {

        const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated', 'match');
        allDropzones.forEach(zone => {
            const existingImg = zone.querySelector('img');
            if (existingImg) {
                const existingFilename = existingImg.src.split('/').pop();
                if (existingFilename === filename) {
                    zone.innerHTML = '';
                    resetDropzoneStyle(zone);
                }
            }
        });
    }

    if (isPlayoffs && imageInDropzone != null) {
        const imageToRemoveName = imageInDropzone.src.split('/').pop()

        const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated', 'match');

        const dropzonesToRemove = getDropzonesAfter(dropzone.id, allDropzones);

        dropzonesToRemove.forEach(zone => {
            const existingImg = zone.querySelector('img');
            if (existingImg) {
                const existingFilename = existingImg.src.split('/').pop();

                if (existingFilename === imageToRemoveName) {
                    zone.innerHTML = '';
                }
            }
        });
    }

    dropzone.innerHTML = '';

    const img = document.createElement("img");
    img.src = imageSrc;
    img.className = "dropped-img";

    if (!isPlayoffs) img.draggable = false;

    dropzone.appendChild(img);

    dropzone.style.width = "64px";
    dropzone.style.height = "64px";
    dropzone.style.justifyContent = "center";
    dropzone.style.alignItems = "center";

    checkDropzonesFilled();
}

function getDropzonesAfter(currentId, allDropzones) {
    const currentNumber = parseInt(currentId.replace('pick', ''), 10);

    return Array.from(allDropzones).filter(dz => {
        const dzNumber = parseInt(dz.id.replace('pick', ''), 10);
        return dzNumber > currentNumber;
    });
}

document.getElementById('saveForm').addEventListener('submit', function (e) {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    const imagesData = Array.from(dropzones).map(zone => {
        const img = zone.querySelector('img.dropped-img');
        if (!img) return "";

        const fullSrc = img.src;
        const fileName = fullSrc.substring(fullSrc.lastIndexOf('/') + 1);

        return fileName;
    });

    const jsonData = JSON.stringify(imagesData);
    const imageData = document.getElementById('DroppedImagesData');

    if (imageData == null) return "";

    imageData.value = jsonData
});

document.addEventListener("DOMContentLoaded", () => {
    const { eventId } = window.pageData;

    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = `/css/events/event-${eventId}.css`;
    document.head.appendChild(link);
});

document.getElementById("profileImage").addEventListener('click', function (e) {

    const navigation = document.getElementById("navigation");

    const dropdown = document.getElementById("dropDown");

    switch (dropdown.style.display) {
        case "flex":
            dropdown.style.display = "";
            navigation.style.borderRadius = "inherit";
            break;
        case "none":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
        case "":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
    }
});