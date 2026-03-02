function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

let loaderShortlog = document.getElementById('loadingShortLogs');

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