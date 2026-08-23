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
