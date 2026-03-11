function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('portMappingsContainer');
    const addBtn = document.getElementById('addPortBtn');
    const saveBtn = document.getElementById('saveMappingsBtn');
    const bdpSwitch = document.getElementById("BdpSwitch");
    const loader = document.getElementById('loadingMappings');
    const mapErrorBlock = document.getElementById('mappingError');
    const domainErrorBlock = document.getElementById('domainError');
    const namedescErrorBlock = document.getElementById('namedescError');
    const MAX_ITEMS = 3;

    let loaderBdP = document.getElementById('loadingBdp');
    let loaderDesc = document.getElementById('loadingDesc');
    let loaderName = document.getElementById('loadingName');
    let loaderDomn = document.getElementById('loadingDomn');
    const serverId = window.location.pathname.split('/').pop();

    bdpSwitch.addEventListener("change", async (event) => {
        const oldIsChecked = !event.target.checked;
        loaderBdP.className = 'showFZ form-check-label loader-sm';
        try {
            const response = await fetch(`/server/settings/${serverId}/bedrockport`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(event.currentTarget.checked),
            });
            if (!response.ok) {
                throw new Error(`Ошибка сервера: ${response.status}`);
            }

        } catch (err) {
            event.target.checked = oldIsChecked;
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderBdP.className = 'hideFZ form-check-label loader-sm' });
        }
    });

    document.getElementById('descBtn').addEventListener('click', async () => {
        var input = document.getElementById('descInput');
        const descRegex = /^[a-zA-Z0-9 \\-]+$/;
        var v = input.value.toString();
        if (!v) {
            v = 'A Minecraft server';
        } else if (v.length > 255) {
            showError("Описание слишком длинное (" + v.length + " символов из 255)", "namedesc");
        }
        try {
            loaderDesc.className = 'showFZ loader ms-2 my-auto';
            const response = await fetch(`/Server/configure/${serverId}/set`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    preference: "motd",
                    value: v
                }),
            });
        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderDesc.className = 'hideFZ loader ms-2 my-auto' });
        }
    });

    document.getElementById('domnBtn').addEventListener('click', async () => {
        var input = document.getElementById('domnInput');
        var v = input.value.toString().trim();
        if (v.length > 15) {
            showError("домен слишком длинный", "domain");
            return;
        }
        const domainRegex = /^[a-zA-Z0-9-]+$/;
        if (!domainRegex.test(v)) {
            showError("Домен может содержать только латинские буквы, цифры и дефис", "domain");
            return;
        }
        try {
            loaderDomn.className = 'showFZ loader ms-2 my-auto'
            const response = await fetch(`/server/settings/${serverId}/domain`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(v),
            });
            if (!response.ok) {
                showError('Ошибка сервера при сохранении', 'domain')
            }
        } catch (err) {
            showError("Не удалось связаться с сервером", 'domain');
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderDomn.className = 'hideFZ loader ms-2 my-auto' });
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
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    preference: "name",
                    value: v
                }),
            });

        } catch (err) {
            console.error('Ошибка отправки команды:', err);
        } finally {
            sleep(100).then(() => { loaderName.className = 'hideFZ loader ms-2 my-auto' });
        }
    });

    function showError(msg, department) {
        switch (department) {
            case "mapping":
                mapErrorBlock.innerText = msg;
                mapErrorBlock.classList.remove('d-none');
                setTimeout(() => mapErrorBlock.classList.add('d-none'), 5000);
                break;
            case "domain":
                domainErrorBlock.innerText = msg;
                domainErrorBlock.classList.remove('d-none');
                setTimeout(() => domainErrorBlock.classList.add('d-none'), 5000);
                break;
            case "namedesc":
                namedescErrorBlock.innerText = msg;
                namedescErrorBlock.classList.remove('d-none');
                setTimeout(() => namedescErrorBlock.classList.add('d-none'), 5000);
                break;
        }
    }

    function updateUI() {
        const items = container.querySelectorAll('.port-mapping-item');
        const count = items.length;

        addBtn.classList.toggle('d-none', count >= MAX_ITEMS);

        items.forEach(item => {
            const delBtn = item.querySelector('.remove-port');
            delBtn.classList.toggle('d-none', count <= 1);
            const textInp = item.querySelector('.path-val');
            textInp.classList.toggle('rounded-end', count <= 1);
        });
    }

    updateUI();

    addBtn.addEventListener('click', () => {
        const html = `
            <div class="input-group port-mapping-item">
                <span class="input-group-text text-white border-secondary">Порт</span>
                <input type="number" class="form-control port-val" placeholder="80" />
                <span class="input-group-text text-white border-secondary">Путь</span>
                <input type="text" class="form-control path-val" placeholder="" />
                <button type="button" class="btn btn-danger remove-port">×</button>
            </div>`;
        container.insertAdjacentHTML('beforeend', html);
        updateUI();
    });

    container.addEventListener('click', (e) => {
        if (e.target.closest('.remove-port')) {
            e.target.closest('.port-mapping-item').remove();
            updateUI();
        }
    });

    saveBtn.addEventListener('click', async () => {
        const items = container.querySelectorAll('.port-mapping-item');
        const mappings = [];
        const ports = new Set();
        const paths = new Set();

        let hasError = false;
        const regex = /^[a-zA-Z0-9/-]+$/;

        items.forEach((el) => {
            const port = el.querySelector('.port-val').value.trim();
            let path = el.querySelector('.path-val').value.trim();

            if (path === "") path = "/";

            if (!port) {
                showError("Укажите порт для всех элементов", 'mapping');
                hasError = true;
                return;
            }

            if (ports.has(port)) {
                showError(`Порт ${port} указан дважды!`, 'mapping');
                hasError = true;
                return;
            }
            ports.add(port);

            if (!regex.test(path)) { 
                showError("Путь может содержать только латинские буквы, цифры и дефис", 'mapping');
                hasError = true;
                return;
            }

            if (paths.has(path)) {
                showError(`Путь "${path}" указан дважды! Пути должны быть уникальными.`, 'mapping');
                hasError = true;
                return;
            }
            paths.add(path);

            mappings.push({ Port: parseInt(port), Path: path });
        });

        if (hasError) return;

        loader.classList.remove('hideFZ');
        try {
            const response = await fetch('/server/settings/' + serverId + '/portmaps', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(mappings)
            });

            if (response.ok) {
                //alert("Данные успешно сохранены");
            } else {
                showError("Ошибка сервера при сохранении", 'mapping');
            }
        } catch (err) {
            showError("Не удалось связаться с сервером", 'mapping');
        } finally {
            sleep(100).then(() => loader.classList.add('hideFZ'));
        }
    });

    document.getElementById('backBtn').addEventListener('click', () => {
        window.location.href = `/Server/See/${serverId}`;
    });
});