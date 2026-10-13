// The browser half of PixataEditor. The .NET component renders the toolbar and the content area, and this module turns the
// content area into the editable document, runs the toolbar's commands, and reports the value and the selection's
// formatting back to .NET

import { EditorState, Plugin } from "prosemirror-state";
import { EditorView, Decoration, DecorationSet } from "prosemirror-view";
import { keymap } from "prosemirror-keymap";
import { baseKeymap, chainCommands, toggleMark } from "prosemirror-commands";
import { history, undo, redo } from "prosemirror-history";
import { splitListItem, liftListItem, sinkListItem } from "prosemirror-schema-list";
import { dropCursor } from "prosemirror-dropcursor";
import { gapCursor } from "prosemirror-gapcursor";
import { tableEditing, goToNextCell } from "prosemirror-tables";
import { schema, docFromHtml, docToHtml, sanitiseHtml, isEmptyDoc } from "./schema.js";
import {
  commands, conditionalCommands, activeCommands, currentFormat, textStyleAt, linkInfo, imageInfo
} from "./commands.js";
import { cleanPastedHtml, defaultPasteSettings } from "./paste.js";
import { formatHtml } from "./format.js";

const instances = new Map();
let nextId = 1;

const contentClass = "pixata-blazor-editor-document";
const placeholderClass = "pixata-blazor-editor-placeholder";

// Placeholder

function placeholderPlugin(instance) {
  return new Plugin({
    props: {
      decorations(state) {
        const text = instance.options.placeholder;
        if (!text || !isEmptyDoc(state.doc)) {
          return null;
        }
        const first = state.doc.firstChild;
        return DecorationSet.create(state.doc, [
          Decoration.node(0, first.nodeSize, { class: placeholderClass, "data-placeholder": text })
        ]);
      }
    }
  });
}

// Key bindings

function insertHardBreak(state, dispatch) {
  if (dispatch) {
    dispatch(state.tr.replaceSelectionWith(schema.nodes.hard_break.create()).scrollIntoView());
  }
  return true;
}

function buildKeymap(instance) {
  const { marks, nodes } = schema;
  return {
    "Mod-z": undo,
    "Shift-Mod-z": redo,
    "Mod-y": redo,
    "Mod-b": toggleMark(marks.strong),
    "Mod-i": toggleMark(marks.em),
    "Mod-u": toggleMark(marks.underline),
    "Shift-Enter": insertHardBreak,
    "Mod-Enter": insertHardBreak,
    "Enter": splitListItem(nodes.list_item),
    "Mod-[": liftListItem(nodes.list_item),
    "Mod-]": sinkListItem(nodes.list_item),
    "Tab": chainCommands(goToNextCell(1), sinkListItem(nodes.list_item)),
    "Shift-Tab": chainCommands(goToNextCell(-1), liftListItem(nodes.list_item)),
    "Alt-F10": () => instance.focusToolbar()
  };
}

// Copies the content styles (every rule in the page's stylesheets that mentions the content class) and the custom
// properties they use into an iframe, so the content looks the same in both edit modes while page styles stay out
function contentStyles(host) {
  const rules = [];
  for (const sheet of document.styleSheets) {
    let cssRules;
    try {
      cssRules = sheet.cssRules;
    } catch {
      continue;
    }
    collectRules(cssRules, rules);
  }
  const css = rules.join("\n");
  const computed = getComputedStyle(host);
  const variables = [...new Set(css.match(/--pixata-blazor-[a-z0-9-]+/gi) || [])]
    .map(name => [name, computed.getPropertyValue(name).trim()])
    .filter(([, value]) => value)
    .map(([name, value]) => `${name}: ${value};`);
  return `:root { ${variables.join(" ")} }\n${css}`;
}

