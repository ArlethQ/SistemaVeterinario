(() => {
    const selector = 'textarea, input:not([type]), input:is([type="text"], [type="search"], [type="email"], [type="tel"], [type="url"])';
    let editing;
    let expiration;

    const observer = new MutationObserver(() => {
        if (!editing || editing.field.isConnected) return;

        const { parent, field, value, start, end, direction, url } = editing;
        clear();
        if (location.href !== url || !parent.isConnected || document.activeElement !== document.body) return;

        // Only recover the same field in the same surviving container, with the same value.
        const candidates = Array.from(parent.children).filter(candidate =>
            eligible(candidate) && candidate.tagName === field.tagName && candidate.type === field.type &&
            candidate.id === field.id && candidate.name === field.name &&
            candidate.placeholder === field.placeholder && candidate.value === value);
        if (candidates.length !== 1) return;

        const replacement = candidates[0];
        if (!replacement.getClientRects().length || replacement.closest('[hidden], [inert]')) return;
        replacement.focus({ preventScroll: true });
        if (start !== null && end !== null && document.activeElement === replacement) {
            replacement.setSelectionRange(start, end, direction);
        }
    });

    function eligible(field) {
        return field instanceof HTMLElement && field.matches(selector) &&
            !field.matches(':disabled') && !field.readOnly;
    }

    function clear() {
        editing = null;
        clearTimeout(expiration);
        observer.disconnect();
    }

    document.addEventListener('input', event => {
        const field = event.target;
        if (!eligible(field) || document.activeElement !== field || !field.parentElement || event.isComposing) return;
        if (!field.id && !field.name && !field.placeholder) return;

        clear();
        editing = {
            field,
            parent: field.parentElement,
            value: field.value,
            start: field.selectionStart,
            end: field.selectionEnd,
            direction: field.selectionDirection,
            url: location.href
        };
        observer.observe(editing.parent, { childList: true });
        expiration = setTimeout(clear, 1500);
    }, true);

    document.addEventListener('pointerdown', clear, true);
    document.addEventListener('focusin', clear, true);
    document.addEventListener('focusout', event => {
        if (event.relatedTarget || event.target.isConnected) clear();
    }, true);
    document.addEventListener('keydown', event => {
        if (event.key === 'Tab' || event.key === 'Escape' || event.key === 'Enter') clear();
    }, true);
    window.addEventListener('pagehide', clear);
})();
