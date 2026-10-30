// The presenter is an append-only debugging timeline.  Scroll only after the
// new component has rendered so both Plotly and BugViewer entries behave alike.
export function scrollToNewestViewer() {
    requestAnimationFrame(() => {
        window.scrollTo({
            top: document.documentElement.scrollHeight,
            behavior: "smooth"
        });
    });
}

let keyboardHandler;

export function registerKeyboardShortcuts(dotNetReference) {
    keyboardHandler = event => {
        const target = event.target;
        if (!event.defaultPrevented && event.ctrlKey && event.altKey && !event.shiftKey && !event.metaKey
            && !event.isComposing && !event.getModifierState?.('AltGraph') && event.key.toLowerCase() === 's') {
            const editor = target instanceof HTMLElement &&
                (target.isContentEditable || ["INPUT", "SELECT", "TEXTAREA"].includes(target.tagName));
            const save = document.querySelector('[data-presenter-save]');
            if (!save || save.disabled || document.querySelector('dialog[open]') ||
                (editor && !target.closest('.scene-viewer'))) return;
            event.preventDefault();
            if (!event.repeat) save.click();
            return;
        }
        if (event.ctrlKey || event.altKey || event.metaKey || event.isComposing)
            return;

        if (target instanceof HTMLElement &&
            (target.isContentEditable || ["INPUT", "SELECT", "TEXTAREA"].includes(target.tagName)))
            return;

        const policies = { c: "Clear", n: "Save", h: "Hold" };
        const policy = policies[event.key.toLowerCase()];
        if (policy === undefined)
            return;

        event.preventDefault();
        dotNetReference.invokeMethodAsync("ContinueWithPolicy", policy);
    };

    window.addEventListener("keydown", keyboardHandler);
}

export function unregisterKeyboardShortcuts() {
    if (keyboardHandler !== undefined) {
        window.removeEventListener("keydown", keyboardHandler);
        keyboardHandler = undefined;
    }
}
