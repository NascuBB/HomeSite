document.getElementById('fileSendBtn').addEventListener('click', async () => {
    var input = document.querySelector('input[type="file"]')

    var data = new FormData()
    data.append('file', input.files[0])
    data.append('user', 'hubot')

    fetch('/shared/uploadfile', {
        method: 'POST',
        body: data
    })
});