function drop(dropzone) {
    const imageCurrentlyInDropzone = dropzone.querySelector('.dropped-img');
    const currentImageSource = currentDraggedElement.src;

    if (!isPlayoffs && imageCurrentlyInDropzone) {
        if (dragOriginElement.id.includes('team')) {
            enableTeamFromDropzone(imageCurrentlyInDropzone);
            placeImageInDropzone(currentImageSource, dropzone, false);
        }

        if (dragOriginElement.id.includes('pick')) {
            //Dragging from another dropzone in a stage
            swapImagesInDropzones(dragOriginElement, dropzone);
        }
    }

    else {
        if (isPlayoffs && imageCurrentlyInDropzone) {
            //Handle playoff drop
            handleImageDroppedOnSelf(imageCurrentlyInDropzone);
            dropzone.classList.remove('drag-hover');
        }
        placeImageInDropzone(currentImageSource, dropzone, false);
    }

    toggleSelectionButtons(false);
}

function allowPlayoffDrop(sourceId, targetId) {
    if ((sourceId == "team0" || sourceId == "team1") && targetId == "pick0") return true;
    if ((sourceId == "team2" || sourceId == "team3") && targetId == "pick1") return true;
    if ((sourceId == "team4" || sourceId == "team5") && targetId == "pick2") return true;
    if ((sourceId == "team6" || sourceId == "team7") && targetId == "pick3") return true;

    if ((sourceId == "pick0" || sourceId == "pick1") && targetId == "pick4") return true;
    if ((sourceId == "pick2" || sourceId == "pick3") && targetId == "pick5") return true;

    if ((sourceId == "pick4" || sourceId == "pick5") && targetId == "pick6") return true;

    else return false;
}

function imageIsDroppedOnSelf(imageCurrentlyInDropzone) {
    const currentImageSource = currentDraggedElement.src;

    if (imageCurrentlyInDropzone) {
        const imageCurrentlyInDropzoneSource = imageCurrentlyInDropzone.src;

        return currentImageSource === imageCurrentlyInDropzoneSource;
    }

    return false;
}

function handleImageDroppedOnSelf(imageCurrentlyInDropzone) {
    const imageDroppedOnSelf = imageIsDroppedOnSelf(imageCurrentlyInDropzone);
    if (imageDroppedOnSelf)
        resetCurrentDraggedElement();
}