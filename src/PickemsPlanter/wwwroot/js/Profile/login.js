function startBombBlinking() {
    const image = document.getElementById("bomb-blinker");
    image.className = "bomb-blinking-img";
}

function stopBombBlinking() {
    const image = document.getElementById("bomb-blinker");
    image.className = "bomb-blinker-img";
}