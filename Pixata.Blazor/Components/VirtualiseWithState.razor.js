let handlers = new WeakMap();

export function initialise(element, dotnet) {
  // The component can be removed while the module is being imported, in which case its element has already gone
  if (!element) {
    return;
  }
  let timeout;
  const handler = function () {
    clearTimeout(timeout);
    timeout = setTimeout(() => {
      dotnet.invokeMethodAsync("ScrollChanged", element.scrollTop);
    }, 300);
  };
  element.addEventListener("scroll", handler);
  handlers.set(element, handler);
}

export function restore(element, value) {
  if (element) {
    element.scrollTop = value;
  }
}
export function getScrollTop(element) {
  return element?.scrollTop ?? 0;
}

export function dispose(element) {
  const handler = handlers.get(element);
  if (handler) {
    element.removeEventListener("scroll", handler);
    handlers.delete(element);
  }
}