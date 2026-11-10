// Shows the mark-up over the pages, and lets the user add to it with the tool chosen on the toolbar: a note pinned to a point,
// a highlight or strike-through (of selected text, or of an area dragged out where there's no text), freehand drawing, or
// text. The .NET component holds the mark-up, and is told what the user adds, changes or deletes. It sends the mark-up back
// with setMarkup, so what's shown is always what it holds. Items the user hasn't sent yet (drawing that's still going on, or
// an item waiting for the component to answer) are kept in state.pending, and shown as well
import { drawPage, element, inkPath, joinLines, round, sizeTextBoxes } from "./markup.js";

// Strokes drawn within this long of each other on the same page make one item, so a word written by hand is one item
const inkPause = 1200;
// How far, in CSS pixels, the pointer can move and still count as a click
const clickDistance = 5;
const inkWidth = 2;
const textSize = 12;

const invoke = (state, method, ...args) =>
  state.dotNet.invokeMethodAsync(method, ...args).catch(() => { });

export function setUpMarkup(state, options) {
  Object.assign(state, {
    markup: [],
    pending: [],
    pendingInk: null,
    inkTimer: 0,
    overlays: new Map(),
    tool: "",
    colour: "#d32f2f",
    canEdit: false,
    markupVisible: true,
    selected: null,
    popover: null,
    editor: null,
    gesture: null,
    markupText: options.markupText ?? {},
    buttonClass: options.buttonClass ?? "",
    inputClass: options.inputClass ?? ""
  });
  const { container, eventBus } = state;
  const signal = state.abort.signal;

  eventBus.on("pagerender", ({ source }) => attach(state, source));
  eventBus.on("pagerendered", ({ source }) => attach(state, source));
  eventBus.on("scalechanging", () => closePopups(state, true));
  eventBus.on("rotationchanging", () => closePopups(state, true));

  // PDF.js empties a page when it's zoomed or turned, and draws it again a moment later. The mark-up is put back straight
  // away, so it doesn't disappear in the meantime
  const observer = new MutationObserver(records => {
    for (const record of records) {
      for (const node of record.removedNodes) {
        if (node.classList?.contains("pixata-blazor-pdfviewer-markup") && record.target.isConnected && state.overlays.get(Number(node.dataset.page)) === node) {
          record.target.appendChild(node);
        }
      }
    }
  });
  observer.observe(container.firstElementChild, { childList: true, subtree: true });
  signal.addEventListener("abort", () => {
    observer.disconnect();
    clearTimeout(state.inkTimer);
    closePopups(state, false);
  });

  container.addEventListener("pointerdown", e => pointerDown(state, e), { signal });
  container.addEventListener("pointermove", e => pointerMove(state, e), { signal });
  container.addEventListener("pointerup", e => pointerUp(state, e), { signal });
  container.addEventListener("pointercancel", () => cancelGesture(state), { signal });
  container.addEventListener("click", e => clicked(state, e), { signal });
  container.addEventListener("scroll", () => closePopups(state, true), { signal, passive: true });
  // Clicking anywhere else closes a note
  document.addEventListener("pointerdown", e => {
    if (state.popover && !state.popover.contains(e.target) && !e.target.closest?.(".pixata-blazor-pdfviewer-markup-item")) {
      closePopups(state, true);
    }
  }, { signal, capture: true });
}

// ---------- Called through index.js by the component ----------

export function setMarkup(state, items, canEdit) {
  state.markup = items;
  state.canEdit = canEdit;
  // What's been sent has come back as part of the mark-up, apart from drawing that's still going on
  state.pending = state.pending.filter(item => item === state.pendingInk);
  if (state.popoverState && !state.popoverState.isNew && !items.some(item => item.id === state.popoverState.item.id)) {
    closePopups(state, false);
  }
  redrawAll(state);
}

export function setTool(state, tool, colour) {
  if (tool !== state.tool) {
    commitInk(state);
    closePopups(state, true);
    window.getSelection()?.removeAllRanges();
  }
  state.tool = tool;
  state.colour = colour;
  if (tool) {
    state.container.dataset.markupTool = tool;
  } else {
    delete state.container.dataset.markupTool;
  }
}

