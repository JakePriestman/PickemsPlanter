// On /PickEms/Stage, the live bracket's own hidden tracking placeholders (used internally
// by the Simulator engine for Buchholz bookkeeping — see bracketRoot() in simulator.js)
// also carry class "team", same as the 16 draggable team source cards. Most of those
// placeholders have no .team-img child until that outcome is actually decided, so any
// unscoped document.querySelectorAll('.team') call that assumes every match has a filled
// team-img (eg. team.querySelector('.team-img').src) throws the moment it hits one. Every
// call site that means "the draggable team source cards" — not the bracket engine's
// internals — should go through this instead of querying '.team' directly. No-op on
// Playoffs, which has no #bracketProgression.
function getTeamSourceElements() {
    return Array.from(document.querySelectorAll('.team')).filter(t => !t.closest('#bracketProgression'));
}

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
    const teams = getTeamSourceElements();

    const teamImageSources = teams
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

function getPlayoffsStageBefore(stage) {
    switch (stage) {
        case "pick6":
            return ".grand-final";

        case "pick5":
        case "pick4":
            return ".semi-finals";

        case "pick3":
        case "pick2":
        case "pick1":
        case "pick0":
            return ".quarter-finals";
    }
}

function getPlayoffsMatchBeforeId(id) {
    switch (id) {
        case "pick6":
            return "match0";

        case "pick5":
            return "match1";
        case "pick4":
            return "match0";

        case "pick3":
            return "match3";
        case "pick2":
            return "match2";
        case "pick1":
            return "match1";
        case "pick0":
            return "match0";
    }
}

function getPlayoffsPickAfterId(id) {
    switch (id) {
        case "pick6":
            return null;

        case "pick5":
        case "pick4":
            return "pick6";

        case "pick3":
            return "pick5";
        case "pick2":
            return "pick5";
        case "pick1":
            return "pick4";
        case "pick0":
            return "pick4";

        case "team0":
        case "team1":
            return "pick0";
        case "team2":
        case "team3":
            return "pick1";
        case "team4":
        case "team5":
            return "pick2";
        case "team6":
        case "team7":
            return "pick3";
    }
}

function findPickByImage(filename, pickIds) {
    for (const id of pickIds) {
        const dropzone = document.getElementById(id);
        if (!dropzone) continue;
        const img = dropzone.querySelector('.dropped-img');
        if (img && img.src.split('/').pop() === filename) return dropzone;
    }
    return null;
}
