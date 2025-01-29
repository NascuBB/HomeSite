function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

document.getElementById('getModsBtn').addEventListener('click', async () => {
    window.location.href = '/shared/downloadfile?id=mods';
});

var sendBtn = document.getElementById('fileSendBtn');

if (sendBtn != null) {
    sendBtn.addEventListener('click', async () => {
        var input = document.querySelector('input[type="file"]')
        const errorSpan = document.getElementById('errorUplSpan');
        if (input.files.length == 0) {
            errorSpan.textContent = 'Файл не выбран';
            errorSpan.className = 'text-danger align-self-center showFZ';
            return;
        }
        var data = new FormData()
        data.append('file', input.files[0])
        data.append('user', 'hubot')
        let loaderC = document.getElementById('loading');
        loaderC.className = 'showFZ loader';
        const response = await fetch('/shared/uploadfile', {
            method: 'POST',
            body: data
        });
        errorSpan.className = 'text-danger align-self-center hideFZ';
        if (response.status != 200) {
            errorSpan.textContent = 'Ошибка загрузки. Возможно файл слишком большой';
            errorSpan.className = 'text-danger align-self-center showFZ';
            loaderC.className = 'hideFZ loader';
            return;
        }
        loaderC.className = 'hideFZ loader';
        const result = await response.text();
        document.getElementById('fileIdResult').textContent = result;
    });

    const copyBtn = document.getElementById('copyIdButton');
    copyBtn.addEventListener('click', () => {
        // Get the text field
        var copyText = document.getElementById('fileIdResult');

        // Select the text field

        // Copy the text inside the text field
        navigator.clipboard.writeText(copyText.textContent);
        copyBtn.textContent = "Готово!";
        sleep(1000).then(() => {
            copyBtn.textContent = "Копировать";
        });
        // Alert the copied text
        // alert("Copied the text: " + copyText.value);
    });
}

document.getElementById('getFileButton').addEventListener('click', async () => {
    const id = document.getElementById('fileIdInput').value;
    const errorSpan = document.getElementById('errorGetSpan');
    if (id == '') {
        errorSpan.textContent = 'И где ID?';
        errorSpan.className = 'text-danger align-self-center showFZ';
        return;
    }
    errorSpan.className = 'text-danger align-self-center hideFZ';

    const response = await fetch('/shared/downloadfile?id=' + id, {
        method: 'GET',
    });

    if (response.status == 404) {
        errorSpan.textContent = 'Файл не найден. Возможно он удален';
        errorSpan.className = 'text-danger align-self-center showFZ';
        return;
    }
    window.location.href = response.url;

});