export function setVisible(state, visible) {
  state.markupVisible = visible;
  state.container.classList.toggle("pixata-blazor-pdfviewer-markup-hidden", !visible);
  if (!visible) {
    closePopups(state, true);
  }
}

// When the PDF is closed, so its pages have gone
export function resetMarkup(state) {
  clearTimeout(state.inkTimer);
  state.pendingInk = null;
  state.pending = [];
  state.gesture = null;
  closePopups(state, false);
  state.overlays.clear();
}

// Draws a page's mark-up onto a canvas the page has been drawn on, for printing
export async function drawMarkupForPrint(state, pdfPage, pageNumber, rotation, context, width, height) {
  const items = state.markupVisible ? state.markup.filter(item => item.page === pageNumber) : [];
  if (!items.length) {
    return;
  }
  const svg = element("svg", { width, height });
  drawPage(svg, items, pdfPage, rotation);
  const url = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(svg)], { type: "image/svg+xml" }));
  try {
    const image = new Image();
    image.src = url;
    await image.decode();
    context.drawImage(image, 0, 0, width, height);
  } finally {
    URL.revokeObjectURL(url);
  }
}

// ---------- Drawing ----------

// Puts the page's mark-up on it, when PDF.js draws it
function attach(state, pageView) {
  if (!pageView?.pdfPage || !state.document) {
    return;
  }
  let svg = state.overlays.get(pageView.id);
  if (!svg) {
    svg = element("svg", { class: "pixata-blazor-pdfviewer-markup", "aria-hidden": "true" });
    svg.dataset.page = pageView.id;
    svg.dataset.dirty = "1";
    state.overlays.set(pageView.id, svg);
  }
  if (svg.parentNode !== pageView.div) {
    pageView.div.appendChild(svg);
  }
  if (svg.dataset.dirty || svg.dataset.rotation !== String(pageView.viewport.rotation)) {
    draw(state, pageView);
  }
}

function draw(state, pageView) {
  const svg = state.overlays.get(pageView.id);
  if (!svg || !pageView.pdfPage) {
    return;
  }
  svg.dataset.rotation = String(pageView.viewport.rotation);
  delete svg.dataset.dirty;
  const items = [...state.markup, ...state.pending].filter(item => item.page === pageView.id);
  drawPage(svg, items, pageView.pdfPage, pageView.viewport.rotation, state.selected);
  if (svg.isConnected) {
    sizeTextBoxes(svg);
  }
}

function redrawAll(state) {
  for (const [page, svg] of state.overlays) {
    const pageView = state.viewer.getPageView(page - 1);
    if (pageView?.pdfPage && svg.isConnected) {
      draw(state, pageView);
    } else {
      svg.dataset.dirty = "1";
    }
  }
}

function redrawPage(state, page) {
  const pageView = state.viewer.getPageView(page - 1);
  if (pageView) {
    draw(state, pageView);
  }
}

// ---------- Positions ----------

function pageAt(state, target) {
  const div = target?.closest?.(".page");
  if (!div || !state.container.contains(div)) {
    return null;
  }
  const pageView = state.viewer.getPageView(Number(div.dataset.pageNumber) - 1);
  return pageView?.pdfPage ? pageView : null;
}

function pageAtPoint(state, x, y) {
  for (const div of state.container.querySelectorAll(".page")) {
    const box = div.getBoundingClientRect();
    if (x >= box.left && x <= box.right && y >= box.top && y <= box.bottom) {
      return pageAt(state, div);
    }
  }
  return null;
}

// A point on the screen as a point of the page, in its own coordinates
function toPdf(pageView, clientX, clientY) {
  const box = pageView.div.getBoundingClientRect();
  const [x, y] = pageView.viewport.convertToPdfPoint(clientX - box.left - pageView.div.clientLeft, clientY - box.top - pageView.div.clientTop);
  return { x: round(x), y: round(y) };
}

function rectToPdf(pageView, rect) {
  const a = toPdf(pageView, rect.left, rect.top);
  const b = toPdf(pageView, rect.right, rect.bottom);
  return { left: Math.min(a.x, b.x), bottom: Math.min(a.y, b.y), right: Math.max(a.x, b.x), top: Math.max(a.y, b.y) };
}

// ---------- Adding, changing and deleting ----------

function add(state, item) {
  state.pending.push(item);
  redrawPage(state, item.page);
  invoke(state, "MarkupAdded", JSON.stringify(item));
}

