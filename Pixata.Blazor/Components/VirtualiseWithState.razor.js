let handlers = new WeakMap();

export function initialise(element, dotnet) {
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
  element.scrollTop = value;
}
export function getScrollTop(element) {
  return element.scrollTop;
}

export function dispose(element) {
  const handler = handlers.get(element);
  if (handler) {
    element.removeEventListener("scroll", handler);
    handlers.delete(element);
  }
}