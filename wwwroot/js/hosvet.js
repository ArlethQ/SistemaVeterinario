window.hosvetDownloadFile = (fileName, contentType, base64Data) => {
    const link = document.createElement("a");
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    link.remove();
};

window.descargarArchivoBase64 = window.hosvetDownloadFile;

window.hosvetPrintElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) {
        window.print();
        return;
    }

    const printWindow = window.open("", "_blank", "width=1100,height=800");
    if (!printWindow) {
        return;
    }

    const documentContent = [
        "<!DOCTYPE html><html><head>",
        "<title>Impresión Kardex e-ForVets</title>",
        '<link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css">',
        "<style>",
        "body{font-family:Arial,sans-serif;font-size:11px;margin:15px}",
        "table{width:100%;border-collapse:collapse}",
        "th,td{border:1px solid #999;padding:3px 5px}",
        ".turno-noche{color:#dc2626!important;font-weight:bold}",
        ".turno-dia{color:#1d4ed8!important;font-weight:bold}",
        "</style></head><body>",
        element.innerHTML,
        "</body></html>"
    ].join("");

    printWindow.document.open();
    printWindow.document.write(documentContent);
    printWindow.document.close();
    printWindow.focus();
    window.setTimeout(() => {
        printWindow.print();
        printWindow.close();
    }, 350);
};
