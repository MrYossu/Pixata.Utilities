// Browser helpers for PixataDropDownList, PixataComboBox and PixataMultiSelect. Each component marks its own element and its
// popup with the same data-pixata-dropdown="<id>" attribute, so the listeners below (installed once, on the document) can
// tell which component an event belongs to

const dropDowns = new Map();
let listening = false;

const ownerAttribute = "data-pixata-dropdown";
const focusableSelector = "input, textarea, select, button, a[href], [contenteditable=true], [tabindex]:not([tabindex='-1'])";

function ownerOf(element) {
  const root = element && element.closest ? element.closest(`[${ownerAttribute}]`) : null;
  return root ? { id: root.getAttribute(ownerAttribute), root } : null;
}

// Tells the component when focus has moved somewhere outside both its own element and its popup
function onFocusOut(e) {
  const target = e.target;
  const from = ownerOf(target);
  if (!from) {
    return;
  }
  const to = ownerOf(e.relatedTarget);
  if (to && to.id === from.id) {
    return;
  }
  // Some browsers raise focusout while the focused element is being removed (eg the popup closing while its filter box had the
  // focus), when it is still in the document, so wait until that is over. An element that was removed isn't the user leaving,
  // unless the focus has since gone somewhere else (eg the Tab key moved it on)
  setTimeout(() => {
    if (!target.isConnected) {
      const now = document.activeElement;
      const owner = ownerOf(now);
      if (!now || now === document.body || (owner && owner.id === from.id)) {
        return;
      }
    }
    const dropDown = dropDowns.get(from.id);
    if (dropDown) {
      dropDown.dotNet.invokeMethodAsync("HandleFocusLeft").catch(() => { });
    }
  });
}

// Stops the browser's own action for the keys the component handles: the arrow keys (which would move the caret or scroll the
// page), and Enter, Escape, Home, End, Page Up and Page Down while the list is open. Space is stopped on the drop-down list
// itself, where it opens the list rather than scrolling the page. Enter is always stopped in the multi-select's text box, which
// uses it to open the list or add the typed text, so it doesn't submit a form the multi-select is in
function onKeyDown(e) {
  const owner = ownerOf(e.target);
  if (!owner || !dropDowns.has(owner.id)) {
    return;
  }
  const open = !!document.querySelector(`[${ownerAttribute}="${owner.id}"][aria-expanded='true'], [${ownerAttribute}="${owner.id}"] [aria-expanded='true']`);
  const textInput = e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA";
  let prevent = false;
  switch (e.key) {
    case "ArrowDown":
    case "ArrowUp":
      prevent = true;
      break;
    case "Enter":
      prevent = open || (textInput && owner.root.classList.contains("pixata-blazor-multiselect"));
      break;
    case "Escape":
    case "PageDown":
    case "PageUp":
      prevent = open;
      break;
    case "Home":
    case "End":
      prevent = open || !textInput;
      break;
    case " ":
      prevent = !textInput;
      break;
  }
  if (prevent) {
    e.preventDefault();
  }
}

// Keeps the focus where it is when the user presses on a part of the component that can't take the focus itself (an item in
// the list, a tag, the arrow button), so the input doesn't lose the focus and close the list before the click arrives
function onPointerDown(e) {
  const owner = ownerOf(e.target);
  if (!owner || !dropDowns.has(owner.id)) {
    return;
  }
  const focusable = e.target.closest(focusableSelector);
  if (!focusable || !owner.root.contains(focusable)) {
    e.preventDefault();
  }
}

export function attachDropDown(id, dotNet) {
  dropDowns.set(id, { dotNet });
  if (!listening) {
    listening = true;
    document.addEventListener("focusout", onFocusOut, true);
    document.addEventListener("keydown", onKeyDown, true);
    document.addEventListener("mousedown", onPointerDown, true);
  }
}

export function detachDropDown(id) {
  dropDowns.delete(id);
}

// Scrolls the list so the option is visible. If the option isn't rendered (it is outside the part of a virtual list that is
// drawn) the position it would be at is used instead
export function scrollToOption(container, optionId, top, height) {
  if (!container) {
    return;
  }
  const option = optionId ? document.getElementById(optionId) : null;
  if (option && container.contains(option)) {
    const c = container.getBoundingClientRect();
    const o = option.getBoundingClientRect();
    ensureVisible(container, o.top - c.top + container.scrollTop, o.height);
  } else if (top >= 0) {
    ensureVisible(container, top, height);
  }
}

function ensureVisible(container, top, height) {
  // Leave room for the rows that stick to the top of the list ("Select all" and the group headers)
  const sticky = container.querySelector(".pixata-blazor-dropdown-sticky");
  const groupHeader = container.querySelector(".pixata-blazor-dropdown-group-header");
  const covered = (sticky ? sticky.offsetHeight : 0) + (groupHeader ? groupHeader.offsetHeight : 0);
  if (top - covered < container.scrollTop) {
    container.scrollTop = Math.max(0, top - covered);
  } else if (top + height > container.scrollTop + container.clientHeight) {
    container.scrollTop = top + height - container.clientHeight;
  }
}

export function focusElement(element) {
  if (element && element.focus) {
    element.focus({ preventScroll: true });
  }
}

// Writes the text of a text box, as long as it still holds what the component last knew it held (expected). If the user has
// typed since, what they typed is on its way to the component, so it is left alone rather than lost
export function setValue(element, value, expected) {
  if (element && "value" in element && element.value !== value && (typeof expected !== "string" || element.value === expected)) {
    element.value = value;
  }
}

export function selectText(element) {
  if (element && element.select) {
    element.select();
  }
}