function collectRules(cssRules, rules) {
  for (const rule of cssRules) {
    if (rule instanceof CSSStyleRule) {
      if (rule.selectorText.includes(contentClass)) {
        rules.push(rule.cssText);
      }
    } else if (rule instanceof CSSKeyframesRule) {
      if (/ProseMirror|pixata-blazor/.test(rule.name)) {
        rules.push(rule.cssText);
      }
    } else if (rule.cssRules) {
      const inner = [];
      collectRules(rule.cssRules, inner);
      if (!inner.length) {
        continue;
      }
      if (rule instanceof CSSMediaRule) {
        rules.push(`@media ${rule.media.mediaText} { ${inner.join("\n")} }`);
      } else if (window.CSSSupportsRule && rule instanceof CSSSupportsRule) {
        rules.push(`@supports ${rule.conditionText} { ${inner.join("\n")} }`);
      } else {
        rules.push(...inner);
      }
    }
  }
}

const iframeBaseStyles = "html, body { margin: 0; padding: 0; height: 100%; } body { overflow-y: auto; }";

function prepareIframe(iframe, host) {
  return new Promise(resolve => {
    iframe.addEventListener("load", () => {
      const doc = iframe.contentDocument;
      doc.documentElement.lang = document.documentElement.lang || "";
      doc.documentElement.dir = getComputedStyle(host).direction;
      const style = doc.createElement("style");
      style.textContent = `${iframeBaseStyles}\n${contentStyles(host)}`;
      doc.head.appendChild(style);
      const hostStyle = getComputedStyle(iframe);
      doc.body.style.fontFamily = hostStyle.fontFamily;
      doc.body.style.fontSize = hostStyle.fontSize;
      doc.body.style.lineHeight = hostStyle.lineHeight;
      doc.body.style.color = hostStyle.color;
      resolve(doc.body);
    }, { once: true });
    iframe.srcdoc = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body></body></html>";
  });
}

class EditorInstance {
  constructor(id, elements, dotNet, options) {
    this.id = id;
    this.host = elements.host;
    this.mount = elements.mount;
    this.toolbar = elements.toolbar;
    this.dotNet = dotNet;
    this.options = { ...options };
    this.pasteSettings = { ...defaultPasteSettings, ...(options.pasteSettings || {}) };
    this.lastValue = null;
    this.lastState = null;
    this.valueTimer = 0;
    this.stateTimer = 0;
    this.destroyed = false;
  }

  async init() {
    const parent = this.options.editMode === "iframe" ? await prepareIframe(this.mount, this.host) : this.mount;
    if (this.destroyed) {
      return;
    }
    this.view = new EditorView(parent, {
      state: this.createState(this.options.value),
      attributes: () => this.attributes(),
      editable: () => !this.options.readOnly,
      dispatchTransaction: tr => this.dispatch(tr),
      transformPastedHTML: html => cleanPastedHtml(html, this.pasteSettings),
      handleDOMEvents: {
        focus: () => {
          this.call("OnEditorFocus");
          return false;
        },
        blur: () => {
          this.flushValue();
          this.call("OnEditorBlur");
          return false;
        }
      }
    });
    this.lastValue = docToHtml(this.view.state.doc);
    if (this.options.editMode === "iframe") {
      this.initIframe(parent);
    }
    this.initToolbar();
    this.reportState();
  }

  // Tabbing to the iframe, or clicking below the content, puts the focus in the document
  initIframe(body) {
    const frame = this.mount;
    this.frameWindow = frame.contentWindow;
    this.onFrameFocus = () => {
      const active = frame.contentDocument ? frame.contentDocument.activeElement : null;
      if (this.view && (!active || active === body)) {
        this.view.focus();
      }
    };
    this.onFrameMouseDown = e => {
      if (this.view && (e.target === body || e.target === body.parentElement)) {
        e.preventDefault();
        this.view.focus();
      }
    };
    this.frameWindow.addEventListener("focus", this.onFrameFocus);
    body.parentElement.addEventListener("mousedown", this.onFrameMouseDown);
    this.frameBody = body;
  }

  createState(html) {
    return EditorState.create({
      doc: docFromHtml(html),
      plugins: [
        history(),
        keymap(buildKeymap(this)),
        keymap(baseKeymap),
        dropCursor(),
        gapCursor(),
        tableEditing(),
        placeholderPlugin(this)
      ]
    });
  }

