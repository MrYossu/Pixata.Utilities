// Browser helpers for PixataScheduler: dragging appointments to move them, dragging their edges to resize them, and
// scrolling the views. Pointer events are used throughout, so this works with a mouse, a pen or a finger. On touch screens an
// appointment has to be selected (tapped) before it can be dragged, so that swiping over the scheduler still scrolls it

const schedulers = new Map();
let nextId = 1;

const dragThreshold = 4;
const edgeScrollZone = 32;
const edgeScrollStep = 12;

export function attach(root, dotNet) {
  if (!root) {
    return 0;
  }
  const id = nextId++;
  const state = { root, dotNet, drag: null };
  state.onPointerDown = e => pointerDown(state, e);
  root.addEventListener("pointerdown", state.onPointerDown);
  schedulers.set(id, state);
  return id;
}

export function detach(id) {
  const state = schedulers.get(id);
  if (!state) {
    return;
  }
  endDrag(state, null);
  state.root.removeEventListener("pointerdown", state.onPointerDown);
  schedulers.delete(id);
}

// Scrolls a view's content so that the element matching the selector (eg the start of the working day) is at the top, or
// (when horizontal is true) at the start
export function scrollToSelector(container, selector, horizontal) {
  if (!container) {
    return;
  }
  const target = container.querySelector(selector);
  if (!target) {
    return;
  }
  const containerRect = container.getBoundingClientRect();
  const targetRect = target.getBoundingClientRect();
  if (horizontal) {
    const sticky = container.querySelector(".pixata-blazor-scheduler-side-cell");
    const offset = sticky ? sticky.getBoundingClientRect().width : 0;
    const rtl = getComputedStyle(container).direction === "rtl";
    container.scrollLeft += rtl ? targetRect.right - (containerRect.right - offset) : targetRect.left - (containerRect.left + offset);
  } else {
    const head = container.querySelector(".pixata-blazor-scheduler-head");
    const offset = head ? head.getBoundingClientRect().height : 0;
    container.scrollTop += targetRect.top - (containerRect.top + offset);
  }
}

function pointerDown(state, e) {
  if (e.button !== 0 || state.drag) {
    return;
  }
  const appointment = e.target.closest(".pixata-blazor-scheduler-event");
  if (!appointment || !state.root.contains(appointment) || appointment.dataset.pixataDraggable !== "true") {
    return;
  }
  if (e.target.closest(".pixata-blazor-scheduler-event-delete")) {
    return;
  }
  if (e.pointerType === "touch" && !appointment.classList.contains("pixata-blazor-selected")) {
    return;
  }
  const handle = e.target.closest(".pixata-blazor-scheduler-resize-handle");
  const grab = slotAt(state.root, e.clientX, e.clientY, appointment);
  if (!grab) {
    return;
  }
  const scroller = appointment.closest(".pixata-blazor-scheduler-content");
  state.drag = {
    appointment,
    scroller,
    mode: handle ? handle.dataset.pixataEdge : "move",
    vertical: appointment.dataset.pixataOrientation === "vertical",
    rtl: getComputedStyle(appointment).direction === "rtl",
    grab,
    pointerId: e.pointerId,
    x: e.clientX,
    y: e.clientY,
    scrollLeft: scroller ? scroller.scrollLeft : 0,
    scrollTop: scroller ? scroller.scrollTop : 0,
    width: appointment.offsetWidth,
    height: appointment.offsetHeight,
    originalStyle: appointment.getAttribute("style"),
    started: false,
    target: null
  };
  state.onMove = ev => pointerMove(state, ev);
  state.onUp = ev => pointerUp(state, ev);
  state.onCancel = () => endDrag(state, null);
  state.onKey = ev => {
    if (ev.key === "Escape") {
      ev.stopPropagation();
      endDrag(state, null);
    }
  };
  document.addEventListener("pointermove", state.onMove);
  document.addEventListener("pointerup", state.onUp);
  document.addEventListener("pointercancel", state.onCancel);
  document.addEventListener("keydown", state.onKey, true);
}

function pointerMove(state, e) {
  const drag = state.drag;
  if (!drag || e.pointerId !== drag.pointerId) {
    return;
  }
  drag.lastX = e.clientX;
  drag.lastY = e.clientY;
  if (!drag.started) {
    if (Math.abs(e.clientX - drag.x) < dragThreshold && Math.abs(e.clientY - drag.y) < dragThreshold) {
      return;
    }
    drag.started = true;
    drag.appointment.classList.add("pixata-blazor-scheduler-dragging");
    state.root.classList.add("pixata-blazor-scheduler-drag-active");
    try {
      drag.appointment.setPointerCapture(e.pointerId);
    } catch {
    }
  }
  e.preventDefault();
  autoScroll(drag, e.clientX, e.clientY);
  showDrag(state, e.clientX, e.clientY);
}

