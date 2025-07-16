function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function shortenFileName(fileName, maxStart = 6, maxEnd = 8) {
    if (fileName.length <= (maxStart + maxEnd + 3)) return fileName;
    return fileName.substring(0, maxStart) + "..." + fileName.slice(-maxEnd);
}

//document.getElementById('getModsBtn').addEventListener('click', async () => {
//    window.location.href = '/shared/downloadfile?id=mods';
//});

var sendBtn = document.getElementById('fileSendBtn');
let modal;
let shareModal;
let renameModal;
let renameInput;

let currentFileId = null;
let currentFileShare = false;
let selectedElement = null;

// Простой drag-and-drop (необязательно, если drag не нужен — можно удалить)
const dropZone = document.getElementById('dropZone');
const fileInput = document.getElementById('inputGroupFile04');
const previewContainer = document.getElementById('filePreviewContainer');

window.addEventListener('DOMContentLoaded', function () {
    document.getElementById('dwld-div').addEventListener('click', function () {
        window.location.href = `/shared/downloadfile?id=${currentFileId}`;
    });

    const select = document.getElementById('shareSelect');
    select.addEventListener('change', async function (event) {
        const selectedValue = event.target.value;
        const response = await fetch("/shared/changeshare?id=" + currentFileId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: JSON.stringify(selectedValue),
        });
        if (response.status !== 200) {
            alert('Ошибка поделиться файлом');
        }
        else {
            selectedElement.setAttribute('data-share', selectedValue);
            const span = document.getElementById('shareSpan');
            span.classList.remove('hideFZ');
            span.classList.add('showFZ');
            sleep(2000).then(() => {
                span.classList.add('hideFZ');
                span.classList.remove('showFZ');
            });
        }
    });

    renameInput = document.getElementById('newNameInput');
    renameInput.addEventListener('click', function () {
        const extArr = selectedElement.querySelector('#filename').textContent.split('.');
        renameInput.value = extArr.length > 1 ? '.' + extArr[extArr.length - 1] : '';
        renameInput.setSelectionRange(0, 0);
    });

    document.getElementById('fileRenameBtn').addEventListener('click', async function () {
        const newname = renameInput.value;
        if (!newname) return;
        const response = await fetch('/shared/rename?id=' + currentFileId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: JSON.stringify(newname)
        });
        const span = document.getElementById('renameSpan');
        if (response.status !== 200) {
            span.classList.remove('text-green');
            span.classList.add('text-danger');
            span.textContent = 'Ошибка переименования';
            span.classList.remove('hideFZ');
            span.classList.add('showFZ');
            return;
        }
        span.classList.add('text-green');
        span.classList.remove('text-danger');
        span.innerHTML = '<i class="bi bi-check-circle-fill me-1"></i>Переименовано';
        span.classList.remove('hideFZ');
        span.classList.add('showFZ');
        const ext = newname.split('.').pop();
        const iconClass = getFileIcon(ext);
        selectedElement.querySelector('#filename').textContent = shortenFileName(newname);
        selectedElement.querySelector('#fileicon').className = 'file-icon mb-2 ' + iconClass;
        setTimeout(() => {
            span.classList.add('hideFZ');
            span.classList.remove('showFZ');
            renameModal.hide();
            renameInput.value = '';
        }, 1000);
    });

    document.getElementById('share-div').addEventListener('click', function () {
        copyLink.innerHTML = '';

        const linkText = document.createTextNode(location.origin + '/fileshare/' + currentFileId);
        const badge = document.createElement('span');
        badge.id = 'copiedBadge';
        badge.className = 'position-absolute top-0 translate-middle margin-badge badge ms-4 bg-green p-2 hideFZ';
        badge.innerHTML = ' скопировано<span class="visually-hidden">copied</span>';

        copyLink.append(linkText, badge);
        if (currentFileShare === 'True') {
            select.selectedIndex = 1; // Выбирает второй option
        } else {
            select.selectedIndex = 0; // Выбирает первый option
        }
        shareModal = new bootstrap.Modal(document.getElementById('shareModal'));
        shareModal.show();
    });

    document.getElementById('feat-div').addEventListener('click', async function () {
        if (selectedElement.getAttribute('data-feat') === 'True') {
            selectedElement.setAttribute('data-feat', 'False');

            // Удаляем звёздочку
            const star = selectedElement.querySelector('#feat-star');
            if (star) {
                star.remove();
            }
        } else {
            selectedElement.setAttribute('data-feat', 'True');

            // Добавляем звёздочку
            const icon = document.createElement('i');
            icon.id = 'feat-star';
            icon.className = 'bi file-icon bi-star-fill position-absolute align-self-start';
            icon.style.fontSize = '1.1rem';

            // Вставляем как первый дочерний элемент .d-flex ...
            const innerDiv = selectedElement.querySelector('.d-flex');
            if (innerDiv) {
                innerDiv.insertBefore(icon, innerDiv.firstChild);
            }
        }

        const response = await fetch('/shared/feature?id=' + currentFileId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: JSON.stringify(selectedElement.getAttribute('data-feat'))
        });
        if (response.status !== 200) {
            alert('Ошибка избранных');
        }
    });

    document.getElementById('rename-div').addEventListener('click', async function () {
        const response = await fetch('/shared/filename?id=' + currentFileId);
        if (response.status !== 200) {
            alert('ошибка получения файла');
            return;
        }
        const filename = await response.text();
        document.getElementById('fileSpan').textContent = filename;
        renameModal = new bootstrap.Modal(document.getElementById('renameModal'));
        renameModal.show();
    });

    document.getElementById('delete-div').addEventListener('click', async function () {
        console.log("Удалить файл с ID:", currentFileId);
        const response = await fetch('/shared/deletefile?id=' + currentFileId, {
                method: 'DELETE'
        });
        if (response.status != 200) {
            alert('Ошибка удаления');
            return;
        }
        location.reload()
    });

    const copyBtn = document.getElementById('copyLink');

    copyBtn.addEventListener('click', () => {
        navigator.clipboard.writeText(copyBtn.textContent.split(' ')[0]);
        const badge = document.getElementById('copiedBadge');
        badge.classList.remove('hideFZ');
        badge.classList.add('showFZ');
        sleep(2000).then(() => {
            badge.classList.add('hideFZ');
            badge.classList.remove('showFZ');
        });
    });
});
let contextMenuOpen = false;

