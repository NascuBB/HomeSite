const serverId = window.location.pathname.split('/')[3];
// Получаем параметры из строки запроса
const urlParams = new URLSearchParams(window.location.search);
const username = urlParams.get('user');

let loaderC = document.getElementById('loading');
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/Server/See/' + serverId;
});

document.getElementById('deleteBtn').addEventListener('click', async () => {
    try {
        const response = await fetch('/Server/See/' + serverId + '/allow/delete?user=' + username, {
            method: 'POST',
        });

        if (!response.ok) {
            alert("Ошибка удаления");
        }
        sleep(500).then(() => {
            loaderC.className = "hideFZ loader ms-1";
            window.location.href = '/Server/See/' + serverId;
        });
    } catch (error) {
        console.error("Ошибка при отправке данных:", error);
        alert("Ошибка при отправке данных.");
    }
});

document.getElementById('saveBtn').addEventListener('click', async () => {
    // Собираем данные из чекбоксов
    const sharedRights = {
        EditServerPreferences: document.getElementById('EditSPSwitch').checked,
        EditMods: document.getElementById('EditModsSwitch').checked,
        StartStopServer: document.getElementById('StopStartServerSwitch').checked,
        UploadMods: document.getElementById('UploadModsSwitch').checked,
        SendCommands: document.getElementById('SendCommandsSwitch').checked,
        AddShareds: document.getElementById('EditSharedsSwitch').checked
    };
    loaderC.className = "showFZ loader ms-1";
    try {
        const response = await fetch('/Server/See/' + serverId + '/allow/save?user=' + username, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(sharedRights)
        });

        if (!response.ok) {
            alert("Ошибка сохранения прав.");
        }
        sleep(500).then(() => {
            loaderC.className = "hideFZ loader ms-1";
        });
    } catch (error) {
        console.error("Ошибка при отправке данных:", error);
        alert("Ошибка при отправке данных.");
    }
});