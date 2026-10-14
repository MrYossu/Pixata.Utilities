// JavaScript for the Telerik-style components in Pixata.Blazor. It only does what Blazor can't: controlling the selection
// inside an input, positioning popups, and noticing clicks outside them. The logic itself stays in C#

const dateInputs = new WeakMap();

// Keys the date input handles itself. Anything else (Tab, shortcuts such as Ctrl+C) is left to the browser
const dateInputKeys = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Backspace", "Delete", "Home", "End"]);

function applyDateInputState(input, state) {
  if (!state) {
    return;
  }
  input.value = state.text;
  if (document.activeElement === input && state.start >= 0) {
    input.setSelectionRange(state.start, state.end);
  }
}

export function initDateInput(input, dotNet) {
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

const popups = new WeakMap();

// Positions a popup below its anchor (or above it, if there's more room there), keeping it inside the viewport. It uses
// fixed positioning, so it isn't clipped by containers with overflow hidden, such as a grid cell
function positionPopup(anchor, popup) {
  const a = anchor.getBoundingClientRect();
  // A list is at least as wide as the control it drops down from
  popup.style.minWidth = `${a.width}px`;
  const p = popup.getBoundingClientRect();
  const below = window.innerHeight - a.bottom;
  const top = below >= p.height || below >= a.top ? a.bottom : a.top - p.height;
  const left = Math.max(0, Math.min(a.left, window.innerWidth - p.width));
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
  positionPopup(anchor, popup);
  const reposition = () => positionPopup(anchor, popup);
  // A press outside both the anchor and the popup closes it, as in Telerik's components
  const outside = e => {
    if (!anchor.contains(e.target) && !popup.contains(e.target)) {
      dotNet.invokeMethodAsync("ClosePopup");
    }
  };
  window.addEventListener("scroll", reposition, true);
  window.addEventListener("resize", reposition);
  document.addEventListener("pointerdown", outside, true);
  popups.set(popup, { reposition, outside });
}

export function closePopup(popup) {
  const handlers = popups.get(popup);
  if (handlers) {
    window.removeEventListener("scroll", handlers.reposition, true);
    window.removeEventListener("resize", handlers.reposition);
    document.removeEventListener("pointerdown", handlers.outside, true);
    popups.delete(popup);
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
    return { th, before: x < rect.left + rect.width / 2 };
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
    hint.style.left = `${target.before ? rect.left : rect.right}px`;
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
        resize = { handle, table, cols, col, x: e.clientX, width: col.width, moved: false };
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
      const width = clampWidth(resize.width + e.clientX - resize.x, resize.handle);
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
    dotNet.invokeMethodAsync("VirtualScrolled", element.scrollTop, element.scrollLeft, element.clientWidth, element.clientHeight);
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
