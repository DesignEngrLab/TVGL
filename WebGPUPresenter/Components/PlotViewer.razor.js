function color(c) { return `rgba(${c.r},${c.g},${c.b},${c.a / 255})`; }
function marker(m) { return ["circle", "circle", "square", "diamond", "triangle-up", "x", "cross", "star"][m] ?? "circle"; }
function expandXAxisToFill(element) {
    const layout = element._fullLayout;
    if (!layout) return;

    const xRange = layout.xaxis.range;
    const yRange = layout.yaxis.range;
    const xSpan = xRange[1] - xRange[0];
    const ySpan = yRange[1] - yRange[0];
    const plotWidth = element.clientWidth - layout.margin.l - layout.margin.r;
    const plotHeight = element.clientHeight - layout.margin.t - layout.margin.b;
    if (!(xSpan > 0 && ySpan > 0 && plotWidth > 0 && plotHeight > 0)) return;

    // Keep a unit of X equal to a unit of Y, but expose enough horizontal
    // range to use the available plot width. This is at least a square range.
    const requiredXSpan = Math.max(xSpan, ySpan, ySpan * plotWidth / plotHeight);
    if (requiredXSpan <= xSpan * (1 + 1e-12)) return;

    const xCenter = (xRange[0] + xRange[1]) / 2;
    return Plotly.relayout(element, { "xaxis.range": [xCenter - requiredXSpan / 2, xCenter + requiredXSpan / 2] });
}
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
    }).then(() => expandXAxisToFill(element));
}
export function dispose() { }
