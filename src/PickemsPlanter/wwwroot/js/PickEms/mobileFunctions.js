let selectedMobileTeam = null;

function isMobileView() {
    return window.innerWidth <= 640;
}

function initMobileTapMode() {
    document.querySelectorAll('.team').forEach(team => {
        team.addEventListener('click', onTeamClick);
    });

    document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated').forEach(dz => {
        dz.addEventListener('click', onDropzoneClick);
    });
}

function onTeamClick(e) {
    if (!isMobileView() || window._dragJustCompleted) return;

    const team = e.currentTarget;
    if (team.hasAttribute('disabled')) return;

    if (selectedMobileTeam === team) {
        deselectMobileTeam();
        return;
    }

    if (selectedMobileTeam) deselectMobileTeam();
    selectedMobileTeam = team;
    team.classList.add('mobile-selected');
}

function onDropzoneClick(e) {
    if (!isMobileView() || window._dragJustCompleted) return;

    const dropzone = e.currentTarget;

    if (selectedMobileTeam) {
        const teamImg = selectedMobileTeam.querySelector('.team-img');
        if (!teamImg) return;

        currentDraggedElement = createDroppedImage(teamImg.src);
        dragOriginElement = selectedMobileTeam;
        selectedMobileTeam.setAttribute('disabled', 'true');

        placeImageInDropzone(teamImg.src, dropzone, false);
        deselectMobileTeam();
        return;
    }

    const droppedImg = dropzone.querySelector('.dropped-img');
    if (droppedImg && picksAllowed && !dropzone.classList.contains('dropzone-advanced-not-allowed') && !dropzone.classList.contains('dropzone-eliminated-not-allowed')) {
        enableTeamFromDropzone(droppedImg);
        dropzone.innerHTML = '';
        resetDropzoneStyle(dropzone);
        disableDrag(dropzone.id);
        updateSaveButton();
    }
}

function deselectMobileTeam() {
    if (selectedMobileTeam) {
        selectedMobileTeam.classList.remove('mobile-selected');
        selectedMobileTeam = null;
    }
}
