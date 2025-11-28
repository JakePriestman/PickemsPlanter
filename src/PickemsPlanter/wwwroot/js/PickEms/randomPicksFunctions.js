
function placeRandomQuarterFinals(teamImageSources) {

    for (let i = 0; i < 8; i += 2) {
        const dropzone = document.getElementById(`pick${i / 2}`);

        if (dropzone.querySelector('.dropped-img') != null) continue;

        const winnerIndex = Math.random() < 0.5 ? i : i + 1

        const randomTeam = teamImageSources[winnerIndex];

        placeImageInDropzone(randomTeam, dropzone, picksAllowed, true);
    }
}

function placeRandomSemiFinals(dropzones) {
    const quarterFinals = Array.from(dropzones).slice(3, 7)
        .map(div => div.querySelector('.dropped-img').src);

    for (let i = 0; i < 4; i += 2) {
        const dropzone = document.getElementById(i < 2 ? `pick${4}` : `pick${5}`);

        if (dropzone.querySelector('img.dropped-img') != null) continue;

        const winnerIndex = Math.random() < 0.5 ? i : i + 1

        const randomTeam = quarterFinals[winnerIndex];

        placeImageInDropzone(randomTeam, dropzone, picksAllowed, true);
    }
}

function placeRandomFinal(dropzones) {
    const semiFinals = Array.from(dropzones).slice(1, 3)
        .map(div => div.querySelector('.dropped-img').src);

    const dropzone = document.getElementById(`pick6`);

    if (dropzone.querySelector('.dropped-img') != null) return;

    const winnerIndex = Math.random() < 0.5 ? 0 : 1

    const randomTeam = semiFinals[winnerIndex];

    placeImageInDropzone(randomTeam, dropzone, picksAllowed, true);
}

function selectRandomPicks() {
    const teamImageSources = getAllTeamImageSources();

    if (isPlayoffs) {
        const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

        placeRandomQuarterFinals(teamImageSources);

        placeRandomSemiFinals(dropzones);

        placeRandomFinal(dropzones);

    }
    else {
        const emptyDropzones = getEmptyDropzones();

        for (const dropzone of emptyDropzones) {
            const randomIndex = Math.floor(Math.random() * teamImageSources.length);
            const randomTeam = teamImageSources[randomIndex];

            placeImageInDropzone(randomTeam, dropzone, false, false);
            mapElementTitle(randomTeam, dropzone);

            teamImageSources.splice(randomIndex, 1);
        }
    }

    const teamImages = teamImageSources.map(img => img.split('/').pop());

    toggleSelectionButtons(false);
    disableTeamsFunctionality(teamImages);
}