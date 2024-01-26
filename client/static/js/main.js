let connection;
let playerName = localStorage.getItem('playerName');

$(document).ready(function () {
    $('#player-name-input').val(playerName);
    initServer();
});

function initServer() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://578d-87-206-130-93.ngrok-free.app/TestHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connectToServer();
    initReceiveMethods();
    initSendMethods();
}

function connectToServer() {
    connection.start().catch(function (err) {
        return console.error("connectToServer :: " + err.toString());
    });
}

function initReceiveMethods(){
    connection.on("ReceiveMessage", function (user, message) {
        const msg = user + " mówi: " + message;
        const li = document.createElement("li");
        li.textContent = msg;
        document.getElementById("games-list").appendChild(li);
    });
}

function initSendMethods(){
    document.getElementById("createRoomButton").addEventListener("click", function (event) {
        playerName = $('#player-name-input').val();
        localStorage.setItem('playerName', playerName);
        connection.invoke("CreateGame", playerName).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });

    document.getElementById("joinRoomButton").addEventListener("click", function (event) {
        roomID = $('#room-id-input').val();
        playerName = $('#player-name-input').val();
        localStorage.setItem('playerName', playerName);
        connection.invoke("JoinRoom", playerName, roomID).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });
}


