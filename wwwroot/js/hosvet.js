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

    // Imprimir en un iframe oculto evita abrir una ventana about:blank con una
    // segunda copia visible del reporte detrás del diálogo de impresión.
    const printFrame = document.createElement("iframe");
    printFrame.setAttribute("aria-hidden", "true");
    printFrame.style.position = "fixed";
    printFrame.style.width = "0";
    printFrame.style.height = "0";
    printFrame.style.border = "0";
    printFrame.style.visibility = "hidden";
    document.body.appendChild(printFrame);

    const printWindow = printFrame.contentWindow;
    if (!printWindow) {
        printFrame.remove();
        window.print();
        return;
    }

    const printStyles = `
        @page {
            size: A4 ${orientation === 'landscape' ? 'landscape' : 'portrait'};
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

    window.setTimeout(() => {
        try {
            const removePrintFrame = () => printFrame.remove();
            printWindow.addEventListener("afterprint", removePrintFrame, { once: true });
            printWindow.focus();
            printWindow.print();
            window.setTimeout(removePrintFrame, 60000);
        } catch (e) {
            console.error("Error al imprimir:", e);
            printFrame.remove();
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

// ============================================================================
// Editor Visual de Documentos y Plantillas Clínicas (WYSIWYG) con Redimensionamiento
// ============================================================================
window.hosvetEditor = {
    activeImg: null,
    getContent: function(elementId) {
        const el = document.getElementById(elementId);
        return el ? el.innerHTML : "";
    },
    setContent: function(elementId, html) {
        const el = document.getElementById(elementId);
        if (el) {
            el.innerHTML = html || "";
            window.hosvetEditor.initImageResizer(elementId);
        }
    },
    format: function(command, value = null) {
        try {
            document.execCommand(command, false, value);
        } catch (e) {
            console.warn("Editor format error:", e);
        }
    },
    insertImage: function(elementId, src) {
        const editor = document.getElementById(elementId);
        if (!editor) return;

        editor.focus();
        const img = document.createElement("img");
        img.src = src;
        img.style.maxWidth = "100%";
        img.style.height = "auto";
        img.style.display = "block";
        img.style.margin = "12px auto";
        img.style.borderRadius = "4px";

        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0 && editor.contains(sel.anchorNode)) {
            const range = sel.getRangeAt(0);
            range.collapse(false);
            range.insertNode(img);

            const newRange = document.createRange();
            newRange.setStartAfter(img);
            newRange.collapse(true);
            sel.removeAllRanges();
            sel.addRange(newRange);
        } else {
            editor.appendChild(img);
        }

        window.hosvetEditor.initImageResizer(elementId);
        setTimeout(function() {
            window.hosvetEditor.selectImage(img);
        }, 80);
    },
    initImageResizer: function(elementId) {
        const editor = document.getElementById(elementId);
        if (!editor) return;

        // Ensure overlay elements exist once in document.body
        let resizerBox = document.getElementById("hosvet-img-resizer-overlay");
        if (!resizerBox) {
            resizerBox = document.createElement("div");
            resizerBox.id = "hosvet-img-resizer-overlay";
            resizerBox.style.cssText = "position:absolute; display:none; pointer-events:none; z-index:9999; border:2px dashed #0d6efd; box-sizing:border-box;";

            // Corner handles: nw, ne, sw, se
            const handles = ['nw', 'ne', 'sw', 'se'];
            handles.forEach(function(pos) {
                const h = document.createElement("div");
                h.className = "hosvet-resize-handle";
                h.dataset.pos = pos;
                let cursor = (pos === 'nw' || pos === 'se') ? 'nwse-resize' : 'nesw-resize';
                h.style.cssText = 'position:absolute; width:12px; height:12px; background:#0d6efd; border:2px solid #ffffff; border-radius:50%; box-shadow:0 0 4px rgba(0,0,0,0.4); pointer-events:auto; cursor:' + cursor + ';';
                if (pos.includes('n')) h.style.top = "-6px";
                if (pos.includes('s')) h.style.bottom = "-6px";
                if (pos.includes('w')) h.style.left = "-6px";
                if (pos.includes('e')) h.style.right = "-6px";
                resizerBox.appendChild(h);
            });

            // Floating Mini-Toolbar
            const toolbar = document.createElement("div");
            toolbar.id = "hosvet-img-toolbar";
            toolbar.style.cssText = "position:absolute; top:-40px; left:50%; transform:translateX(-50%); display:flex; gap:3px; background:#212529; padding:4px 8px; border-radius:6px; box-shadow:0 4px 14px rgba(0,0,0,0.35); pointer-events:auto; z-index:10000; white-space:nowrap; align-items:center;";
            toolbar.innerHTML = [
                '<span style="color:#adb5bd; font-size:10px; font-weight:bold; margin-right:4px;">TAMAÑO:</span>',
                '<button type="button" class="btn btn-outline-light btn-xs py-0 px-2" data-act="p25" title="Ajustar al 25% del ancho" style="font-size:11px;">25%</button>',
                '<button type="button" class="btn btn-outline-light btn-xs py-0 px-2" data-act="p50" title="Ajustar al 50% del ancho" style="font-size:11px;">50%</button>',
                '<button type="button" class="btn btn-outline-light btn-xs py-0 px-2" data-act="p75" title="Ajustar al 75% del ancho" style="font-size:11px;">75%</button>',
                '<button type="button" class="btn btn-outline-light btn-xs py-0 px-2" data-act="p100" title="Ajustar al 100% del ancho" style="font-size:11px;">100%</button>',
                '<span style="border-right:1px solid #495057; height:18px; margin:0 3px;"></span>',
                '<button type="button" class="btn btn-dark btn-xs py-0 px-2 border border-secondary" data-act="zoomOut" title="Reducir 15%" style="font-size:12px; font-weight:bold;">➖</button>',
                '<button type="button" class="btn btn-dark btn-xs py-0 px-2 border border-secondary" data-act="zoomIn" title="Aumentar 15%" style="font-size:12px; font-weight:bold;">➕</button>',
                '<span style="border-right:1px solid #495057; height:18px; margin:0 3px;"></span>',
                '<button type="button" class="btn btn-dark btn-xs py-0 px-2 border border-secondary" data-act="alignLeft" title="Alinear a la izquierda" style="font-size:11px;">⬅</button>',
                '<button type="button" class="btn btn-dark btn-xs py-0 px-2 border border-secondary" data-act="alignCenter" title="Centrar imagen" style="font-size:11px;">⏺</button>',
                '<button type="button" class="btn btn-dark btn-xs py-0 px-2 border border-secondary" data-act="alignRight" title="Alinear a la derecha" style="font-size:11px;">➡</button>',
                '<span style="border-right:1px solid #495057; height:18px; margin:0 3px;"></span>',
                '<button type="button" class="btn btn-danger btn-xs py-0 px-2" data-act="remove" title="Eliminar imagen" style="font-size:11px;">🗑</button>'
            ].join('');
            resizerBox.appendChild(toolbar);
            document.body.appendChild(resizerBox);

            // Handle toolbar clicks
            toolbar.addEventListener('click', function(e) {
                const btn = e.target.closest('button[data-act]');
                if (!btn || !window.hosvetEditor.activeImg) return;
                e.preventDefault();
                e.stopPropagation();

                const act = btn.dataset.act;
                const img = window.hosvetEditor.activeImg;
                const parentEd = img.closest('[contenteditable="true"]');
                img.style.maxHeight = 'none';

                if (act === 'p25') {
                    img.style.width = '25%';
                    img.style.height = 'auto';
                } else if (act === 'p50') {
                    img.style.width = '50%';
                    img.style.height = 'auto';
                } else if (act === 'p75') {
                    img.style.width = '75%';
                    img.style.height = 'auto';
                } else if (act === 'p100') {
                    img.style.width = '100%';
                    img.style.height = 'auto';
                } else if (act === 'zoomOut') {
                    const curW = img.offsetWidth;
                    const newW = Math.max(50, Math.round(curW * 0.85));
                    img.style.width = newW + 'px';
                    img.style.height = 'auto';
                } else if (act === 'zoomIn') {
                    const curW = img.offsetWidth;
                    const maxW = parentEd ? (parentEd.clientWidth - 20) : 1000;
                    const newW = Math.min(maxW, Math.round(curW * 1.15));
                    img.style.width = newW + 'px';
                    img.style.height = 'auto';
                } else if (act === 'alignLeft') {
                    img.style.display = 'block';
                    img.style.margin = '10px auto 10px 0';
                } else if (act === 'alignCenter') {
                    img.style.display = 'block';
                    img.style.margin = '10px auto';
                } else if (act === 'alignRight') {
                    img.style.display = 'block';
                    img.style.margin = '10px 0 10px auto';
                } else if (act === 'remove') {
                    img.remove();
                    window.hosvetEditor.hideResizer();
                    if (parentEd) parentEd.dispatchEvent(new Event('input', { bubbles: true }));
                    return;
                }

                window.hosvetEditor.updateResizerPosition();
                if (parentEd) parentEd.dispatchEvent(new Event('input', { bubbles: true }));
            });

            // Handle dragging corner handles
            let isDragging = false;
            let dragPos = '';
            let startX = 0;
            let startW = 0;

            resizerBox.querySelectorAll('.hosvet-resize-handle').forEach(function(h) {
                h.addEventListener('mousedown', function(e) {
                    if (!window.hosvetEditor.activeImg) return;
                    e.preventDefault();
                    e.stopPropagation();

                    isDragging = true;
                    dragPos = h.dataset.pos;
                    startX = e.clientX;
                    startW = window.hosvetEditor.activeImg.offsetWidth;

                    function onDragMove(ev) {
                        if (!isDragging || !window.hosvetEditor.activeImg) return;
                        ev.preventDefault();

                        let deltaX = ev.clientX - startX;
                        if (dragPos.includes('w')) {
                            deltaX = -deltaX;
                        }

                        const currentImg = window.hosvetEditor.activeImg;
                        const parentEd = currentImg.closest('[contenteditable="true"]');
                        const maxW = parentEd ? (parentEd.clientWidth - 20) : 1200;
                        let newW = Math.max(50, Math.min(maxW, startW + deltaX));

                        currentImg.style.width = newW + 'px';
                        currentImg.style.height = 'auto';
                        currentImg.style.maxHeight = 'none';

                        window.hosvetEditor.updateResizerPosition();
                    }

                    function onDragEnd() {
                        if (!isDragging) return;
                        isDragging = false;
                        document.removeEventListener('mousemove', onDragMove);
                        document.removeEventListener('mouseup', onDragEnd);
                        if (window.hosvetEditor.activeImg) {
                            window.hosvetEditor.updateResizerPosition();
                            const parentEd = window.hosvetEditor.activeImg.closest('[contenteditable="true"]');
                            if (parentEd) parentEd.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    }

                    document.addEventListener('mousemove', onDragMove);
                    document.addEventListener('mouseup', onDragEnd);
                });
            });

            // Global click outside to dismiss
            document.addEventListener('mousedown', function(e) {
                if (resizerBox.contains(e.target)) return;
                const ed = document.getElementById(elementId);
                if (ed && ed.contains(e.target)) {
                    if (e.target.tagName !== 'IMG') {
                        window.hosvetEditor.hideResizer();
                    }
                } else {
                    window.hosvetEditor.hideResizer();
                }
            });

            // Window and scroll adjustments
            window.addEventListener('resize', function() { window.hosvetEditor.updateResizerPosition(); });
            document.addEventListener('scroll', function() { window.hosvetEditor.updateResizerPosition(); }, true);
        }

        // Setup image click listeners on editor
        if (!editor.__hasResizerListener) {
            editor.__hasResizerListener = true;
            editor.addEventListener('click', function(e) {
                if (e.target && e.target.tagName === 'IMG') {
                    window.hosvetEditor.selectImage(e.target);
                    e.stopPropagation();
                } else {
                    window.hosvetEditor.hideResizer();
                }
            });

            editor.addEventListener('scroll', function() { window.hosvetEditor.updateResizerPosition(); });
            editor.addEventListener('input', function() {
                if (window.hosvetEditor.activeImg) window.hosvetEditor.updateResizerPosition();
            });
        }
    },
    selectImage: function(img) {
        this.activeImg = img;
        this.updateResizerPosition();
    },
    hideResizer: function() {
        const resizerBox = document.getElementById("hosvet-img-resizer-overlay");
        if (resizerBox) resizerBox.style.display = 'none';
        this.activeImg = null;
    },
    updateResizerPosition: function() {
        const resizerBox = document.getElementById("hosvet-img-resizer-overlay");
        if (!resizerBox) return;
        if (!this.activeImg || !this.activeImg.isConnected) {
            resizerBox.style.display = 'none';
            this.activeImg = null;
            return;
        }

        const rect = this.activeImg.getBoundingClientRect();
        resizerBox.style.display = 'block';
        resizerBox.style.top = (rect.top + window.scrollY) + 'px';
        resizerBox.style.left = (rect.left + window.scrollX) + 'px';
        resizerBox.style.width = rect.width + 'px';
        resizerBox.style.height = rect.height + 'px';
    },
    printDocument: function(title, html) {
        const printWindow = window.open('', '_blank', 'width=900,height=900');
        if (!printWindow) {
            alert('Por favor permita las ventanas emergentes (popups) en su navegador para poder imprimir el documento.');
            return;
        }

        printWindow.document.open();
        printWindow.document.write(`<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>${title || 'Documento Clínico'}</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
            font-size: 11pt;
            line-height: 1.5;
            color: #212529;
            margin: 20mm 25mm;
            padding: 0;
            background: #fff;
        }
        img {
            max-width: 100%;
            height: auto;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 12px 0;
        }
        table, th, td {
            border: 1px solid #adb5bd;
        }
        th, td {
            padding: 6px 10px;
        }
        @media print {
            @page {
                margin: 15mm 20mm;
                size: auto;
            }
            body {
                margin: 0;
            }
        }
    </style>
</head>
<body>
    ${html || ''}
    <script>
        window.onload = function() {
            setTimeout(function() {
                window.print();
            }, 300);
        };
    <\/script>
</body>
</html>`);
        printWindow.document.close();
    }
};

window.hosvetColaClinica = (() => {
    let audio;

    return {
        async activar() {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return false;

            audio ??= new AudioContext();
            if (audio.state !== 'running') await audio.resume();
            return audio.state === 'running';
        },

        sonar() {
            if (audio?.state !== 'running') return;

            const tono = audio.createOscillator();
            const volumen = audio.createGain();
            const ahora = audio.currentTime;

            tono.frequency.setValueAtTime(660, ahora);
            tono.frequency.setValueAtTime(880, ahora + 0.13);
            volumen.gain.setValueAtTime(0.14, ahora);
            volumen.gain.exponentialRampToValueAtTime(0.001, ahora + 0.35);
            tono.connect(volumen);
            volumen.connect(audio.destination);
            tono.start(ahora);
            tono.stop(ahora + 0.35);
        }
    };
})();
