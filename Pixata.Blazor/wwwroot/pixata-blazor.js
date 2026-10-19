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

// ---------- The grid and the date pickers ----------

// What Blazor can't do for PixataGrid and the date pickers: controlling the selection inside an input, positioning popups,
// noticing clicks outside them, dragging, scrolling and keyboard navigation. The logic itself stays in C#

const dateInputs = new WeakMap();

// Keys the date input handles itself. Anything else (Tab, shortcuts such as Ctrl+C) is left to the browser
const dateInputKeys = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Backspace", "Delete", "Home", "End"]);

function applyDateInputState(input, state) {
  if (!input || !state) {
    return;
  }
  input.value = state.text;
  if (document.activeElement === input && state.start >= 0) {
    input.setSelectionRange(state.start, state.end);
  }
}

export function initDateInput(input, dotNet) {
  // The component can be removed while the module is being imported, in which case its input has already gone
  if (!input) {
    return;
  }
  const call = async (method, ...args) => applyDateInputState(input, await dotNet.invokeMethodAsync(method, ...args));
  const handlers = {
    keydown: e => {
      // Alt+arrows open and close a picker's popup, and Enter and Escape are handled in C#, so they're left alone here
      if (e.altKey || e.ctrlKey || e.metaKey || e.key === "Tab" || e.key === "Enter" || e.key === "Escape" || e.key === "Unidentified") {
        return;
      }
      if (e.key.length === 1 || dateInputKeys.has(e.key)) {
        e.preventDefault();
        if (!input.readOnly) {
          call("OnKey", e.key);
        }
      }
    },
    // Phone keyboards often don't say which key was pressed, so the typed text is taken from beforeinput instead
    beforeinput: e => {
      e.preventDefault();
      if (input.readOnly) {
        return;
      }
      if (e.inputType === "deleteContentBackward") {
        call("OnKey", "Backspace");
      } else if (e.inputType === "deleteContentForward") {
        call("OnKey", "Delete");
      } else if (e.data) {
        [...e.data].reduce((p, c) => p.then(() => call("OnKey", c)), Promise.resolve());
      }
    },
    // Runs after the browser has placed the caret, so it can select the segment that was clicked
    mouseup: () => setTimeout(() => call("OnCaret", input.selectionStart ?? 0), 0),
    focus: () => call("OnInputFocus"),
    blur: () => call("OnInputBlur"),
    paste: e => {
      e.preventDefault();
      if (!input.readOnly) {
        call("OnPaste", e.clipboardData?.getData("text") ?? "");
      }
    }
  };
  for (const [name, handler] of Object.entries(handlers)) {
    input.addEventListener(name, handler);
  }
  dateInputs.set(input, handlers);
}

export function setDateInputState(input, state) {
  applyDateInputState(input, state);
}

export function disposeDateInput(input) {
  const handlers = dateInputs.get(input);
  if (handlers) {
    for (const [name, handler] of Object.entries(handlers)) {
      input.removeEventListener(name, handler);
    }
    dateInputs.delete(input);
  }
}

// Stops the browser scrolling the page when the arrow and paging keys are used to move around inside an element, such as
// the calendar, while leaving Tab alone
const navigationKeys = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageUp", "PageDown", "Home", "End", "Enter", " "]);
const keyBlockers = new WeakMap();

export function preventNavigationKeys(element) {
  if (!element) {
    return;
  }
  const handler = e => {
    if (navigationKeys.has(e.key)) {
      e.preventDefault();
    }
  };
  element.addEventListener("keydown", handler);
  keyBlockers.set(element, handler);
}

export function allowNavigationKeys(element) {
  const handler = keyBlockers.get(element);
  if (handler) {
    element.removeEventListener("keydown", handler);
    keyBlockers.delete(element);
  }
}

const gridPopups = new WeakMap();

