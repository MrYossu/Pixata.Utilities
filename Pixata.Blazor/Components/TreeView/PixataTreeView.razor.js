// Browser helpers for PixataTreeView: stopping the keys the tree handles from scrolling the page, moving the focus, and
// dragging items with pointer events (so mouse, pen and touch all work), within a tree, on to another tree or on to a grid

const trees = new Map();
const keys = ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", " "];
let drag = null;

const icons = {
  Over: '<path d="M12 5v14M5 12h14"/>',
  Before: '<path d="M12 19V7M7 12l5-5 5 5M5 4h14"/>',
  After: '<path d="M12 5v12M7 12l5 5 5-5M5 20h14"/>',
  denied: '<circle cx="12" cy="12" r="9"/><path d="M5.6 5.6l12.8 12.8"/>'
};

// Returns true if the tree is laid out right to left, so .NET can swap the left and right arrow keys
export function attach(root, list, dotNet, options) {
  if (!root || !list) {
    return false;
  }
  detach(root);
  const state = { root, list, dotNet, options: options || {} };
  state.keydown = e => {
    if (keys.includes(e.key) && e.target.getAttribute && e.target.getAttribute("role") === "treeitem" && e.target.closest(".pixata-blazor-treeview") === root) {
      e.preventDefault();
    }
  };
  state.pointerdown = e => pointerDown(state, e);
  list.addEventListener("keydown", state.keydown);
  root.addEventListener("pointerdown", state.pointerdown);
  trees.set(root, state);
  return getComputedStyle(root).direction === "rtl";
}

export function configure(root, options) {
  const state = trees.get(root);
  if (state) {
    state.options = options || {};
  }
}

export function detach(root) {
  const state = trees.get(root);
  if (!state) {
    return;
  }
  if (drag && drag.state === state) {
    endDrag();
  }
  state.list.removeEventListener("keydown", state.keydown);
  state.root.removeEventListener("pointerdown", state.pointerdown);
  trees.delete(root);
}

export function focusItem(list, index) {
  const item = list && list.querySelector(`[data-pixata-tree-index="${index}"]`);
  if (item) {
    item.focus();
    item.scrollIntoView({ block: "nearest", inline: "nearest" });
  }
}

// ---------- Dragging ----------

function pointerDown(state, e) {
  if (!state.options.draggable || drag || (e.pointerType === "mouse" && e.button !== 0)) {
    return;
  }
  const leaf = e.target.closest(".pixata-blazor-treeview-leaf");
  const item = leaf && leaf.closest("[data-pixata-tree-index]");
  if (!item || item.closest(".pixata-blazor-treeview") !== state.root || item.getAttribute("aria-disabled") === "true") {
    return;
  }
  drag = {
    state,
    leaf,
    index: item.dataset.pixataTreeIndex,
    pointerId: e.pointerId,
    x: e.clientX,
    y: e.clientY,
    touch: e.pointerType === "touch",
    // On a touch screen, the item has to be held for a moment first, so swiping over the tree still scrolls the page
    ready: e.pointerType !== "touch",
    started: false,
    waiting: false,
    lastDrag: 0
  };
  if (drag.touch) {
    drag.holdTimer = setTimeout(() => {
      if (drag) {
        drag.ready = true;
      }
    }, 300);
  }
  document.addEventListener("pointermove", pointerMove);
  document.addEventListener("pointerup", pointerUp);
  document.addEventListener("pointercancel", pointerCancel);
  document.addEventListener("keydown", keyDown, true);
}

function pointerMove(e) {
  if (!drag || e.pointerId !== drag.pointerId) {
    return;
  }
  const distance = Math.abs(e.clientX - drag.x) + Math.abs(e.clientY - drag.y);
  if (!drag.started) {
    if (drag.waiting) {
      return;
    }
    if (!drag.ready) {
      if (distance > 8) {
        // The finger moved before the hold time was up, so it's a scroll
        endDrag();
      }
      return;
    }
    if (distance < 5) {
      return;
    }
    if (drag.state.options.dragStart) {
      drag.waiting = true;
      const current = drag;
      current.state.dotNet.invokeMethodAsync("DragStart", current.index)
        .then(ok => {
          if (drag !== current) {
            return;
          }
          current.waiting = false;
          if (ok) {
            startDrag();
          } else {
            endDrag();
          }
        })
        .catch(() => endDrag());
      return;
    }
    startDrag();
  }
  e.preventDefault();
  drag.lastX = e.clientX;
  drag.lastY = e.clientY;
  update(e);
}

function startDrag() {
  drag.started = true;
  const clue = document.createElement("div");
  clue.className = "pixata-blazor-treeview-drag-clue";
  clue.innerHTML = '<span class="pixata-blazor-icon pixata-blazor-svg-icon pixata-blazor-treeview-drag-status"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></svg></span><span class="pixata-blazor-treeview-drag-text"></span>';
  clue.querySelector(".pixata-blazor-treeview-drag-text").textContent = drag.leaf.textContent.trim();
  document.body.appendChild(clue);
  drag.clue = clue;
  document.body.classList.add("pixata-blazor-treeview-dragging");
  window.getSelection()?.removeAllRanges();
  if (drag.lastX === undefined) {
    drag.lastX = drag.x;
    drag.lastY = drag.y;
  }
  update({ clientX: drag.lastX, clientY: drag.lastY, pageX: drag.lastX + window.scrollX, pageY: drag.lastY + window.scrollY });
}

