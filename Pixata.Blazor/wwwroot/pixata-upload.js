// Browser side of PixataFileSelect and PixataUpload: picking and dropping files, keeping hold of them so .NET can read them,
// and posting them to a server with XMLHttpRequest (which, unlike fetch, reports upload progress). Loaded once per circuit
// (or once per WASM app) by PixataUploadJs

const instances = new Map();
let nextInstanceId = 1;
let nextFileId = 1;

// How long after the last dragover event the "files are being dragged" state is cleared. The browser fires dragover every
// 50ms or so while files are dragged over the page, so this avoids having to count dragenter and dragleave events
const dragIdleMs = 150;

const listKeys = ["ArrowDown", "ArrowUp", "Home", "End", " "];

export function init(root, input, button, dotNet, options) {
  if (!root || !input) {
    return 0;
  }
  const id = nextInstanceId++;
  const state = { id, root, input, button, dotNet, options: options || {}, files: new Map(), requests: new Map(), dragTimer: 0 };

  // Keyboard users focus the button; clicking it (or pressing Enter or Space) opens the browser's file dialog. Mouse and
  // touch users click the input itself, which covers the button
  state.onButtonClick = () => {
    if (state.options.enabled !== false) {
      input.click();
    }
  };
  state.onButtonKeyDown = e => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
    }
  };
  state.onChange = () => {
    const files = [...(input.files || [])];
    // Clearing the value means choosing the same file again still raises a change event
    input.value = "";
    addFiles(state, files);
  };
  // The arrow keys (and so on) move between the files in the list, so they mustn't scroll the page as well
  state.onListKeyDown = e => {
    if (listKeys.includes(e.key) && e.target && e.target.matches && e.target.matches("li[data-index]") && root.contains(e.target)) {
      e.preventDefault();
    }
  };
  state.onDragOver = e => dragOver(state, e);
  state.onDrop = e => drop(state, e);

  if (button) {
    button.addEventListener("click", state.onButtonClick);
    button.addEventListener("keydown", state.onButtonKeyDown);
  }
  input.addEventListener("change", state.onChange);
  root.addEventListener("keydown", state.onListKeyDown);
  document.addEventListener("dragenter", state.onDragOver);
  document.addEventListener("dragover", state.onDragOver);
  document.addEventListener("drop", state.onDrop);
  instances.set(id, state);
  return id;
}

// Called when the component's parameters change
export function update(id, options) {
  const state = instances.get(id);
  if (state) {
    state.options = options || {};
  }
}

export function openDialog(id) {
  const state = instances.get(id);
  if (state && state.options.enabled !== false) {
    state.input.click();
  }
}

// Returns the file itself, which .NET receives as an IJSStreamReference and reads as a stream
export function getFile(id, fileId) {
  const state = instances.get(id);
  const file = state ? state.files.get(fileId) : null;
  if (!file) {
    throw new Error(`The file ${fileId} is no longer available`);
  }
  return file;
}

// Forgets files that have been removed from the list, so the browser can release them
export function forgetFiles(id, fileIds) {
  const state = instances.get(id);
  if (state) {
    (fileIds || []).forEach(fileId => state.files.delete(fileId));
  }
}

export function dispose(id) {
  const state = instances.get(id);
  if (!state) {
    return;
  }
  if (state.button) {
    state.button.removeEventListener("click", state.onButtonClick);
    state.button.removeEventListener("keydown", state.onButtonKeyDown);
  }
  state.input.removeEventListener("change", state.onChange);
  state.root.removeEventListener("keydown", state.onListKeyDown);
  document.removeEventListener("dragenter", state.onDragOver);
  document.removeEventListener("dragover", state.onDragOver);
  document.removeEventListener("drop", state.onDrop);
  clearDragState(state);
  state.requests.forEach(request => {
    request.cancelled = true;
    if (request.xhr) {
      request.xhr.abort();
    }
    clearTimeout(request.retryTimer);
  });
  state.requests.clear();
  state.files.clear();
  instances.delete(id);
}

function addFiles(state, files) {
  if (!files.length || state.options.enabled === false) {
    return;
  }
  if (!state.options.multiple) {
    files = files.slice(0, 1);
  }
  const selected = files.map(file => {
    const fileId = `f${nextFileId++}`;
    state.files.set(fileId, file);
    return { id: fileId, name: file.name, size: file.size, contentType: file.type || "", lastModified: file.lastModified || 0 };
  });
  state.dotNet.invokeMethodAsync("OnFilesSelected", selected).catch(() => { });
}

