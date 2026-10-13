// Stacking, focus, moving and resizing for PixataDialog and PixataWindow. Loaded once per circuit (or once per WASM app) by
// PixataLayerJs, so every dialog and window on the page shares the same stack.
//
// Each open dialog or window is a "layer". Its wrapper element is moved to the end of <body>, so that it isn't clipped or
// covered by the container it was declared in, and is given a z-index above the layers below it in the stack. The layer at
// the top of the stack is the active one: a window comes to the top when it is clicked or focused, but never above a modal
// layer that was opened after it. Modal layers keep the focus inside them, and every layer gives the focus back to whatever
// had it before it opened when it closes (unless the user has moved the focus somewhere else in the meantime)

const layers = [];
const layersById = new Map();
let nextLayerId = 1;
let listeningToDocument = false;

const focusableSelector = "a[href], area[href], button:not([disabled]), input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled]), iframe, audio[controls], video[controls], [contenteditable]:not([contenteditable=false]), [tabindex]:not([tabindex='-1'])";
const interactiveSelector = "button, a[href], input, select, textarea, [contenteditable]:not([contenteditable=false])";
const keyboardStep = 10;

// Opening and closing

export function openLayer(anchor, wrapper, element, dotNet, options) {
  if (!wrapper || !element) {
    return 0;
  }
  const active = document.activeElement;
  const layer = {
    id: nextLayerId++,
    anchor,
    wrapper,
    element,
    dotNet,
    options: options || {},
    returnFocus: active && active !== document.body ? active : null,
    cleanup: []
  };
  if (anchor && getComputedStyle(anchor).direction === "rtl") {
    wrapper.setAttribute("dir", "rtl");
  }
  if (wrapper.parentElement !== document.body) {
    document.body.appendChild(wrapper);
  }
  layers.push(layer);
  layersById.set(layer.id, layer);
  listen(layer, element, "pointerdown", () => activate(layer), true);
  listen(layer, element, "focusin", () => activate(layer));
  // Pressing on the overlay mustn't take the focus out of the layer
  listen(layer, wrapper, "mousedown", e => {
    if (e.target.classList && e.target.classList.contains("pixata-blazor-overlay")) {
      e.preventDefault();
    }
  });
  if (layer.options.kind === "window") {
    setUpWindow(layer);
  }
  restack();
  updateDocumentListeners();
  focusInitially(layer);
  return layer.id;
}

export function updateLayer(id, options) {
  const layer = layersById.get(id);
  if (!layer) {
    return;
  }
  layer.options = options || {};
  if (layer.options.kind === "window") {
    requestAnimationFrame(() => keepInView(layer, true));
  }
}

export function closeLayer(id) {
  const layer = layersById.get(id);
  if (!layer) {
    return;
  }
  layersById.delete(id);
  const index = layers.indexOf(layer);
  if (index >= 0) {
    layers.splice(index, 1);
  }
  layer.cleanup.forEach(c => c());
  layer.cleanup = [];
  // Blazor removes the wrapper when the layer is hidden. If it's still here, the component was removed from the page while
  // the layer was open, so Blazor took away the placeholder it was declared in, but not the wrapper we moved to <body>
  if (layer.wrapper.isConnected) {
    layer.wrapper.remove();
  }
  restack();
  updateDocumentListeners();
  restoreFocus(layer);
}

// Stacking

function baseZIndex() {
  const value = parseInt(getComputedStyle(document.documentElement).getPropertyValue("--pixata-blazor-overlay-z-index"), 10);
  return isNaN(value) ? 10000 : value;
}

function restack() {
  const base = baseZIndex();
  layers.forEach((layer, index) => {
    layer.wrapper.style.zIndex = String(base + 1 + index);
    if (index === layers.length - 1) {
      layer.wrapper.setAttribute("data-pixata-blazor-active", "");
    } else {
      layer.wrapper.removeAttribute("data-pixata-blazor-active");
    }
  });
}

function activate(layer) {
  const index = layers.indexOf(layer);
  if (index < 0 || index === layers.length - 1) {
    return;
  }
  let target = layers.length - 1;
  for (let i = index + 1; i < layers.length; i++) {
    if (layers[i].options.modal) {
      target = i - 1;
      break;
    }
  }
  if (target <= index) {
    return;
  }
  layers.splice(index, 1);
  layers.splice(target, 0, layer);
  restack();
}

