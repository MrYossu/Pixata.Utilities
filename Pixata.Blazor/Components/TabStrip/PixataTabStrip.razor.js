// Browser helpers for PixataTabStrip: scrolling the tab list, and telling the component when the tabs overflow so it can
// show or enable its scroll buttons

const strips = new Map();
let nextId = 1;

export function attach(list, dotNet, vertical) {
  if (!list) {
    return 0;
  }
  const id = nextId++;
  const state = { list, dotNet, vertical: !!vertical, overflowing: null, back: null, forward: null };
  state.update = () => report(state);
  list.addEventListener("scroll", state.update, { passive: true });
  window.addEventListener("resize", state.update);
  if (window.ResizeObserver) {
    state.resizeObserver = new ResizeObserver(state.update);
    state.resizeObserver.observe(list);
  }
  // Tabs being added, removed or renamed change how much there is to scroll without changing the list's own size
  state.mutationObserver = new MutationObserver(state.update);
  state.mutationObserver.observe(list, { childList: true, subtree: true, characterData: true });
  strips.set(id, state);
  state.update();
  return id;
}

export function detach(id) {
  const state = strips.get(id);
  if (!state) {
    return;
  }
  state.list.removeEventListener("scroll", state.update);
  window.removeEventListener("resize", state.update);
  if (state.resizeObserver) {
    state.resizeObserver.disconnect();
  }
  state.mutationObserver.disconnect();
  strips.delete(id);
}

function report(state) {
  const { list, vertical } = state;
  if (!list.isConnected) {
    return;
  }
  const visible = vertical ? list.clientHeight : list.clientWidth;
  const total = vertical ? list.scrollHeight : list.scrollWidth;
  // scrollLeft is negative in right-to-left layouts, so measure the distance from the start either way
  const position = Math.abs(vertical ? list.scrollTop : list.scrollLeft);
  const overflowing = total - visible > 1;
  const back = overflowing && position > 1;
  const forward = overflowing && position + visible < total - 1;
  if (overflowing !== state.overflowing || back !== state.back || forward !== state.forward) {
    state.overflowing = overflowing;
    state.back = back;
    state.forward = forward;
    state.dotNet.invokeMethodAsync("OnScrollStateChanged", overflowing, back, forward).catch(() => { });
  }
}

// Scrolls the list by most of its visible length, backwards (direction -1) or forwards (1)
export function scrollTabs(id, direction) {
  const state = strips.get(id);
  if (!state) {
    return;
  }
  const { list, vertical } = state;
  const amount = Math.max((vertical ? list.clientHeight : list.clientWidth) * 0.75, 40) * direction;
  const rtl = !vertical && getComputedStyle(list).direction === "rtl";
  list.scrollBy(vertical ? { top: amount, behavior: smooth() } : { left: rtl ? -amount : amount, behavior: smooth() });
}

// Scrolls the list just enough to show the tab, without scrolling the page
export function scrollToTab(id, tab) {
  const state = strips.get(id);
  if (!state || !tab || !tab.isConnected) {
    return;
  }
  const { list, vertical } = state;
  const l = list.getBoundingClientRect();
  const t = tab.getBoundingClientRect();
  if (vertical) {
    if (t.top < l.top) {
      list.scrollBy({ top: t.top - l.top, behavior: smooth() });
    } else if (t.bottom > l.bottom) {
      list.scrollBy({ top: t.bottom - l.bottom, behavior: smooth() });
    }
  } else if (t.left < l.left) {
    list.scrollBy({ left: t.left - l.left, behavior: smooth() });
  } else if (t.right > l.right) {
    list.scrollBy({ left: t.right - l.right, behavior: smooth() });
  }
}

// The ids of the tab strip's panels, in the order they are in the page (ignoring those of any tab strip nested inside it)
export function tabOrder(root) {
  if (!root) {
    return [];
  }
  return [...root.querySelectorAll("[data-pixata-tab-panel]")]
    .filter(panel => panel.parentElement && panel.parentElement.closest(".pixata-blazor-tabstrip") === root)
    .map(panel => panel.getAttribute("data-pixata-tab-panel"));
}

function smooth() {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth";
}
