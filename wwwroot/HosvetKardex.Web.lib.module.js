export function afterWebStarted(blazor) {
    blazor.registerCustomEventType('consultaridentificacion', {
        createEventArgs: event => ({ valor: event.detail })
    });

    document.addEventListener('keydown', event => {
        const campo = event.target;
        if (event.key !== 'Enter' || event.isComposing ||
            !(campo instanceof HTMLInputElement) || !campo.hasAttribute('data-consultar-identificacion')) {
            return;
        }

        event.preventDefault();
        if (!event.repeat) {
            campo.dispatchEvent(new CustomEvent('consultaridentificacion', {
                bubbles: true,
                detail: campo.value
            }));
        }
    });
}