// Positions a popup below its anchor (or above it, if there's more room there), keeping it inside the viewport. It uses
// fixed positioning, so it isn't clipped by containers with overflow hidden, such as a grid cell
function positionGridPopup(anchor, popup) {
  const a = anchor.getBoundingClientRect();
  // A list is at least as wide as the control it drops down from
  popup.style.minWidth = `${a.width}px`;
  const p = popup.getBoundingClientRect();
  const below = window.innerHeight - a.bottom;
  const top = below >= p.height || below >= a.top ? a.bottom : a.top - p.height;
  // In a right-to-left page, the popup lines up with the anchor's right edge
  const start = getComputedStyle(anchor).direction === "rtl" ? a.right - p.width : a.left;
  const left = Math.max(0, Math.min(start, window.innerWidth - p.width));
  popup.style.position = "fixed";
  popup.style.top = `${Math.max(0, top)}px`;
  popup.style.left = `${left}px`;
}

export function openPopup(anchor, popup, dotNet) {
  // The anchor can be an element, or the id of one
  if (typeof anchor === "string") {
    anchor = document.getElementById(anchor);
  }
  if (!anchor || !popup) {
    return;
  }
  positionGridPopup(anchor, popup);
  // Marks the popup as placed. The date pickers' popups are hidden until then, so they don't flash up in the wrong place
  popup.dataset.pixataPositioned = "";
  const reposition = () => positionGridPopup(anchor, popup);
  // A press outside both the anchor and the popup closes it, as in Telerik's components
  const outside = e => {
    if (!anchor.contains(e.target) && !popup.contains(e.target)) {
      dotNet.invokeMethodAsync("ClosePopup");
    }
  };
  window.addEventListener("scroll", reposition, true);
  window.addEventListener("resize", reposition);
  document.addEventListener("pointerdown", outside, true);
  gridPopups.set(popup, { reposition, outside });
}

export function closePopup(popup) {
  const handlers = gridPopups.get(popup);
  if (handlers) {
    window.removeEventListener("scroll", handlers.reposition, true);
    window.removeEventListener("resize", handlers.reposition);
    document.removeEventListener("pointerdown", handlers.outside, true);
    gridPopups.delete(popup);
  }
}

// Focuses the first editor inside an element, such as a grid cell that has just started being edited
export function focusIn(id) {
  const element = document.getElementById(id);
  const target = element?.querySelector("input:not([type=hidden]), textarea, select, button, [tabindex]:not([tabindex='-1'])");
  if (target) {
    target.focus();
    if (typeof target.select === "function" && target.type !== "checkbox") {
      target.select();
    }
  }
}

const outsideWatchers = new Map();

// Calls back when the user presses outside an element, such as the grid cell being edited, and stops Tab moving the focus
// out of it, so the grid can move to the next cell instead
export function watchOutside(id, dotNet, method) {
  unwatchOutside(id);
  const pointerDown = e => {
    const element = document.getElementById(id);
    if (!element || !element.contains(e.target)) {
      dotNet.invokeMethodAsync(method);
    }
  };
  const keyDown = e => {
    const element = document.getElementById(id);
    if (e.key === "Tab" && element && element.contains(e.target)) {
      e.preventDefault();
    }
  };
  document.addEventListener("pointerdown", pointerDown, true);
  document.addEventListener("keydown", keyDown, true);
  outsideWatchers.set(id, { pointerDown, keyDown });
}

export function unwatchOutside(id) {
  const watcher = outsideWatchers.get(id);
  if (watcher) {
    document.removeEventListener("pointerdown", watcher.pointerDown, true);
    document.removeEventListener("keydown", watcher.keyDown, true);
    outsideWatchers.delete(id);
  }
}

// ---------- Dragging grid headers: grouping, reordering and resizing ----------

const headerDrags = new Map();

// The grid's own columns, leaving out the narrow group columns, with their keys and current widths
function columnWidths(grid) {
  return [...grid.querySelectorAll(".pixata-blazor-grid-table > colgroup > col")]
    .map(col => ({ col, key: col.dataset.pixataColumnKey, width: col.getBoundingClientRect().width }));
}

function clampWidth(width, handle) {
  const min = Number(handle.dataset.pixataMin) || 10;
  const max = Number(handle.dataset.pixataMax) || Infinity;
  return Math.min(Math.max(width, min), max);
}

// Fixes every column at its current width and the table at their total, so resizing one column leaves the others alone
function fixWidths(grid) {
  const table = grid.querySelector(".pixata-blazor-grid-table");
  const cols = columnWidths(grid);
  cols.forEach(c => c.col.style.width = `${c.width}px`);
  table.style.width = `${cols.reduce((total, c) => total + c.width, 0)}px`;
  return { table, cols };
}