// Dragging and dropping

function hasFiles(e) {
  return !!e.dataTransfer && [...(e.dataTransfer.types || [])].includes("Files");
}

// The element the files would be dropped on: the component itself, or the external drop zone named by DropZoneId
function dropTarget(state, e) {
  if (state.options.enabled === false || !e.target || !e.target.closest) {
    return null;
  }
  if (state.root.contains(e.target)) {
    return state.root;
  }
  const zoneId = state.options.dropZoneId;
  if (zoneId) {
    const zone = e.target.closest(`#${CSS.escape(zoneId)}`);
    if (zone && zone.dataset.pixataDisabled !== "true") {
      return zone;
    }
  }
  return null;
}

function externalZone(state) {
  return state.options.dropZoneId ? document.getElementById(state.options.dropZoneId) : null;
}

function dragOver(state, e) {
  if (!hasFiles(e)) {
    return;
  }
  const target = dropTarget(state, e);
  const zone = externalZone(state);
  state.root.classList.add("pixata-blazor-upload-drag-active");
  state.root.classList.toggle("pixata-blazor-upload-drag-hover", target === state.root);
  if (zone) {
    zone.classList.add("pixata-blazor-external-dropzone-active");
    setZoneHover(zone, target === zone);
  }
  if (target) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
  }
  clearTimeout(state.dragTimer);
  state.dragTimer = setTimeout(() => clearDragState(state), dragIdleMs);
}

function setZoneHover(zone, hover) {
  zone.classList.toggle("pixata-blazor-external-dropzone-hover", hover);
  const extra = (zone.dataset.pixataDragoverClass || "").split(" ").filter(c => c);
  extra.forEach(c => zone.classList.toggle(c, hover));
}

function clearDragState(state) {
  clearTimeout(state.dragTimer);
  state.root.classList.remove("pixata-blazor-upload-drag-active", "pixata-blazor-upload-drag-hover");
  const zone = externalZone(state);
  if (zone) {
    zone.classList.remove("pixata-blazor-external-dropzone-active");
    setZoneHover(zone, false);
  }
}

function drop(state, e) {
  const target = dropTarget(state, e);
  clearDragState(state);
  if (!target || !hasFiles(e)) {
    return;
  }
  e.preventDefault();
  let files = [...e.dataTransfer.files];
  if (target !== state.root && target.dataset.pixataMultiple === "false") {
    files = files.slice(0, 1);
  }
  addFiles(state, files);
}

// Sending requests

// Sends a request, reporting progress and the result to .NET through OnRequestProgress and OnRequestComplete. The request
// is either an upload (fileId set: the file, or its chunks, go in the form field named by field) or a remove (values set:
// each one goes in the form field named by field)
export function send(id, requestId, request) {
  const state = instances.get(id);
  if (!state) {
    return;
  }
  const job = { requestId, request, file: null, chunkIndex: 0, totalChunks: 1, retries: 0, lastPercent: -1, xhr: null, paused: false, cancelled: false, retryTimer: 0 };
  if (request.fileId) {
    job.file = state.files.get(request.fileId);
    if (!job.file) {
      complete(state, job, { status: 0, statusText: "The file is no longer available", responseType: "text", responseText: "", success: false });
      return;
    }
    if (isChunked(job)) {
      job.totalChunks = Math.max(1, Math.ceil(job.file.size / request.chunk.size));
    }
  }
  state.requests.set(requestId, job);
  sendNext(state, job);
}

// Pausing lets the chunk being sent finish, and then stops. Aborting it instead would mean sending it again on resume, and
// if it was the last chunk and the server had already received it, the server would have finished the file, and would
// reject the chunk sent again
export function pause(id, requestId) {
  const job = findJob(id, requestId);
  if (job && !job.paused) {
    job.paused = true;
    // A failed chunk waiting to be sent again waits for the upload to be resumed instead
    clearTimeout(job.retryTimer);
    job.retryTimer = 0;
  }
}

export function resume(id, requestId) {
  const state = instances.get(id);
  const job = findJob(id, requestId);
  if (state && job && job.paused) {
    job.paused = false;
    // If a chunk is still on its way, the next one is sent when it has finished
    if (!job.xhr) {
      sendNext(state, job);
    }
  }
}

export function cancel(id, requestId) {
  const state = instances.get(id);
  const job = findJob(id, requestId);
  if (state && job) {
    job.cancelled = true;
    clearTimeout(job.retryTimer);
    if (job.xhr) {
      job.xhr.abort();
    }
    complete(state, job, { status: 0, statusText: "Cancelled", responseType: "text", responseText: "", success: false, cancelled: true });
  }
}

