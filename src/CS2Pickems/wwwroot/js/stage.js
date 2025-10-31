function drop(ev) {
    ev.preventDefault();
    const sourceId = ev.dataTransfer.getData("sourceId");
    const div = document.getElementById(sourceId);
    const imageSrc = div.querySelector("img").src;
    if (!imageSrc) return;

    if (imageSrc.includes("unknown")) {
        return;
    }

    let dropzone = ev.target;
    if (!dropzone.classList.contains("match-dropzone-advanced") &&
        !dropzone.classList.contains("match-dropzone-eliminated") && !dropzone.classList.contains("match")) {
        dropzone = dropzone.closest(".match-dropzone-advanced, .match-dropzone-eliminated, .match");
    }

    placeImageInDropzone(imageSrc, dropzone, false);
}

function resetDropzoneStyle(dropzone) {
    dropzone.removeAttribute("style"); // Removes inline styles

    switch (dropzone.id) {
        case "pick0":
        case "pick1":
            dropzone.textContent = "3-0";
            break;
        case "pick2":
        case "pick3":
        case "pick4":
            dropzone.textContent = "3-1";
            break;
        case "pick5":
        case "pick6":
        case "pick7":
            dropzone.textContent = "3-2";
            break;
        case "pick8":
        case "pick9":
            dropzone.textContent = "0-3";
            break;
    }
}

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId, stage } = window.pageData;
    fetch(`/PickEms/Stage?handler=Images&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
        .then(response => response.json())
        .then(imageUrls => {
            imageUrls.forEach((url, index) => {
                const container = document.getElementById(`team${index}`);
                if (container) {
                    const img = document.createElement("img");
                    img.src = url;
                    img.className = "team-img";
                    container.appendChild(img);
                }
            });
        });
});

document.addEventListener("DOMContentLoaded", function () {
    const { eventId, steamId, stage } = window.pageData;
    fetch(`/PickEms/Stage?handler=Picks&eventId=${eventId}&steamId=${steamId}&stage=${stage}`)
        .then(response => response.json())
        .then(imageUrls => {
            console.log("Fetched image URLs:", imageUrls);

            imageUrls.forEach((url, index) => {
                const container = document.getElementById(`pick${index}`);

                if (container) {
                    placeImageInDropzone(url, container);
                } else {
                    console.warn(`Dropzone with id="pick${index}" not found`);
                }
            });
        });
});

document.addEventListener("DOMContentLoaded", () => {
    // Possibly auto-fill from server
    checkDropzonesFilled(); // Run after populating with data
});

document.addEventListener("DOMContentLoaded", () => {
    const stages = document.querySelectorAll(".stage");

    stages.forEach(stage => {
        stage.addEventListener("click", () => {
            // remove active class from all
            stages.forEach(s => s.classList.remove("active"));
            // add active class to clicked
            stage.classList.add("active");
        });
    });
});