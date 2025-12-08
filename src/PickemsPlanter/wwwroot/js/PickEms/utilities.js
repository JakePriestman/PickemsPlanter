function resetGlobals() {
    isDragging = false;
    currentDraggedElement = null;
    dragOriginElement = null;
}

function resetCurrentDraggedElement() {
    if (currentDraggedElement) {
        currentDraggedElement.remove();
        dragOriginElement.removeAttribute('disabled');
        resetGlobals();
    }
}

function getTeamsToEnable(teams, pickImageSources) {    
    const teamImages = teams.map(team => team.querySelector('.team-img').src.split('/').pop());
    let teamsToEnable = [];
    
    if (isPlayoffs) {
        teamsToEnable = teamImages.filter(item => item != "unknown.png");
    }
    else {
        teamsToEnable = teamImages.filter(item => !pickImageSources.includes(item) && item != "unknown.png");
    }

    return teamsToEnable;
}

function getTeamsToDisable(teams, leftOverTeams) {
    const teamImages = teams.map(team => team.querySelector('.team-img').src.split('/').pop());
    const teamsToDisable = teamImages.filter(item => !leftOverTeams.includes(item) && item != "unknown.png");

    return teamsToDisable;
}

function getRectCenter(rect) {
    return {
        x: rect.left + rect.width / 2,
        y: rect.top + rect.height / 2
    };
}

function getAllTeamImageSources() {
    const teams = document.querySelectorAll('.team');

    const teamImageSources = Array.from(teams)
        .filter(div => !div.hasAttribute('disabled'))
        .map(div => div.querySelector('img.team-img').src);

    return teamImageSources;
}

function picksAreSameAsPicksFromApi() {
    let areSame = false;

    if (picks.length == 0) {
        return areSame;
    }

    const imagesFromDropzones = Array.from(document.querySelectorAll('img.dropped-img'))
        .map(img => img.src.split('/').pop());

    if (isPlayoffs) {
        areSame = true;
        const compareMap = [6, 4, 5, 0, 1, 2, 3];

        areSame = compareMap.every((i, j) => {
            return imagesFromDropzones[j] === picks[i];
        });
    }

    else {
        areSame = picks.every((val, i) => val === imagesFromDropzones[i]);
    }

    return areSame;
}

function removeSucceedingImages(imageInDropzone, dropzone) {
    const imageName = imageInDropzone.src.split('/').pop();
    const dropzonesToRemove = getDropzonesAfter(dropzone.id);

    dropzonesToRemove.forEach(dz => {
        const existingImage = dz.querySelector('.dropped-img');

        if (existingImage) {
            const existingImageName = existingImage.src.split('/').pop();

            if (existingImageName === imageName) {
                resetDropzoneStyle(dz);
                disableDrag(dz.id);
            }
        }
    });
}
