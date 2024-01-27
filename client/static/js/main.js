let connection;
let playerName = localStorage.getItem('playerName');

$(document).ready(function () {
    $('#player-name-input').val(playerName);
    initServer();
});

function initServer() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://memethegatheringapi.azurewebsites.net/GameHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connectToServer();
    initReceiveMethods();
    initSendMethods();
}

function connectToServer() {
    connection.start().then(() => {
        $('#createRoomButton').removeAttr('disabled', 'disabled');
        $('#joinRoomButton').removeAttr('disabled', 'disabled');
    }).catch(function (err) {
        return console.error("connectToServer :: " + err.toString());
    });

    connection.onclose(error => {
        $('#createRoomButton').attr('disabled');
        $('#joinRoomButton').attr('disabled');
    });
}

function initReceiveMethods() {
    connection.on("ReceiveMessage", function (user, message) {
        const msg = user + " mówi: " + message;
        const li = document.createElement("li");
        li.textContent = msg;
        document.getElementById("games-list").appendChild(li);
    });

    connection.on("ReceiveMessage", function (user, message) {
        const msg = user + " mówi: " + message;
        const li = document.createElement("li");
        li.textContent = msg;
        document.getElementById("games-list").appendChild(li);
    });

    connection.on("JoinedToRoom", function (user, message) {
        switchToRoom();
    });

    connection.on("NewPlayerJoinedToRoom", function (playerName) {
        addToGameLog("Player " + playerName + "joined to the room!")
    });
}

function initSendMethods() {
    document.getElementById("createRoomButton").addEventListener("click", function (event) {
        playerName = $('#player-name-input').val();
        localStorage.setItem('playerName', playerName);
        connection.invoke("CreateRoom", playerName).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });

    document.getElementById("joinRoomButton").addEventListener("click", function (event) {
        let roomID = $('#room-id-input').val();
        playerName = $('#player-name-input').val();
        localStorage.setItem('playerName', playerName);
        connection.invoke("JoinRoom", playerName, roomID).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });
}

function switchToLobby() {
    $('#lobby').show();
    $('#room').hide();
}

function switchToRoom() {
    $('#lobby').hide();
    $('#room').show();
}

function addToGameLog(message) {
    let li = document.createElement("li");
    li.textContent = message;
    document.querySelector("#gameLog ul").appendChild(li);
}


