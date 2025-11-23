function allowDrop(ev) {
    ev.preventDefault();
}

function enableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "true");
    item.setAttribute("ondragstart", "drag(event)");
}

function disableDrag(itemId) {
    const item = document.getElementById(itemId);
    if (!item) return;
    item.setAttribute("draggable", "false");
}

function drag(ev) {
    if (ev.target.src.includes("unknown"))
        return;
    ev.dataTransfer.setData("sourceId", ev.currentTarget.id);
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

function toggleClearAllDropzonesButton(picksAllowed) {
    const button = document.getElementById('clearAllPicks');

    if (picksAllowed) {
        const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

        const allEmpty = Array.from(allDropzones).every(zone => zone.querySelector('img.dropped-img') === null);

        if (allEmpty) {
            button.disabled = true;
        }
        else {
            button.disabled = false;
        }
    }
    else {
        button.disabled = true;
    }
}

async function clearAllDropzonesAsync(isPlayoffs) {
    const {eventId, stage, picks } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

    const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

    allDropzones.forEach(zone => {
        zone.innerHTML = '';
        resetDropzoneStyle(zone);
        disableDrag(zone.id);
    });

    const button = document.getElementById('clearAllPicks');
    button.disabled = true;

    const teams = document.querySelectorAll('.team');

    teams.forEach(team => {
        toggleImageFunctionality(team, picksAllowed);
    });

    toggleRandomPicksButton(picksAllowed);

    await checkDropzonesFilledAsync(picksAllowed, picks);
}

function toggleRandomPicksButton(picksAllowed) {
    const teamDivs = document.querySelectorAll('.team');

    const notAllTeamsYet = Array.from(teamDivs).map(div => div.querySelector('img.team-img').src).some(x => x.includes('unknown'));

    const button = document.getElementById('randomPicks');

    if (!notAllTeamsYet) {
        if (picksAllowed) {
            const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

            const allFilled = Array.from(allDropzones).every(zone => zone !== null && zone.querySelector('img.dropped-img') !== null);

            if (allFilled) {
                button.disabled = true;
            }
            else {
                button.disabled = false;
            }
        }
        else {
            button.disabled = true;
        }
    }
    else {
        button.disabled = true;
    }
}

async function selectRandomPicksAsync(isPlayoffs) {
    const { eventId, stage } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

    if (picksAllowed) {

        if (!isPlayoffs) {
            const teamDivs = document.querySelectorAll('.team');

            const teams = Array.from(teamDivs)
                .filter(div => !div.hasAttribute('disabled'))
                .map(div => div.querySelector('img.team-img').src);

            const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

            const emptyDropzones = Array.from(allDropzones).filter(div => div.querySelector('img') === null); 

            for (const zone of emptyDropzones) {
                const randomIndex = Math.floor(Math.random() * teams.length);
                const randomTeam = teams[randomIndex];

                await placeImageInDropzoneAsync(randomTeam, zone, isPlayoffs, picksAllowed);

                teams.splice(randomIndex, 1);
            }

            toggleClearAllDropzonesButton(picksAllowed);
            toggleRandomPicksButton(picksAllowed);
        }
        else {

            //Quarter Finals
            const teamDivs = document.querySelectorAll('.team');

            const teams = Array.from(teamDivs).map(div => div.querySelector('img.team-img').src);

            for (let i = 0; i < 8; i += 2) {
                const pick = document.getElementById(`pick${i / 2}`);

                if (pick.querySelector('img.dropped-img') != null) continue;

                const winnerIndex = Math.random() < 0.5 ? i : i + 1

                const randomTeam = teams[winnerIndex];

                await placeImageInDropzoneAsync(randomTeam, pick, isPlayoffs, picksAllowed);
            }

            //Semi Finals
            const pickDivs = document.querySelectorAll('.match-dropzone-advanced');

            const quarterFinals = Array.from(pickDivs).slice(3, 7).map(div => div.querySelector('img.dropped-img').src);

            for (let i = 0; i < 4; i += 2) {
                const pick = document.getElementById(i < 2 ? `pick${4}` : `pick${5}`);

                if (pick.querySelector('img.dropped-img') != null) continue;

                const winnerIndex = Math.random() < 0.5 ? i : i + 1

                const randomTeam = quarterFinals[winnerIndex];

                await placeImageInDropzoneAsync(randomTeam, pick, isPlayoffs, picksAllowed);
            }

            //Final
            const semiFinals = Array.from(pickDivs).slice(1, 3).map(div => div.querySelector('img.dropped-img').src);

            const pick = document.getElementById(`pick6`);

            if (pick.querySelector('img.dropped-img') != null) return;

            const winnerIndex = Math.random() < 0.5 ? 0 : 1

            const randomTeam = semiFinals[winnerIndex];

            await placeImageInDropzoneAsync(randomTeam, pick, isPlayoffs, picksAllowed);

            toggleClearAllDropzonesButton(picksAllowed);
            toggleRandomPicksButton(picksAllowed);
        }
    }
}

function removeDuplicateImage(filename) {
    const allDropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');
    allDropzones.forEach(zone => {
        const existingImg = zone.querySelector('img');
        if (existingImg) {
            const existingFilename = existingImg.src.split('/').pop();
            if (existingFilename === filename) {
                zone.innerHTML = '';
                resetDropzoneStyle(zone);
                disableDrag(zone);
            }
        }
    });
}

function swapImagesInDropzones(div, dropzone, picksAllowed, picks) {
    if (picksAllowed) {
        const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

        const allFilled = Array.from(dropzones).every(zone =>
            zone !== null && zone.querySelector('img.dropped-img') !== null
        );

        const innerHTML = div.innerHTML;

        div.innerHTML = dropzone.innerHTML;
        dropzone.innerHTML = innerHTML;

        toggleSaveButton(allFilled, picksAllowed, picks);
    }
}

async function drop(ev, isPlayoffs) {
    ev.preventDefault();
    
    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;

    let dropzone = ev.target;

    const { eventId, stage, picks } = window.pageData;
    const picksAllowed = await getPicksAllowedAsync(eventId, stage, isPlayoffs);

    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    if (sourceId.includes('pick') && !isPlayoffs)
        swapImagesInDropzones(div, dropzone, picksAllowed, picks);
    else
        await placeImageInDropzoneAsync(imageSrc, dropzone, isPlayoffs, picksAllowed);

    toggleClearAllDropzonesButton(picksAllowed);
    toggleRandomPicksButton(picksAllowed);
}


async function checkDropzonesFilledAsync(picksAllowed, picks) {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated, .match');

    const allFilled = Array.from(dropzones).every(zone =>
        zone !== null && zone.querySelector('img.dropped-img') !== null
    );

    toggleSaveButton(allFilled, picksAllowed, picks);
}

function toggleSaveButton(allFilled, picksAllowed, picks) {

    const saveButton = document.getElementById('saveButton');

    if (saveButton) {

        if (picksAllowed) {
            if (!picks || picks.length === 0) {
                saveButton.disabled = !allFilled || !picksAllowed;
                saveButton.textContent = allFilled ? "Plant Picks" : "All picks need to be within the dropzones to plant your picks";
            }
            else if (allFilled) {
                //The order for playoffs messes things up here. Chekc this out and figure out a solution.
                const imagesFromDropzones = Array.from(document.querySelectorAll('img.dropped-img'))
                    .map(img => img.src.split('/').pop());

                const areSame = picks.every((val, i) => val === imagesFromDropzones[i]);

                if (areSame) {
                    saveButton.disabled = true;
                    saveButton.textContent = "Picks already planted";
                }
                else {
                    saveButton.disabled = false;
                    saveButton.textContent = "Plant Picks";
                }
            }
            else {
                saveButton.disabled = !allFilled || !picksAllowed;
                saveButton.textContent = allFilled ? "Plant Picks" : "All picks need to be within the dropzones to plant your picks";
            }
        }
        else {
            saveButton.disabled = true;
            saveButton.textContent = "Picks can't be planted";
        }
    }
}

function getDropzonesAfter(currentId, allDropzones) {
    const currentNumber = parseInt(currentId.replace('pick', ''), 10);

    return Array.from(allDropzones).filter(dz => {
        const dzNumber = parseInt(dz.id.replace('pick', ''), 10);
        return dzNumber > currentNumber;
    });
}

function removeSucceedingImages(imageInDropzone, dropzone) {
    const imageToRemoveName = imageInDropzone.src.split('/').pop()
    const allDropzones = document.querySelectorAll('.match-dropzone-advanced');
    const dropzonesToRemove = getDropzonesAfter(dropzone.id, allDropzones);

    dropzonesToRemove.forEach(zone => {
        const existingImg = zone.querySelector('img');
        if (existingImg) {
            const existingFilename = existingImg.src.split('/').pop();

            if (existingFilename === imageToRemoveName) {
                zone.innerHTML = '';
                resetDropzoneStyle(zone);
                disableDrag(zone);
            }
        }
    });
}

function addImageToDropzone(dropzone, imageSrc, imageInDropzone, picksAllowed, isPlayoffs) {

    if (picksAllowed) {
        enableDrag(dropzone.id);

        if (!isPlayoffs) {
            if (imageInDropzone) {
                const filename = imageInDropzone.src.split('/').pop();
                const teams = document.querySelectorAll('.team[disabled]');
                teams.forEach(team => {
                    const existingImg = team.querySelector('img');
                    if (existingImg) {
                        const existingFilename = existingImg.src.split('/').pop();
                        if (existingFilename === filename) {
                            team.removeAttribute('disabled');
                        }
                    }
                });
            }

            const filename = imageSrc.split('/').pop();
            const teams = document.querySelectorAll('.team');
            teams.forEach(team => {
                const existingImg = team.querySelector('img');
                if (existingImg) {
                    const existingFilename = existingImg.src.split('/').pop();
                    if (existingFilename === filename) {
                        team.setAttribute('disabled', 'true');
                    }
                }
            });
        }
    }

    dropzone.innerHTML = '';

    const img = document.createElement("img");
    img.src = imageSrc;
    img.className = "dropped-img";

    dropzone.appendChild(img);
}


async function placeImageInDropzoneAsync(imageSrc, dropzone, isPlayoffs, picksAllowed) {
    const { picks } = window.pageData;

    if (!dropzone) return;

    if (!imageSrc || imageSrc.includes("unknown")) {
        resetDropzoneStyle(dropzone);
        disableDrag(dropzone.id);
        return;
    }

    const imageInDropzone = dropzone.querySelector('img');

    //Only remove the images after the current dropzone in playoffs mode
    if (isPlayoffs && imageInDropzone != null)
        removeSucceedingImages(imageInDropzone, dropzone);

    addImageToDropzone(dropzone, imageSrc, imageInDropzone, picksAllowed, isPlayoffs);

    await checkDropzonesFilledAsync(picksAllowed, picks);
}


async function getPicksAllowedAsync(eventId, stage, isPlayoffs) {
    const url = isPlayoffs ? 
        `/PickEms/Playoffs?handler=PicksAllowed&eventId=${eventId}` :
        `/PickEms/Stage?handler=PicksAllowed&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url)
    return await response.json();
}

function toggleImageFunctionality(team, picksAllowed) {
    if (picksAllowed) {
        const image = team.querySelector('img');

        if (image) {
            if (Array.from(image.classList).includes('unknown')) {
                disableDrag(team.id);
                team.setAttribute('disabled', 'true');
            }
            else {
                enableDrag(team.id);
                team.removeAttribute('disabled');
            }
        }
        else {
            disableDrag(team.id);
            team.setAttribute('disabled', 'true');
        }
    }
    else {
        disableDrag(team.id);
        team.setAttribute('disabled', 'true');
    }
}


async function LoadImagesAsync(eventId, steamId, stage, isPlayoffs, picksAllowed) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Images&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    const imagesResponse = await fetch(url);
    const imageUrls = await imagesResponse.json();

    for (const [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`team${index}`);
        if (container) {
            const img = document.createElement("img");
            img.src = url;
            img.className = "team-img";
            if (url.includes('unknown'))
                img.classList.add('unknown');

            container.appendChild(img);

            toggleImageFunctionality(container, picksAllowed);
        }
    }

    if (!picksAllowed)
        togglePicksNotAllowedConfirmation();

    await checkDropzonesFilledAsync(picksAllowed);
}


async function LoadPicksAsync(eventId, steamId, stage, isPlayoffs, picksAllowed) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Picks&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();

    for (let [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`pick${index}`);

        if (container) {
            await placeImageInDropzoneAsync(url, container, isPlayoffs, picksAllowed);

            if (picksAllowed) {
                switch (container.className) {
                    case "match-dropzone-advanced-not-allowed":
                        container.className = "match-dropzone-advanced";
                        break;
                    case "match-dropzone-eliminated-not-allowed":
                        container.className = "match-dropzone-eliminated";
                        break;
                }
            }

            if (isPlayoffs) {
                toggleImageFunctionality(container, picksAllowed);
            }

        } else {
            console.warn(`Dropzone with id="pick${index}" not found`);
        }
    };

    const images = imageUrls.map(img => img.split('/').pop());
    window.pageData.picks = images;
    toggleSaveButton(images.length != 0, picksAllowed, images);
}


async function LoadResultsAsync(eventId, stage, isPlayoffs) {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Results&eventId=${eventId}` :
        `/PickEms/Stage?handler=Results&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();

    const teams = document.querySelectorAll('.team');

    teams.forEach(team => {
        toggleImageFunctionality(team, false);
    });

    for (let [index, url] of imageUrls.entries()) {
        const container = document.getElementById(`pick${index}`);

        if (container) {
            {
                await placeImageInDropzoneAsync(url, container, isPlayoffs, false);

                switch (container.className) {
                    case "match-dropzone-advanced":
                        container.className = "match-dropzone-advanced-not-allowed";
                        break;
                    case "match-dropzone-eliminated":
                        container.className = "match-dropzone-eliminated-not-allowed";
                        break;
                }
            }
        } else {
            console.warn(`Dropzone with id="pick${index}" not found`);
        }
    };
}

function toggleSaveForm() {
    const saveForm = document.getElementById('saveForm');
    if (saveForm) {
        saveForm.style.visibility = saveForm.style.visibility === 'hidden' ? 'visible' : 'hidden';
    }
}

document.getElementById('saveForm').addEventListener('submit', function (e) {
    const dropzones = document.querySelectorAll('.match-dropzone-advanced, .match-dropzone-eliminated');

    const imagesData = Array.from(dropzones).map(zone => {
        const img = zone.querySelector('img.dropped-img');
        if (!img) return "";

        const fullSrc = img.src;
        const fileName = fullSrc.substring(fullSrc.lastIndexOf('/') + 1);

        return fileName;
    });

    const jsonData = JSON.stringify(imagesData);
    const imageData = document.getElementById('DroppedImagesData');

    if (imageData == null) return "";

    imageData.value = jsonData
});

document.addEventListener("DOMContentLoaded", () => {
    const { eventId } = window.pageData;

    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = `/css/events/event-${eventId}.css`;
    document.head.appendChild(link);
});

document.getElementById("profileImage").addEventListener('click', function (e) {

    const navigation = document.getElementById("navigation");

    const dropdown = document.getElementById("dropDown");

    switch (dropdown.style.display) {
        case "flex":
            dropdown.style.display = "";
            navigation.style.borderRadius = "inherit";
            break;
        case "none":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
        case "":
            dropdown.style.display = "flex";
            navigation.style.borderRadius = "0px 50px 0px 0px";
            break;
    }
});