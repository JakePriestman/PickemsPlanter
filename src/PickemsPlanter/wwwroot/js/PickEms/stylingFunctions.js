function mapElementTitle(image, dropzone) {
    const logo = image.split('/').pop().split('.')[0];
    dropzone.title = (typeof teamNameMap !== 'undefined' ? teamNameMap[logo] : null) ?? logo;
}

function updateSaveButton() {
    const saveButton = document.getElementById('saveButton');

    const allFilled = allDropzonesFilled();

    const numberFilled = numberOfFilledDropzones();

    const total = isPlayoffs ? 7 : 10;

    if (saveButton) {

        if (!picksAllowed) {
            saveButton.disabled = true;
            saveButton.textContent = "Picks locked";
            return;
        }

        if (allFilled) {
            const areSame = picksAreSameAsPicksFromApi();

            if (!areSame) {
                saveButton.disabled = false;
                saveButton.textContent = "Plant Picks";
                return;
            }

            saveButton.disabled = true;
            saveButton.innerHTML = '<img src="/Images/lock.png"/> Picks already planted';
            return;
        }

        saveButton.disabled = !allFilled || !picksAllowed;
        saveButton.textContent = allFilled ? "Plant Picks" : `${numberFilled} / ${total} Planted`;
    }
}

function toggleSelectionButtons(isResults) {
    toggleClearAllDropzonesButton(isResults);
    toggleRandomPicksButton(isResults);
}

function toggleClearAllDropzonesButton(isResults) {
    const button = document.getElementById('clearAllPicks');

    if (!picksAllowed || isResults) {
        button.disabled = true;
        return;
    }

    const allEmpty = allDropzonesEmpty();

    if (allEmpty) {
        button.disabled = true;
        return;
    }

    button.disabled = false;
}

function toggleRandomPicksButton(isResults) {
    const button = document.getElementById('randomPicks');

    if (!picksAllowed || isResults) {
        button.disabled = true;
        return;
    }

    const teams = getTeamSourceElements();

    const stageIsNotCompleteWithTeams = teams
        .map(div => div.querySelector('.team-img').src)
        .some(x => x.includes('unknown'));
    //teams-section contains unknown teams
    if (stageIsNotCompleteWithTeams) {
        button.disabled = true;
        return;
    }

    const allFilled = allDropzonesFilled();

    if (allFilled) {
        button.disabled = true;
        return;
    }

    button.disabled = false;
}

function showCheckmarkAndReduceResultOpacity(dropzone, picksToCheck, resultImageSource) {
    const checkmark = dropzone.querySelector('.checkmark');
    const image = dropzone.querySelector('img.dropped-img');

    if (picksToCheck.includes(resultImageSource)) {
        checkmark.classList.add('show');
        image.classList.add('reduced-opacity');
    }
}

function toggleCheckmark(index, resultImageSource) {

    if (picks.length == 0) return;

    if (isPlayoffs) {
        const champion    = picks.slice(6, 7);
        const finalists   = picks.slice(4, 6);
        const semiFinalists = picks.slice(0, 4);

        if (index >= 0 && index <= 3) {
            // Semi-finalist: tick the QF team tile this team came from
            if (!semiFinalists.includes(resultImageSource)) return;

            const matchId    = getPlayoffsMatchBeforeId(`pick${index}`);
            const matchIndex = parseInt(matchId.replace('match', ''), 10);
            const qfSection  = document.querySelector('.quarter-finals');
            const match      = qfSection.querySelectorAll('.match-card')[matchIndex];

            const teamImage = Array.from(match.querySelectorAll('.team-img'))
                .find(img => img.src.split('/').pop() === resultImageSource);

            if (teamImage) {
                const teamDiv     = teamImage.closest('.team');
                const checkmarkEl = document.createElement('div');
                checkmarkEl.className = 'checkmark show';
                checkmarkEl.innerHTML = '<div class="check"></div>';
                teamDiv.appendChild(checkmarkEl);
                teamImage.classList.add('reduced-opacity');
            }
        }

        else if (index === 4 || index === 5) {
            // Finalist: tick the SF dropzone (pick0–3) this team came from
            if (!finalists.includes(resultImageSource)) return;

            const prevDropzone = findPickByImage(resultImageSource, ['pick0', 'pick1', 'pick2', 'pick3']);
            if (prevDropzone) showCheckmarkAndReduceResultOpacity(prevDropzone, finalists, resultImageSource);
        }

        else if (index === 6) {
            // Champion: tick the GF dropzone (pick4–5) this team came from
            if (!champion.includes(resultImageSource)) return;

            const prevDropzone = findPickByImage(resultImageSource, ['pick4', 'pick5']);
            if (prevDropzone) showCheckmarkAndReduceResultOpacity(prevDropzone, champion, resultImageSource);
        }
    }
    else {
        const dropzone = document.getElementById(`pick${index}`);
        const threeZero       = picks.slice(0, 2);
        const threeOneThreeTwo = picks.slice(2, 8);
        const zeroThree       = picks.slice(8, 10);

        if (index === 0 || index === 1) {
            showCheckmarkAndReduceResultOpacity(dropzone, threeZero, resultImageSource);
        }
        else if (index === 8 || index === 9) {
            showCheckmarkAndReduceResultOpacity(dropzone, zeroThree, resultImageSource);
        }
        else {
            showCheckmarkAndReduceResultOpacity(dropzone, threeOneThreeTwo, resultImageSource);
        }
    }
}

