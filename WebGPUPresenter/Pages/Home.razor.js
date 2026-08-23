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
        if (event.ctrlKey || event.altKey || event.metaKey || event.isComposing)
            return;

        const target = event.target;
        if (target instanceof HTMLElement &&
            (target.isContentEditable || ["INPUT", "SELECT", "TEXTAREA"].includes(target.tagName)))
            return;

        const policies = { c: "Clear", s: "Save", h: "Hold" };
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
