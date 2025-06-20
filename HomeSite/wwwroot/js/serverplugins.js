const serverId = window.location.pathname.split('/').reverse()[1];

async function deleteFile(fileName) {
    if (!confirm(`Удалить плагин ${fileName}?`)) return;

    const response = await fetch(`/server/configure/${serverId}/deleteplugin?file=${fileName}`, {
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

    const response = await fetch(`/server/configure/${serverId}/uploadplugin`, {
        method: "POST",
        body: formData
    });

    if (response.ok) {
        alert("Файл загружен");
        location.reload();
    } else {
        alert("Ошибка загрузки");
    }
}