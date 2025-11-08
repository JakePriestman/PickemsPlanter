function startKitBlinking() {
    const image = document.getElementById("defuse-kit");
    image.className = "defuse-kit-blinking";
}

function stopKitBlinking() {
    const image = document.getElementById("defuse-kit");
    image.className = "defuse-kit";
}