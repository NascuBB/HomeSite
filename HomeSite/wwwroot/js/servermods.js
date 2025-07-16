const serverId = window.location.pathname.split('/').reverse()[1];

async function deleteFile(fileName) {
    if (!confirm(`Удалить мод ${fileName}?`)) return;

    const response = await fetch(`/Server/configure/${serverId}/deletemod?file=${fileName}`, {
        headers: {
            "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        method: "DELETE"
    });

    if (response.ok) {
        alert("Файл удалён");
        location.reload();
    } else {
        alert("Ошибка удаления");
    }
}

async function uploadFile() {
    const fileInput = document.getElementById("fileInput");
    if (fileInput.files.length === 0) {
        alert("Выберите файл");
        return;
    }

    let formData = new FormData();
    formData.append("file", fileInput.files[0]);

    const response = await fetch(`/Server/configure/${serverId}/uploadmod`, {
        method: "POST",
        headers: {
            "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: formData
    });

    if (response.ok) {
        alert("Файл загружен");
        location.reload();
    } else {
        alert("Ошибка загрузки");
    }
}

//var uploadbtn = document.getElementById('uploadButton');

//if (uploadbtn != null) {
//    uploadbtn.addEventListener('click', async () => {
//        await uploadFile();
//    });

//    document.getElementById('deleteButton').addEventListener('click', async () => {

//    });
//}