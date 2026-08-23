function color(c) { return `rgba(${c.r},${c.g},${c.b},${c.a / 255})`; }
function marker(m) { return ["circle", "circle", "square", "diamond", "triangle-up", "x", "cross", "star"][m] ?? "circle"; }
export function render(element, plot, title) {
    const traces = (plot.traces ?? []).map(t => {
        const hasMarkers = t.marker !== 0;
        const mode = t.type === 1 ? (hasMarkers ? "markers" : "lines") : (hasMarkers ? "lines+markers" : "lines");
        const trace = { x: t.x, y: t.y, name: t.name, mode, type: t.type === 2 ? "bar" : "scatter", marker: { symbol: marker(t.marker), color: color(t.color) }, line: { color: color(t.color) } };
        if (t.closed && t.x.length) { trace.x = [...t.x, t.x[0]]; trace.y = [...t.y, t.y[0]]; }
        if (t.type === 4) trace.fill = "tozeroy";
        return trace;
    });
    if (plot.heatmap) traces.push({ z: plot.heatmap, type: "heatmap", colorscale: "Jet" });
    // A geometrically meaningful plot needs one unit on X to occupy the same
    // screen distance as one unit on Y. Plotly keeps that relationship while
    // panning and wheel/box zooming when Y is anchored to X.
    const layout = {
        title,
        margin: { t: 42, r: 24, b: 46, l: 55 },
        autosize: true,
        xaxis: { constrain: "domain" },
        yaxis: { scaleanchor: "x", scaleratio: 1, constrain: "domain" }
    };

    return Plotly.react(element, traces, layout, {
        responsive: true,
        scrollZoom: true,
        displaylogo: false
    });
}
export function dispose() { }
