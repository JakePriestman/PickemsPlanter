function allDropzonesFilled() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    let allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('.dropped-img') !== null
    );

    if (dropzones.length == 0) {
        allFilled = false;
    }

    return allFilled;
}

function numberOfFilledDropzones() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    const numberFilled = Array.from(dropzones).filter(dz => dz.querySelector('.dropped-img') !== null).length;

    return numberFilled;
}

function getEmptyDropzones() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    const emptyDropzones = Array.from(dropzones).filter(div => div.querySelector('img') === null);

    return emptyDropzones;
}

function allDropzonesEmpty() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    const allEmpty = Array.from(dropzones).every(zone => zone.querySelector('.dropped-img') === null);

    return allEmpty;
}

function clearAllDropzones() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    dropzones.forEach(dz => {
        dz.innerHTML = '';
        resetDropzoneStyle(dz);
        disableDrag(dz.id);
    });

    if (isPlayoffs) {
        const teams = document.querySelectorAll('.team-img');

        teams.forEach(t => t.classList.remove('eliminated'));
    }

    toggleSelectionButtons(false);

    enableTeamsFunctionality([]);

    updateSaveButton();
}

function swapImagesInDropzones(originDropzone, destinationDropzone) {
    originDropzone.innerHTML = destinationDropzone.innerHTML;

    destinationDropzone.innerHTML = '';
    currentDraggedElement.style = '';
    currentDraggedElement.classList.remove('dragging');
    currentDraggedElement.className = 'dropped-img';
    destinationDropzone.appendChild(currentDraggedElement);
    destinationDropzone.classList.remove('drag-hover');

    updateSaveButton();
    resetGlobals();
}

function getDropzonesAfter(currentId) {
    const currentNumber = parseInt(currentId.replace('pick', ''), 10);
    const allDropzones = document.querySelectorAll('.dropzone-advanced');

    return Array.from(allDropzones).filter(dz => {
        const dzNumber = parseInt(dz.id.replace('pick', ''), 10);
        return dzNumber > currentNumber;
    });
}

function enableTeamFromDropzone(imageInDropzone) {
    const filename = imageInDropzone.src.split('/').pop();
    const teams = document.querySelectorAll('.team[disabled]');

    teams.forEach(team => {
        const existingImg = team.querySelector('.team-img');

        if (existingImg) {
            const existingFilename = existingImg.src.split('/').pop();

            if (existingFilename === filename) {
                team.removeAttribute('disabled');
                enableDrag(team.id);
            }
        }
    });
}

function placeImageInDropzone(imageSource, dropzone, isResults) {
    const imageCurrentlyInDropzone = dropzone.querySelector('.dropped-img');
    const innerHTML = isResults ? '<div class="checkmark"><div class="check"></div></div>' : '';

    if (picksAllowed && !imageCurrentlyInDropzone) {
        if (!imageSource.includes('unknown')) {
            enableDrag(dropzone.id);

            if (isPlayoffs) {
                dropzone.removeAttribute('disabled');
            }
        }
    }

    if (imageCurrentlyInDropzone) {
        if (imageCurrentlyInDropzone.src.includes('unknown')) {
            enableDrag(dropzone.id);
        }
    }

    if (currentDraggedElement && dragOriginElement) {

        if (isPlayoffs && imageCurrentlyInDropzone) {
            const droppedOnSelf = imageIsDroppedOnSelf(imageCurrentlyInDropzone);

            if (!droppedOnSelf)
                removeSucceedingImages(imageCurrentlyInDropzone, dropzone);
        }

        if (isPlayoffs) {
            const allowDrop = allowPlayoffDrop(dragOriginElement.id, dropzone.id);

            if (!allowDrop) {
                //Optionally throw and error or show something to the user.
                return;
            }
        }

        currentDraggedElement.style = '';
        currentDraggedElement.classList.remove('dragging');
        currentDraggedElement.className = 'dropped-img';

        dropzone.innerHTML = innerHTML;
        dropzone.appendChild(currentDraggedElement);
        dropzone.classList.remove('drag-hover');
    }
    else {
        dropzone.innerHTML = innerHTML;

        const image = createDroppedImage(imageSource);

        dropzone.appendChild(image);

        if (imageSource.includes('unknown')) {
            disableDrag(dropzone.id);
        }
    }

    if (isPlayoffs) {
        greyOutImages();
        resetEliminatedImages();
    }


    resetGlobals();

    updateSaveButton();
}

function returnCollidedDrozoneId(currentBoundingClientRect) {

    const center = getRectCenter(currentBoundingClientRect);

    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    for (const dz of dropzones) {
        const dzRect = dz.getBoundingClientRect();

        const isColliding =
            center.x > dzRect.left &&
            center.x < dzRect.right &&
            center.y > dzRect.top &&
            center.y < dzRect.bottom;

        if (isColliding) {
            return dz.id;
        }

        else {
            dz.classList.remove('drag-hover');
        }
    }

    return null;
}