function showContextMenu(event, element) {
    event.preventDefault();

    const contextMenu = document.getElementById('contextMenu');

    // Закрыть предыдущее меню
    if (contextMenuOpen) {
        contextMenu.style.display = 'none';
        document.removeEventListener('click', closeMenu);
    }

    contextMenu.style.left = event.pageX + 'px';
    contextMenu.style.top = event.pageY + 'px';
    contextMenu.style.display = 'block';

    selectedElement = element;
    currentFileId = element.getAttribute('data-fileid');
    currentFileShare = element.getAttribute('data-share');
    contextMenuOpen = true;

    setTimeout(() => {
        document.addEventListener('click', closeMenu);
    }, 0);
}

function closeMenu(e) {
    const contextMenu = document.getElementById('contextMenu');
    if (!contextMenu.contains(e.target)) {
        contextMenu.style.display = 'none';
        contextMenuOpen = false;
        document.removeEventListener('click', closeMenu);
    }
}
document.querySelector('.btn-green').addEventListener('click', function () {
    modal = new bootstrap.Modal(document.getElementById('uploadModal'));
    modal.show();
});

sendBtn.addEventListener('click', async () => {
    const errorSpan = document.getElementById('errorUplSpan');
    const progressBar = document.querySelector('#uploadModal .progress-bar');

    if (fileInput.files.length === 0) {
        errorSpan.textContent = 'Файл не выбран';
        errorSpan.className = 'text-danger align-self-center showFZ';
        return;
    }

    const file = fileInput.files[0];

    // Сначала проверка доступного места
    const checkResponse = await fetch('/shared/checkupload', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value },
        body: JSON.stringify(file.size)
    });

    if (!checkResponse.ok) {
        const errorText = await checkResponse.text();
        errorSpan.textContent = errorText;
        errorSpan.className = 'text-danger align-self-center showFZ';
        return;
    }

    // Если всё ок — начинаем загрузку файла
    const formData = new FormData();
    formData.append('file', file);
    formData.append('user', 'hubot');

    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/shared/uploadfile');
    xhr.setRequestHeader('RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    xhr.upload.addEventListener('progress', function (e) {
        if (e.lengthComputable) {
            const percent = (e.loaded / e.total) * 100;
            progressBar.style.width = percent + '%';
        }
    });

    xhr.onload = function () {
        if (xhr.status === 200) {
            progressBar.style.width = '100%';
            setTimeout(() => {
                modal.hide();
                progressBar.style.width = '0%';
                previewContainer.innerHTML = '';
                location.reload();
            }, 1000);
        } else {
            errorSpan.textContent = 'Ошибка загрузки: ' + xhr.responseText;
            errorSpan.className = 'text-danger align-self-center showFZ';
        }
    };

    xhr.onerror = function () {
        loaderC.className = 'hideFZ loader';
        errorSpan.textContent = 'Ошибка соединения с сервером.';
        errorSpan.className = 'text-danger align-self-center showFZ';
    };

    errorSpan.className = 'text-danger align-self-center hideFZ';
    progressBar.style.width = '0%';
    xhr.send(formData);
});


