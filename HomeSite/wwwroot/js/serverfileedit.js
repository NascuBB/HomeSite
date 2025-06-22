function confirmDelete(fileName, deleteUrl) {
    const form = document.getElementById('deleteForm');
    const bodyText = document.getElementById('deleteModalBodyText');

    form.action = deleteUrl;
    bodyText.innerText = `Вы действительно хотите удалить файл "${fileName}"?`;

    const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
    modal.show();
}

function switchToEdit() {
    //var text = document.getElementById('mainLabel').innerText.replace('Просмотр', 'Редактирование');
    document.getElementById('mainLabel').innerText = "Редактирование файла";
    document.getElementById("viewMode").classList.add("d-none");
    document.getElementById("viewModeBtn").classList.add("d-none");
    document.getElementById("editMode").classList.remove("d-none");
}

function cancelEdit() {
    //var text = document.getElementById('mainLabel').innerText.replace('Редактирование', 'Просмотр');
    document.getElementById('mainLabel').innerText = "Просмотр файла";
    document.getElementById("editMode").classList.add("d-none");
    document.getElementById("viewMode").classList.remove("d-none");
    document.getElementById("viewModeBtn").classList.remove("d-none");
}
document.addEventListener('DOMContentLoaded', function () {
    hljs.highlightAll();

    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key === "s") {
            e.preventDefault();

            const form = document.querySelector("#editMode form");
            if (form && !form.classList.contains("d-none")) {
                form.submit();
            }
        }
    });

});