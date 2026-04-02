document.getElementById('serverCore').addEventListener('change', async function () {
    const type = this.value;
    const versionSelect = document.getElementById('serverVersion');
    const versionGroup = document.getElementById('versionGroup');
    const curseforgeGroup = document.getElementById('curseforgeGroup');

    if (type === 'CURSEFORGE') {
        versionGroup.classList.add('d-none');
        curseforgeGroup.classList.remove('d-none');
        versionSelect.innerHTML = '';
        return;
    }

    versionGroup.classList.remove('d-none');
    curseforgeGroup.classList.add('d-none');

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