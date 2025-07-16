function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

let loaderShortlog = document.getElementById('loadingShortLogs');
const saveDescBtn = document.getElementById('descBtn');

document.getElementById("ShortLogsSwitch").addEventListener("change", async (event) => {
    loaderShortlog.className = 'form-check-label loader-sm showFZ mt-1 ms-2';
    try {
        const response = await fetch(`/account/setpref`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: JSON.stringify({
                preference: '',
                value: event.currentTarget.checked.toString()
            }),
        });

        //alert(await response.status);
    } catch (err) {
        console.error('Ошибка отправки команды:', err);
    } finally {
        sleep(100).then(() => { loaderShortlog.className = 'form-check-label loader-sm hideFZ mt-1 ms-2' });
    }
});

if (saveDescBtn != null) {
    let loaderDesc = document.getElementById('loadingDesc');
    let loaderName = document.getElementById('loadingName');
    const username = document.getElementById('username').textContent.split(' ')[1];
    const serverId = document.getElementById('serverId').textContent;

    saveDescBtn.addEventListener('click', async () => {
        loaderDesc.className = 'showFZ loader ms-2 my-auto'
        var input = document.getElementById('descInput');
        var v = input.value.toString() ? input.value.toString() : 'A Minecraft server';
        try {
            const response = await fetch(`/Server/configure/${serverId}/set`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
                body: JSON.stringify({
                    preference: "motd",
                    value: v
                }),
            });

            //alert(await response.text());
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderDesc.className = 'hideFZ loader ms-2 my-auto' });
        }
    });

    document.getElementById('nameBtn').addEventListener('click', async () => {
        loaderName.className = 'showFZ loader ms-2 my-auto'
        var input = document.getElementById('nameInput');
        var v = input.value.toString() ? input.value.toString() : `${username}'s server`;
        try {
            const response = await fetch(`/server/configure/${serverId}/set`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
                body: JSON.stringify({
                    preference: "name",
                    value: v
                }),
            });

            //alert(await response.text());
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderName.className = 'hideFZ loader ms-2 my-auto' });
        }
    });
}