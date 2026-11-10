// The browser half of PixataPdfViewer. The .NET component renders the toolbar and an empty container, and this module
// loads the PDF into PDF.js's viewer in that container, tells the component what's happening (the page, the zoom, search
// matches), and does what the toolbar asks (go to a page, zoom, rotate, search, download, print)
import "./global.js";
import { AnnotationEditorType, AnnotationMode, getDocument, GlobalWorkerOptions } from "pdfjs-dist";
import { EventBus, FindState, LinkTarget, PDFFindController, PDFLinkService, PDFViewer } from "pdfjs-dist/web/pdf_viewer.mjs";

// The files PDF.js needs are next to this module. The query string it was loaded with changes with each build of the
// package, so it's added to the worker's URL, as a browser mustn't use an old worker with a new library
const moduleUrl = new URL(import.meta.url);
const asset = path => new URL(path, moduleUrl).href;
GlobalWorkerOptions.workerSrc = asset(`pdf.worker.min.mjs${moduleUrl.search}`);

// PDF.js's styles are added at the start of the page's head, so the page's own styles win over them
if (!document.querySelector("link[data-pixata-pdfviewer]")) {
  const link = document.createElement("link");
  link.rel = "stylesheet";
  link.href = asset(`pdf_viewer.css${moduleUrl.search}`);
  link.dataset.pixataPdfviewer = "";
  document.head.prepend(link);
}

// Fetches the character maps, fonts and image decoders PDF.js asks for. It's PDF.js's own way of doing it, except that the
// character maps have .bin on the end of their names, as ASP.NET Core won't serve a .bcmap file
class BinaryDataFactory {
  constructor({ cMapUrl, standardFontDataUrl, wasmUrl }) {
    this.cMapUrl = cMapUrl;
    this.standardFontDataUrl = standardFontDataUrl;
    this.wasmUrl = wasmUrl;
  }

