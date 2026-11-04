(function () {
    "use strict";

    const ui = window.numericaUi = window.numericaUi || {};

    // Las vistas de Facturacion/E-Contax usan SweetAlert tanto directamente
    // como por medio de SweetAlertService. El host veterinario debe exponer la
    // misma API compartida que el host original de Numerica.
    if (!window.Swal || typeof window.Swal.fire !== "function") {
        window.Swal = {
            fire: async function (options, positionalText, positionalIcon) {
                const settings = typeof options === "object" && options !== null
                    ? options
                    : { title: options, text: positionalText, icon: positionalIcon };
                const message = [settings.title, settings.text || settings.html]
                    .filter(Boolean)
                    .join("\n\n");
                if (settings.showCancelButton) {
                    return { isConfirmed: window.confirm(message || "Confirmar") };
                }
                window.alert(message || "Mensaje");
                return { isConfirmed: true };
            },
            showLoading: function () { },
            close: function () { }
        };
    }

    ui.mergeClassNames = function () {
        return Array.from(arguments)
            .flatMap(function (value) { return (value || "").toString().split(" "); })
            .map(function (value) { return value.trim(); })
            .filter(Boolean)
            .filter(function (value, index, list) { return list.indexOf(value) === index; })
            .join(" ");
    };

    ui.getSwalThemeOptions = function (options) {
        const settings = options && typeof options === "object" ? { ...options } : {};
        const icon = (settings.icon || "").toString().toLowerCase();
        const confirmLabel = (settings.confirmButtonText || "").toString().toLowerCase();
        const isDanger = icon === "warning" || icon === "error" ||
            confirmLabel.includes("eliminar") || confirmLabel.includes("anular");
        const classes = settings.customClass || {};

        settings.buttonsStyling = false;
        settings.customClass = {
            ...classes,
            popup: ui.mergeClassNames("numerica-swal-popup", classes.popup),
            title: ui.mergeClassNames("numerica-swal-title", classes.title),
            htmlContainer: ui.mergeClassNames("numerica-swal-body", classes.htmlContainer),
            actions: ui.mergeClassNames("numerica-swal-actions", classes.actions),
            confirmButton: ui.mergeClassNames(
                "numerica-swal-confirm",
                isDanger ? "numerica-swal-confirm-danger" : "numerica-swal-confirm-primary",
                classes.confirmButton),
            cancelButton: ui.mergeClassNames("numerica-swal-cancel", classes.cancelButton)
        };
        return settings;
    };

    ui.patchSweetAlert = function () {
        if (!window.Swal || typeof window.Swal.fire !== "function" || ui.__swalPatched) return;
        const originalFire = window.Swal.fire.bind(window.Swal);
        ui.__originalSwalFire = originalFire;
        window.Swal.fire = function () {
            if (arguments.length === 1 && arguments[0] && typeof arguments[0] === "object") {
                return originalFire(ui.getSwalThemeOptions(arguments[0]));
            }
            if (arguments.length > 0) {
                return originalFire(ui.getSwalThemeOptions({
                    title: arguments[0],
                    html: arguments.length > 1 ? arguments[1] : undefined,
                    icon: arguments.length > 2 ? arguments[2] : undefined
                }));
            }
            return originalFire();
        };
        ui.__swalPatched = true;
    };

    ui.showAlert = async function (options) {
        const settings = typeof options === "string" ? { text: options } : (options || {});
        const icon = settings.icon || "info";
        const title = settings.title || (
            icon === "success" ? "Proceso completado" :
                icon === "error" ? "Revisa la información" :
                    icon === "warning" ? "Atención" : "Información");

        ui.patchSweetAlert();
        return await window.Swal.fire({
            title: title,
            text: settings.html ? undefined : (settings.text || ""),
            html: settings.html || undefined,
            icon: icon,
            confirmButtonText: settings.confirmButtonText || "Entendido",
            allowOutsideClick: settings.allowOutsideClick !== false,
            allowEscapeKey: settings.allowEscapeKey !== false
        });
    };

    ui.patchSweetAlert();

    ui.toggleBodyScrollLock = function (locked) {
        document.body.classList.toggle("numerica-scroll-locked", !!locked);
        document.body.style.overflow = locked ? "hidden" : "";
    };

    ui.resetBodyScrollLock = function () {
        ui.toggleBodyScrollLock(false);
    };

    ui.downloadTextFile = function (fileName, content, mimeType) {
        const blob = new Blob([content || ""], { type: mimeType || "text/plain;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName || "archivo.txt";
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    };

    ui.openAsistenteFacturacion = function () {
        window.dispatchEvent(new CustomEvent("numerica:open-assistant"));
    };

    function visibleTarget(selector) {
        if (!selector) return null;
        let matches;
        try {
            matches = Array.from(document.querySelectorAll(selector));
        } catch (_) {
            return null;
        }

        return matches.find(function (element) {
            const rect = element.getBoundingClientRect();
            const style = window.getComputedStyle(element);
            return rect.width > 0 && rect.height > 0 &&
                style.display !== "none" && style.visibility !== "hidden";
        }) || null;
    }

    function metrics(element, padding, shape) {
        if (!element) return null;
        const rect = element.getBoundingClientRect();
        const pad = Number.isFinite(padding) ? padding : 16;
        const left = Math.max(8, rect.left - pad);
        const top = Math.max(8, rect.top - pad);
        const right = Math.min(window.innerWidth - 8, rect.right + pad);
        const bottom = Math.min(window.innerHeight - 8, rect.bottom + pad);
        return {
            top: top,
            left: left,
            width: Math.max(0, right - left),
            height: Math.max(0, bottom - top),
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            shape: shape || "rounded"
        };
    }

    ui.getTutorialTargetMetrics = function (selector, padding, shape) {
        return metrics(visibleTarget(selector), padding, shape);
    };

    ui.measureTutorialTarget = async function (selector, padding, shape) {
        const target = visibleTarget(selector);
        if (!target) return null;
        target.scrollIntoView({ behavior: "auto", block: "center", inline: "nearest" });
        await new Promise(function (resolve) { requestAnimationFrame(resolve); });
        await new Promise(function (resolve) { requestAnimationFrame(resolve); });
        return metrics(visibleTarget(selector) || target, padding, shape);
    };

    ui.waitForTutorialTarget = async function (selector, timeoutMs) {
        const timeout = Number.isFinite(timeoutMs) && timeoutMs > 0 ? timeoutMs : 3000;
        const started = Date.now();
        while (Date.now() - started < timeout) {
            if (visibleTarget(selector)) return true;
            await new Promise(function (resolve) { setTimeout(resolve, 80); });
        }
        return !!visibleTarget(selector);
    };

    ui.scrollTutorialTargetIntoView = function (selector) {
        const target = visibleTarget(selector);
        if (!target) return false;
        target.scrollIntoView({ behavior: "auto", block: "center", inline: "nearest" });
        return true;
    };
})();
