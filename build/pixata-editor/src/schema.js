// The document schema for PixataEditor. Everything the editor holds, and so everything it can emit, has to fit this schema:
// HTML that is set or pasted is parsed against it, and anything it doesn't describe (scripts, event handler attributes,
// iframes, forms, unknown attributes, style properties other than the few listed here) is dropped at that point. Links and
// images are only kept when their URL is safe (see safeUrl)

import { Schema, DOMParser as PmDOMParser, DOMSerializer } from "prosemirror-model";
import { tableNodes } from "prosemirror-tables";

const indentStep = 30;
const maxIndent = 20;
const alignments = ["left", "center", "right", "justify"];
const linkTargets = ["_blank", "_self", "_parent", "_top"];
const dataImage = /^data:image\/(png|gif|jpe?g|webp|bmp|avif);base64,[a-z0-9+/=]*$/i;

// Returns the URL if it is relative or uses a safe scheme (http, https, mailto or tel, and base64 data URLs for images),
// otherwise null. Browsers ignore tabs, new lines and control characters in a URL's scheme, so they are ignored here too
export function safeUrl(value, allowDataImage) {
  if (value === null || value === undefined) {
    return null;
  }
  const url = String(value).trim();
  if (!url) {
    return null;
  }
  const compact = url.replace(/[\u0000- \u007f-\u009f]/g, "");
  const scheme = /^([a-z][a-z0-9+.-]*):/i.exec(compact);
  if (!scheme) {
    return url;
  }
  const name = scheme[1].toLowerCase();
  if (name === "http" || name === "https" || name === "mailto" || name === "tel") {
    return url;
  }
  if (allowDataImage && name === "data" && dataImage.test(compact)) {
    return compact;
  }
  return null;
}

