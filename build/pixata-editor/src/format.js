// Lays out the editor's HTML for the View HTML dialog: each block element on its own line, indented to show the nesting.
// Only white space between blocks is added, which the editor ignores when the HTML is read back

const blockTags = new Set([
  "P", "H1", "H2", "H3", "H4", "H5", "H6", "UL", "OL", "LI", "TABLE", "THEAD", "TBODY", "TFOOT", "TR", "TD", "TH",
  "BLOCKQUOTE", "PRE", "HR", "DIV"
]);

function isBlock(node) {
  return node.nodeType === 1 && blockTags.has(node.tagName);
}

function openTag(element) {
  const html = element.cloneNode(false).outerHTML;
  const close = html.lastIndexOf("</");
  return close > 0 ? html.slice(0, close) : html;
}

function escapeText(doc, text) {
  const span = doc.createElement("span");
  span.textContent = text;
  return span.innerHTML;
}

function write(node, depth, lines) {
  const indent = "  ".repeat(depth);
  for (const child of node.childNodes) {
    if (isBlock(child)) {
      const hasBlocks = child.tagName !== "PRE" && [...child.childNodes].some(isBlock);
      if (!hasBlocks) {
        lines.push(indent + child.outerHTML);
      } else {
        lines.push(indent + openTag(child));
        write(child, depth + 1, lines);
        lines.push(`${indent}</${child.tagName.toLowerCase()}>`);
      }
    } else if (child.nodeType === 1) {
      lines.push(indent + child.outerHTML);
    } else if (child.nodeType === 3 && child.textContent.trim()) {
      lines.push(indent + escapeText(node.ownerDocument, child.textContent.trim()));
    }
  }
}

export function formatHtml(html) {
  if (!html) {
    return "";
  }
  const body = new window.DOMParser().parseFromString(html, "text/html").body;
  const lines = [];
  write(body, 0, lines);
  return lines.join("\n");
}