function topModalIndex() {
  for (let i = layers.length - 1; i >= 0; i--) {
    if (layers[i].options.modal) {
      return i;
    }
  }
  return -1;
}

// The layers the focus has to stay in: the top modal one, and any opened above it
function trappingLayers() {
  const index = topModalIndex();
  return index < 0 ? [] : layers.slice(index);
}

// Focus

function focusables(container) {
  const items = [...container.querySelectorAll(focusableSelector)];
  if (container.matches(focusableSelector)) {
    items.unshift(container);
  }
  return items.filter(e => !e.closest("[inert]") && !e.closest("[hidden]") && e.getClientRects().length > 0);
}

function focusInitially(layer) {
  const { element, options } = layer;
  let target = options.focusSelector ? element.querySelector(options.focusSelector) : null;
  if (!target && options.focusFirst) {
    target = focusables(element).find(e => e !== element) || null;
  }
  (target || element).focus({ preventScroll: true });
  if (target && target.matches("input[type=text], input:not([type]), input[type=search]") && target.value) {
    target.select();
  }
}

function isBehindModal(target) {
  const trapping = trappingLayers();
  return trapping.length > 0 && !trapping.some(l => l.wrapper.contains(target));
}

function restoreFocus(layer) {
  const active = document.activeElement;
  const focusLost = !active || active === document.body || !active.isConnected || layer.wrapper.contains(active);
  if (!focusLost) {
    return;
  }
  const target = layer.returnFocus;
  if (target && target.isConnected && !isBehindModal(target)) {
    target.focus({ preventScroll: true });
    if (document.activeElement === target) {
      return;
    }
  }
  const top = layers[layers.length - 1];
  if (top) {
    top.element.focus({ preventScroll: true });
  }
}

function updateDocumentListeners() {
  if (layers.length && !listeningToDocument) {
    document.addEventListener("keydown", onDocumentKeyDown);
    listeningToDocument = true;
  } else if (!layers.length && listeningToDocument) {
    document.removeEventListener("keydown", onDocumentKeyDown);
    listeningToDocument = false;
  }
}

function onDocumentKeyDown(e) {
  if (e.defaultPrevented) {
    return;
  }
  if (e.key === "Tab") {
    trapTab(e);
  } else if (e.key === "Escape") {
    // Inside a layer, Esc is handled by the component itself. This covers the focus having been lost (eg the focused
    // button was removed) while a modal layer is open
    const active = document.activeElement;
    const top = layers[layers.length - 1];
    if (top && top.options.modal && (!active || active === document.body)) {
      top.dotNet.invokeMethodAsync("OnEscapeKey").catch(() => { });
    }
  }
}

function trapTab(e) {
  const trapping = trappingLayers();
  if (!trapping.length) {
    return;
  }
  const items = trapping.flatMap(l => focusables(l.element));
  if (!items.length) {
    e.preventDefault();
    trapping[trapping.length - 1].element.focus({ preventScroll: true });
    return;
  }
  const active = document.activeElement;
  const inside = trapping.some(l => l.wrapper.contains(active));
  const index = items.indexOf(active);
  const first = items[0];
  const last = items[items.length - 1];
  if (!inside) {
    e.preventDefault();
    (e.shiftKey ? last : first).focus();
  } else if (e.shiftKey && index <= 0) {
    e.preventDefault();
    last.focus();
  } else if (!e.shiftKey && index === items.length - 1) {
    e.preventDefault();
    first.focus();
  }
}

// Windows: moving, resizing and staying in view