function reportWidths(grid, dotNet, key) {
  const widths = columnWidths(grid).filter(c => c.key).map(c => ({ key: c.key, width: c.width }));
  dotNet.invokeMethodAsync("ColumnsResized", key, widths);
}

// Lets the user drag a grid's headers into its group panel, onto other headers to move them, and drag their edges to resize
// them (or double-click an edge to fit the column to its content). Group chips can be dragged to reorder them or, dropped
// outside the panel, to remove them. It uses pointer events rather than HTML drag and drop, so it works with touch too
export function initHeaderDrag(gridId, dotNet) {
  const grid = document.getElementById(gridId);
  if (!grid || headerDrags.has(gridId)) {
    return;
  }
  let drag = null;
  let resize = null;
  let hint = null;
  const isRtl = () => getComputedStyle(grid).direction === "rtl";
  const panelOf = () => grid.querySelector(".pixata-blazor-grid-group-panel");
  const isOverPanel = (x, y) => {
    const panel = panelOf();
    const target = document.elementFromPoint(x, y);
    return panel && target && panel.contains(target) ? target : null;
  };
  // The header a dragged header would be dropped next to, and which side of it
  const reorderTarget = (x, y) => {
    const scope = drag?.source.dataset.pixataReorderScope;
    if (scope === undefined) {
      return null;
    }
    const th = document.elementFromPoint(x, y)?.closest("th[data-pixata-reorder-key]");
    if (!th || !grid.contains(th) || th === drag.source || th.dataset.pixataReorderScope !== scope) {
      return null;
    }
    const rect = th.getBoundingClientRect();
    const firstHalf = x < rect.left + rect.width / 2;
    return { th, before: isRtl() ? !firstHalf : firstHalf };
  };
  const showHint = target => {
    if (!target) {
      hint?.remove();
      hint = null;
      return;
    }
    if (!hint) {
      hint = document.createElement("div");
      hint.className = "pixata-blazor-grid-drop-hint";
      document.body.appendChild(hint);
    }
    const rect = target.th.getBoundingClientRect();
    hint.style.left = `${target.before !== isRtl() ? rect.left : rect.right}px`;
    hint.style.top = `${rect.top}px`;
    hint.style.height = `${rect.height}px`;
  };
  const end = () => {
    if (drag?.clue) {
      drag.clue.remove();
      grid.classList.remove("pixata-blazor-grid-group-dragging");
      panelOf()?.classList.remove("pixata-blazor-grid-group-panel-drop");
    }
    showHint(null);
    drag = null;
  };
  const swallowNextClick = () => {
    const swallow = ev => {
      ev.stopPropagation();
      ev.preventDefault();
    };
    window.addEventListener("click", swallow, { capture: true, once: true });
    setTimeout(() => window.removeEventListener("click", swallow, { capture: true }), 0);
  };
  const down = e => {
    if (e.button !== 0) {
      return;
    }
    const handle = e.target.closest(".pixata-blazor-grid-column-resizer");
    if (handle && grid.contains(handle)) {
      e.preventDefault();
      const { table, cols } = fixWidths(grid);
      const col = cols.find(c => c.key === handle.dataset.pixataResize);
      if (col) {
        resize = { handle, table, cols, col, x: e.clientX, width: col.width, moved: false, rtl: isRtl() };
      }
      return;
    }
    if (e.target.closest(".pixata-blazor-grid-header-menu, .pixata-blazor-grid-group-chip-remove")) {
      return;
    }
    const source = e.target.closest("[data-pixata-group-field], [data-pixata-group-index], [data-pixata-reorder-key]");
    if (source && grid.contains(source)) {
      drag = { source, x: e.clientX, y: e.clientY, clue: null };
    }
  };
  const move = e => {
    if (resize) {
      e.preventDefault();
      resize.moved = true;
      // In a right-to-left page, the handle is on the left, so dragging left widens the column
      const width = clampWidth(resize.width + (e.clientX - resize.x) * (resize.rtl ? -1 : 1), resize.handle);
      resize.col.col.style.width = `${width}px`;
      resize.table.style.width = `${resize.cols.reduce((total, c) => total + (c === resize.col ? width : c.width), 0)}px`;
      return;
    }
    if (!drag) {
      return;
    }
    if (!drag.clue) {
      // A small movement is still a click
      if (Math.abs(e.clientX - drag.x) + Math.abs(e.clientY - drag.y) < 5) {
        return;
      }
      drag.clue = document.createElement("div");
      drag.clue.className = "pixata-blazor-grid-drag-clue";
      drag.clue.textContent = drag.source.dataset.pixataGroupTitle ?? drag.source.textContent.trim();
      document.body.appendChild(drag.clue);
      grid.classList.add("pixata-blazor-grid-group-dragging");
      window.getSelection()?.removeAllRanges();
    }
    e.preventDefault();
    drag.clue.style.left = `${e.clientX + 12}px`;
    drag.clue.style.top = `${e.clientY + 12}px`;
    const overPanel = drag.source.dataset.pixataReorderKey === undefined || drag.source.dataset.pixataGroupField !== undefined
      ? isOverPanel(e.clientX, e.clientY) !== null
      : false;
    const target = overPanel ? null : reorderTarget(e.clientX, e.clientY);
    panelOf()?.classList.toggle("pixata-blazor-grid-group-panel-drop", overPanel);
    drag.clue.classList.toggle("pixata-blazor-grid-drag-clue-drop", overPanel || target !== null);
    showHint(target);
  };
  const up = e => {
    if (resize) {
      const { moved, handle } = resize;
      resize = null;
      if (moved) {
        swallowNextClick();
        reportWidths(grid, dotNet, handle.dataset.pixataResize);
      }
      return;
    }
    if (!drag?.clue) {
      drag = null;
      return;
    }
    const source = drag.source;
    const target = reorderTarget(e.clientX, e.clientY);
    end();
    swallowNextClick();
    if (target) {
      dotNet.invokeMethodAsync("ColumnDrop", source.dataset.pixataReorderKey, target.th.dataset.pixataReorderKey, target.before);
      return;
    }
    if (source.dataset.pixataGroupField === undefined && source.dataset.pixataGroupIndex === undefined) {
      return;
    }
    const overPanel = isOverPanel(e.clientX, e.clientY);
    const chip = overPanel?.closest("[data-pixata-group-index]");
    const from = source.dataset.pixataGroupIndex === undefined ? -1 : Number(source.dataset.pixataGroupIndex);
    const to = chip ? Number(chip.dataset.pixataGroupIndex) : -1;
    dotNet.invokeMethodAsync("GroupDrop", source.dataset.pixataGroupField ?? null, from, to, overPanel !== null);
  };
  const doubleClick = e => {
    const handle = e.target.closest(".pixata-blazor-grid-column-resizer");
    if (handle && grid.contains(handle)) {
      const widths = autoFitColumns(gridId, [handle.dataset.pixataResize]);
      dotNet.invokeMethodAsync("ColumnsResized", handle.dataset.pixataResize, widths);
    }
  };
  const cancel = () => {
    resize = null;
    end();
  };
  grid.addEventListener("pointerdown", down);
  grid.addEventListener("dblclick", doubleClick);
  document.addEventListener("pointermove", move);
  document.addEventListener("pointerup", up);
  document.addEventListener("pointercancel", cancel);
  headerDrags.set(gridId, () => {
    end();
    grid.removeEventListener("pointerdown", down);
    grid.removeEventListener("dblclick", doubleClick);
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
    document.removeEventListener("pointercancel", cancel);
  });
}