  attributes() {
    const o = this.options;
    const attrs = {
      class: contentClass,
      role: "textbox",
      "aria-multiline": "true"
    };
    if (o.id && o.editMode !== "iframe") {
      attrs.id = o.id;
    }
    if (o.ariaLabel) {
      attrs["aria-label"] = o.ariaLabel;
    }
    if (o.ariaLabelledBy) {
      attrs["aria-labelledby"] = o.ariaLabelledBy;
    }
    if (o.ariaDescribedBy) {
      attrs["aria-describedby"] = o.ariaDescribedBy;
    }
    if (o.readOnly) {
      attrs["aria-readonly"] = "true";
    }
    if (o.invalid) {
      attrs["aria-invalid"] = "true";
    }
    if (o.spellCheck) {
      attrs.spellcheck = o.spellCheck;
    }
    return attrs;
  }

  dispatch(tr) {
    if (this.destroyed) {
      return;
    }
    this.view.updateState(this.view.state.apply(tr));
    if (tr.docChanged) {
      this.scheduleValue();
    }
    this.scheduleState();
  }

  call(method, ...args) {
    if (this.destroyed || !this.dotNet) {
      return;
    }
    try {
      this.dotNet.invokeMethodAsync(method, ...args).catch(() => { });
    } catch {
      // The .NET object has gone (the component was disposed or the circuit closed)
    }
  }

  // Value

  scheduleValue() {
    clearTimeout(this.valueTimer);
    const delay = Math.max(0, this.options.debounceDelay || 0);
    this.valueTimer = setTimeout(() => this.flushValue(), delay);
  }

  flushValue() {
    clearTimeout(this.valueTimer);
    this.valueTimer = 0;
    if (!this.view) {
      return;
    }
    const html = docToHtml(this.view.state.doc);
    if (html !== this.lastValue) {
      this.lastValue = html;
      this.call("OnEditorValueChanged", html);
    }
  }

  setValue(html) {
    const value = html || "";
    if (!this.view || value === this.lastValue) {
      return;
    }
    clearTimeout(this.valueTimer);
    this.view.updateState(this.createState(value));
    this.lastValue = value;
    this.reportState();
  }

  getHtml() {
    return this.view ? docToHtml(this.view.state.doc) : "";
  }

  // Selection state, so the toolbar can show which tools are on and which can't be used

  scheduleState() {
    if (this.stateTimer) {
      return;
    }
    this.stateTimer = setTimeout(() => {
      this.stateTimer = 0;
      this.reportState();
    }, 30);
  }

  reportState() {
    if (!this.view || this.destroyed) {
      return;
    }
    const state = this.view.state;
    const style = textStyleAt(state);
    const result = {
      active: activeCommands(state),
      disabled: conditionalCommands.filter(name => !commands[name]({})(state)),
      format: currentFormat(state),
      fontFamily: style.fontFamily || null,
      fontSize: style.fontSize || null,
      foreColor: style.color || null,
      backColor: style.backgroundColor || null
    };
    const json = JSON.stringify(result);
    if (json !== this.lastState) {
      this.lastState = json;
      this.call("OnEditorStateChanged", json);
    }
  }

  // Commands

  execute(name, args) {
    if (!this.view || this.options.readOnly) {
      return false;
    }
    const factory = commands[name];
    if (!factory) {
      return false;
    }
    const result = factory(args || {})(this.view.state, tr => this.dispatch(tr));
    if (!args || args.shouldFocusView !== false) {
      this.view.focus();
    }
    return result;
  }

  focus() {
    if (this.view) {
      this.view.focus();
    }
  }

  update(options) {
    const previous = this.options;
    this.options = { ...previous, ...options };
    if (options.pasteSettings) {
      this.pasteSettings = { ...defaultPasteSettings, ...options.pasteSettings };
    }
    if (this.view) {
      // Setting the props again makes the view re-read editable and attributes, and redraw the placeholder
      this.view.setProps({});
    }
    if (previous.adaptive !== this.options.adaptive) {
      this.measureToolbar();
    }
  }

  // Toolbar: a single tab stop with the arrow keys moving between the tools, and (when adaptive) the tools that don't fit
  // moved into the overflow menu

