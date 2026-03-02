//---------------------------
// INCLUDE descriptionPreview.css 
//---------------------------
const mcStyles = {
    '0': 'color: #000000', '1': 'color: #0000AA', '2': 'color: #00AA00', '3': 'color: #00AAAA',
    '4': 'color: #AA0000', '5': 'color: #AA00AA', '6': 'color: #FFAA00', '7': 'color: #AAAAAA',
    '8': 'color: #555555', '9': 'color: #5555FF', 'a': 'color: #55FF55', 'b': 'color: #55FFFF',
    'c': 'color: #FF5555', 'd': 'color: #FF55FF', 'e': 'color: #FFFF55', 'f': 'color: #FFFFFF'
};

function parseMinecraftCodes(text, shouldTrimEachLine = false) {
    if (!text) return "";

    let processedText = text.split(/\\n|\n/).join('\n');
    if (shouldTrimEachLine) {
        processedText = text.split(/\\n|\n/).map(line => line.trim()).join('\n');
    }

    let html = processedText.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    html = html.replace(/\\u00A7/g, '§');

    const parts = html.split('§');
    let finalHtml = parts[0];

    let state = {
        color: null,
        bold: false,
        italic: false,
        underline: false,
        strike: false,
        obfuscated: false
    };

    for (let i = 1; i < parts.length; i++) {
        const part = parts[i];
        if (part.length === 0) continue;

        const code = part.charAt(0).toLowerCase();
        const content = part.substring(1);

        if (mcStyles[code]) {
            state = { color: mcStyles[code], bold: false, italic: false, underline: false, strike: false, obfuscated: false };
        } else {
            switch (code) {
                case 'l': state.bold = true; break;      // Bold
                case 'm': state.strike = true; break;    // Strikethrough
                case 'n': state.underline = true; break; // Underline
                case 'o': state.italic = true; break;    // Italic
                case 'k': state.obfuscated = true; break;// Obfuscated
                case 'r':                                // Reset
                    state = { color: null, bold: false, italic: false, underline: false, strike: false, obfuscated: false };
                    break;
            }
        }

        if (content.length > 0) {
            let styles = [];
            let classes = [];

            if (state.color) styles.push(state.color);
            if (state.bold) styles.push("font-weight:bold");
            if (state.italic) styles.push("font-style:italic");

            if (state.underline) classes.push("mc-underline");
            if (state.strike) classes.push("mc-strikethrough");
            if (state.obfuscated) classes.push("mc-obfuscated");

            const styleAttr = styles.length > 0 ? `style="${styles.join(';')}"` : "";
            const classAttr = classes.length > 0 ? `class="${classes.join(' ')}"` : "";

            finalHtml += `<span ${styleAttr} ${classAttr}>${content}</span>`;
        }
    }

    return finalHtml.replace(/\n/g, '<br>').replace(/\\n/g, '<br>');
}
document.addEventListener('DOMContentLoaded', () => {
    const inputs = document.querySelectorAll('.mc-input');
    inputs.forEach(input => {
        const previewId = input.getAttribute('data-preview');
        const previewElem = document.getElementById(previewId);
        if (previewElem) {
            const update = () => {
                const val = (input.tagName === 'INPUT' || input.tagName === 'TEXTAREA') ? input.value : input.innerText;
                previewElem.innerHTML = parseMinecraftCodes(val, false);
            };
            input.addEventListener('input', update);
            update();
        }
    });

    const staticTexts = document.querySelectorAll('.mc-parse');
    staticTexts.forEach(elem => {
        const shouldTrim = elem.classList.contains('mc-trim');
        elem.innerHTML = parseMinecraftCodes(elem.innerText || elem.textContent, shouldTrim);
    });
});