function mapElementTitle(image, dropzone) {
    let name = image.split('/').pop();

    name = name.split('.');

    switch (name[0]) {
        case "3dm":
            dropzone.title = "3DMAX";
            break;
        case "astr":
            dropzone.title = "Astralis";
            break;
        case "aura":
            dropzone.title = "Aurora";
            break;
        case "b8":
            dropzone.title = "B8";
            break;
        case "bb":
            dropzone.title = "BetBoom";
            break;
        case "chin":
            dropzone.title = "Chinggis Warriors";
            break;
        case "cplx":
            dropzone.title = "Complexity";
            break;
        case "fal":
            dropzone.title = "Falcons";
            break;
        case "faze":
            dropzone.title = "FaZe";
            break;
        case "flux":
            dropzone.title = "Fluxo";
            break;
        case "fntc":
            dropzone.title = "fnatic";
            break;
        case "fq":
            dropzone.title = "FlyQuest";
            break;
        case "furi":
            dropzone.title = "FURIA";
            break;
        case "g2":
            dropzone.title = "G2";
            break;
        case "gl":
            dropzone.title = "GamerLegion";
            break;
        case "hero":
            dropzone.title = "Heroic";
            break;
        case "huns":
            dropzone.title = "The Huns";
            break;
        case "imp":
            dropzone.title = "Imperial";
            break;
        case "lgcy":
            dropzone.title = "Legacy";
            break;
        case "liq":
            dropzone.title = "Liquid";
            break;
        case "lynn":
            dropzone.title = "Lynn Vision";
            break;
        case "m80":
            dropzone.title = "M80";
            break;
        case "meti":
            dropzone.title = "Metizport";
            break;
        case "mibr":
            dropzone.title = "MIBR";
            break;
        case "mngz":
            dropzone.title = "The MongolZ";
            break;
        case "mouz":
            dropzone.title = "MOUZ";
            break;
        case "navi":
            dropzone.title = "Natus Vincere";
            break;
        case "nemi":
            dropzone.title = "Nemiga";
            break;
        case "nip":
            dropzone.title = "Ninjas in Pyjamas";
            break;
        case "nrg":
            dropzone.title = "NRG";
            break;
        case "og":
            dropzone.title = "OG";
            break;
        case "pain":
            dropzone.title = "paiN";
            break;
        case "pari":
            dropzone.title = "PARIVISION";
            break;
        case "psnu":
            dropzone.title = "Passion UA";
            break;
        case "ratm":
            dropzone.title = "Rare Atom";
            break;
        case "redc":
            dropzone.title = "RED Canids";
            break;
        case "spir":
            dropzone.title = "Spirit";
            break;
        case "tyl":
            dropzone.title = "TYLOO";
            break;
        case "unknown":
            dropzone.title = "unknown";
            break;
        case "vita":
            dropzone.title = "Vitality";
            break;
        case "vp":
            dropzone.title = "Virtus.pro";
            break;
        case "wcrd":
            dropzone.title = "Wildcard";
            break;
    }
}

function updateSaveButton() {
    const saveButton = document.getElementById('saveButton');

    const allFilled = allDropzonesFilled();

    const numberFilled = numberOfFilledDropzones();

    const total = isPlayoffs ? 7 : 10;

    if (saveButton) {

        if (!picksAllowed) {
            saveButton.disabled = true;
            saveButton.textContent = "Picks not allowed";
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
            saveButton.innerHTML = '<img src="/Images/lock.png"/> Picks already planted'; //COME BACK HERE FOR ADDING LOCK IMAGE.
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

    const teams = document.querySelectorAll('.team');

    const stageIsNotCompleteWithTeams = Array.from(teams)
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

    const threeZero = picks.slice(0, 2);
    const threeOneThreeTwo = picks.slice(2, 8);
    const zeroThree = picks.slice(8, 10);

    const dropzone = document.getElementById(`pick${index}`);

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

function getEventSpecificStylesheet() {
    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = `/css/events/event-${eventId}.css`;
    document.head.appendChild(link);
}

function handleNavBarStyling() {
    const navigation = document.getElementById("navigation");

    const dropdown = document.getElementById("dropDown");

    switch (dropdown.style.display) {
        case "flex":
            dropdown.style.display = "";
            navigation.style.borderRadius = "inherit";
            break;
        case "none":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 45% 0px 0px";
            break;
        case "":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 45% 0px 0px";
            break;
    }
}
