// Browser helpers for PixataChart. The chart is drawn as SVG by Blazor; this module only does what needs a browser: it
// reports the size of the chart so the drawing can fit it (and follows it when it changes), measures text so the axes leave
// the right amount of room for their labels, and stops the arrow keys scrolling the page while they move round the chart

const charts = new Map();
let nextId = 1;

const navigationKeys = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", " "];

export function attach(chart, plot, dotNet) {
  if (!chart || !plot) {
    return null;
  }
  const id = nextId++;
  const state = { chart, plot, dotNet, chartWidth: 0, width: 0, height: 0, frame: 0 };
  const measure = () => {
    const c = chart.getBoundingClientRect();
    const p = plot.getBoundingClientRect();
    return { chartWidth: Math.round(c.width), width: Math.round(p.width), height: Math.round(p.height) };
  };
  const report = () => {
    state.frame = 0;
    if (!chart.isConnected) {
      return;
    }
    const size = measure();
    if (size.chartWidth !== state.chartWidth || size.width !== state.width || size.height !== state.height) {
      Object.assign(state, size);
      dotNet.invokeMethodAsync("OnPlotResized", size.chartWidth, size.width, size.height).catch(() => { });
    }
  };
  state.schedule = () => {
    if (!state.frame) {
      state.frame = requestAnimationFrame(report);
    }
  };
  if (window.ResizeObserver) {
    state.observer = new ResizeObserver(state.schedule);
    state.observer.observe(chart);
    state.observer.observe(plot);
  } else {
    window.addEventListener("resize", state.schedule);
  }
  state.onKeyDown = e => {
    if (navigationKeys.includes(e.key) && e.target && e.target.classList && e.target.classList.contains("pixata-blazor-chart-surface")) {
      e.preventDefault();
    }
  };
  chart.addEventListener("keydown", state.onKeyDown);
  if (document.fonts && document.fonts.ready) {
    // Text measured before a web font arrived is the wrong width, so measure it again once the fonts are in
    document.fonts.ready.then(() => {
      if (charts.has(id)) {
        dotNet.invokeMethodAsync("OnFontsLoaded").catch(() => { });
      }
    });
  }
  charts.set(id, state);
  Object.assign(state, measure());
  return { id, chartWidth: state.chartWidth, width: state.width, height: state.height };
}

export function detach(id) {
  const state = charts.get(id);
  if (!state) {
    return;
  }
  if (state.observer) {
    state.observer.disconnect();
  } else {
    window.removeEventListener("resize", state.schedule);
  }
  if (state.frame) {
    cancelAnimationFrame(state.frame);
  }
  state.chart.removeEventListener("keydown", state.onKeyDown);
  charts.delete(id);
}

// Measures the width of each piece of text, styled as it would be in the chart (by its class, and its own font if it has
// one), returning the font size and widths for each request
export function measureText(svg, requests) {
  if (!svg || !svg.isConnected) {
    return null;
  }
  const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
  text.setAttribute("x", "-10000");
  text.setAttribute("y", "-10000");
  text.setAttribute("aria-hidden", "true");
  text.style.visibility = "hidden";
  svg.appendChild(text);
  const results = [];
  try {
    for (const request of requests) {
      text.setAttribute("class", request.cssClass);
      text.style.font = request.font || "";
      const fontSize = parseFloat(getComputedStyle(text).fontSize) || 12;
      const widths = request.texts.map(t => {
        text.textContent = t;
        return text.getComputedTextLength();
      });
      results.push({ style: request.style, fontSize, widths });
    }
  } finally {
    text.remove();
  }
  return results;
}