function toggleSaveForm() {
    const saveForm = document.getElementById('saveForm');
    if (saveForm) {
        saveForm.style.visibility = saveForm.style.visibility === 'hidden' ? 'visible' : 'hidden';
    }
}

function setDropzoneClassName(dropzone, isResults) {
    if (picksAllowed && !isResults) {
        switch (dropzone.className) {
            case "dropzone-advanced-not-allowed":
                dropzone.className = "dropzone-advanced";
                break;
            case "dropzone-eliminated-not-allowed":
                dropzone.className = "dropzone-eliminated";
                break;
        }
    }
    else {
        switch (dropzone.className) {
            case "dropzone-advanced":
                dropzone.className = "dropzone-advanced-not-allowed";
                break;
            case "dropzone-eliminated":
                dropzone.className = "dropzone-eliminated-not-allowed";
                break;
        }
    }
}

function createTeamImage(imageSource) {
    const image = document.createElement("img");
    image.src = imageSource;
    image.className = "team-img";

    if (imageSource.includes('unknown'))
        image.classList.add('unknown');

    return image;
}

function createDroppedImage(imageSource) {
    const image = document.createElement("img");
    image.src = imageSource;
    image.className = "dropped-img";

    return image;
}


function handleNavBarStyling() {
    const navigation = document.getElementById("navigation");
    const dropdown = document.getElementById("dropDown");

    const isOpen = dropdown.classList.contains('open');

    if (isOpen) {
        dropdown.classList.remove('open');
        navigation.style.borderRadius = "inherit";
    } else {
        dropdown.classList.add('open');
        navigation.style.borderRadius = "0px 45% 0px 0px";
    }
}

function greyOutImages() {
    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-advanced-not-allowed');

    for (const dropzone of dropzones) {
        const imageInDropzone = dropzone.querySelector('.dropped-img');

        if (!imageInDropzone) continue;

        const stageBefore = getPlayoffsStageBefore(dropzone.id);

        const matchId = getPlayoffsMatchBeforeId(dropzone.id);

        const stageBeforeElement = document.querySelector(stageBefore);

        const matchIndex = parseInt(matchId.replace('match', ''), 10);
        const match = stageBeforeElement.querySelectorAll('.match-card')[matchIndex];

        const images = [...match.querySelectorAll('.dropped-img, .team-img')];

        const image = images.find(x => x.src.split('/').pop() !== imageInDropzone.src.split('/').pop());

        if (image) {
            image.classList.add('eliminated');
        }
    }
}

function resetEliminatedImages() {
    const dropzones = [...document.querySelectorAll('.team, .dropzone-advanced, .dropzone-advanced-not-allowed')];

    for (const dropzone of dropzones.reverse()) {
        const imageInDropzone = dropzone.querySelector('img');

        if (!imageInDropzone) continue;

        const pickAfterId = getPlayoffsPickAfterId(dropzone.id);

        if (!pickAfterId) continue;

        const pick = document.getElementById(pickAfterId);


        const image = pick.querySelector('.dropped-img');


        if (!image || imageInDropzone.src.split('/').pop() === image.src.split('/').pop())
            imageInDropzone.classList.remove('eliminated')

    }
}
