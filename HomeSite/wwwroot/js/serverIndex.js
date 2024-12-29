function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

const startBtn = document.getElementById("start-server");
const stopBtn = document.getElementById("stopServer");

if (startBtn != null) {
    startBtn.addEventListener("click", async () => {
        const response = await fetch("/api/Server/start", {
            method: "POST",
        });
        const result = await response.text();
        if (result == "Сервер запускается.") {
            startBtn.className = "hideFZ btn btn-success";
            document.getElementById("loading").className = "showFZ loader";
            document.getElementById("text-top").textContent = result;
            sleep(1000).then(() => {
                document.getElementById("text-top").textContent = "Сайт сейчас перезагрузиться";
                sleep(1000).then(() => { location.reload(); });
            });
        }
    });
}

if (stopBtn != null) {
    stopBtn.addEventListener("click", async () => {
        const pass = document.getElementById('passInput').value;
        if (!pass) return alert('Введите пароль');
        const response = await fetch('/api/server/stop', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(pass),
        });
        const result = await response.text();
        if (result == "Выключение.") {
            document.getElementById('text-bottom').textContent = result;
        }
        else {
            return alert(result);
        }
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/minecraftHub")
        .build();

    connection.on("ReceiveLog", (message) => {
        const logsContainer = document.getElementById('logs');
        logsContainer.innerText += `\n${message}`;
        logsContainer.scrollTop = logsContainer.scrollHeight;
    });

    connection.start().catch(err => console.error(err.toString()));

    document.getElementById('sendCommand').addEventListener('click', async () => {
        const command = document.getElementById('commandInput').value;
        if (!command) return alert('Введите команду');
        if (command == "stop") return alert('Ага, че умный дофига?');
        document.getElementById('commandInput').value = "";
        try {
            const response = await fetch('/api/server/command', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(command),
            });
            alert(await response.text());
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        }
    });
}
