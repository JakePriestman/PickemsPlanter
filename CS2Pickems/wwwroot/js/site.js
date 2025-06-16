function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {
    if (ev.target.src.includes("unknown"))
    return;
    // Set only the image source to transfer
    ev.dataTransfer.setData("imageSrc", ev.target.src);
}

function placeImageInDropzone(imageSrc, dropzone, isPlayoffs) {
    if (!imageSrc || imageSrc.includes("unknown") || !dropzone) return;

    const filename = imageSrc.split('/').pop();

    // Remove this image from all other dropzones
    if (!isPlayoffs) {

        // Add check for playoffs if the team is play validly

        const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated', 'match');
        allDropzones.forEach(zone => {
            const existingImg = zone.querySelector('img');
            if (existingImg) {
                const existingFilename = existingImg.src.split('/').pop();
                if (existingFilename === filename) {
                    zone.innerHTML = ''; // Clear it
                    resetDropzoneStyle(zone); // 🔁 Reset styles
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
    img.draggable = false;

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

function drop(ev) {
    ev.preventDefault();
    const imageSrc = ev.dataTransfer.getData("imageSrc");
    if (!imageSrc) return;

    if (imageSrc.includes("unknown")) {
        return;
    }

    let dropzone = ev.target;
    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    placeImageInDropzone(imageSrc, dropzone, false);
}

function dropPlayoffs(ev) {
    ev.preventDefault();
    const imageSrc = ev.dataTransfer.getData("imageSrc");
    if (!imageSrc) return;

    if (imageSrc.includes("unknown")) {
        return;
    }

    let dropzone = ev.target;
    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    placeImageInDropzone(imageSrc, dropzone, true);
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
    document.getElementById('DroppedImagesData').value = jsonData;
});

function checkDropzonesFilled() {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    // Check for null or invalid dropzones
    const allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('img.dropped-img') !== null
    );

    const saveButton = document.getElementById('saveButton');
    if (saveButton) {
        saveButton.disabled = !allFilled;
        saveButton.title = allFilled ? "" : "Please fill all dropzones to enable saving.";
    } else {
        console.warn("Save button with id 'saveButton' not found.");
    }
}

function resetDropzoneStyle(dropzone) {
    dropzone.removeAttribute("style"); // Removes inline styles

    switch (dropzone.id) {
        case "pick0":
        case "pick1":
            dropzone.textContent = "3-0";
            break;
        case "pick2":
        case "pick3":
        case "pick4":
            dropzone.textContent = "3-1";
            break;
        case "pick5":
        case "pick6":
        case "pick7":
            dropzone.textContent = "3-2";
            break;
        case "pick8":
        case "pick9":
            dropzone.textContent = "0-3";
            break;
    }
}

