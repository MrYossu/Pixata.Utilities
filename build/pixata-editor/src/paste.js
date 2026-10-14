// Cleans HTML pasted into the editor, according to the paste settings sent from .NET. This runs before the schema parses
// the HTML, so it is about tidying (Word's list paragraphs, its classes and styles, attributes you don't want), not about
// safety: the schema drops anything unsafe whatever the settings say

export const defaultPasteSettings = {
  convertMsLists: true,
  removeMsClasses: true,
  removeMsStyles: true,
  removeHtmlComments: false,
  removeAllAttributes: false,
  removeAttributes: [],
  stripTags: []
};

function rawStyle(element) {
  return element.getAttribute("style") || "";
}

function msListInfo(element) {
  const match = /mso-list\s*:\s*l(\d+)\s+level(\d+)/i.exec(rawStyle(element));
  return match ? { list: match[1], level: parseInt(match[2], 10) } : null;
}

function markerElements(element) {
  return [...element.querySelectorAll("[style]")].filter(e => /mso-list\s*:\s*ignore/i.test(rawStyle(e)));
}

function isOrderedMarker(text) {
  const marker = text.replace(/[\s ]/g, "");
  return /^(\d+|[a-z]{1,2}|[ivxlcdm]+)[.)]$/i.test(marker);
}

// Word pastes a list as a run of paragraphs whose style says which list and level they are in (mso-list: l0 level1 lfo1),
// with the bullet or number as text in a span marked mso-list: Ignore. This turns each run into real (nested) lists
function convertMsLists(body) {
  const doc = body.ownerDocument;
  const paragraphs = [...body.querySelectorAll("p, h1, h2, h3, h4, h5, h6")].filter(p => msListInfo(p));
  const done = new Set();
  for (const first of paragraphs) {
    if (done.has(first)) {
      continue;
    }
    const run = [first];
    let next = first.nextSibling;
    while (next) {
      if (next.nodeType === 3 && !next.textContent.trim()) {
        next = next.nextSibling;
        continue;
      }
      if (next.nodeType === 8) {
        next = next.nextSibling;
        continue;
      }
      if (next.nodeType === 1 && msListInfo(next)) {
        run.push(next);
        next = next.nextSibling;
        continue;
      }
      break;
    }
    const stack = [];
    let root = null;
    for (const paragraph of run) {
      done.add(paragraph);
      const { level } = msListInfo(paragraph);
      const markers = markerElements(paragraph);
      const ordered = markers.length > 0 && isOrderedMarker(markers.map(m => m.textContent).join(""));
      markers.forEach(m => m.remove());
      while (stack.length && stack[stack.length - 1].level > level) {
        stack.pop();
      }
      if (!stack.length || stack[stack.length - 1].level < level) {
        const list = doc.createElement(ordered ? "ol" : "ul");
        if (stack.length) {
          const parentList = stack[stack.length - 1].list;
          const lastItem = parentList.lastElementChild || parentList.appendChild(doc.createElement("li"));
          lastItem.appendChild(list);
        } else {
          root = list;
        }
        stack.push({ level, list });
      }
      const item = doc.createElement("li");
      while (paragraph.firstChild) {
        item.appendChild(paragraph.firstChild);
      }
      stack[stack.length - 1].list.appendChild(item);
    }
    if (root) {
      first.parentNode.insertBefore(root, first);
    }
    run.forEach(p => p.remove());
  }
}

function removeComments(root) {
  const walker = root.ownerDocument.createTreeWalker(root, 128);
  const comments = [];
  while (walker.nextNode()) {
    comments.push(walker.currentNode);
  }
  comments.forEach(c => c.remove());
}

function unwrap(element) {
  const parent = element.parentNode;
  while (element.firstChild) {
    parent.insertBefore(element.firstChild, element);
  }
  element.remove();
}

function removeMsStyles(element) {
  const style = rawStyle(element);
  if (!/mso-/i.test(style)) {
    return;
  }
  const kept = style.split(";").map(d => d.trim()).filter(d => d && !/^mso-/i.test(d));
  if (kept.length) {
    element.setAttribute("style", kept.join("; "));
  } else {
    element.removeAttribute("style");
  }
}

function removeMsClasses(element) {
  const classes = (element.getAttribute("class") || "").split(/\s+/).filter(c => c && !/^mso/i.test(c));
  if (classes.length) {
    element.setAttribute("class", classes.join(" "));
  } else {
    element.removeAttribute("class");
  }
}

// Cleans pasted HTML and returns the result
export function cleanPastedHtml(html, settings) {
  const options = { ...defaultPasteSettings, ...(settings || {}) };
  const body = new window.DOMParser().parseFromString(html || "", "text/html").body;
  if (options.removeHtmlComments) {
    removeComments(body);
  }
  if (options.convertMsLists) {
    convertMsLists(body);
  }
  for (const tag of options.stripTags || []) {
    if (/^[a-z][a-z0-9:-]*$/i.test(tag)) {
      [...body.getElementsByTagName(tag)].forEach(unwrap);
    }
  }
  const removeAttributes = (options.removeAttributes || []).map(a => String(a).toLowerCase());
  for (const element of body.querySelectorAll("*")) {
    if (options.removeAllAttributes) {
      [...element.attributes].map(a => a.name).forEach(name => element.removeAttribute(name));
      continue;
    }
    for (const name of removeAttributes) {
      element.removeAttribute(name);
    }
    if (options.removeMsStyles && element.hasAttribute("style")) {
      removeMsStyles(element);
    }
    if (options.removeMsClasses && element.hasAttribute("class")) {
      removeMsClasses(element);
    }
  }
  return body.innerHTML;
}
