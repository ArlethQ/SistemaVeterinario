window.hosvetDownloadFile = (fileName, contentType, base64Data) => {
    const link = document.createElement("a");
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    link.remove();
};

window.descargarArchivoBase64 = window.hosvetDownloadFile;

window.hosvetPrintElement = (elementId, orientation = 'portrait') => {
    const originalElement = document.getElementById(elementId);
    if (!originalElement) {
        window.print();
        return;
    }

    // Clonar elemento profundamente para procesar inputs sin alterar la interfaz activa
    const cloned = originalElement.cloneNode(true);

    // Reemplazar inputs, selects y textareas por spans limpios con su valor actual
    const originalInputs = originalElement.querySelectorAll("input, select, textarea");
    const clonedInputs = cloned.querySelectorAll("input, select, textarea");

    for (let i = 0; i < originalInputs.length; i++) {
        const orig = originalInputs[i];
        const clone = clonedInputs[i];
        if (!clone) continue;

        let valText = "";
        const tag = orig.tagName.toLowerCase();

        if (tag === "select") {
            const sel = orig;
            if (sel.selectedOptions && sel.selectedOptions.length > 0) {
                valText = sel.selectedOptions[0].text;
            } else if (sel.selectedIndex >= 0 && sel.options[sel.selectedIndex]) {
                valText = sel.options[sel.selectedIndex].text;
            } else {
                valText = sel.value || "";
            }
        } else if (orig.type === "checkbox" || orig.type === "radio") {
            valText = orig.checked ? "✔" : "";
        } else if (orig.type === "date") {
            const rawDate = orig.value || orig.getAttribute("value") || "";
            if (rawDate && /^\d{4}-\d{2}-\d{2}/.test(rawDate)) {
                const parts = rawDate.split("T")[0].split("-");
                if (parts.length === 3) {
                    valText = `${parts[2]}/${parts[1]}/${parts[0]}`;
                } else {
                    valText = rawDate;
                }
            } else {
                valText = rawDate;
            }
        } else {
            valText = orig.value || orig.getAttribute("value") || "";
        }

        const span = document.createElement("span");
        span.className = "hosvet-print-text";
        if (valText && valText.trim().length > 0) {
            span.textContent = valText.trim();
        } else {
            span.innerHTML = "&nbsp;";
        }

        // Preservar formato clínico (rojo para pulso/turno noche, azul para temperatura/turno día)
        if (orig.classList.contains("turno-noche") || orig.classList.contains("text-danger") || (orig.style.color && orig.style.color.includes("red"))) {
            span.style.color = "#dc2626";
            span.style.fontWeight = "bold";
        } else if (orig.classList.contains("turno-dia") || orig.classList.contains("text-primary") || (orig.style.color && orig.style.color.includes("blue"))) {
            span.style.color = "#1d4ed8";
            span.style.fontWeight = "bold";
        }

        if (orig.classList.contains("fw-bold")) {
            span.style.fontWeight = "bold";
        }

        if (clone.parentNode) {
            clone.parentNode.replaceChild(span, clone);
        }
    }

    // Quitar botones, enlaces y elementos con la clase no-print
    cloned.querySelectorAll(".no-print, button, a.btn").forEach(el => el.remove());

    // Asegurar estructura de columnas proporcional en tablas clinicas (A4)
    const tables = cloned.querySelectorAll("table");
    tables.forEach(table => {
        if (table.querySelector("colgroup")) return;

        const rows = table.querySelectorAll("tr");
        let maxCols = 0;
        rows.forEach(r => {
            let colCount = 0;
            r.querySelectorAll("th, td").forEach(c => {
                colCount += parseInt(c.getAttribute("colspan") || "1", 10);
            });
            if (colCount > maxCols) maxCols = colCount;
        });

        if (maxCols === 19) {
            // Tabla 1: Concepto (17%) + 18 turnos (4.61% cada uno)
            const colgroup = document.createElement("colgroup");
            const col0 = document.createElement("col");
            col0.style.width = "17%";
            colgroup.appendChild(col0);
            for (let c = 0; c < 18; c++) {
                const col = document.createElement("col");
                col.style.width = "4.61%";
                colgroup.appendChild(col);
            }
            table.insertBefore(colgroup, table.firstChild);
        } else if (maxCols === 10) {
            // Tablas 2 y 3: Concepto (17%) + 9 dias (9.22% cada uno, perfectamente alineados a 2 turnos)
            const colgroup = document.createElement("colgroup");
            const col0 = document.createElement("col");
            col0.style.width = "17%";
            colgroup.appendChild(col0);
            for (let c = 0; c < 9; c++) {
                const col = document.createElement("col");
                col.style.width = "9.22%";
                colgroup.appendChild(col);
            }
            table.insertBefore(colgroup, table.firstChild);
        }
    });

    const printWindow = window.open("", "_blank", "width=1000,height=900");
    if (!printWindow) {
        window.print();
        return;
    }

    const printStyles = `
        @page {
            size: A4 portrait;
            margin: 4mm 4mm 4mm 4mm;
        }
        * {
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
            color-adjust: exact !important;
            box-sizing: border-box !important;
        }
        html, body {
            margin: 0 !important;
            padding: 0 !important;
            background: #ffffff !important;
            color: #0f172a !important;
            font-family: Arial, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif !important;
            font-size: 6.8px !important;
            line-height: 1.15 !important;
        }
        .no-print, button, .btn {
            display: none !important;
        }
        .table-responsive {
            overflow: visible !important;
            display: block !important;
            width: 100% !important;
            margin: 0 !important;
            padding: 0 !important;
        }
        table {
            width: 100% !important;
            max-width: 100% !important;
            table-layout: fixed !important;
            border-collapse: collapse !important;
            font-size: 6.5px !important;
            margin-bottom: 2px !important;
        }
        th, td {
            border: 0.5px solid #94a3b8 !important;
            padding: 1px 0.5px !important;
            text-align: center !important;
            vertical-align: middle !important;
            word-wrap: break-word !important;
            overflow: hidden !important;
            min-width: 0 !important;
            height: 12.5px !important;
        }
        /* Primera columna con el nombre de la variable o concepto clinico */
        table th:first-child, table td:first-child {
            width: 17% !important;
            min-width: 17% !important;
            max-width: 17% !important;
            text-align: left !important;
            padding-left: 3px !important;
            font-weight: 600 !important;
            font-size: 6.5px !important;
            white-space: normal !important;
            word-break: break-word !important;
        }
        thead th {
            background-color: #f1f5f9 !important;
            font-size: 6.5px !important;
            font-weight: bold !important;
            color: #0f172a !important;
        }
        .table-primary { background-color: #e0f2fe !important; }
        .table-light { background-color: #f8fafc !important; }
        .table-warning { background-color: #fef3c7 !important; }
        .table-success { background-color: #ecfdf5 !important; }
        .bg-light { background-color: #f8fafc !important; }
        .bg-primary { background-color: #1d4ed8 !important; color: #ffffff !important; }
        .bg-danger { background-color: #dc2626 !important; color: #ffffff !important; }
        .bg-warning { background-color: #f59e0b !important; color: #111827 !important; }
        .bg-secondary { background-color: #64748b !important; color: #ffffff !important; }
        .badge {
            display: inline-block !important;
            padding: 1px 3px !important;
            font-size: 6.8px !important;
            border-radius: 2px !important;
        }
        .card {
            border: 0.5px solid #cbd5e1 !important;
            box-shadow: none !important;
            margin-bottom: 2.5px !important;
            page-break-inside: avoid !important;
            break-inside: avoid !important;
            border-radius: 3px !important;
        }
        .card-header {
            padding: 2px 4px !important;
            font-size: 7.2px !important;
        }
        .card-body {
            padding: 2px !important;
        }
        .card-body .row {
            display: flex !important;
            flex-direction: row !important;
            flex-wrap: nowrap !important;
            justify-content: space-between !important;
            margin: 0 !important;
        }
        .card-body .row > div {
            flex: 1 1 auto !important;
            padding: 1px 2px !important;
            font-size: 6.8px !important;
            text-align: center !important;
        }
        svg {
            width: 100% !important;
            max-height: 90px !important;
            display: block !important;
            margin: 0 auto !important;
        }
        .hosvet-print-text {
            display: block !important;
            width: 100% !important;
            text-align: center !important;
            font-size: 6.5px !important;
            line-height: 1.1 !important;
            min-height: 8px !important;
            white-space: nowrap !important;
            overflow: hidden !important;
            text-overflow: ellipsis !important;
        }
        .turno-noche, .text-danger {
            color: #dc2626 !important;
            font-weight: bold !important;
        }
        .turno-dia, .text-primary {
            color: #1d4ed8 !important;
            font-weight: bold !important;
        }
        .fw-bold { font-weight: bold !important; }
        .fw-semibold { font-weight: 600 !important; }
    `;

    const htmlDoc = [
        "<!DOCTYPE html>",
        "<html>",
        "<head>",
        "<meta charset='utf-8'>",
        "<title>Impresión Kardex A4 - e-ForVets</title>",
        '<link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css">',
        "<style>",
        printStyles,
        "</style>",
        "</head>",
        "<body>",
        cloned.innerHTML,
        "</body>",
        "</html>"
    ].join("");

    printWindow.document.open();
    printWindow.document.write(htmlDoc);
    printWindow.document.close();

    printWindow.focus();
    window.setTimeout(() => {
        try {
            printWindow.print();
        } catch (e) {
            console.error("Error al imprimir:", e);
        }
    }, 450);
};