export function disposeHeaderDrag(gridId) {
  headerDrags.get(gridId)?.();
  headerDrags.delete(gridId);
}

// Works out how wide some columns need to be to fit their content without wrapping, by copying their cells into a hidden
// table that sizes itself to its content. Returns the widths of all the columns, with those ones changed, as the others
// are fixed at their current widths
export function autoFitColumns(gridId, keys) {
  const grid = document.getElementById(gridId);
  if (!grid) {
    return [];
  }
  const { table, cols } = fixWidths(grid);
  const measure = document.createElement("table");
  measure.className = "pixata-blazor-grid-table pixata-blazor-grid-measure";
  const body = document.createElement("tbody");
  measure.appendChild(body);
  grid.appendChild(measure);
  for (const key of keys) {
    const index = cols.findIndex(c => c.key === key);
    const handle = grid.querySelector(`[data-pixata-resize="${key}"]`);
    if (index < 0) {
      continue;
    }
    // The header, and the cells of each row that has a cell for every column
    const cells = [grid.querySelector(`th[data-pixata-column-key="${key}"]`),
      ...[...table.querySelectorAll("tbody > tr, tfoot > tr")].filter(tr => tr.children.length === cols.length).map(tr => tr.children[index])]
      .filter(cell => cell);
    body.replaceChildren(...cells.map(cell => {
      const tr = document.createElement("tr");
      const copy = cell.cloneNode(true);
      copy.removeAttribute("style");
      copy.removeAttribute("rowspan");
      copy.removeAttribute("id");
      tr.appendChild(copy);
      return tr;
    }));
    const width = Math.ceil(Math.max(0, ...[...body.querySelectorAll("tr > *")].map(cell => cell.getBoundingClientRect().width)));
    cols[index].width = handle ? clampWidth(width, handle) : width;
    cols[index].col.style.width = `${cols[index].width}px`;
  }
  measure.remove();
  table.style.width = `${cols.reduce((total, c) => total + c.width, 0)}px`;
  return cols.filter(c => c.key).map(c => ({ key: c.key, width: c.width }));
}