function commitInk(state) {
  clearTimeout(state.inkTimer);
  const ink = state.pendingInk;
  if (ink) {
    state.pendingInk = null;
    invoke(state, "MarkupAdded", JSON.stringify(ink));
  }
}

const selectionIsEmpty = state => {
  const selection = window.getSelection();
  return !selection || selection.isCollapsed || !state.container.contains(selection.anchorNode);
};

// Highlights or strikes through the selected text, with one item for each page it's on
function markSelection(state, selection) {
  const pages = new Map();
  for (const rect of selection.getRangeAt(0).getClientRects()) {
    if (rect.width < 1 || rect.height < 1) {
      continue;
    }
    const pageView = pageAtPoint(state, rect.left + rect.width / 2, rect.top + rect.height / 2);
    if (pageView) {
      pages.set(pageView, [...(pages.get(pageView) ?? []), rect]);
    }
  }
  let text = selection.toString().replace(/\s+/g, " ").trim();
  for (const [pageView, rects] of pages) {
    // Text runs across the screen if its rectangles are wider than they're tall, taken together
    const horizontal = rects.reduce((sum, r) => sum + r.width, 0) >= rects.reduce((sum, r) => sum + r.height, 0);
    add(state, {
      type: state.tool,
      page: pageView.id,
      colour: state.colour,
      rects: joinLines(rects, horizontal).map(rect => rectToPdf(pageView, rect)),
      text
    });
    text = "";
  }
}

// ---------- The pointer ----------

function pointerDown(state, e) {
  if (e.button !== 0 || !state.tool || !state.canEdit || !state.markupVisible) {
    return;
  }
  const pageView = pageAt(state, e.target);
  if (!pageView) {
    return;
  }
  const gesture = { x: e.clientX, y: e.clientY, pageView, point: toPdf(pageView, e.clientX, e.clientY) };
  switch (state.tool) {
    case "Ink": {
      e.preventDefault();
      if (state.pendingInk && state.pendingInk.page !== pageView.id) {
        commitInk(state);
      }
      clearTimeout(state.inkTimer);
      const svg = state.overlays.get(pageView.id);
      svg?.setPointerCapture?.(e.pointerId);
      gesture.points = [gesture.point];
      gesture.preview = element("path", {
        d: inkPath(gesture.points), fill: "none", stroke: state.colour, "stroke-width": inkWidth, "stroke-linecap": "round", "stroke-linejoin": "round"
      }, svg?.firstElementChild ?? null);
      break;
    }
    case "Note":
    case "Text":
      e.preventDefault();
      break;
  }
  state.gesture = gesture;
}

function pointerMove(state, e) {
  const gesture = state.gesture;
  if (!gesture) {
    return;
  }
  if (state.tool === "Ink") {
    if (Math.hypot(e.clientX - gesture.lastX, e.clientY - gesture.lastY) >= 2 || gesture.lastX === undefined) {
      gesture.lastX = e.clientX;
      gesture.lastY = e.clientY;
      gesture.points.push(toPdf(gesture.pageView, e.clientX, e.clientY));
      gesture.preview?.setAttribute("d", inkPath(gesture.points));
    }
  } else if ((state.tool === "Highlight" || state.tool === "StrikeThrough") && selectionIsEmpty(state)
    && Math.hypot(e.clientX - gesture.x, e.clientY - gesture.y) >= clickDistance) {
    // Dragging where there's no text, such as on a scanned page, marks the area dragged over
    const point = toPdf(gesture.pageView, e.clientX, e.clientY);
    const page = state.overlays.get(gesture.pageView.id)?.firstElementChild;
    gesture.preview ??= page ? element("rect", { class: "pixata-blazor-pdfviewer-markup-area", fill: state.colour, "fill-opacity": 0.25, stroke: state.colour }, page) : null;
    gesture.preview?.setAttribute("x", Math.min(gesture.point.x, point.x));
    gesture.preview?.setAttribute("y", Math.min(gesture.point.y, point.y));
    gesture.preview?.setAttribute("width", Math.abs(point.x - gesture.point.x));
    gesture.preview?.setAttribute("height", Math.abs(point.y - gesture.point.y));
  }
}