  initToolbar() {
    const toolbar = this.toolbar;
    if (!toolbar) {
      return;
    }
    this.onToolbarKeyDown = e => this.toolbarKeyDown(e);
    this.onToolbarFocusIn = e => {
      const tool = this.toolElement(e.target);
      if (tool) {
        this.currentTool = tool;
        this.updateTabStops();
      }
    };
    toolbar.addEventListener("keydown", this.onToolbarKeyDown);
    toolbar.addEventListener("focusin", this.onToolbarFocusIn);
    const scheduleMeasure = () => {
      if (!this.measureFrame) {
        this.measureFrame = requestAnimationFrame(() => {
          this.measureFrame = 0;
          this.measureToolbar();
        });
      }
    };
    if (window.ResizeObserver) {
      this.resizeObserver = new ResizeObserver(scheduleMeasure);
    }
    const observeItems = () => {
      if (this.resizeObserver) {
        this.resizeObserver.disconnect();
        this.resizeObserver.observe(toolbar);
        for (const item of this.toolbarItems()) {
          this.resizeObserver.observe(item);
        }
      }
    };
    this.mutationObserver = new MutationObserver(() => {
      observeItems();
      this.updateTabStops();
      scheduleMeasure();
    });
    this.mutationObserver.observe(toolbar, { childList: true, subtree: true, attributes: true, attributeFilter: ["class", "disabled"] });
    observeItems();
    this.updateTabStops();
    this.measureToolbar();
  }

  toolbarItems() {
    return [...this.toolbar.querySelectorAll(":scope > .pixata-blazor-editor-toolbar-item")];
  }

  isHidden(element) {
    return !!element.closest(".pixata-blazor-editor-toolbar-item-hidden, .pixata-blazor-editor-toolbar-overflow-hidden, .pixata-blazor-popup");
  }

  focusableTools() {
    const selector = "[data-pixata-editor-tool]:not(:disabled), .pixata-blazor-editor-custom-tool :is(button, input, select, textarea, a[href]):not(:disabled)";
    return [...this.toolbar.querySelectorAll(selector)].filter(t => !this.isHidden(t));
  }

  toolElement(target) {
    if (!target || !this.toolbar.contains(target)) {
      return null;
    }
    return this.focusableTools().find(t => t === target) || null;
  }

  updateTabStops() {
    if (!this.toolbar) {
      return;
    }
    const tools = this.focusableTools();
    if (!tools.includes(this.currentTool)) {
      this.currentTool = tools[0] || null;
    }
    for (const tool of tools) {
      const tabIndex = tool === this.currentTool ? "0" : "-1";
      if (tool.getAttribute("tabindex") !== tabIndex) {
        tool.setAttribute("tabindex", tabIndex);
      }
    }
  }

  toolbarKeyDown(e) {
    const tool = this.toolElement(e.target);
    // A tool whose drop-down is open uses the keys itself
    if (!tool || tool.getAttribute("aria-expanded") === "true") {
      return;
    }
    const tools = this.focusableTools();
    const index = tools.indexOf(tool);
    const rtl = getComputedStyle(this.toolbar).direction === "rtl";
    let next = -1;
    switch (e.key) {
      case "ArrowRight":
        next = rtl ? index - 1 : index + 1;
        break;
      case "ArrowLeft":
        next = rtl ? index + 1 : index - 1;
        break;
      case "Home":
        next = 0;
        break;
      case "End":
        next = tools.length - 1;
        break;
      case "Escape":
        if (!e.defaultPrevented) {
          this.focus();
        }
        return;
      default:
        return;
    }
    e.preventDefault();
    next = (next + tools.length) % tools.length;
    this.currentTool = tools[next];
    this.updateTabStops();
    tools[next].focus();
  }

  focusToolbar() {
    if (!this.toolbar) {
      return false;
    }
    this.updateTabStops();
    if (this.currentTool) {
      this.currentTool.focus();
      return true;
    }
    return false;
  }