function setUpWindow(layer) {
  const { element } = layer;
  layer.container = layer.options.containmentSelector ? document.querySelector(layer.options.containmentSelector) : null;
  listen(layer, element, "pointerdown", e => onWindowPointerDown(layer, e));
  listen(layer, element, "keydown", e => onWindowKeyDown(layer, e));
  const update = () => {
    if (layer.frame) {
      return;
    }
    layer.frame = requestAnimationFrame(() => {
      layer.frame = 0;
      placeWrapper(layer);
      keepInView(layer, true);
    });
  };
  listen(layer, window, "resize", update);
  if (layer.container) {
    listen(layer, window, "scroll", update, true);
  }
  if (window.ResizeObserver) {
    const observer = new ResizeObserver(update);
    observer.observe(element);
    if (layer.container) {
      observer.observe(layer.container);
    }
    layer.cleanup.push(() => observer.disconnect());
  }
  layer.cleanup.push(() => {
    if (layer.frame) {
      cancelAnimationFrame(layer.frame);
    }
    if (layer.endPointer) {
      layer.endPointer();
    }
  });
  placeWrapper(layer);
  keepInView(layer, true);
}

// A window with a containment element lives in a wrapper laid over that element, so its position, centring and maximised
// size are all relative to the element rather than the viewport
function placeWrapper(layer) {
  const { container, wrapper } = layer;
  if (!container || !container.isConnected) {
    return;
  }
  // The window goes inside the container's border, as an absolutely positioned child of it would
  const r = container.getBoundingClientRect();
  wrapper.style.right = "auto";
  wrapper.style.bottom = "auto";
  wrapper.style.left = `${r.left + container.clientLeft}px`;
  wrapper.style.top = `${r.top + container.clientTop}px`;
  wrapper.style.width = `${container.clientWidth}px`;
  wrapper.style.height = `${container.clientHeight}px`;
}

function isMaximised(element) {
  return element.classList.contains("pixata-blazor-window-maximised");
}

function isMinimised(element) {
  return element.classList.contains("pixata-blazor-window-minimised");
}

function box(layer) {
  const area = layer.wrapper.getBoundingClientRect();
  const r = layer.element.getBoundingClientRect();
  return { left: r.left - area.left, top: r.top - area.top, width: r.width, height: r.height, areaWidth: area.width, areaHeight: area.height };
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(value, max));
}

// Sets the window's position in pixels, which also takes it out of the centred layout
function place(element, left, top) {
  element.style.left = `${left}px`;
  element.style.top = `${top}px`;
  element.style.right = "auto";
  element.style.bottom = "auto";
  element.style.margin = "0";
}

function reportMove(layer, left, top) {
  layer.dotNet.invokeMethodAsync("OnWindowMoved", Math.round(left), Math.round(top)).catch(() => { });
}

function reportResize(layer, b) {
  layer.dotNet.invokeMethodAsync("OnWindowResized", Math.round(b.left), Math.round(b.top), Math.round(b.width), Math.round(b.height)).catch(() => { });
}

function keepInView(layer, report) {
  const { element } = layer;
  if (!element.isConnected || isMaximised(element) || layer.endPointer) {
    return;
  }
  const b = box(layer);
  const left = clamp(b.left, 0, Math.max(0, b.areaWidth - b.width));
  const top = clamp(b.top, 0, Math.max(0, b.areaHeight - b.height));
  if (Math.abs(left - b.left) < 1 && Math.abs(top - b.top) < 1) {
    return;
  }
  place(element, left, top);
  if (report) {
    reportMove(layer, left, top);
  }
}

function onWindowPointerDown(layer, e) {
  if (e.pointerType === "mouse" && e.button !== 0) {
    return;
  }
  const { element, options } = layer;
  if (isMaximised(element) || layer.endPointer) {
    return;
  }
  const handle = e.target.closest(".pixata-blazor-window-resize-handle");
  if (handle && options.resizable && !isMinimised(element)) {
    e.preventDefault();
    track(layer, e, handle, (start, dx, dy) => resizeBy(layer, start, handle.getAttribute("data-pixata-blazor-direction") || "se", dx, dy), b => reportResize(layer, b));
    return;
  }
  const titlebar = e.target.closest(".pixata-blazor-window-titlebar");
  if (titlebar && options.draggable && !e.target.closest(interactiveSelector)) {
    track(layer, e, titlebar, (start, dx, dy) => moveBy(layer, start, dx, dy), b => reportMove(layer, b.left, b.top));
  }
}