// Подсветка при наведении на элементы контекстного меню
document.querySelectorAll('.context-menu-item').forEach(item => {
    item.addEventListener('mouseenter', function () {
        this.style.backgroundColor = 'rgba(0, 255, 0, 0.1)';
    });
    item.addEventListener('mouseleave', function () {
        this.style.backgroundColor = '';
    });
});

customFileBtn.addEventListener('click', () => {
    fileInput.click();
});

fileInput.addEventListener('change', () => {
    showFilePreviews(fileInput.files);
});

dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropZone.style.backgroundColor = 'rgba(255, 255, 255, 0.1)';
});

dropZone.addEventListener('dragleave', () => {
    dropZone.style.backgroundColor = 'rgba(255, 255, 255, 0)';
});

dropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    dropZone.style.backgroundColor = 'rgba(255, 255, 255, 0)';
    if (e.dataTransfer.files.length) {
        fileInput.files = e.dataTransfer.files;
        showFilePreviews(e.dataTransfer.files);
    }
});

// Карта расширений к Bootstrap Icons (или своим иконкам)
const extensionIconMap = {
    'mp3': 'bi-file-earmark-music-fill',
    'wav': 'bi-file-earmark-music-fill',
    'mp4': 'bi-file-earmark-play-fill',
    'mov': 'bi-file-earmark-play-fill',
    'pdf': 'bi-file-earmark-pdf-fill',
    'doc': 'bi-file-earmark-word-fill',
    'docx': 'bi-file-earmark-word-fill',
    'xls': 'bi-file-earmark-excel-fill',
    'xlsx': 'bi-file-earmark-excel-fill',
    'jpg': 'bi-file-earmark-image-fill',
    'jpeg': 'bi-file-earmark-image-fill',
    'png': 'bi-file-earmark-image-fill',
    'gif': 'bi-file-earmark-image-fill',
    'txt': 'bi-file-earmark-text-fill',
    'log': 'bi-file-earmark-text-fill',
    'zip': 'bi-file-earmark-zip-fill',
    'rar': 'bi-file-earmark-zip-fill',
    '7z': 'bi-file-earmark-zip-fill',
};

function getFileIcon(extension) {
    return extensionIconMap[extension.toLowerCase()] || 'bi-file-earmark-fill';
}

function showFilePreviews(files) {
    previewContainer.innerHTML = ''; // Очищаем старые карточки

    Array.from(files).forEach(file => {
        const ext = file.name.split('.').pop();
        const iconClass = getFileIcon(ext);

        const col = document.createElement('div');
        col.className = 'col-md-3 col-xxl-auto';

        col.innerHTML = `
            <div class="file-card p-3 h-100">
                <div class="d-flex flex-column align-items-center text-center">
                    <i class="${iconClass} file-icon mb-2" style="font-size: 2rem;"></i>
                    <h6 class="mb-1 text-truncate" title="${file.name}">${file.name}</h6>
                </div>
            </div>
        `;

        previewContainer.appendChild(col);
    });
}