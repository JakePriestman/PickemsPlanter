function enableDrag(elementId) {
    const element = document.getElementById(elementId);

    if (element && !element._dragHandler) {
        element._dragHandler = function (event) {
            dragStart(event, isPlayoffs);
        };

        element.addEventListener('mousedown', element._dragHandler);
    }
}

function disableDrag(elementId) {
    const element = document.getElementById(elementId);

    if (element && element._dragHandler) {
        element.removeEventListener('mousedown', element._dragHandler);
        delete element._dragHandler;
    }
}

function dragStart(event) {
    const rect = event.target.getBoundingClientRect();
    dragOriginElement = event.target.parentElement;

    if ((event.target.parentElement.id.includes('team') && event.target.parentElement.id != "teamSection") || isPlayoffs) {
        currentDraggedElement = event.target.cloneNode(true);
    }
    else if (event.target.parentElement.id.includes('pick')) {
        currentDraggedElement = event.target;
        resetDropzoneStyle(currentDraggedElement.parentElement);
    }
    else {
        return;
    }

    //Set styling on the item being dragged
    currentDraggedElement.classList.add('dragging');
    currentDraggedElement.style.width = rect.width + "px";
    currentDraggedElement.style.height = rect.height + "px";
    currentDraggedElement.style.left = (event.clientX - rect.width / 2.0) + "px";
    currentDraggedElement.style.top = (event.clientY - rect.height / 2.0) + "px";

    document.body.appendChild(currentDraggedElement);

    isDragging = true;

    //This should only happen when dragged from the teams section
    if (dragOriginElement.id.includes('team') && !isPlayoffs) {
        dragOriginElement.setAttribute('disabled', 'true');
    }
}

function dragging(event) {

    if (!isDragging) return;

    const rect = currentDraggedElement.getBoundingClientRect();

    const collidedDropzoneId = returnCollidedDrozoneId(rect);

    if (collidedDropzoneId) {
        const collidedDropzone = document.getElementById(collidedDropzoneId);
        collidedDropzone.classList.add('drag-hover');
    }

    //Update currently dragged element position
    currentDraggedElement.style.left = (event.clientX - rect.width / 2.0) + "px";
    currentDraggedElement.style.top = (event.clientY - rect.height / 2.0) + "px";
}

function dragEnd() {
    if (currentDraggedElement) {
        const rect = currentDraggedElement.getBoundingClientRect();

        const collidedDropzoneId = returnCollidedDrozoneId(rect);

        if (collidedDropzoneId) {
            const collidedDropzone = document.getElementById(collidedDropzoneId);
            drop(collidedDropzone);
        }

        else if (dragOriginElement.id.includes('pick')) {
            const imageInDropzone = dragOriginElement.querySelector('.dropped-img');

            if (isPlayoffs) {
                removeSucceedingImages(imageInDropzone, dragOriginElement);
            }

            dragOriginElement.innerHTML = '';
            resetDropzoneStyle(dragOriginElement);
            disableDrag(dragOriginElement.id);
            enableTeamFromDropzone(currentDraggedElement);
            resetCurrentDraggedElement();
            updateSaveButton();
        }

        else {
            resetCurrentDraggedElement();
        }

        toggleSelectionButtons(false);
    }
}

function enableTeamsFunctionality(pickImageSources) {
    const teams = Array.from(document.querySelectorAll('.team'));
    const teamsToEnable = getTeamsToEnable(teams, pickImageSources);

    teamsToEnable.forEach(team => {
        const teamContainer = teams.find(t => t.querySelector('.team-img').src.split('/').pop() === team);
        teamContainer.removeAttribute('disabled');
        enableDrag(teamContainer.id);
    });
}

function disableTeamsFunctionality(leftOverTeams) {
    const teams = Array.from(document.querySelectorAll('.team'));
    const teamsToDisable = getTeamsToDisable(teams, leftOverTeams);

    teamsToDisable.forEach(team => {
        const teamContainer = teams.find(t => t.querySelector('.team-img').src.split('/').pop() === team);
        teamContainer.setAttribute('disabled', 'true');
        disableDrag(teamContainer.id);
    });
}

function disableAllTeamsFunctionality() {
    const teams = Array.from(document.querySelectorAll('.team'));

    teams.forEach(team => {
        team.setAttribute('disabled', 'true');
        disableDrag(team.id);
    });
}