function update(e) {
  if (!drag.started) {
    return;
  }
  drag.clue.style.left = `${e.clientX + 12}px`;
  drag.clue.style.top = `${e.clientY + 12}px`;
  const target = findTarget(e.clientX, e.clientY);
  drag.target = target;
  showTarget(target);
  const status = target && !target.denied ? (target.index === null && !target.grid ? "Over" : target.position) : "denied";
  drag.clue.classList.toggle("pixata-blazor-treeview-drag-denied", status === "denied");
  drag.clue.querySelector("svg").innerHTML = icons[status];
  const options = drag.state.options;
  if (options.drag && Date.now() - drag.lastDrag >= (options.throttle || 0)) {
    drag.lastDrag = Date.now();
    const valid = target && !target.denied;
    drag.state.dotNet.invokeMethodAsync("Drag", drag.index, valid ? target.dotNet : null, valid ? target.index : null, valid ? target.id : null,
      valid ? target.position : "Over", e.pageX ?? e.clientX + window.scrollX, e.pageY ?? e.clientY + window.scrollY).catch(() => { });
  }
}

// What is under the pointer: an item of a tree (or a tree with no item there), a row of a grid (or a grid), or nothing
function findTarget(x, y) {
  const element = document.elementFromPoint(x, y);
  if (!element || drag.clue.contains(element)) {
    return null;
  }
  const tree = element.closest(".pixata-blazor-treeview");
  const grid = element.closest(".pixata-blazor-grid");
  if (tree && (!grid || grid.contains(tree))) {
    const treeState = trees.get(tree);
    const content = element.closest(".pixata-blazor-treeview-content");
    const item = content && content.closest("[data-pixata-tree-index]");
    if (item && item.closest(".pixata-blazor-treeview") === tree) {
      const index = item.dataset.pixataTreeIndex;
      if (tree === drag.state.root && (index === drag.index || index.startsWith(`${drag.index}_`))) {
        // An item can't go inside itself
        return { denied: true };
      }
      return { dotNet: treeState ? treeState.dotNet : null, id: tree.id, index, position: position(content, y), element: content };
    }
    return { dotNet: treeState ? treeState.dotNet : null, id: tree.id, index: null, position: "Over", element: null };
  }
  if (grid) {
    const row = element.closest("tr[data-pixata-row-index]");
    if (row && row.closest(".pixata-blazor-grid") === grid) {
      return { grid: true, dotNet: null, id: grid.id, index: row.dataset.pixataRowIndex, position: position(row, y), element: row };
    }
    return { grid: true, dotNet: null, id: grid.id, index: null, position: "Over", element: null };
  }
  return null;
}

function position(element, y) {
  const rect = element.getBoundingClientRect();
  const fraction = (y - rect.top) / rect.height;
  return fraction < .25 ? "Before" : fraction > .75 ? "After" : "Over";
}

function showTarget(target) {
  if (drag.over) {
    drag.over.classList.remove("pixata-blazor-treeview-drop-over");
    drag.over = null;
  }
  const element = target && !target.denied && target.element && !target.grid ? target.element : null;
  if (!element || target.position === "Over") {
    if (element) {
      drag.over = element.querySelector(".pixata-blazor-treeview-leaf") || element;
      drag.over.classList.add("pixata-blazor-treeview-drop-over");
    }
    drag.hint?.remove();
    drag.hint = null;
    return;
  }
  if (!drag.hint) {
    drag.hint = document.createElement("div");
    drag.hint.className = "pixata-blazor-treeview-drop-hint";
    document.body.appendChild(drag.hint);
  }
  const leaf = element.querySelector(".pixata-blazor-treeview-leaf") || element;
  const leafRect = leaf.getBoundingClientRect();
  drag.hint.style.left = `${leafRect.left}px`;
  drag.hint.style.width = `${leafRect.width}px`;
  drag.hint.style.top = `${target.position === "Before" ? leafRect.top : leafRect.bottom}px`;
}

function pointerUp(e) {
  if (!drag || e.pointerId !== drag.pointerId) {
    return;
  }
  if (!drag.started) {
    endDrag();
    return;
  }
  const target = findTarget(e.clientX, e.clientY);
  finish(target && !target.denied ? target : null);
  // The pointer going up after a drag would otherwise click the item it ends over
  const stopClick = event => event.stopPropagation();
  window.addEventListener("click", stopClick, true);
  setTimeout(() => window.removeEventListener("click", stopClick, true), 0);
}

function pointerCancel(e) {
  if (drag && e.pointerId === drag.pointerId) {
    if (drag.started) {
      finish(null);
    } else {
      endDrag();
    }
  }
}

function keyDown(e) {
  if (drag && e.key === "Escape") {
    e.preventDefault();
    e.stopPropagation();
    if (drag.started) {
      finish(null);
    } else {
      endDrag();
    }
  }
}

function finish(target) {
  const { state, index } = drag;
  endDrag();
  state.dotNet.invokeMethodAsync("Drop", index, target ? target.dotNet : null, target ? target.index : null, target ? target.id : null,
    target ? target.position : "Over", !!target).catch(() => { });
}

function endDrag() {
  if (!drag) {
    return;
  }
  clearTimeout(drag.holdTimer);
  drag.clue?.remove();
  drag.hint?.remove();
  drag.over?.classList.remove("pixata-blazor-treeview-drop-over");
  document.body.classList.remove("pixata-blazor-treeview-dragging");
  document.removeEventListener("pointermove", pointerMove);
  document.removeEventListener("pointerup", pointerUp);
  document.removeEventListener("pointercancel", pointerCancel);
  document.removeEventListener("keydown", keyDown, true);
  drag = null;
}

// Once a touch drag has started, the finger moving mustn't scroll the page
document.addEventListener("touchmove", e => {
  if (drag && drag.ready && e.cancelable) {
    e.preventDefault();
  }
}, { passive: false });
