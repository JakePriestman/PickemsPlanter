async function getPicksAllowedAsync() {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=PicksAllowed&eventId=${eventId}` :
        `/PickEms/Stage?handler=PicksAllowed&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url)
    return await response.json();
}

async function loadTeamNamesAsync() {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=TeamNames&eventId=${eventId}` :
        `/PickEms/Stage?handler=TeamNames&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url);
    teamNameMap = await response.json();
}

async function LoadImagesAsync() {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Images&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    getTeamSourceElements().forEach(t => t.classList.add('skeleton'));

    const imagesResponse = await fetch(url);
    const imageUrls = await imagesResponse.json();

    for (const [index, imageSource] of imageUrls.entries()) {
        const container = document.getElementById(`team${index}`);
        if (container) {
            container.classList.remove('skeleton');

            if (isPlayoffs)
                container.innerHTML = "";

            const teamImage = createTeamImage(imageSource);
            container.appendChild(teamImage);

            container.setAttribute('disabled', 'true');

            container.addEventListener('dragstart', (e) => e.preventDefault());
        }
    }

    document.querySelectorAll('.team.skeleton').forEach(t => t.classList.remove('skeleton'));

    if (!picksAllowed)
        togglePicksNotAllowedConfirmation();

    updateSaveButton();
}

async function LoadPicksAsync() {
    if (isPlayoffs) {
        document.querySelectorAll('.team .checkmark').forEach(el => el.remove());
        document.querySelectorAll('.team .team-img').forEach(el => el.classList.remove('reduced-opacity'));
    }

    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Picks&eventId=${eventId}&steamId=${steamId}` :
        `/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();
    const pickImageSources = imageUrls.map(img => img.split('/').pop());
    picks = pickImageSources;

    if (picksAllowed) {
        enableTeamsFunctionality(pickImageSources);
    }

    const dropzones = document.querySelectorAll('.dropzone-advanced, .dropzone-eliminated');

    dropzones.forEach(dz => {
        dz.addEventListener('dragstart', (e) => e.preventDefault());
        setDropzoneClassName(dz, false);
    });


    if (imageUrls.length > 0) {
        for (let [index, imageSource] of imageUrls.entries()) {
            const dropzone = document.getElementById(`pick${index}`);

            if (dropzone) {
                placeImageInDropzone(imageSource, dropzone, false);

                setDropzoneClassName(dropzone, false);
            }
        }
    }
    else {
        const dropzones = document.querySelectorAll('.dropzone-advanced-not-allowed, .dropzone-eliminated-not-allowed');

        dropzones.forEach(dz => {
            const imageInDropzone = dz.querySelector('.dropped-img');

            if (imageInDropzone) {
                if (imageInDropzone.src.includes('unknown')) {
                    imageInDropzone.innerHTML = '';
                    resetDropzoneStyle(dz);

                    if (!isPlayoffs) {
                        setDropzoneClassName(dz, false);
                    }
                }
            }
        });

        updateSaveButton();
    }


    toggleSelectionButtons(false);
}

async function LoadResultsAsync() {
    const url = isPlayoffs ?
        `/PickEms/Playoffs?handler=Results&eventId=${eventId}` :
        `/PickEms/Stage?handler=Results&eventId=${eventId}&stage=${stage}`;

    const response = await fetch(url);
    const imageUrls = await response.json();

    disableAllTeamsFunctionality();

    for (let [index, imageSource] of imageUrls.entries()) {
        const dropzone = document.getElementById(`pick${index}`);

        if (dropzone) {
            placeImageInDropzone(imageSource, dropzone, true);

            const resultImageSource = imageSource.split('/').pop();

            toggleCheckmark(index, resultImageSource);

            setDropzoneClassName(dropzone, true);
        }
    }

    toggleSelectionButtons(true);
}

async function LoadTeamsAndPicksAsync() {
    await loadTeamNamesAsync();

    picksAllowed = await getPicksAllowedAsync(isPlayoffs);

    await LoadImagesAsync(isPlayoffs);

    await LoadPicksAsync(isPlayoffs);
}

async function showResultsAsync(showResultsCheckBox) {
    picksAllowed = await getPicksAllowedAsync(isPlayoffs);

    if (showResultsCheckBox.checked) {
        toggleSaveForm();
        await LoadResultsAsync(isPlayoffs);
    }
    else {
        await LoadPicksAsync(isPlayoffs);
        toggleSaveForm();
    }
}

function getImageNamesAndParseToJson() {
    // Reads pick0..pickN explicitly by id rather than DOM order — on Stage, Bracket View
    // relocates the pick elements into the live bracket's terminal containers, so their
    // position in the document no longer matches pick0-9's positional save-order contract.
    const pickCount = isPlayoffs ? 7 : 10;

    const imagesData = [];

    for (let i = 0; i < pickCount; i++) {
        const dropzone = document.getElementById(`pick${i}`);
        imagesData.push(dropzone.querySelector('.dropped-img').src.split('/').pop());
    }

    const jsonData = JSON.stringify(imagesData);
    const imageData = document.getElementById('picksToPost');

    imageData.value = jsonData
}