  measureToolbar() {
    const toolbar = this.toolbar;
    if (!toolbar || !this.options.adaptive || this.destroyed) {
      return;
    }
    const items = this.toolbarItems();
    const overflow = toolbar.querySelector(":scope > .pixata-blazor-editor-toolbar-overflow");
    const style = getComputedStyle(toolbar);
    const gap = parseFloat(style.columnGap) || 0;
    const available = toolbar.clientWidth - (parseFloat(style.paddingLeft) || 0) - (parseFloat(style.paddingRight) || 0);
    const widths = items.map(i => i.getBoundingClientRect().width);
    const total = widths.reduce((sum, w) => sum + w, 0) + gap * Math.max(0, widths.length - 1);
    let count = items.length;
    if (total > available + 0.5) {
      const reserve = (overflow ? overflow.getBoundingClientRect().width : 0) + gap;
      let used = 0;
      count = 0;
      for (const width of widths) {
        const next = used + (count ? gap : 0) + width;
        if (next + reserve > available + 0.5) {
          break;
        }
        used = next;
        count++;
      }
    }
    if (count !== this.visibleTools) {
      this.visibleTools = count;
      this.call("OnToolbarOverflow", count);
    }
  }

  destroy() {
    this.destroyed = true;
    clearTimeout(this.valueTimer);
    clearTimeout(this.stateTimer);
    if (this.measureFrame) {
      cancelAnimationFrame(this.measureFrame);
    }
    if (this.toolbar && this.onToolbarKeyDown) {
      this.toolbar.removeEventListener("keydown", this.onToolbarKeyDown);
      this.toolbar.removeEventListener("focusin", this.onToolbarFocusIn);
    }
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
    if (this.frameWindow) {
      try {
        this.frameWindow.removeEventListener("focus", this.onFrameFocus);
        this.frameBody.parentElement.removeEventListener("mousedown", this.onFrameMouseDown);
      } catch {
        // The iframe has already gone
      }
      this.frameWindow = null;
    }
    if (this.view) {
      this.view.destroy();
      this.view = null;
    }
    this.dotNet = null;
  }
}

// The functions .NET calls

export async function create(host, mount, toolbar, dotNet, options, value) {
  if (!host || !mount) {
    return 0;
  }
  const id = nextId++;
  const instance = new EditorInstance(id, { host, mount, toolbar }, dotNet, { ...(options || {}), value: value || "" });
  instances.set(id, instance);
  await instance.init();
  return id;
}

export function destroy(id) {
  const instance = instances.get(id);
  if (instance) {
    instance.destroy();
    instances.delete(id);
  }
}

export function setValue(id, html) {
  instances.get(id)?.setValue(html);
}

export function update(id, options) {
  instances.get(id)?.update(options || {});
}

export function execute(id, name, args) {
  return instances.get(id)?.execute(name, args) || false;
}

export function focus(id) {
  instances.get(id)?.focus();
}

export function getHtml(id, formatted) {
  const html = instances.get(id)?.getHtml() || "";
  return formatted ? formatHtml(html) : html;
}

export function getLinkInfo(id) {
  const instance = instances.get(id);
  return instance && instance.view ? linkInfo(instance.view.state) : { href: "", title: "", target: "", text: "" };
}

export function getImageInfo(id) {
  const instance = instances.get(id);
  return instance && instance.view ? imageInfo(instance.view.state) : { src: "", alt: "", width: "", height: "" };
}

export function flush(id) {
  instances.get(id)?.flushValue();
}

// Keeps Tab and Shift+Tab inside a modal dialog. Returns a handle for releaseFocus
const traps = new Map();
let nextTrap = 1;

export function trapFocus(element) {
  if (!element) {
    return 0;
  }
  const handler = e => {
    if (e.key !== "Tab") {
      return;
    }
    const focusable = [...element.querySelectorAll("button:not([disabled]), input:not([disabled]), textarea:not([disabled]), select:not([disabled]), a[href], [tabindex]:not([tabindex='-1'])")]
      .filter(f => f.offsetParent !== null);
    if (!focusable.length) {
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  };
  element.addEventListener("keydown", handler);
  const id = nextTrap++;
  traps.set(id, { element, handler });
  return id;
}

export function releaseFocus(id) {
  const trap = traps.get(id);
  if (trap) {
    trap.element.removeEventListener("keydown", trap.handler);
    traps.delete(id);
  }
}

// Exposed for the tests in build/pixata-editor/test, and for anyone who wants to clean HTML the way the editor does
export { sanitiseHtml, cleanPastedHtml, formatHtml };
