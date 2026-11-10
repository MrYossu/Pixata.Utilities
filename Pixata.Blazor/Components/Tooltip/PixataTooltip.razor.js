// PixataTooltip's script. It watches the page for the tooltip's targets being hovered, focused, tapped or clicked, tells the
// component which target to show the tooltip for (with the target's id, title and data- attributes, for the template), and
// places the tooltip next to the target once the component has rendered it. Each tooltip's listeners are on the document,
// so targets matched by a selector work wherever they are, and however late they're added

const tooltips = new Map();

// Elements that handle Enter and Space themselves, either making a click (which shows a click tooltip anyway) or typing
const handlesKeys = "button, a[href], input, select, textarea, summary, [contenteditable]";

// Whether an element has the focus from the keyboard, rather than a click (which older browsers can't tell apart)
const focusVisible = element => {
  try {
    return element.matches(":focus-visible");
  } catch {
    return true;
  }
};

const keyboardFocused = target => {
  const active = document.activeElement;
  return !!target && !!active && target.contains(active) && focusVisible(active);
};

export function attach(optionsJson, dotNet) {
  const state = {
    options: JSON.parse(optionsJson),
    dotNet,
    target: null,
    described: null,
    describedBefore: null,
    titled: null,
    title: null,
    shown: false,
    showTimer: 0,
    hideTimer: 0
  };
  const options = () => state.options;
  const scope = () => options().targetsId ? document.getElementById(options().targetsId) : null;
  const tip = () => document.getElementById(options().tooltipId);
  const inTip = node => node instanceof Node && !!tip()?.contains(node);

  // The target an event happened in, if any. With child content and no selector, that's the child content as a whole
  const targetOf = node => {
    const element = node instanceof Element ? node : node?.parentElement;
    if (!element) {
      return null;
    }
    const within = scope();
    if (options().targetsId && (!within || !within.contains(element))) {
      return null;
    }
    if (options().selector) {
      let match = null;
      try {
        match = element.closest(options().selector);
      } catch {
        return null;
      }
      // The target being shown for is still a target, even though the selector may not match it now (as with [title], with
      // its title taken off)
      if (state.target && state.target.contains(element) && (!match || match.contains(state.target))) {
        return state.target;
      }
      return match && (!within || within.contains(match)) ? match : null;
    }
    return within;
  };

  // The element that's described by the tooltip, for screen readers, and whose id, title and data- attributes it's given.
  // That's the target, or for child content without a selector, the child element the event happened in (as the wrapper
  // around the content is only there to find it by)
  const describedElement = (target, node) => {
    if (options().selector || target !== scope()) {
      return target;
    }
    let element = node instanceof Element ? node : node?.parentElement;
    if (!element || element === target || !target.contains(element)) {
      return target.firstElementChild ?? target;
    }
    while (element.parentElement !== target) {
      element = element.parentElement;
    }
    return element;
  };

  const clearTimers = () => {
    clearTimeout(state.showTimer);
    clearTimeout(state.hideTimer);
    state.showTimer = 0;
    state.hideTimer = 0;
  };

  // Puts back the title and aria-describedby the tooltip took over
  const release = () => {
    if (state.titled && state.title !== null && !state.titled.hasAttribute("title")) {
      state.titled.setAttribute("title", state.title);
    }
    if (state.described) {
      if (state.describedBefore === null) {
        state.described.removeAttribute("aria-describedby");
      } else {
        state.described.setAttribute("aria-describedby", state.describedBefore);
      }
    }
    state.titled = state.described = null;
    state.title = state.describedBefore = null;
  };

  const show = (target, node) => {
    clearTimers();
    if (state.shown && state.target === target) {
      return;
    }
    const described = describedElement(target, node);
    const title = described === state.titled ? state.title : described.getAttribute("title");
    // Without a template or text of its own, the tooltip shows the target's title, so there's nothing to show if it has none
    if (!options().content && !title) {
      hide();
      return;
    }
    // Another tooltip that's only waiting for its hide delay goes now, so two aren't shown at once
    for (const other of tooltips.values()) {
      if (other !== state && other.shown && other.hideTimer) {
        other.hide();
      }
    }
    release();
    tip()?.removeAttribute("data-placed");
    state.target = target;
    state.shown = true;
    // The title is taken off while the tooltip is shown, so the browser doesn't show its own tooltip as well
    state.titled = described;
    state.title = title;
    if (title !== null) {
      described.removeAttribute("title");
    }
    state.described = described;
    state.describedBefore = described.getAttribute("aria-describedby");
    described.setAttribute("aria-describedby", [state.describedBefore, options().tooltipId].filter(Boolean).join(" "));
    const data = {};
    for (const attribute of described.attributes) {
      if (attribute.name.startsWith("data-")) {
        data[attribute.name.slice(5)] = attribute.value;
      }
    }
    state.dotNet.invokeMethodAsync("ShowFor", described.id || null, title, data).catch(() => { });
  };

  const hide = () => {
    clearTimers();
    if (!state.shown) {
      return;
    }
    state.shown = false;
    state.target = null;
    release();
    tip()?.removeAttribute("data-placed");
    state.dotNet.invokeMethodAsync("Hidden").catch(() => { });
  };
  state.hide = hide;

  const showSoon = (target, node) => {
    clearTimeout(state.hideTimer);
    state.hideTimer = 0;
    if (state.shown && state.target === target) {
      return;
    }
    clearTimeout(state.showTimer);
    state.showTimer = setTimeout(() => show(target, node), options().showDelay);
  };

  const hideSoon = () => {
    clearTimeout(state.showTimer);
    state.showTimer = 0;
    // It stays while the target has the keyboard focus
    if (state.shown && keyboardFocused(state.target)) {
      return;
    }
    if (state.shown && !state.hideTimer) {
      state.hideTimer = setTimeout(hide, options().hideDelay);
    }
  };

  const pointerOver = e => {
    if (e.pointerType === "touch" || options().showOn !== "hover") {
      return;
    }
    const target = targetOf(e.target);
    if (target) {
      showSoon(target, e.target);
    } else if (inTip(e.target)) {
      // The pointer has moved onto the tooltip, so it stays
      clearTimeout(state.hideTimer);
      state.hideTimer = 0;
    }
  };

  const pointerOut = e => {
    if (e.pointerType === "touch" || options().showOn !== "hover") {
      return;
    }
    const from = targetOf(e.target);
    const leaving = from ? !from.contains(e.relatedTarget) : inTip(e.target) && !inTip(e.relatedTarget);
    if (!leaving || (state.target && state.target.contains(e.relatedTarget)) || inTip(e.relatedTarget)) {
      return;
    }
    if (from || inTip(e.target)) {
      hideSoon();
    }
  };

  // Only the keyboard focus shows the tooltip, not the focus a click gives a button, as the pointer has done that already
  const focusIn = e => {
    if (options().showOn !== "hover" || !focusVisible(e.target)) {
      return;
    }
    const target = targetOf(e.target);
    if (target) {
      show(target, e.target);
    }
  };

  const focusOut = e => {
    if (options().showOn !== "hover" || !state.target || !state.target.contains(e.target)) {
      return;
    }
    if (!state.target.contains(e.relatedTarget) && !inTip(e.relatedTarget)) {
      hide();
    }
  };

  // A tap on a touch screen shows the tooltip, and another tap (on the target or anywhere else) hides it
  const pointerDown = e => {
    if (options().showOn !== "hover" || e.pointerType !== "touch" || inTip(e.target)) {
      return;
    }
    const target = targetOf(e.target);
    if (target && !(state.shown && state.target === target)) {
      show(target, e.target);
    } else {
      hide();
    }
  };

  const toggle = (target, node) => {
    if (state.shown && state.target === target) {
      hide();
    } else {
      show(target, node);
    }
  };

  const click = e => {
    if (options().showOn !== "click" || inTip(e.target)) {
      return;
    }
    const target = targetOf(e.target);
    if (target) {
      toggle(target, e.target);
    } else {
      hide();
    }
  };

  const keyDown = e => {
    if (e.key === "Escape") {
      if (state.shown) {
        hide();
      }
      return;
    }
    if (options().showOn === "click" && (e.key === "Enter" || e.key === " ")) {
      const target = targetOf(e.target);
      if (target && !e.target.closest?.(handlesKeys)) {
        e.preventDefault();
        toggle(target, e.target);
      }
    }
  };

  // While it's shown, the tooltip follows its target as the page scrolls or resizes, and goes if the target does
  const reposition = () => {
    if (!state.shown) {
      return;
    }
    if (!state.target?.isConnected) {
      hide();
      return;
    }
    place(options().key);
  };

  const listeners = [
    ["pointerover", pointerOver],
    ["pointerout", pointerOut],
    ["focusin", focusIn],
    ["focusout", focusOut],
    ["pointerdown", pointerDown],
    ["click", click],
    ["keydown", keyDown]
  ];
  for (const [type, listener] of listeners) {
    document.addEventListener(type, listener);
  }
  window.addEventListener("scroll", reposition, true);
  window.addEventListener("resize", reposition);
  state.detach = () => {
    clearTimers();
    release();
    for (const [type, listener] of listeners) {
      document.removeEventListener(type, listener);
    }
    window.removeEventListener("scroll", reposition, true);
    window.removeEventListener("resize", reposition);
  };
  state.anchor = () => {
    const target = state.target;
    if (!target) {
      return null;
    }
    // Child content is wrapped in an element with display: contents, which has no box, so its content's box is used
    if (target === scope() && !options().selector) {
      const range = document.createRange();
      range.selectNodeContents(target);
      return range.getBoundingClientRect();
    }
    return target.getBoundingClientRect();
  };
  tooltips.set(state.options.key, state);
}

