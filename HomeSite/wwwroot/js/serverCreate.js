document.getElementById('serverCore').addEventListener('change', async function () {
    const type = this.value;
    const versionSelect = document.getElementById('serverVersion');
    if (type === '') {
        versionSelect.innerHTML = '<option></option>';
        return;
    }
    versionSelect.innerHTML = '<option>Загрузка...</option>';

    try {
        const response = await fetch(`/create/versions?type=${type}`);
        const versions = await response.json();

        versionSelect.innerHTML = '';
        versions.forEach(v => {
            const opt = document.createElement('option');
            opt.value = v;
            opt.textContent = v;
            versionSelect.appendChild(opt);
        });
    } catch (e) {
        versionSelect.innerHTML = '<option>Ошибка загрузки</option>';
    }
});