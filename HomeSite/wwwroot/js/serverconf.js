let loaderFl = document.getElementById('loadingFlight');
let loaderForce = document.getElementById('loadingForceGM');
let loaderNether = document.getElementById('loadingNether');
let loaderOnline = document.getElementById('loadingOnline');
let loaderPvp = document.getElementById('loadingPvp');
let loaderMonst = document.getElementById('loadingSpawnMonsters');
let loaderWhite = document.getElementById('loadingWhiteList');
let loaderCommand = document.getElementById('loadingCommand');
let loaderGameMode = document.getElementById('loadingGameMode');
let loaderDifficulty = document.getElementById('loadingDifficulty');
let loaderPlayers = document.getElementById('loadingPlayers');
let loaderSpawn = document.getElementById('loadingSpawn');
const serverId = window.location.pathname.split('/').pop();

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

//Flight
document.getElementById("FlightSwitch").addEventListener("change", async (event) => {
    loaderFl.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "allow-flight",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderFl.className = 'hideFZ loader' });
    }
});

//Force game mode
document.getElementById("ForceGMSwitch").addEventListener("change", async (event) => {
    loaderForce.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "force-gamemode",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderForce.className = 'hideFZ loader' });
    }
});

////Nether
document.getElementById("NetherSwitch").addEventListener("change", async (event) => {
    loaderNether.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "allow-nether",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderNether.className = 'hideFZ loader' });
    }
});

////Online mode
document.getElementById("OnlineSwitch").addEventListener("change", async (event) => {
    loaderOnline.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "online-mode",
                value: (!event.currentTarget.checked).toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderOnline.className = 'hideFZ loader' });
    }
});

////Pvp
document.getElementById("PvpSwitch").addEventListener("change", async (event) => {
    loaderPvp.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "pvp",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderPvp.className = 'hideFZ loader' });
    }
});

////Spawn monsters
document.getElementById("SpawnMonstersSwitch").addEventListener("change", async (event) => {
    loaderMonst.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "spawn-monsters",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderMonst.className = 'hideFZ loader' });
    }
});

////White list
document.getElementById("WhiteListSwitch").addEventListener("change", async (event) => {
    loaderWhite.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "white-list",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderWhite.className = 'hideFZ loader' });
    }
});

////Command block
document.getElementById("CommandSwitch").addEventListener("change", async (event) => {
    loaderCommand.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "enable-command-block",
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderCommand.className = 'hideFZ loader' });
    }
});

//Game mode
document.getElementById("gameModeSelect").addEventListener("change", async (event) => {
    loaderGameMode.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "gamemode",
                value: event.target.value.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderGameMode.className = 'hideFZ loader' });
    }
});

//Difficulty
document.getElementById("difficultySelect").addEventListener("change", async (event) => {
    loaderDifficulty.className = 'showFZ loader';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "difficulty",
                value: event.target.value.toString()
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderDifficulty.className = 'hideFZ loader' });
    }
});

//Players max-players
document.getElementById('playersButton').addEventListener('click', async () => {
    loaderPlayers.className = 'showFZ loader';
    var input = document.getElementById('playersInput');
    var v = input.value.toString() ? input.value.toString() : '0';
    try {
        const response = await fetch(`/Server/configure/${serverId}/set`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                preference: "max-players",
                value: v
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderPlayers.className = 'hideFZ loader' });
    }
});

//spawn protection
document.getElementById('spawnButton').addEventListener('click', async () => {
    loaderSpawn.className = 'showFZ loader';
    var input = document.getElementById('spawnInput');
    var v = input.value.toString() ? input.value.toString() : '0';
    try {
            const response = await fetch(`/Server/configure/${serverId}/set`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    preference: "spawn-protection",
                    value: v
            }),
        });

        //alert(await response.text());
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderSpawn.className = 'hideFZ loader' });
    }
});


var sendBtn = document.getElementById('fileSendBtn');

sendBtn.addEventListener('click', async () => {
    var input = document.querySelector('input[type="file"]');
    //const successSpan = document.getElementById('completeSpan');
    const errorSpan = document.getElementById('errorUplSpan');
    errorSpan.className = 'hideFZ align-self-center text-danger';


    if (input.files.length === 0) {
        errorSpan.textContent = 'Файл не выбран';
        errorSpan.className = 'text-danger align-self-center showFZ';
        return;
    }

    var file = input.files[0];

    var data = new FormData();
    data.append('file', file);

    let loaderC = document.getElementById('loading');
    loaderC.className = 'showFZ loader';

    try {
        const response = await fetch(`/shared/uploadmods?Id=${serverId}`, {
            method: 'POST',
            body: data
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }
        errorSpan.className = 'text-success align-self-center showFZ';
        errorSpan.textContent = 'Моды успешно загружены';
    } catch (err) {
        console.error("Ошибка загрузки файла:", err);
        errorSpan.textContent = 'Ошибка загрузки. Возможно, файл слишком большой';
        errorSpan.className = 'text-danger align-self-center showFZ';
    } finally {
        loaderC.className = 'hideFZ loader';
    }
});

document.getElementById("finishBtn").addEventListener("click", async () => {
    let loader = document.getElementById("loadingCreating"); // Индикатор загрузки
    loader.className = "loader showFZ"; // Показываем индикатор

    try {
        let serverId = window.location.pathname.split("/").pop(); // Получаем ID из URL

        const response = await fetch(`/Server/configure/${serverId}/finish`, {
            method: "POST",
            headers: { "Content-Type": "application/json" }
        });

        if (!response.ok) {
            throw new Error(await response.text()); // Выводим ошибку, если не 200 OK
        }

        let isSuccess = await response.json(); // Получаем `true` или `false`

        if (isSuccess) {
            window.location.href = `/Server/See/${serverId}`; // Перенаправляем на сервер
        } else {
            alert("Ошибка: сервер не удалось создать.");
        }
    } catch (error) {
        alert("Ошибка: " + error.message);
    } finally {
        lloader.className = "loader hideFZ"; // Скрываем индикатор
    }
});