export function update(optionsJson) {
  const options = JSON.parse(optionsJson);
  const state = tooltips.get(options.key);
  if (!state) {
    return;
  }
  const targetsChanged = options.selector !== state.options.selector || options.targetsId !== state.options.targetsId || options.showOn !== state.options.showOn;
  state.options = options;
  if (targetsChanged) {
    state.hide();
  } else if (state.shown) {
    place(options.key);
  }
}

// Called once the component has rendered the tooltip, to put it next to its target. It goes on the side asked for, or the
// opposite side if there isn't room there, and is kept inside the window, with its arrow still pointing at the target
export function place(key) {
  const state = tooltips.get(key);
  const tip = state ? document.getElementById(state.options.tooltipId) : null;
  const anchor = state?.shown ? state.anchor() : null;
  if (!tip || !anchor) {
    return;
  }
  const style = getComputedStyle(tip);
  const gap = parseFloat(style.getPropertyValue("--pixata-blazor-tooltip-gap")) || 8;
  const margin = 4;
  const width = document.documentElement.clientWidth;
  const height = document.documentElement.clientHeight;
  // Measured at the top left, so its width isn't squeezed by the edge of the window. Where that ends up says how far its
  // containing block is from the window's corner (it's not the window if an ancestor has a transform)
  tip.style.left = "0px";
  tip.style.top = "0px";
  const origin = tip.getBoundingClientRect();
  const w = origin.width;
  const h = origin.height;
  const room = {
    top: anchor.top - gap - h >= margin,
    bottom: anchor.bottom + gap + h <= height - margin,
    left: anchor.left - gap - w >= margin,
    right: anchor.right + gap + w <= width - margin
  };
  const opposite = { top: "bottom", bottom: "top", left: "right", right: "left" };
  let side = state.options.position;
  if (side === "auto") {
    side = ["top", "bottom", "right", "left"].find(s => room[s]) ?? "top";
  } else if (!room[side] && room[opposite[side]]) {
    side = opposite[side];
  }
  const clamp = (value, low, high) => Math.max(low, Math.min(value, high));
  let x;
  let y;
  let arrow;
  if (side === "top" || side === "bottom") {
    x = clamp(anchor.left + anchor.width / 2 - w / 2, margin, width - w - margin);
    y = side === "top" ? anchor.top - gap - h : anchor.bottom + gap;
    arrow = clamp(anchor.left + anchor.width / 2 - x, 10, w - 10);
  } else {
    y = clamp(anchor.top + anchor.height / 2 - h / 2, margin, height - h - margin);
    x = side === "left" ? anchor.left - gap - w : anchor.right + gap;
    arrow = clamp(anchor.top + anchor.height / 2 - y, 10, h - 10);
  }
  tip.style.left = `${Math.round(x - origin.left)}px`;
  tip.style.top = `${Math.round(y - origin.top)}px`;
  tip.style.setProperty("--pixata-blazor-tooltip-arrow", `${Math.round(arrow)}px`);
  tip.dataset.position = side;
  tip.dataset.placed = "";
}

export function hide(key) {
  tooltips.get(key)?.hide();
}

export function detach(key) {
  tooltips.get(key)?.detach();
  tooltips.delete(key);
}
