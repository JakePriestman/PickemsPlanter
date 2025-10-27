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