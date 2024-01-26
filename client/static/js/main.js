let connection;

$(document).ready(function () {
    initServer();
});

function initServer() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://578d-87-206-130-93.ngrok-free.app/TestHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connectToServer();
    initMethods();

}

function connectToServer() {
    connection.start().catch(function (err) {
        return console.error("connectToServer :: " + err.toString());
    });
}

function initMethods(){
    connection.on("ReceiveMessage", function (user, message) {
        const msg = user + " mówi: " + message;
        const li = document.createElement("li");
        li.textContent = msg;
        document.getElementById("games-list").appendChild(li);
    });

    document.getElementById("sendButton").addEventListener("click", function (event) {
        connection.invoke("SendMessage", "Wise", "(nie)lubię Cię!").catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    });
}


