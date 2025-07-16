function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

//const started = false;
const serverId = window.location.pathname.split('/').pop();
const startBtn = document.getElementById("start-server");
const stopBtn = document.getElementById("stopServer");
const sendCommandBtn = document.getElementById('sendCommand');
const getLogsBtn = document.getElementById('getLogsBtn');

let IsShuttingDown = true;

let statsId;

let loaderC = document.getElementById('loading');
let textIndicator = document.getElementById('text-top');

if (startBtn != null) {
    startBtn.addEventListener("click", async () => {
        const response = await fetch("/Server/See/" + serverId + "/api/start", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
            }
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

async function MainTimer() {
    let timerElement = document.getElementById("timer");
    let timerSpan = document.getElementById("timerSpan");
    timerSpan.className = 'showFZ';

    try {
        let response = await fetch("/Server/See/" + serverId + "/api/remaining-time");
        if (!response.ok) throw new Error("Ошибка при получении времени");
        let data = await response.json();
        let remainingTime = data.remainingTime;
        if (remainingTime === -1) {
            IsShuttingDown = false
            timerSpan.className = 'hideFZ';
            return;
        }
        else {
            IsShuttingDown = true;
        }

        function updateTimer() {
            if (IsShuttingDown === false) {
                return;
            }
            if (remainingTime <= 0) {
                timerSpan.textContent = "Время истекло! Сервер отключается.";
                timerElement.textContent = '';
                return;
            }

            let minutes = Math.floor(remainingTime / 60);
            let seconds = remainingTime % 60;
            timerElement.textContent = `Осталось времени: ${minutes}:${seconds.toString().padStart(2, '0')}`;
            remainingTime--;
            setTimeout(updateTimer, 1000);
        }

        updateTimer(); // Запускаем таймер

    } catch (error) {
        timerElement.textContent = "Ошибка загрузки таймера.";
        console.error(error);
    }
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
            sendCommandBtn.removeAttribute('disabled');
            eventSource.close();
            fetchServerStats();
            MainTimer();
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

const addUserBtn = document.getElementById('addUserBtn');

if (addUserBtn != null) {
    addUserBtn.addEventListener('click', () => {
        document.getElementById('addUserBtn').classList.add('hideFZ');
        document.getElementById('addUserBtn').classList.remove('d-flex');
        document.getElementById('addUserBtn').classList.add('d-none');
        document.getElementById('addUserInputGroup').classList.remove('hideFZ');
        document.getElementById('addUserInputGroup').classList.add('showFZ');
    });

    document.getElementById('confirmAddUserBtn').addEventListener('click', async () => {
        const username = document.getElementById('addUserInput').value;
        if (!username) return alert('Введите имя пользователя');
        document.getElementById('addUserInput').value = "";
        try {
            const response = await fetch("/Server/See/" + serverId + "/allow/add?user=" + username, {
                method: 'GET'
            });
            let res = await response.json();
            if (res.result == 'done') {
                window.location.href = '/Server/See/' + serverId + '/allow?user=' + username;
            }
            else if (res.result == 'alreadyshared') {
                alert('Пользователь уже добавлен');
            }
            else if (res.result == 'usernotfound') {
                alert('Пользователь не найден');
            }
            else if (res.result == 'self') {
                alert('Ты и так создатель сервера');
            }
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        }
    });
}

const copyBtn = document.getElementById('copyAddr');

copyBtn.addEventListener('click', () => {
    navigator.clipboard.writeText(copyBtn.textContent.split(' ')[0]);
    const badge = document.getElementById('copiedBadge');
    badge.classList.remove('hide');
    badge.classList.add('show');
    sleep(2000).then(() => {
        badge.classList.add('hide');
        badge.classList.remove('show');
    });
});

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
            if (data.players > 0  && IsShuttingDown === true) {
                document.getElementById('timerSpan').className = 'hideFZ';
                IsShuttingDown = false;
                //timerSpan.;
            }
            if (data.players === 0 && IsShuttingDown === false) {
                MainTimer();
            }
            document.getElementById('players-online').textContent = `Онлайн: ${data.players}`;
            document.getElementById('ram-free').textContent = `Занято: ${data.memoryUsage} MB`;
            document.getElementById('ram-usage').textContent = `Использование: ${parseFloat(((data.memoryUsage / 6000) * 100)).toFixed(2)}%`;
        }
        else if (data.type == "Stop") {
            document.getElementById('timerSpan').className = 'hideFZ';
            IsShuttingDown = false;
            clearInterval(statsId);
            stopBtn.setAttribute('disabled', '');
            sendCommandBtn.setAttribute('disabled', '');
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

if (getLogsBtn != null) {
    getLogsBtn.addEventListener('click', async () => {

        const response = await fetch('/shared/getlogs?id=' + serverId, {
            method: 'GET',
        });

        if (response.status == 401) {
            alert('Запрещено');
            return;
        }

        if (response.status == 404) {
            alert('Сервер не найден');
            return;
        }
        window.open(response.url, '_blank');
    });
}

if (stopBtn != null) {
    if (!document.getElementById('check').classList.contains('showFZ')) {
        loaderC.className = "showFZ loader ms-1";
        textIndicator.textContent = 'Сервер запускаеться';
    }


    document.addEventListener('DOMContentLoaded', () => {

        const serverId = window.location.pathname.split('/').pop(); // Берём ID из URL
        const socket = new WebSocket(`wss://${window.location.host}/ws/logs/${serverId}`, [], {
            credentials: 'include'
        });
        window.addEventListener("beforeunload", () => {
            if (socket.readyState === WebSocket.OPEN) {
                socket.close(1000, "Page reloading");
            }
        });
        const logsContainer = document.getElementById('logs');

        socket.onmessage = (event) => {
            logsContainer.innerText += `\n${event.data}`;
            logsContainer.scrollTop = logsContainer.scrollHeight;
        };

        if (document.getElementById('check').classList.contains('hideFZ')) {
            subscribeToServerStart();
        }
        else {
            fetchServerStats();
            MainTimer();
            statsId = setInterval(fetchServerStats, 5000);
        }




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
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
            }
            //body: //JSON.stringify(pass),
        });
        const result = await response.text();
        if (result == "Выключение.") {
            textIndicator.textContent = result;
            document.getElementById('check').className = 'hideFZ checkmark ms-3';
            loaderC.className = 'showFZ loader ms-1';
            stopBtn.setAttribute('disabled', '');
            sendCommandBtn.setAttribute('disabled', '');
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


    sendCommandBtn.addEventListener('click', async () => {
        const command = document.getElementById('commandInput').value;
        if (!command) return alert('Введите команду');
        if (command == "stop") return alert('Ага, че умный дофига?');
        document.getElementById('commandInput').value = "";
        try {
            const response = await fetch("/Server/See/" + serverId + "/api/command", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(command),
            });
            alert(await response.text());
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        }
    });
}