function findJob(id, requestId) {
  const state = instances.get(id);
  return state ? state.requests.get(requestId) : null;
}

function isChunked(job) {
  const chunk = job.request.chunk;
  return !!(job.file && chunk && chunk.size > 0);
}

function sendNext(state, job) {
  if (job.cancelled || job.paused) {
    return;
  }
  try {
    post(state, job);
  } catch (e) {
    // The browser wouldn't send the request, eg because a header name or the URL isn't valid
    job.xhr = null;
    complete(state, job, { status: 0, statusText: (e && e.message) || String(e), responseType: "text", responseText: "", success: false });
  }
}

function post(state, job) {
  const request = job.request;
  const form = new FormData();
  Object.entries(request.data || {}).forEach(([key, value]) => form.append(key, value));
  let chunkStart = 0;
  let chunkSize = 0;
  if (job.file && isChunked(job)) {
    chunkStart = job.chunkIndex * request.chunk.size;
    chunkSize = Math.min(request.chunk.size, job.file.size - chunkStart);
    form.append(request.chunk.metadataField, JSON.stringify({
      fileId: request.uploadId,
      fileName: job.file.name,
      fileSize: job.file.size,
      contentType: job.file.type || "",
      chunkIndex: job.chunkIndex,
      totalChunks: job.totalChunks,
      offset: chunkStart
    }));
    form.append(request.field, job.file.slice(chunkStart, chunkStart + chunkSize), job.file.name);
  } else if (job.file) {
    chunkSize = job.file.size;
    form.append(request.field, job.file, job.file.name);
  } else {
    (request.values || []).forEach(value => form.append(request.field, value));
  }

  const xhr = new XMLHttpRequest();
  job.xhr = xhr;
  xhr.open("POST", request.url, true);
  xhr.withCredentials = !!request.withCredentials;
  Object.entries(request.headers || {}).forEach(([key, value]) => xhr.setRequestHeader(key, value));
  if (job.file) {
    xhr.upload.onprogress = e => {
      if (!e.lengthComputable || !job.file.size) {
        return;
      }
      const sent = chunkStart + chunkSize * (e.loaded / e.total);
      reportProgress(state, job, Math.floor(sent * 100 / job.file.size));
    };
  }
  xhr.onload = () => {
    if (job.cancelled) {
      return;
    }
    job.xhr = null;
    const ok = xhr.status >= 200 && xhr.status < 300;
    if (ok && job.file && isChunked(job) && job.chunkIndex < job.totalChunks - 1) {
      job.chunkIndex++;
      job.retries = 0;
      sendNext(state, job);
      return;
    }
    if (!ok && retry(state, job)) {
      return;
    }
    if (ok && job.file) {
      reportProgress(state, job, 100);
    }
    complete(state, job, result(xhr, ok));
  };
  xhr.onerror = () => {
    if (job.cancelled) {
      return;
    }
    job.xhr = null;
    if (!retry(state, job)) {
      complete(state, job, result(xhr, false));
    }
  };
  xhr.send(form);
}

// A chunk that fails is sent again, after a pause, up to the number of times allowed
function retry(state, job) {
  const chunk = job.request.chunk;
  if (!isChunked(job) || job.retries >= (chunk.maxAutoRetries || 0)) {
    return false;
  }
  job.retries++;
  // A paused upload sends the chunk again when it is resumed
  if (!job.paused) {
    job.retryTimer = setTimeout(() => {
      job.retryTimer = 0;
      sendNext(state, job);
    }, chunk.autoRetryAfter || 0);
  }
  return true;
}

function reportProgress(state, job, percent) {
  percent = Math.max(0, Math.min(100, percent));
  if (percent !== job.lastPercent) {
    job.lastPercent = percent;
    state.dotNet.invokeMethodAsync("OnRequestProgress", job.requestId, percent).catch(() => { });
  }
}

function result(xhr, success) {
  let responseText = "";
  try {
    responseText = xhr.responseText || "";
  } catch {
    // responseText throws if responseType isn't text
  }
  return { status: xhr.status, statusText: xhr.statusText || "", responseType: xhr.responseType || "text", responseText, success };
}

function complete(state, job, outcome) {
  state.requests.delete(job.requestId);
  state.dotNet.invokeMethodAsync("OnRequestComplete", job.requestId, outcome).catch(() => { });
}
