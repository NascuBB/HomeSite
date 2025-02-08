function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

//const started = false;
const serverId = window.location.pathname.split('/').pop();
const startBtn = document.getElementById("start-server");
const stopBtn = document.getElementById("stopServer");

let statsId;

let loaderC = document.getElementById('loading');
let textIndicator = document.getElementById('text-top');

if (startBtn != null) {
    startBtn.addEventListener("click", async () => {
        const response = await fetch("/Server/See/" + serverId + "/api/start", {
            method: "POST",
        });
        const result = await response.text();
        if (result == "Сервер запускается.") {
            //startBtn.className = "hideFZ btn btn-success";   removeAttribute("disabled")
            startBtn.setAttribute("disabled", "")
            loaderC.className = "showFZ loader ms-1";
            textIndicator.textContent = result;
            sleep(1000).then(() => {
                textIndicator.textContent = "Сайт сейчас перезагрузиться";
                sleep(1000).then(() => { location.reload(); });
            });
        }
    });
}
async function subscribeToServerStart() {
    const eventSource = new EventSource(window.location.href + "/sti/subscribe");

    eventSource.onmessage = function (event) {
        const data = JSON.parse(event.data);

        if (data.Type === "ServerStarted") {
            textIndicator.textContent = 'Сервер запущен';
            loaderC.className = 'hideFZ loader ms-1';
            document.getElementById('check').className = 'showFZ checkmark ms-3';
            stopBtn.removeAttribute('disabled');
            eventSource.close();
            fetchServerStats();
            statsId = setInterval(fetchServerStats, 5000);
        }
        else if (data.Type === "ServerCrashed") {
            stopBtn.setAttribute('disabled', '');
            textIndicator.textContent = 'Ошибка запуска сервера';
            loaderC.className = 'hideFZ loader ms-1';
            document.getElementById('cross').className = 'showFZ cross ms-3';
        }
    };

    eventSource.onerror = function () {
        console.error("Ошибка SSE, повторное подключение...");
        setTimeout(subscribeToServerStart, 0); // Повторное подключение через 3 секунды
    };
}

async function fetchServerStats() {
    try {
        const response = await fetch(window.location.href + '/sti', { method: 'GET' });
        const data = await response.json();

        if (data.type == "Info") {
            //if (loaderC.classList.contains('hideFZ')) {
            //    document.getElementById('text-bottom').textContent = 'Сервер запущен';
            //    loaderC.className = 'hideFZ loader';
            //    document.getElementById('check').className = 'showFZ checkmark';
            //}
            document.getElementById('players-online').textContent = `Онлайн: ${data.players}`;
            document.getElementById('ram-free').textContent = `Занято: ${data.memoryUsage} MB`;
            document.getElementById('ram-usage').textContent = `Использование: ${parseFloat(((data.MemoryUsage / 6000) * 100)).toFixed(2)}%`;
        }
        else if (data.type == "Stop") {
            clearInterval(statsId);
            stopBtn.setAttribute('disabled', '');
            textIndicator.textContent = 'Сервер завершил роботу';
            loaderC.className = 'hideFZ loader ms-1';
            document.getElementById('check').className = 'hideFZ checkmark ms-3';
            document.getElementById('players-online').textContent = `Онлайн: уже нету`;
            document.getElementById('ram-free').textContent = `Занято: опять хз MB`;
            document.getElementById('ram-usage').textContent = `Использование: уже нет%`;
        }
    } catch (error) {
        console.error('Ошибка запроса:', error);
    }
}

if (stopBtn != null) {
    if (!document.getElementById('check').classList.contains('showFZ')) {
        loaderC.className = "showFZ loader ms-1";
        textIndicator.textContent = 'Сервер запускаеться';
    }


    document.addEventListener('DOMContentLoaded', () => {

        const serverId = window.location.pathname.split('/').pop(); // Берём ID из URL
        const socket = new WebSocket(`ws://${window.location.host}/ws/logs/${serverId}`, [], {
            credentials: 'include'
        });
        const logsContainer = document.getElementById('logs');

        socket.onmessage = (event) => {
            logsContainer.innerText += `\n${event.data}`;
            logsContainer.scrollTop = logsContainer.scrollHeight;
        };

        subscribeToServerStart();



        //const eventSource = new EventSource(window.location.href + '/sti');

        //eventSource.onmessage = function (event) {
        //    const data = JSON.parse(event.data);

        //    if (data.Type == "Info") {
        //        //if (loaderC.classList.contains('hideFZ')) {
        //        //    document.getElementById('text-bottom').textContent = 'Сервер запущен';
        //        //    loaderC.className = 'hideFZ loader';
        //        //    document.getElementById('check').className = 'showFZ checkmark';
        //        //}
        //        document.getElementById('players-online').textContent = `Онлайн: ${data.Players}`;
        //        document.getElementById('ram-free').textContent = `Занято: ${data.MemoryUsage} MB`;
        //        document.getElementById('ram-usage').textContent = `Использование: ${parseFloat(((data.MemoryUsage / 6000) * 100)).toFixed(2)}%`;
        //    }
        //    else if (data.Type == "Server") {
                
        //        document.getElementById('text-bottom').textContent = 'Сервер запущен';
        //        loaderC.className = 'hideFZ loader';
        //        document.getElementById('check').className = 'showFZ checkmark';
        //    }
        //};

        //eventSource.onerror = function (error) {
        //    console.error('SSE Error:', error);
        //};
    });

    stopBtn.addEventListener("click", async () => {
        //const pass = document.getElementById('passInput').value;
        //if (!pass) return alert('Введите пароль');
        const response = await fetch("/Server/See/" + serverId + "/api/stop", {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            //body: //JSON.stringify(pass),
        });
        const result = await response.text();
        if (result == "Выключение.") {
            textIndicator.textContent = result;
            document.getElementById('check').className = 'hideFZ checkmark ms-3';
            loaderC.className = 'showFZ loader ms-1';
            stopBtn.setAttribute('disabled', '');
        }
        else {
            return alert(result);
        }
    });

    //const connection = new signalR.HubConnectionBuilder()
    //    .withUrl("/minecraftHub")
    //    .build();

    //connection.on("ReceiveLog", (message) => {
    //    const logsContainer = document.getElementById('logs');
    //    logsContainer.innerText += `\n${message}`;
    //    logsContainer.scrollTop = logsContainer.scrollHeight;
    //});

    //connection.start().catch(err => console.error(err.toString()));

    document.getElementById('sendCommand').addEventListener('click', async () => {
        const command = document.getElementById('commandInput').value;
        if (!command) return alert('Введите команду');
        if (command == "stop") return alert('Ага, че умный дофига?');
        document.getElementById('commandInput').value = "";
        try {
            const response = await fetch("/Server/See/" + serverId + "/api/command", {
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