// ---------- Virtual scrolling ----------

// Tells the grid where its content has been scrolled to, at most every 16ms, so it can render the rows and columns in view.
// A timer is used rather than requestAnimationFrame, which doesn't run while the page isn't being drawn. It also tells the
// grid once at the start, so it knows how big the content is
export function initVirtualScroll(element, dotNet) {
  if (!element) {
    return;
  }
  let pending = false;
  const report = () => {
    pending = false;
    // Scrolling sideways in a right-to-left page gives a negative scrollLeft
    dotNet.invokeMethodAsync("VirtualScrolled", element.scrollTop, Math.abs(element.scrollLeft), element.clientWidth, element.clientHeight);
  };
  element.addEventListener("scroll", () => {
    if (!pending) {
      pending = true;
      setTimeout(report, 16);
    }
  }, { passive: true });
  new ResizeObserver(() => report()).observe(element);
}

export function scrollContentTo(element, top) {
  if (element) {
    element.scrollTop = top;
  }
}

// ---------- Dragging rows ----------

const rowDrags = new Map();

// Lets the user drag a grid's rows by their handles, within the grid or onto another grid. Rows are only dropped on rows in
// a grid that is itself RowDraggable, as only those rows say where they are; anywhere else in a grid drops after its rows
export function initRowDrag(gridId, dotNet) {
  const grid = document.getElementById(gridId);
  if (!grid || rowDrags.has(gridId)) {
    return;
  }
  let drag = null;
  let hint = null;
  let over = null;
  const target = (x, y) => {
    const element = document.elementFromPoint(x, y);
    const destination = element?.closest(".pixata-blazor-grid");
    if (!destination) {
      return null;
    }
    const tr = element.closest("tr[data-pixata-row-index]");
    if (tr && tr.closest(".pixata-blazor-grid") === destination) {
      const rect = tr.getBoundingClientRect();
      const fraction = (y - rect.top) / rect.height;
      return { gridId: destination.id, index: Number(tr.dataset.pixataRowIndex), tr, position: fraction < .25 ? "Before" : fraction > .75 ? "After" : "Over" };
    }
    return { gridId: destination.id, index: -1, tr: null, position: "After" };
  };
  const show = found => {
    over?.classList.remove("pixata-blazor-grid-row-drop-over");
    over = null;
    if (found?.tr && found.position === "Over") {
      over = found.tr;
      over.classList.add("pixata-blazor-grid-row-drop-over");
    }
    if (!found?.tr || found.position === "Over") {
      hint?.remove();
      hint = null;
      return;
    }
    if (!hint) {
      hint = document.createElement("div");
      hint.className = "pixata-blazor-grid-row-drop-hint";
      document.body.appendChild(hint);
    }
    const rect = found.tr.getBoundingClientRect();
    hint.style.left = `${rect.left}px`;
    hint.style.width = `${rect.width}px`;
    hint.style.top = `${found.position === "Before" ? rect.top : rect.bottom}px`;
  };
  const end = () => {
    drag?.clue?.remove();
    show(null);
    document.body.classList.remove("pixata-blazor-grid-row-dragging");
    drag = null;
  };
  const down = e => {
    const handle = e.target.closest("[data-pixata-row-drag]");
    const tr = handle?.closest("tr[data-pixata-row-index]");
    if (e.button !== 0 || !tr || tr.closest(".pixata-blazor-grid") !== grid) {
      return;
    }
    e.preventDefault();
    drag = { tr, index: Number(tr.dataset.pixataRowIndex), x: e.clientX, y: e.clientY, clue: null };
  };
  const move = e => {
    if (!drag) {
      return;
    }
    if (!drag.clue) {
      if (Math.abs(e.clientX - drag.x) + Math.abs(e.clientY - drag.y) < 5) {
        return;
      }
      drag.clue = document.createElement("div");
      drag.clue.className = "pixata-blazor-grid-drag-clue";
      drag.clue.textContent = [...drag.tr.children].map(td => td.textContent.trim()).filter(text => text).join(" ");
      document.body.appendChild(drag.clue);
      document.body.classList.add("pixata-blazor-grid-row-dragging");
      window.getSelection()?.removeAllRanges();
    }
    e.preventDefault();
    drag.clue.style.left = `${e.clientX + 12}px`;
    drag.clue.style.top = `${e.clientY + 12}px`;
    const found = target(e.clientX, e.clientY);
    drag.clue.classList.toggle("pixata-blazor-grid-drag-clue-drop", found !== null);
    show(found);
  };
  const up = e => {
    if (!drag?.clue) {
      drag = null;
      return;
    }
    const index = drag.index;
    const found = target(e.clientX, e.clientY);
    end();
    dotNet.invokeMethodAsync("RowDrop", index, found?.gridId ?? null, found?.index ?? -1, found?.position ?? "After");
  };
  const cancel = () => end();
  grid.addEventListener("pointerdown", down);
  document.addEventListener("pointermove", move);
  document.addEventListener("pointerup", up);
  document.addEventListener("pointercancel", cancel);
  rowDrags.set(gridId, () => {
    end();
    grid.removeEventListener("pointerdown", down);
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
    document.removeEventListener("pointercancel", cancel);
  });
}