// Follows the pointer from a press on the titlebar or a resize handle until it's released. Pointer events cover the mouse,
// touch and pens alike, and the titlebar and handles have touch-action: none so that dragging them doesn't scroll the page
function track(layer, e, target, onMove, onEnd) {
  const start = box(layer);
  const startX = e.clientX;
  const startY = e.clientY;
  let current = null;
  if (target.setPointerCapture) {
    try {
      target.setPointerCapture(e.pointerId);
    } catch {
    }
  }
  const move = ev => {
    if (ev.pointerId !== e.pointerId) {
      return;
    }
    const dx = ev.clientX - startX;
    const dy = ev.clientY - startY;
    if (!current && Math.abs(dx) + Math.abs(dy) < 3) {
      return;
    }
    current = onMove(start, dx, dy);
  };
  const end = ev => {
    if (ev && ev.pointerId !== e.pointerId) {
      return;
    }
    target.removeEventListener("pointermove", move);
    target.removeEventListener("pointerup", end);
    target.removeEventListener("pointercancel", end);
    layer.endPointer = null;
    if (current && ev) {
      onEnd(current);
    }
  };
  target.addEventListener("pointermove", move);
  target.addEventListener("pointerup", end);
  target.addEventListener("pointercancel", end);
  layer.endPointer = () => end(null);
}

function moveBy(layer, start, dx, dy) {
  const left = clamp(start.left + dx, 0, Math.max(0, start.areaWidth - start.width));
  const top = clamp(start.top + dy, 0, Math.max(0, start.areaHeight - start.height));
  place(layer.element, left, top);
  return { left, top, width: start.width, height: start.height };
}

// Sets the size the pointer asks for, lets the browser apply the window's min and max sizes, then moves the window so that
// the opposite edge stays where it was
function resizeBy(layer, start, direction, dx, dy) {
  const { element } = layer;
  let width = start.width;
  let height = start.height;
  if (direction.includes("e")) {
    width = Math.min(start.width + dx, start.areaWidth - start.left);
  } else if (direction.includes("w")) {
    width = Math.min(start.width - dx, start.left + start.width);
  }
  if (direction.includes("s")) {
    height = Math.min(start.height + dy, start.areaHeight - start.top);
  } else if (direction.includes("n")) {
    height = Math.min(start.height - dy, start.top + start.height);
  }
  place(element, start.left, start.top);
  element.style.width = `${Math.max(width, 0)}px`;
  element.style.height = `${Math.max(height, 0)}px`;
  const r = element.getBoundingClientRect();
  const left = direction.includes("w") ? start.left + start.width - r.width : start.left;
  const top = direction.includes("n") ? start.top + start.height - r.height : start.top;
  place(element, left, top);
  return { left, top, width: r.width, height: r.height };
}

// When the window itself (rather than something in it) has the focus, the arrow keys move it, Ctrl and the arrow keys
// resize it, and Alt+Up and Alt+Down maximise, minimise and restore it
function onWindowKeyDown(layer, e) {
  const { element, options } = layer;
  const onWindow = e.target === element || (e.target.closest && e.target.closest(".pixata-blazor-window-titlebar") && !e.target.closest(interactiveSelector));
  if (!onWindow || e.metaKey) {
    return;
  }
  if (e.altKey && (e.key === "ArrowUp" || e.key === "ArrowDown")) {
    e.preventDefault();
    layer.dotNet.invokeMethodAsync("OnWindowShortcut", e.key === "ArrowUp" ? "maximise" : "minimise").catch(() => { });
    return;
  }
  const deltas = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
  const delta = deltas[e.key];
  if (!delta || e.altKey || isMaximised(element)) {
    return;
  }
  const start = box(layer);
  if (e.ctrlKey) {
    if (!options.resizable || isMinimised(element)) {
      return;
    }
    e.preventDefault();
    reportResize(layer, resizeBy(layer, start, delta[0] ? "e" : "s", delta[0] * keyboardStep, delta[1] * keyboardStep));
  } else if (!e.shiftKey && options.draggable) {
    e.preventDefault();
    const b = moveBy(layer, start, delta[0] * keyboardStep, delta[1] * keyboardStep);
    reportMove(layer, b.left, b.top);
  }
}

// Helpers

function listen(layer, target, type, handler, capture) {
  target.addEventListener(type, handler, capture);
  layer.cleanup.push(() => target.removeEventListener(type, handler, capture));
}