// Restriccion para entradas numericas (Cedula y RUC)
document.addEventListener('keydown', function (e) {
    const target = e.target;
    if (target && target.getAttribute('data-numeric') === 'true') {
        const allowed = ['Backspace', 'Tab', 'Enter', 'Escape', 'Delete', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];
        if (allowed.includes(e.key) || e.ctrlKey || e.metaKey || e.altKey) {
            return;
        }
        if (!/^[0-9]$/.test(e.key)) {
            e.preventDefault();
        }
    }
});

document.addEventListener('paste', function (e) {
    const target = e.target;
    if (target && target.getAttribute('data-numeric') === 'true') {
        const pasteData = (e.clipboardData || window.clipboardData).getData('text');
        if (pasteData && /\D/.test(pasteData)) {
            e.preventDefault();
            const cleaned = pasteData.replace(/\D/g, '');
            const max = parseInt(target.getAttribute('maxlength') || '99', 10);
            const truncated = cleaned.substring(0, max);
            const start = target.selectionStart || 0;
            const end = target.selectionEnd || 0;
            const current = target.value || '';
            target.value = (current.substring(0, start) + truncated + current.substring(end)).substring(0, max);
            target.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }
});

// Prevenir n?meros negativos (-) en inputs num?ricos con min >= 0
document.addEventListener('keydown', function (e) {
    const target = e.target;
    if (target && target.tagName === 'INPUT' && target.type === 'number') {
        const minAttr = target.getAttribute('min');
        if (minAttr !== null && parseFloat(minAttr) >= 0) {
            if (e.key === '-' || e.key === 'Subtract') {
                e.preventDefault();
            }
        }
    }
});

document.addEventListener('paste', function (e) {
    const target = e.target;
    if (target && target.tagName === 'INPUT' && target.type === 'number') {
        const minAttr = target.getAttribute('min');
        if (minAttr !== null && parseFloat(minAttr) >= 0) {
            const pasteData = (e.clipboardData || window.clipboardData).getData('text');
            if (pasteData && pasteData.includes('-')) {
                e.preventDefault();
                const cleaned = pasteData.replace(/-/g, '');
                target.value = cleaned;
                target.dispatchEvent(new Event('input', { bubbles: true }));
                target.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }
    }
});