function pointerUp(state, e) {
  const gesture = state.gesture;
  state.gesture = null;
  if (!gesture) {
    return;
  }
  const moved = Math.hypot(e.clientX - gesture.x, e.clientY - gesture.y);
  const page = gesture.pageView.id;
  switch (state.tool) {
    case "Ink":
      if (!state.pendingInk) {
        state.pendingInk = { type: "Ink", page, colour: state.colour, lineWidth: inkWidth, strokes: [] };
        state.pending.push(state.pendingInk);
      }
      state.pendingInk.strokes.push(gesture.points);
      redrawPage(state, page);
      state.inkTimer = setTimeout(() => commitInk(state), inkPause);
      break;
    case "Note":
      if (moved < clickDistance) {
        openPopover(state, { item: { type: "Note", page, colour: state.colour, position: gesture.point, text: "" }, isNew: true, clientX: e.clientX, clientY: e.clientY });
      }
      break;
    case "Text":
      if (moved < clickDistance) {
        openTextEditor(state, gesture.pageView, gesture.point, e.clientX, e.clientY);
      }
      break;
    case "Highlight":
    case "StrikeThrough": {
      gesture.preview?.remove();
      const selection = window.getSelection();
      if (!selectionIsEmpty(state)) {
        markSelection(state, selection);
        selection.removeAllRanges();
      } else if (moved >= clickDistance) {
        const point = toPdf(gesture.pageView, e.clientX, e.clientY);
        add(state, {
          type: state.tool,
          page,
          colour: state.colour,
          rects: [{ left: Math.min(gesture.point.x, point.x), bottom: Math.min(gesture.point.y, point.y), right: Math.max(gesture.point.x, point.x), top: Math.max(gesture.point.y, point.y) }],
          text: ""
        });
      }
      break;
    }
  }
}

function cancelGesture(state) {
  state.gesture?.preview?.remove();
  state.gesture = null;
}

// With no tool chosen, clicking an item shows it
function clicked(state, e) {
  if (state.tool) {
    return;
  }
  const target = e.target.closest?.(".pixata-blazor-pdfviewer-markup-item[data-id]");
  const item = target && state.markup.find(i => i.id === target.dataset.id);
  if (item) {
    openPopover(state, { item, isNew: false, clientX: e.clientX, clientY: e.clientY });
  }
}

// ---------- The note pop-up and the text box ----------

const dateFormat = new Intl.DateTimeFormat(document.documentElement.lang || undefined, { dateStyle: "medium", timeStyle: "short" });

const html = (name, className, parent, text) => {
  const created = document.createElement(name);
  if (className) {
    created.className = className;
  }
  if (text !== undefined) {
    created.textContent = text;
  }
  parent?.appendChild(created);
  return created;
};