export function disposeRowDrag(gridId) {
  rowDrags.get(gridId)?.();
  rowDrags.delete(gridId);
}

// ---------- Saving files ----------

// Has the browser save a file that .NET has sent as a stream
export async function downloadFile(fileName, contentType, streamReference) {
  const buffer = await streamReference.arrayBuffer();
  const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

// ---------- Keyboard navigation ----------

const navigations = new Map();

// Makes a grid a single tab stop, with the arrow keys (and Home, End, Ctrl+Home and Ctrl+End) moving between its cells. The
// cell the user is on has tabindex 0 and the others -1, which is put back after every render. Keys that do something to the
// data (Page Up and Down, Enter, F2, Space and Alt+Down) are passed to .NET
export function initNavigation(gridId, dotNet) {
  const grid = document.getElementById(gridId);
  const table = grid?.querySelector(".pixata-blazor-grid-table");
  if (!table || navigations.has(gridId)) {
    return;
  }
  let active = { row: 0, col: 0 };
  let hadFocus = false;
  const rows = () => [...table.querySelectorAll(":scope > thead > tr, :scope > tbody > tr, :scope > tfoot > tr")]
    .filter(r => !r.classList.contains("pixata-blazor-grid-virtual-spacer"));
  const cellsOf = row => [...row.children].filter(c => c.matches("th, td"));
  const cellAt = (r, c) => {
    const all = rows();
    const row = all[Math.max(0, Math.min(r, all.length - 1))];
    if (!row) {
      return null;
    }
    const cells = cellsOf(row);
    return cells[Math.max(0, Math.min(c, cells.length - 1))] ?? null;
  };
  const positionOf = cell => ({ row: rows().indexOf(cell.parentElement), col: cellsOf(cell.parentElement).indexOf(cell) });
  const apply = focus => {
    for (const row of rows()) {
      for (const cell of cellsOf(row)) {
        if (cell.tabIndex !== -1) {
          cell.tabIndex = -1;
        }
      }
    }
    const cell = cellAt(active.row, active.col);
    if (cell) {
      cell.tabIndex = 0;
      if (focus) {
        cell.focus();
      }
    }
  };
  const moveTo = cell => {
    if (cell) {
      active = positionOf(cell);
      apply(true);
    }
  };
  // The cell in another row that lines up with this one
  const cellBelow = (cell, step) => {
    const all = rows();
    const rect = cell.getBoundingClientRect();
    const middle = rect.left + rect.width / 2;
    for (let r = all.indexOf(cell.parentElement) + step; r >= 0 && r < all.length; r += step) {
      const match = cellsOf(all[r]).find(c => {
        const box = c.getBoundingClientRect();
        return box.left <= middle && box.right >= middle;
      });
      if (match && match !== cell) {
        return match;
      }
    }
    return null;
  };
  const keyDown = e => {
    const cell = e.target;
    if (!cell.matches?.("th, td") || cell.closest("table") !== table) {
      return;
    }
    const rtl = getComputedStyle(table).direction === "rtl";
    const cells = cellsOf(cell.parentElement);
    const index = cells.indexOf(cell);
    let target = undefined;
    switch (e.key) {
      case "ArrowRight":
      case "ArrowLeft":
        target = cells[index + ((e.key === "ArrowRight") !== rtl ? 1 : -1)] ?? null;
        break;
      case "ArrowDown":
        if (e.altKey && cell.tagName === "TH") {
          dotNet.invokeMethodAsync("NavigationKey", "Menu", -1, cell.dataset.pixataColumnKey ?? null);
          e.preventDefault();
          return;
        }
        target = cellBelow(cell, 1);
        break;
      case "ArrowUp":
        target = cellBelow(cell, -1);
        break;
      case "Home":
        target = e.ctrlKey ? cellAt(0, 0) : cells[0];
        break;
      case "End":
        target = e.ctrlKey ? cellAt(Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER) : cells[cells.length - 1];
        break;
      case "PageDown":
      case "PageUp":
        dotNet.invokeMethodAsync("NavigationKey", e.key, -1, null);
        e.preventDefault();
        return;
      case "Enter":
      case "F2":
      case " ": {
        const row = cell.closest("tr[data-pixata-row-index]");
        if (row && cell.tagName === "TD") {
          dotNet.invokeMethodAsync("NavigationKey", e.key, Number(row.dataset.pixataRowIndex), cell.dataset.pixataColumnKey ?? null);
          e.preventDefault();
        }
        return;
      }
      default:
        return;
    }
    e.preventDefault();
    moveTo(target);
  };
  const focusIn = e => {
    hadFocus = true;
    const cell = e.target.closest?.("th, td");
    if (cell && cell.closest("table") === table) {
      active = positionOf(cell);
      if (e.target === cell) {
        apply(false);
      }
    }
  };
  const focusOut = e => {
    hadFocus = table.contains(e.relatedTarget);
  };
  // After a render, the tab stop is put back, and if the focused cell was replaced (such as by paging), its replacement is
  // focused
  let pending = false;
  const observer = new MutationObserver(() => {
    if (pending) {
      return;
    }
    pending = true;
    setTimeout(() => {
      pending = false;
      apply(hadFocus && (document.activeElement === document.body || !document.activeElement));
    }, 0);
  });
  observer.observe(table, { childList: true, subtree: true });
  table.addEventListener("keydown", keyDown);
  table.addEventListener("focusin", focusIn);
  table.addEventListener("focusout", focusOut);
  apply(false);
  navigations.set(gridId, () => {
    observer.disconnect();
    table.removeEventListener("keydown", keyDown);
    table.removeEventListener("focusin", focusIn);
    table.removeEventListener("focusout", focusOut);
  });
}

export function disposeNavigation(gridId) {
  navigations.get(gridId)?.();
  navigations.delete(gridId);
}
