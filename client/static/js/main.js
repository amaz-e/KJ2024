let connection;
let playerName = localStorage.getItem('playerName');

$(document).ready(function () {
    $('#player-name-input').val(playerName);
    initServer();
});

function initServer() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://memethegatheringapi.azurewebsites.net/GameHub")
        .withAutomaticReconnect()
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

    connection.on("ReceiveServerRoomMessage", function (message) {
        addToGameLog(message);
    });

    connection.on("LobbyError", function (message) {
        showLobbyErrorMessage(message);
    });

    connection.on("RoomError", function (message) {
        addToGameLog("<div class='color: red'>" + message + "</div>");
    });

    connection.on("ReceiveMessage", function (user, message) {
        const msg = user + " mówi: " + message;
        const li = document.createElement("li");
        li.textContent = msg;
        document.getElementById("games-list").appendChild(li);
    });

    connection.on("JoinedToRoom", function (roomID, isOwner, otherPlayers) {
        switchToRoom();
        $("[data-type='roomID']").text("#" + roomID);
        if(isOwner){
            $("#startGameButton").show();
        }
        otherPlayers.forEach((playerName) => addPlayerZone(playerName));
    });

    connection.on("NewPlayerJoinedToRoom", function (playerName) {
        addToGameLog("Player " + playerName + " joined to the room!");
        addPlayerZone(playerName);
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

    document.getElementById("startGameButton").addEventListener("click", function (event) {
        connection.invoke("StartGame").catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });
}

function switchToLobby() {
    $('#lobby').show();
    $('#room').hide();
    window.onbeforeunload = null;
}

function switchToRoom() {
    $('#lobby').hide();
    $('#room').show();
    window.onbeforeunload = function() {
        return "Are you sure you want to leave this page?";
    };
}

function addToGameLog(message) {
    let li = document.createElement("li");
    li.textContent = message;
    document.querySelector("#gameLog ul").appendChild(li);
}

function showLobbyErrorMessage(message) {
    $("#lobbyErrorMessage").text(message);
}

function addPlayerZone(playerName){
    const playerZone = document.getElementById('playersZones');

    const playerDiv = document.createElement('div');
    playerDiv.className = 'player';

    const nameElement = document.createElement('h2');
    nameElement.textContent = playerName;

    playerDiv.appendChild(nameElement);
    playerZone.appendChild(playerDiv);
}

