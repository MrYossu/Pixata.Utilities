// Shared browser helpers for the components in Pixata.Blazor/Components. Loaded once per circuit (or once per WASM app) by
// PixataJs.Module(), so every component shares the same instance

const popups = new Map();
let nextPopupId = 1;

const viewportMargin = 8;
const anchorGap = 2;

// Shows a popup (a drop-down list, a menu and so on) next to its anchor. The popup is position: fixed, so that it isn't
// clipped by a container with overflow set (a dialog's content, a scrolling panel). It follows the anchor when the page
// scrolls or resizes, opens upwards if there isn't room below, and is kept inside the viewport on small screens
export function attachPopup(anchor, popup, dotNet, options) {
  if (!anchor || !popup) {
    return 0;
  }
  const id = nextPopupId++;
  const state = { anchor, popup, dotNet, options: options || {} };
  state.reposition = () => positionPopup(state);
  state.onPointerDown = e => {
    if (!anchor.contains(e.target) && !popup.contains(e.target)) {
      dotNet.invokeMethodAsync("OnOutsideClick").catch(() => { });
    }
  };
  document.addEventListener("pointerdown", state.onPointerDown, true);
  window.addEventListener("scroll", state.reposition, true);
  window.addEventListener("resize", state.reposition);
  if (window.ResizeObserver) {
    state.observer = new ResizeObserver(state.reposition);
    state.observer.observe(popup);
    state.observer.observe(anchor);
  }
  popups.set(id, state);
  state.reposition();
  return id;
}

export function repositionPopup(id) {
  const state = popups.get(id);
  if (state) {
    state.reposition();
  }
}

export function detachPopup(id) {
  const state = popups.get(id);
  if (!state) {
    return;
  }
  document.removeEventListener("pointerdown", state.onPointerDown, true);
  window.removeEventListener("scroll", state.reposition, true);
  window.removeEventListener("resize", state.reposition);
  if (state.observer) {
    state.observer.disconnect();
  }
  popups.delete(id);
}

function positionPopup(state) {
  const { anchor, popup, options } = state;
  if (!anchor.isConnected || !popup.isConnected) {
    return;
  }
  const viewportWidth = document.documentElement.clientWidth;
  const viewportHeight = document.documentElement.clientHeight;
  const a = anchor.getBoundingClientRect();
  popup.style.position = "fixed";
  popup.style.maxWidth = `${viewportWidth - viewportMargin * 2}px`;
  if (options.matchAnchorWidth !== false) {
    popup.style.minWidth = `${Math.min(a.width, viewportWidth - viewportMargin * 2)}px`;
  }
  const p = popup.getBoundingClientRect();
  const rtl = getComputedStyle(anchor).direction === "rtl";
  const alignEnd = options.align === "end" ? !rtl : rtl;
  let left = alignEnd ? a.right - p.width : a.left;
  left = Math.max(viewportMargin, Math.min(left, viewportWidth - p.width - viewportMargin));
  const spaceBelow = viewportHeight - a.bottom - anchorGap - viewportMargin;
  const spaceAbove = a.top - anchorGap - viewportMargin;
  const openUp = p.height > spaceBelow && spaceAbove > spaceBelow;
  const top = openUp ? Math.max(viewportMargin, a.top - anchorGap - p.height) : a.bottom + anchorGap;
  popup.style.left = `${left}px`;
  popup.style.top = `${top}px`;
  popup.classList.toggle("pixata-blazor-popup-up", openUp);
}

// Focus

export function focusElement(element, preventScroll) {
  if (element && element.focus) {
    element.focus({ preventScroll: !!preventScroll });
  }
}

export function focusSelector(container, selector) {
  const element = (container || document).querySelector(selector);
  if (element && element.focus) {
    element.focus();
    return true;
  }
  return false;
}

export function focusFirst(container) {
  const element = focusableElements(container)[0];
  if (element) {
    element.focus();
    return true;
  }
  return false;
}

export function getActiveElementId() {
  return document.activeElement ? document.activeElement.id || "" : "";
}

export function containsFocus(container) {
  return !!container && container.contains(document.activeElement);
}

const focusableSelector = "a[href], area[href], button:not([disabled]), input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled]), iframe, [contenteditable=true], [tabindex]:not([tabindex='-1'])";

function focusableElements(container) {
  if (!container) {
    return [];
  }
  return [...container.querySelectorAll(focusableSelector)].filter(e => e.offsetParent !== null || e === document.activeElement);
}

// Keyboard

// Blazor can't decide whether to call preventDefault() based on which key was pressed, so this does it in the browser for
// the keys a component handles itself (eg the arrow keys in a list, so they don't also scroll the page)
const preventedKeys = new WeakMap();

export function preventDefaultKeys(element, keys) {
  if (!element) {
    return;
  }
  releaseDefaultKeys(element);
  const handler = e => {
    const key = (e.altKey ? "Alt+" : "") + e.key;
    if (keys.includes(key) || keys.includes(e.key)) {
      e.preventDefault();
    }
  };
  element.addEventListener("keydown", handler);
  preventedKeys.set(element, handler);
}

export function releaseDefaultKeys(element) {
  const handler = element ? preventedKeys.get(element) : null;
  if (handler) {
    element.removeEventListener("keydown", handler);
    preventedKeys.delete(element);
  }
}

// Scrolling and measuring

export function scrollIntoView(container, selector) {
  const element = container ? container.querySelector(selector) : null;
  if (element) {
    element.scrollIntoView({ block: "nearest", inline: "nearest" });
  }
}

export function getBoundingRect(element) {
  if (!element) {
    return null;
  }
  const r = element.getBoundingClientRect();
  return { left: r.left, top: r.top, width: r.width, height: r.height, right: r.right, bottom: r.bottom };
}

export function getViewportSize() {
  return { width: document.documentElement.clientWidth, height: document.documentElement.clientHeight };
}

export function matchesMedia(query) {
  return window.matchMedia(query).matches;
}

export function clickElement(element) {
  if (element) {
    element.click();
  }
}