// Returns a CSS value if it is safe to put in a style attribute: no url(), expression(), escapes, comments or anything that
// could end the declaration. Colour functions (rgb, hsl...) are the only functions allowed
export function safeCssValue(value) {
  if (value === null || value === undefined) {
    return null;
  }
  const text = String(value).trim();
  if (!text || text.length > 200) {
    return null;
  }
  if (/[<>\\{};@!]|\/\*|\*\/|javascript:|expression|url\s*\(|image-set|var\s*\(/i.test(text)) {
    return null;
  }
  const functions = text.match(/[a-z-]+\s*\(/gi) || [];
  if (functions.some(f => !/^(rgba?|hsla?)\s*\($/i.test(f))) {
    return null;
  }
  if (!/^[\p{L}\p{N}\s#%.,'"()+-]*$/u.test(text)) {
    return null;
  }
  return text;
}

function size(value) {
  if (value === null || value === undefined) {
    return null;
  }
  const match = /^\s*(\d{1,5}(?:\.\d+)?)\s*(px|%)?\s*$/i.exec(String(value));
  if (!match) {
    return null;
  }
  return match[2] === "%" ? `${match[1]}%` : match[1];
}

function styleString(parts) {
  const declarations = Object.entries(parts).filter(([, v]) => v !== null && v !== undefined && v !== "").map(([k, v]) => `${k}: ${v}`);
  return declarations.length ? declarations.join("; ") : null;
}

function blockAttrsFromDom(dom) {
  let align = (dom.style.textAlign || dom.getAttribute("align") || "").toLowerCase();
  if (align === "start") {
    align = "left";
  } else if (align === "end") {
    align = "right";
  }
  const marginLeft = dom.style.marginLeft || "";
  const margin = /px$/.test(marginLeft) ? parseFloat(marginLeft) : NaN;
  const indent = isNaN(margin) || margin <= 0 ? 0 : Math.min(maxIndent, Math.round(margin / indentStep));
  return {
    textAlign: alignments.includes(align) ? align : null,
    indent
  };
}

function blockAttrsToDom(attrs) {
  const style = styleString({
    "text-align": attrs.textAlign,
    "margin-left": attrs.indent ? `${attrs.indent * indentStep}px` : null
  });
  return style ? { style } : {};
}

const blockAttrs = {
  textAlign: { default: null },
  indent: { default: 0 }
};

// The four style properties the editor's text-style mark carries, keyed by the attribute name in the mark
export const textStyleProperties = {
  color: "color",
  backgroundColor: "background-color",
  fontFamily: "font-family",
  fontSize: "font-size"
};

const fontSizes = ["", "x-small", "small", "medium", "large", "x-large", "xx-large", "xxx-large"];

function textStyleFromDom(dom) {
  const attrs = {};
  let found = false;
  for (const [name, property] of Object.entries(textStyleProperties)) {
    const value = safeCssValue(dom.style.getPropertyValue(property));
    attrs[name] = value;
    found = found || !!value;
  }
  if (dom.nodeName === "FONT") {
    const color = safeCssValue(dom.getAttribute("color"));
    const face = safeCssValue(dom.getAttribute("face"));
    const fontSize = parseInt(dom.getAttribute("size") || "", 10);
    attrs.color = attrs.color || color;
    attrs.fontFamily = attrs.fontFamily || face;
    attrs.fontSize = attrs.fontSize || (fontSize >= 1 && fontSize <= 7 ? fontSizes[fontSize] : null);
    found = found || !!(color || face || attrs.fontSize);
  }
  return found ? attrs : false;
}

const cellBackground = {
  default: null,
  getFromDOM: dom => safeCssValue(dom.style.backgroundColor) || null,
  setDOMAttr: (value, attrs) => {
    if (value) {
      attrs.style = `background-color: ${value}`;
    }
  }
};

const tables = tableNodes({
  tableGroup: "block",
  cellContent: "block+",
  cellAttributes: { background: cellBackground }
});

export const schema = new Schema({
  nodes: {
    doc: { content: "block+" },
    paragraph: {
      content: "inline*",
      group: "block",
      attrs: blockAttrs,
      parseDOM: [{ tag: "p", getAttrs: blockAttrsFromDom }],
      toDOM: node => ["p", blockAttrsToDom(node.attrs), 0]
    },
    heading: {
      content: "inline*",
      group: "block",
      defining: true,
      attrs: { level: { default: 1 }, ...blockAttrs },
      parseDOM: [1, 2, 3, 4, 5, 6].map(level => ({ tag: `h${level}`, getAttrs: dom => ({ level, ...blockAttrsFromDom(dom) }) })),
      toDOM: node => [`h${node.attrs.level}`, blockAttrsToDom(node.attrs), 0]
    },
    blockquote: {
      content: "block+",
      group: "block",
      defining: true,
      parseDOM: [{ tag: "blockquote" }],
      toDOM: () => ["blockquote", 0]
    },
    code_block: {
      content: "text*",
      marks: "",
      group: "block",
      code: true,
      defining: true,
      parseDOM: [{ tag: "pre", preserveWhitespace: "full" }],
      toDOM: () => ["pre", ["code", 0]]
    },
    horizontal_rule: {
      group: "block",
      parseDOM: [{ tag: "hr" }],
      toDOM: () => ["hr"]
    },
    ordered_list: {
      content: "list_item+",
      group: "block",
      attrs: { order: { default: 1 } },
      parseDOM: [{
        tag: "ol",
        getAttrs: dom => {
          const start = parseInt(dom.getAttribute("start") || "1", 10);
          return { order: isNaN(start) ? 1 : start };
        }
      }],
      toDOM: node => ["ol", node.attrs.order === 1 ? {} : { start: String(node.attrs.order) }, 0]
    },
    bullet_list: {
      content: "list_item+",
      group: "block",
      parseDOM: [{ tag: "ul" }],
      toDOM: () => ["ul", 0]
    },
    list_item: {
      content: "paragraph block*",
      defining: true,
      parseDOM: [{ tag: "li" }],
      toDOM: () => ["li", 0]
    },
    ...tables,
    text: { group: "inline" },
    image: {
      inline: true,
      group: "inline",
      draggable: true,
      attrs: {
        src: {},
        alt: { default: null },
        title: { default: null },
        width: { default: null },
        height: { default: null }
      },
      parseDOM: [{
        tag: "img[src]",
        getAttrs: dom => {
          const src = safeUrl(dom.getAttribute("src"), true);
          if (!src) {
            return false;
          }
          return {
            src,
            alt: dom.getAttribute("alt") || null,
            title: dom.getAttribute("title") || null,
            width: size(dom.getAttribute("width") || dom.style.width),
            height: size(dom.getAttribute("height") || dom.style.height)
          };
        }
      }],
      toDOM: node => {
        const { src, alt, title, width, height } = node.attrs;
        const attrs = { src };
        if (alt) {
          attrs.alt = alt;
        }
        if (title) {
          attrs.title = title;
        }
        if (width) {
          attrs.width = width;
        }
        if (height) {
          attrs.height = height;
        }
        return ["img", attrs];
      }
    },
    hard_break: {
      inline: true,
      group: "inline",
      selectable: false,
      parseDOM: [{ tag: "br" }],
      toDOM: () => ["br"]
    }
  },
  marks: {
    link: {
      attrs: {
        href: {},
        title: { default: null },
        target: { default: null }
      },
      inclusive: false,
      parseDOM: [{
        tag: "a[href]",
        getAttrs: dom => {
          const href = safeUrl(dom.getAttribute("href"), false);
          if (!href) {
            return false;
          }
          const target = dom.getAttribute("target");
          return {
            href,
            title: dom.getAttribute("title") || null,
            target: linkTargets.includes(target) ? target : null
          };
        }
      }],
      toDOM: mark => {
        const { href, title, target } = mark.attrs;
        const attrs = { href };
        if (title) {
          attrs.title = title;
        }
        if (target) {
          attrs.target = target;
          if (target === "_blank") {
            attrs.rel = "noopener noreferrer";
          }
        }
        return ["a", attrs, 0];
      }
    },
    textStyle: {
      attrs: {
        color: { default: null },
        backgroundColor: { default: null },
        fontFamily: { default: null },
        fontSize: { default: null }
      },
      parseDOM: [
        { tag: "span[style]", getAttrs: textStyleFromDom },
        { tag: "font", getAttrs: textStyleFromDom }
      ],
      toDOM: mark => {
        const parts = {};
        for (const [name, property] of Object.entries(textStyleProperties)) {
          parts[property] = mark.attrs[name];
        }
        return ["span", { style: styleString(parts) }, 0];
      }
    },
    strong: {
      parseDOM: [
        { tag: "strong" },
        { tag: "b", getAttrs: dom => dom.style.fontWeight !== "normal" && null },
        { style: "font-weight", getAttrs: value => /^(bold(er)?|[5-9]\d\d)$/.test(value) && null }
      ],
      toDOM: () => ["strong", 0]
    },
    em: {
      parseDOM: [
        { tag: "em" },
        { tag: "i", getAttrs: dom => dom.style.fontStyle !== "normal" && null },
        { style: "font-style=italic" }
      ],
      toDOM: () => ["em", 0]
    },
    underline: {
      parseDOM: [
        { tag: "u" },
        { style: "text-decoration", getAttrs: value => /underline/.test(value) && null },
        { style: "text-decoration-line", getAttrs: value => /underline/.test(value) && null }
      ],
      toDOM: () => ["u", 0]
    },
    strikethrough: {
      parseDOM: [
        { tag: "s" },
        { tag: "strike" },
        { tag: "del" },
        { style: "text-decoration", getAttrs: value => /line-through/.test(value) && null },
        { style: "text-decoration-line", getAttrs: value => /line-through/.test(value) && null }
      ],
      toDOM: () => ["s", 0]
    },
    subscript: {
      excludes: "superscript",
      parseDOM: [{ tag: "sub" }, { style: "vertical-align=sub" }],
      toDOM: () => ["sub", 0]
    },
    superscript: {
      excludes: "subscript",
      parseDOM: [{ tag: "sup" }, { style: "vertical-align=super" }],
      toDOM: () => ["sup", 0]
    },
    code: {
      parseDOM: [{ tag: "code" }],
      toDOM: () => ["code", 0]
    }
  }
});

export const parser = PmDOMParser.fromSchema(schema);
export const serializer = DOMSerializer.fromSchema(schema);
export { indentStep, maxIndent };

// An inert document for parsing and serialising: nothing in it runs or loads, so HTML can be handled safely before the
// schema has cleaned it
let inertDocument = null;

export function htmlDocument() {
  if (!inertDocument) {
    inertDocument = document.implementation.createHTMLDocument("");
  }
  return inertDocument;
}

// Parses an HTML string into an element of an inert document, ready for the schema's parser
export function parseHtml(html) {
  return new window.DOMParser().parseFromString(html || "", "text/html").body;
}

export function docFromHtml(html) {
  return parser.parse(parseHtml(html));
}

export function isEmptyDoc(doc) {
  if (doc.childCount !== 1) {
    return false;
  }
  const first = doc.firstChild;
  return first.type === schema.nodes.paragraph && first.content.size === 0 && !first.attrs.textAlign && !first.attrs.indent;
}

// The HTML for a document, or "" for an empty one
export function docToHtml(doc) {
  if (isEmptyDoc(doc)) {
    return "";
  }
  const target = htmlDocument().createElement("div");
  target.appendChild(serializer.serializeFragment(doc.content, { document: htmlDocument() }));
  return target.innerHTML;
}

// Runs HTML through the schema, so what comes out is what the editor would hold
export function sanitiseHtml(html) {
  return docToHtml(docFromHtml(html));
}
