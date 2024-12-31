function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

document.getElementById('fileSendBtn').addEventListener('click', async () => {
    var input = document.querySelector('input[type="file"]')

    var data = new FormData()
    data.append('file', input.files[0])
    data.append('user', 'hubot')

    const response = await fetch('/shared/uploadfile', {
        method: 'POST',
        body: data
    });
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
