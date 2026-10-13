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

// ---------- Dragging grid headers into the group panel ----------

const groupDrags = new Map();

// Lets the user drag a grid's column headers into its group panel, and its group chips to reorder them or (dropped outside
// the panel) remove them. It uses pointer events rather than HTML drag and drop, so it works with touch too
export function initGroupDrag(gridId, dotNet) {
  const grid = document.getElementById(gridId);
  if (!grid || groupDrags.has(gridId)) {
    return;
  }
  let drag = null;
  const panelOf = () => grid.querySelector(".pixata-blazor-grid-group-panel");
  const isOverPanel = (x, y) => {
    const panel = panelOf();
    const target = document.elementFromPoint(x, y);
    return panel && target && panel.contains(target) ? target : null;
  };
  const end = () => {
    if (drag?.clue) {
      drag.clue.remove();
      grid.classList.remove("pixata-blazor-grid-group-dragging");
      panelOf()?.classList.remove("pixata-blazor-grid-group-panel-drop");
    }
    drag = null;
  };
  const down = e => {
    if (e.button !== 0 || e.target.closest(".pixata-blazor-grid-header-menu, .pixata-blazor-grid-group-chip-remove")) {
      return;
    }
    const source = e.target.closest("[data-pixata-group-field], [data-pixata-group-index]");
    if (source && grid.contains(source)) {
      drag = { source, x: e.clientX, y: e.clientY, clue: null };
    }
  };
  const move = e => {
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
    const over = isOverPanel(e.clientX, e.clientY) !== null;
    panelOf()?.classList.toggle("pixata-blazor-grid-group-panel-drop", over);
    drag.clue.classList.toggle("pixata-blazor-grid-drag-clue-drop", over);
  };
  const up = e => {
    if (!drag?.clue) {
      drag = null;
      return;
    }
    const source = drag.source;
    end();
    // The click that follows the drag mustn't sort the column
    const swallow = ev => {
      ev.stopPropagation();
      ev.preventDefault();
    };
    window.addEventListener("click", swallow, { capture: true, once: true });
    setTimeout(() => window.removeEventListener("click", swallow, { capture: true }), 0);
    const target = isOverPanel(e.clientX, e.clientY);
    const chip = target?.closest("[data-pixata-group-index]");
    const from = source.dataset.pixataGroupIndex === undefined ? -1 : Number(source.dataset.pixataGroupIndex);
    const to = chip ? Number(chip.dataset.pixataGroupIndex) : -1;
    dotNet.invokeMethodAsync("GroupDrop", source.dataset.pixataGroupField ?? null, from, to, target !== null);
  };
  const cancel = () => end();
  grid.addEventListener("pointerdown", down);
  document.addEventListener("pointermove", move);
  document.addEventListener("pointerup", up);
  document.addEventListener("pointercancel", cancel);
  groupDrags.set(gridId, () => {
    end();
    grid.removeEventListener("pointerdown", down);
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
    document.removeEventListener("pointercancel", cancel);
  });
}

export function disposeGroupDrag(gridId) {
  groupDrags.get(gridId)?.();
  groupDrags.delete(gridId);
}