// Moves (or stretches) the appointment with the pointer, and highlights the slot under the pointer
function showDrag(state, clientX, clientY) {
  const drag = state.drag;
  const style = drag.appointment.style;
  const dx = clientX - drag.x + (drag.scroller ? drag.scroller.scrollLeft - drag.scrollLeft : 0);
  const dy = clientY - drag.y + (drag.scroller ? drag.scroller.scrollTop - drag.scrollTop : 0);
  if (drag.mode === "move") {
    style.transform = `translate(${dx}px, ${dy}px)`;
  } else if (drag.vertical) {
    if (drag.mode === "end") {
      style.height = `${Math.max(8, drag.height + dy)}px`;
    } else {
      const delta = Math.min(dy, drag.height - 8);
      style.transform = `translateY(${delta}px)`;
      style.height = `${drag.height - delta}px`;
    }
  } else {
    // A horizontal appointment is positioned from its start edge (the left, or the right in a right-to-left layout), so
    // stretching its end only changes its width, while moving its start also moves it
    const along = drag.rtl ? -dx : dx;
    if (drag.mode === "end") {
      style.width = `${Math.max(8, drag.width + along)}px`;
    } else {
      const delta = Math.min(along, drag.width - 8);
      style.width = `${drag.width - delta}px`;
      style.transform = `translateX(${drag.rtl ? -delta : delta}px)`;
    }
  }
  const slot = slotElementAt(state.root, clientX, clientY, drag.appointment);
  if (slot !== drag.target) {
    if (drag.target) {
      drag.target.classList.remove("pixata-blazor-scheduler-drop-target");
    }
    drag.target = slot;
    if (slot) {
      slot.classList.add("pixata-blazor-scheduler-drop-target");
    }
  }
}

function autoScroll(drag, x, y) {
  const scroller = drag.scroller;
  if (!scroller) {
    return;
  }
  const rect = scroller.getBoundingClientRect();
  if (y < rect.top + edgeScrollZone) {
    scroller.scrollTop -= edgeScrollStep;
  } else if (y > rect.bottom - edgeScrollZone) {
    scroller.scrollTop += edgeScrollStep;
  }
  if (x < rect.left + edgeScrollZone) {
    scroller.scrollLeft -= edgeScrollStep;
  } else if (x > rect.right - edgeScrollZone) {
    scroller.scrollLeft += edgeScrollStep;
  }
}

function pointerUp(state, e) {
  const drag = state.drag;
  if (!drag || e.pointerId !== drag.pointerId) {
    return;
  }
  if (!drag.started) {
    endDrag(state, null);
    return;
  }
  const drop = slotAt(state.root, e.clientX, e.clientY, drag.appointment);
  // The click that follows the end of a drag mustn't select or open anything
  const swallow = ev => {
    ev.stopPropagation();
    ev.preventDefault();
  };
  window.addEventListener("click", swallow, { capture: true, once: true });
  setTimeout(() => window.removeEventListener("click", swallow, { capture: true }), 0);
  endDrag(state, drop);
}

function endDrag(state, drop) {
  const drag = state.drag;
  if (!drag) {
    return;
  }
  state.drag = null;
  document.removeEventListener("pointermove", state.onMove);
  document.removeEventListener("pointerup", state.onUp);
  document.removeEventListener("pointercancel", state.onCancel);
  document.removeEventListener("keydown", state.onKey, true);
  // Put back the position and size the scheduler gave the appointment, until it renders it in its new place
  if (drag.originalStyle === null) {
    drag.appointment.removeAttribute("style");
  } else {
    drag.appointment.setAttribute("style", drag.originalStyle);
  }
  drag.appointment.classList.remove("pixata-blazor-scheduler-dragging");
  state.root.classList.remove("pixata-blazor-scheduler-drag-active");
  if (drag.target) {
    drag.target.classList.remove("pixata-blazor-scheduler-drop-target");
  }
  if (drop && drag.started) {
    state.dotNet.invokeMethodAsync("OnDragEnd", drag.appointment.dataset.pixataKey, drag.mode, drag.grab, drop).catch(() => { });
  }
}

function slotElementAt(root, x, y, ignore) {
  for (const element of document.elementsFromPoint(x, y)) {
    if (ignore && (element === ignore || ignore.contains(element))) {
      continue;
    }
    if (element.dataset && element.dataset.pixataSlotStart && root.contains(element)) {
      return element;
    }
  }
  return null;
}

function slotAt(root, x, y, ignore) {
  const element = slotElementAt(root, x, y, ignore);
  if (!element) {
    return null;
  }
  return {
    start: element.dataset.pixataSlotStart,
    end: element.dataset.pixataSlotEnd,
    allDay: element.dataset.pixataSlotAllDay === "true",
    group: parseInt(element.dataset.pixataSlotGroup || "0", 10)
  };
}