  async fetch({ kind, filename }) {
    const url = `${this[kind]}${filename}${kind === "cMapUrl" ? ".bin" : ""}`;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Unable to load ${url}`);
    }
    return new Uint8Array(await response.arrayBuffer());
  }
}

const documentOptions = {
  cMapUrl: asset("cmaps/"),
  cMapPacked: true,
  standardFontDataUrl: asset("standard_fonts/"),
  wasmUrl: asset("wasm/"),
  BinaryDataFactory
};

const presets = ["page-width", "page-fit", "page-actual", "auto"];
const viewers = new Map();

const call = (state, method, ...args) =>
  state.dotNet.invokeMethodAsync(method, ...args).catch(() => { });

// Sets up the viewer in the container. The PDF is loaded separately, with openUrl or openData
export function create(key, containerId, dotNet, optionsJson) {
  const container = document.getElementById(containerId);
  if (!container || viewers.has(key)) {
    return;
  }
  const options = JSON.parse(optionsJson);
  const abort = new AbortController();
  const eventBus = new EventBus();
  const linkService = new PDFLinkService({
    eventBus,
    externalLinkTarget: LinkTarget.BLANK,
    externalLinkRel: "noopener noreferrer nofollow"
  });
  const findController = new PDFFindController({ eventBus, linkService, updateMatchesCountOnProgress: true });
  const viewer = new PDFViewer({
    container,
    viewer: container.firstElementChild,
    eventBus,
    linkService,
    findController,
    textLayerMode: 1,
    annotationMode: AnnotationMode.ENABLE,
    annotationEditorMode: AnnotationEditorType.DISABLE,
    abortSignal: abort.signal
  });
  linkService.setViewer(viewer);
  const state = { key, container, dotNet, eventBus, linkService, findController, viewer, abort, zoom: options.zoom, task: null, document: null };
  viewers.set(key, state);

  eventBus.on("pagesinit", () => {
    viewer.currentScaleValue = state.zoom;
    if (options.page > 1) {
      viewer.currentPageNumber = Math.min(options.page, viewer.pagesCount);
    }
  });
  eventBus.on("pagechanging", ({ pageNumber }) => call(state, "PageShown", pageNumber));
  // Turning the pages changes their heights, so the page that was being shown is scrolled back to
  eventBus.on("rotationchanging", ({ pageNumber }) => {
    viewer.currentPageNumber = pageNumber;
  });
  eventBus.on("scalechanging", ({ scale, presetValue }) => {
    state.zoom = presetValue || String(scale);
    call(state, "ScaleChanged", scale, presetValue || null);
  });
  const findChanged = matches => call(state, "FindChanged", matches?.current ?? 0, matches?.total ?? 0);
  eventBus.on("updatefindmatchescount", ({ matchesCount }) => findChanged(matchesCount));
  eventBus.on("updatefindcontrolstate", ({ state: found, matchesCount }) => {
    if (found !== FindState.PENDING) {
      findChanged(matchesCount);
    }
  });

  // A zoom that fits the page to the container is worked out again when the container changes size
  let size = "";
  const resize = new ResizeObserver(() => {
    const now = `${container.offsetWidth}x${container.offsetHeight}`;
    if (now !== size) {
      size = now;
      if (state.document && presets.includes(viewer.currentScaleValue)) {
        viewer.currentScaleValue = viewer.currentScaleValue;
      }
    }
  });
  resize.observe(container);
  abort.signal.addEventListener("abort", () => resize.disconnect());

  // Ctrl and the mouse wheel (or a pinch on a touchpad) zoom the PDF, around the pointer, rather than the whole page
  let wheel = 0;
  container.addEventListener("wheel", e => {
    if (!e.ctrlKey && !e.metaKey) {
      return;
    }
    e.preventDefault();
    wheel += e.deltaMode === 1 ? e.deltaY * 30 : e.deltaMode === 2 ? e.deltaY * 300 : e.deltaY;
    const steps = Math.trunc(wheel / 80);
    if (steps !== 0) {
      wheel -= steps * 80;
      const rect = container.getBoundingClientRect();
      viewer.updateScale({ steps: -steps, origin: [e.clientX - rect.left, e.clientY - rect.top], drawingDelay: 400 });
    }
  }, { passive: false, signal: abort.signal });
}

function close(state) {
  state.task?.destroy();
  state.task = null;
  state.document = null;
  state.findController.setDocument(null);
  state.viewer.setDocument(null);
  state.linkService.setDocument(null);
}

async function open(state, source) {
  close(state);
  const task = getDocument({ ...documentOptions, ...source });
  state.task = task;
  try {
    const pdf = await task.promise;
    if (state.task !== task) {
      return;
    }
    state.document = pdf;
    state.viewer.setDocument(pdf);
    state.linkService.setDocument(pdf, null);
    state.findController.setDocument(pdf);
    call(state, "Loaded", pdf.numPages);
  } catch (ex) {
    if (state.task === task) {
      const kind = ex?.name === "PasswordException" ? "password"
        : ex?.name === "InvalidPDFException" ? "invalid"
          : ex?.name === "ResponseException" && ex.missing ? "missing"
            : "other";
      call(state, "Failed", kind, String(ex?.message ?? ex));
    }
  }
}

export function openUrl(key, url) {
  const state = viewers.get(key);
  return state ? open(state, { url }) : Promise.resolve();
}

// The PDF's bytes, sent from .NET as a stream, so a large file doesn't have to fit in one message
export async function openData(key, stream) {
  const state = viewers.get(key);
  if (state) {
    const data = new Uint8Array(await stream.arrayBuffer());
    if (viewers.get(key) === state) {
      await open(state, { data });
    }
  }
}

// Shows nothing, when the component has no PDF to show
export function clear(key) {
  const state = viewers.get(key);
  if (state) {
    close(state);
  }
}

// The zoom is page-width, page-fit, page-actual, auto or a scale, where 1 is 100%
export function setZoom(key, zoom) {
  const state = viewers.get(key);
  if (state) {
    state.zoom = zoom;
    if (state.document) {
      state.viewer.currentScaleValue = zoom;
    }
  }
}

export function zoomBy(key, steps) {
  const state = viewers.get(key);
  if (state?.document) {
    state.viewer.updateScale({ steps });
  }
}

export function goToPage(key, page) {
  const state = viewers.get(key);
  if (state?.document && page >= 1 && page <= state.viewer.pagesCount) {
    state.viewer.currentPageNumber = page;
  }
}

// Turns the pages a quarter turn clockwise (1) or anticlockwise (-1)
export function rotate(key, quarters) {
  const state = viewers.get(key);
  if (state?.document) {
    state.viewer.pagesRotation = ((state.viewer.pagesRotation + quarters * 90) % 360 + 360) % 360;
  }
}

// Searches for the text, highlighting every match and scrolling to the next (or previous) one
export function find(key, query, again, previous) {
  const state = viewers.get(key);
  if (!state?.document) {
    return;
  }
  if (!query) {
    state.eventBus.dispatch("findbarclose", { source: null });
    call(state, "FindChanged", 0, 0);
    return;
  }
  state.eventBus.dispatch("find", {
    source: null,
    type: again ? "again" : "",
    query,
    caseSensitive: false,
    entireWord: false,
    highlightAll: true,
    findPrevious: previous,
    matchDiacritics: false
  });
}

export async function download(key, fileName) {
  const pdf = viewers.get(key)?.document;
  if (!pdf) {
    return;
  }
  const url = URL.createObjectURL(new Blob([await pdf.getData()], { type: "application/pdf" }));
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
}

const printFrameId = "pixata-pdfviewer-print";

// Draws each page as an image at 150 dots per inch, and prints them from a hidden frame. That works the same in every
// browser, which handing the PDF itself to the browser's own viewer doesn't
export async function print(key) {
  const state = viewers.get(key);
  const pdf = state?.document;
  if (!pdf) {
    return;
  }
  const rotation = state.viewer.pagesRotation;
  const images = [];
  let pageSize = null;
  for (let number = 1; number <= pdf.numPages; number++) {
    const page = await pdf.getPage(number);
    const turned = (page.rotate + rotation) % 360;
    if (!pageSize) {
      const actual = page.getViewport({ scale: 1, rotation: turned });
      pageSize = `${actual.width}pt ${actual.height}pt`;
    }
    const viewport = page.getViewport({ scale: 150 / 72, rotation: turned });
    const canvas = document.createElement("canvas");
    canvas.width = Math.floor(viewport.width);
    canvas.height = Math.floor(viewport.height);
    const context = canvas.getContext("2d");
    context.fillStyle = "#fff";
    context.fillRect(0, 0, canvas.width, canvas.height);
    await page.render({ canvasContext: context, canvas, viewport, intent: "print", annotationMode: AnnotationMode.ENABLE_FORMS }).promise;
    const blob = await new Promise(resolve => canvas.toBlob(resolve));
    images.push(URL.createObjectURL(blob));
    canvas.width = canvas.height = 0;
    page.cleanup();
  }
  document.getElementById(printFrameId)?.remove();
  const returnFocusTo = document.activeElement;
  const frame = document.createElement("iframe");
  frame.id = printFrameId;
  frame.setAttribute("sandbox", "allow-same-origin allow-modals");
  frame.setAttribute("aria-hidden", "true");
  frame.tabIndex = -1;
  frame.style.cssText = "position: fixed; right: 0; bottom: 0; width: 0; height: 0; border: 0; visibility: hidden";
  const done = () => {
    images.forEach(url => URL.revokeObjectURL(url));
    frame.remove();
    if (returnFocusTo instanceof HTMLElement && returnFocusTo.isConnected) {
      returnFocusTo.focus();
    }
  };
  frame.addEventListener("load", async () => {
    const printing = frame.contentWindow;
    await Promise.all([...frame.contentDocument.images].map(image => image.decode().catch(() => { })));
    printing.addEventListener("afterprint", () => setTimeout(done, 0), { once: true });
    printing.focus();
    printing.print();
  }, { once: true });
  frame.srcdoc = `<!DOCTYPE html><html><head><style>
@page { size: ${pageSize}; margin: 0; }
html, body { margin: 0; padding: 0; height: 100%; }
div { display: flex; align-items: center; justify-content: center; width: 100%; height: 100%; overflow: hidden; break-after: page; }
div:last-child { break-after: auto; }
img { max-width: 100%; max-height: 100%; }
</style></head><body>${images.map(url => `<div><img src="${url}" alt=""></div>`).join("")}</body></html>`;
  document.body.appendChild(frame);
}

export function destroy(key) {
  const state = viewers.get(key);
  if (state) {
    viewers.delete(key);
    close(state);
    state.abort.abort();
  }
}
