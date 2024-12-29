
//function foldCPU() {
//    if (document.getElementById('cpuInput').checked) {
//        const node = document.createElement("p");
//        const textnode = document.createTextNode("frequency");
//        node.appendChild(textnode);
//        node.setAttribute("id", "freq");
//        document.getElementById("cpuLabel").appendChild(node);
//    } else {
//        const element = document.getElementById("freq");
//        element.remove();
//    }
//}
fz = 0;
function hideFirstZero() {
    if (fz != 0) { return; }
    var k = document.getElementById("firstZero");
    k.className = "hideFZ";
    fz = 1;
}

document.addEventListener('DOMContentLoaded', () => {
    const eventSource = new EventSource('/sse');

    eventSource.onmessage = function (event) {
        const data = JSON.parse(event.data);
        if (data.MemoryUsage != 0) {
            hideFirstZero();
        }
        document.getElementById('cpu-usage').textContent = `Использование: ${data.CpuUsage}%`;
        document.getElementById('ram-free').textContent = `Свободно: ${data.MemoryUsage} MB`;
        document.getElementById('ram-usage').textContent = `Использование: ${parseFloat(100 - ((data.MemoryUsage / 15800) * 100)).toFixed(2)}%`;
        document.getElementById('gpu-usage').textContent = `Использование: ${data.GpuUsage}%`;
    };

    eventSource.onerror = function (error) {
        console.error('SSE Error:', error);
        };
});

//document.getElementById('btnSwitch').addEventListener('click', () => {
//    if (document.documentElement.getAttribute('data-bs-theme') == 'dark') {
//        document.documentElement.setAttribute('data-bs-theme', 'light');
//    }
//    else {
//        document.documentElement.setAttribute('data-bs-theme', 'dark');
//    }
//});s