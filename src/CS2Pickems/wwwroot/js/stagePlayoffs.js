function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {
    if (ev.target.src.includes("unknown"))
        return;
    ev.dataTransfer.setData("sourceId", ev.currentTarget.id);
}
function checkDropzonesFilled() {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    // Check for null or invalid dropzones
    const allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('img.dropped-img') !== null
    );

    const saveButton = document.getElementById('saveButton');
    if (saveButton) {
        saveButton.disabled = !allFilled;
        saveButton.textContent = allFilled ? "Save All Picks" : "Please fill all dropzones to enable saving.";
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
                    zone.innerHTML = ''; // Clear it
                    resetDropzoneStyle(zone); // Reset styles
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
                    zone.innerHTML = ''; // Clear it
                }
            }
        });
    }

    // Clear current dropzone and reset it
    dropzone.innerHTML = '';

    // Create and append new image
    const img = document.createElement("img");
    img.src = imageSrc;
    img.className = "dropped-img";

    if (!isPlayoffs) img.draggable = false;

    dropzone.appendChild(img);

    // Apply styling
    dropzone.style.width = "64px";
    dropzone.style.height = "64px";
    dropzone.style.justifyContent = "center";
    dropzone.style.alignItems = "center";

    // Grey out source image
    document.querySelectorAll('img[draggable="true"]').forEach(original => {
        const originalFilename = original.src.split('/').pop();
        if (originalFilename === filename) {
            original.classList.add('used-image');
            original.draggable = false;
        }
    });

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

        // Extract just the filename from the src URL
        const fullSrc = img.src;
        const fileName = fullSrc.substring(fullSrc.lastIndexOf('/') + 1);

        return fileName;
    });

    const jsonData = JSON.stringify(imagesData);
    const imageData = document.getElementById('DroppedImagesData');

    if (imageData == null) return "";

    imageData.value = jsonData
});