// Shows an item, with who added it and when, and lets the user change its text or delete it if it's theirs. A new note is
// added when this is closed, if something has been written in it
function openPopover(state, { item, isNew, clientX, clientY }) {
  closePopups(state, true);
  const text = state.markupText;
  const body = state.container.parentNode;
  const popover = html("div", "pixata-blazor-pdfviewer-popover", null);
  popover.setAttribute("role", "dialog");
  if (isNew) {
    popover.setAttribute("aria-label", text.newNote ?? "");
  } else {
    const header = html("div", "pixata-blazor-pdfviewer-popover-header", popover);
    html("strong", null, header, item.author || "");
    html("span", null, header, item.created ? dateFormat.format(new Date(item.created)) : "");
    popover.setAttribute("aria-label", item.author || "");
  }
  const editable = isNew || item.editable;
  let textarea = null;
  if (editable && (item.type === "Note" || item.type === "Text")) {
    const wrapper = html("span", `${state.inputClass} pixata-blazor-pdfviewer-popover-input`, popover);
    textarea = html("textarea", "pixata-blazor-input-inner", wrapper);
    textarea.rows = 3;
    textarea.dir = "auto";
    textarea.value = item.text ?? "";
    textarea.placeholder = item.type === "Note" ? text.notePlaceholder ?? "" : text.textPlaceholder ?? "";
    textarea.setAttribute("aria-label", item.type === "Note" ? text.note ?? "" : text.text ?? "");
  } else if (item.text) {
    html("div", "pixata-blazor-pdfviewer-popover-text", popover, item.text).dir = "auto";
  }
  const buttons = html("div", "pixata-blazor-pdfviewer-popover-buttons", popover);
  if (!isNew && item.editable) {
    const remove = html("button", state.buttonClass, buttons, text.delete ?? "");
    remove.type = "button";
    remove.addEventListener("click", () => {
      invoke(state, "MarkupDeleted", item.id);
      closePopups(state, false);
      state.container.focus();
    });
  }
  const done = html("button", state.buttonClass, buttons, text.close ?? "");
  done.type = "button";
  done.addEventListener("click", () => {
    closePopups(state, true);
    state.container.focus();
  });
  popover.addEventListener("keydown", e => {
    if (e.key === "Escape") {
      e.preventDefault();
      closePopups(state, false);
      state.container.focus();
    } else if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      closePopups(state, true);
      state.container.focus();
    }
  });

  // Next to where the item was clicked, and inside the viewer
  popover.style.visibility = "hidden";
  body.appendChild(popover);
  const box = body.getBoundingClientRect();
  const width = popover.offsetWidth;
  const height = popover.offsetHeight;
  let left = clientX - box.left + 8;
  let top = clientY - box.top + 12;
  if (left + width > box.width - 4) {
    left = Math.max(4, box.width - width - 4);
  }
  if (top + height > box.height - 4) {
    top = Math.max(4, clientY - box.top - height - 12);
  }
  popover.style.left = `${left}px`;
  popover.style.top = `${top}px`;
  popover.style.visibility = "";

  state.popover = popover;
  state.popoverState = { item, isNew, textarea, original: item.text ?? "" };
  if (!isNew) {
    state.selected = item.id;
    redrawPage(state, item.page);
  }
  (textarea ?? done).focus();
}

// Writing text on the page: a text box where the page was clicked, in the colour chosen, at the size the text will be
function openTextEditor(state, pageView, point, clientX, clientY) {
  closePopups(state, true);
  const body = state.container.parentNode;
  const box = body.getBoundingClientRect();
  const editor = html("textarea", "pixata-blazor-pdfviewer-text-editor", null);
  editor.dir = "auto";
  editor.rows = 1;
  editor.placeholder = state.markupText.textPlaceholder ?? "";
  editor.setAttribute("aria-label", state.markupText.text ?? "");
  editor.style.left = `${clientX - box.left}px`;
  editor.style.top = `${clientY - box.top}px`;
  editor.style.fontSize = `${textSize * pageView.viewport.scale}px`;
  editor.style.color = state.colour;
  const fit = () => {
    const rows = editor.value.split("\n");
    editor.rows = Math.max(1, rows.length);
    editor.style.width = `${Math.max(8, ...rows.map(row => row.length)) + 2}ch`;
  };
  editor.addEventListener("input", fit);
  editor.addEventListener("keydown", e => {
    if (e.key === "Escape") {
      e.preventDefault();
      closePopups(state, false);
      state.container.focus();
    } else if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      closePopups(state, true);
      state.container.focus();
    }
  });
  editor.addEventListener("blur", () => {
    if (state.editor?.element === editor) {
      closePopups(state, true);
    }
  });
  fit();
  body.appendChild(editor);
  state.editor = { element: editor, page: pageView.id, point, colour: state.colour };
  editor.focus();
}

// Closes the note pop-up or the text box, saving what's been written if commit is true
function closePopups(state, commit) {
  const editor = state.editor;
  if (editor) {
    state.editor = null;
    const text = editor.element.value.trimEnd();
    editor.element.remove();
    if (commit && text.trim()) {
      add(state, { type: "Text", page: editor.page, colour: editor.colour, position: editor.point, fontSize: textSize, text });
    }
  }
  const popover = state.popover;
  if (popover) {
    const { item, isNew, textarea, original } = state.popoverState;
    state.popover = null;
    state.popoverState = null;
    popover.remove();
    if (commit && textarea) {
      const text = textarea.value.trimEnd();
      if (isNew) {
        if (text.trim()) {
          add(state, { ...item, text });
        }
      } else if (text !== original) {
        if (text.trim()) {
          invoke(state, "MarkupTextChanged", item.id, text);
        } else {
          invoke(state, "MarkupDeleted", item.id);
        }
      }
    }
    if (state.selected) {
      state.selected = null;
      redrawPage(state, item.page);
    }
  }
}
