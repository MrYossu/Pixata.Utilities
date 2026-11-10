// Draws mark-up (notes, highlights, strike-through, drawing and text) over a page, as SVG. The mark-up's coordinates are the
// page's own, in points from its bottom left, as a PDF's annotations are, so they don't change when the page is zoomed or
// turned. Pixata.Pdf's PdfMarkupFlattener draws the same mark-up into a PDF, so the two should be changed together

const svgNamespace = "http://www.w3.org/2000/svg";

export const highlightOpacity = 0.4;
const lineHeight = 1.2;
const noteRadius = 7;

export const element = (name, attributes = {}, parent = null) => {
  const created = document.createElementNS(svgNamespace, name);
  for (const [attribute, value] of Object.entries(attributes)) {
    if (value !== null && value !== undefined) {
      created.setAttribute(attribute, String(value));
    }
  }
  parent?.appendChild(created);
  return created;
};

export const round = value => Math.round(value * 100) / 100;

// A matrix that draws something upright, as the page is normally shown, with (0, 0) at the point given and y going down. A
// page with /Rotate is shown turned clockwise, so what's drawn on it is turned the other way in its coordinates
export function upright(point, rotate) {
  const radians = rotate * Math.PI / 180;
  const cos = Math.round(Math.cos(radians));
  const sin = Math.round(Math.sin(radians));
  return `matrix(${cos} ${sin} ${sin} ${-cos} ${point.x} ${point.y})`;
}

// A strike-through goes along a rectangle's longer side, which is the way its text runs. For one that's nearly square, such
// as one letter, it goes the way text runs as the page is normally shown
export function strikesAcross(rect, rotate) {
  const width = rect.right - rect.left;
  const height = rect.top - rect.bottom;
  return width > height * 1.2 || (height <= width * 1.2 && rotate % 180 === 0);
}

const rightToLeft = /[\p{Script=Hebrew}\p{Script=Arabic}\p{Script=Syriac}\p{Script=Thaana}\p{Script=Nko}]/u;
const strong = /[\p{L}\p{Nd}]/u;

// Whether a line reads from the right, which is when its first letter is a right-to-left one
export function isRightToLeft(line) {
  const first = [...line].find(c => strong.test(c));
  return !!first && rightToLeft.test(first);
}

export const lines = text => (text ?? "").replace(/\r\n?/g, "\n").split("\n");

// Draws the items on the page into the SVG, replacing what was there. The SVG is the size of the page as it's shown, turned
// by rotation (the page's own /Rotate and any turning in the viewer)
export function drawPage(svg, items, pdfPage, rotation, selectedId = null) {
  const viewport = pdfPage.getViewport({ scale: 1, rotation });
  svg.setAttribute("viewBox", `0 0 ${viewport.width} ${viewport.height}`);
  svg.replaceChildren();
  const page = element("g", { transform: `matrix(${viewport.transform.join(" ")})` }, svg);
  const rotate = pdfPage.rotate;
  // Highlights go first, so the other mark-up is drawn over them
  const sorted = [...items].sort((a, b) => (a.type === "Highlight" ? 0 : 1) - (b.type === "Highlight" ? 0 : 1));
  for (const item of sorted) {
    const group = element("g", {
      "data-id": item.id ?? null,
      class: [
        "pixata-blazor-pdfviewer-markup-item",
        `pixata-blazor-pdfviewer-markup-${item.type.toLowerCase()}`,
        item.id && item.id === selectedId ? "pixata-blazor-pdfviewer-markup-selected" : null,
        item.id ? null : "pixata-blazor-pdfviewer-markup-pending"
      ].filter(Boolean).join(" ")
    }, page);
    drawItem(group, item, rotate);
  }
  return page;
}

function drawItem(group, item, rotate) {
  const colour = item.colour || "#d32f2f";
  switch (item.type) {
    case "Highlight":
      for (const rect of item.rects ?? []) {
        element("rect", { x: rect.left, y: rect.bottom, width: rect.right - rect.left, height: rect.top - rect.bottom, fill: colour, "fill-opacity": highlightOpacity }, group);
      }
      break;
    case "StrikeThrough":
      for (const rect of item.rects ?? []) {
        const across = strikesAcross(rect, rotate);
        const thickness = Math.max(1, (across ? rect.top - rect.bottom : rect.right - rect.left) * 0.08);
        const middle = across ? (rect.bottom + rect.top) / 2 : (rect.left + rect.right) / 2;
        element("rect", { x: rect.left, y: rect.bottom, width: rect.right - rect.left, height: rect.top - rect.bottom, fill: "transparent", class: "pixata-blazor-pdfviewer-markup-hit" }, group);
        element("line", across
          ? { x1: rect.left, y1: middle, x2: rect.right, y2: middle, stroke: colour, "stroke-width": thickness }
          : { x1: middle, y1: rect.bottom, x2: middle, y2: rect.top, stroke: colour, "stroke-width": thickness }, group);
      }
      break;
    case "Ink": {
      const width = item.lineWidth > 0 ? item.lineWidth : 2;
      for (const stroke of item.strokes ?? []) {
        const d = inkPath(stroke);
        if (d) {
          element("path", { d, fill: "none", stroke: "transparent", "stroke-width": Math.max(width, 10), "stroke-linecap": "round", "stroke-linejoin": "round", class: "pixata-blazor-pdfviewer-markup-hit" }, group);
          element("path", { d, fill: "none", stroke: colour, "stroke-width": width, "stroke-linecap": "round", "stroke-linejoin": "round" }, group);
        }
      }
      break;
    }
    case "Text": {
      if (!item.position || !item.text) {
        break;
      }
      const size = item.fontSize > 0 ? item.fontSize : 12;
      const text = element("g", { transform: upright(item.position, rotate) }, group);
      // A box behind the text, so it can be clicked between the letters. Its size is worked out once the text is on the page
      element("rect", { x: 0, y: 0, width: 0, height: 0, fill: "transparent", class: "pixata-blazor-pdfviewer-markup-hit" }, text);
      lines(item.text).forEach((line, index) => {
        const rtl = isRightToLeft(line);
        element("text", {
          x: 0,
          y: round(size * (1 + index * lineHeight)),
          "font-size": size,
          "font-family": "Helvetica, Arial, sans-serif",
          fill: colour,
          direction: rtl ? "rtl" : "ltr",
          "text-anchor": rtl ? "end" : "start",
          "xml:space": "preserve"
        }, text).textContent = line;
      });
      break;
    }
    case "Note": {
      if (!item.position) {
        break;
      }
      const note = element("g", { transform: upright(item.position, rotate) }, group);
      element("circle", { cx: 0, cy: 0, r: noteRadius, fill: colour, stroke: "#fff", "stroke-width": 1 }, note);
      element("path", { d: "M-3.5 -2.5h7M-3.5 0h7M-3.5 2.5h4.5", stroke: "#fff", "stroke-width": 1.2, "stroke-linecap": "round", fill: "none" }, note);
      break;
    }
  }
}

// A line through the points. A single point is drawn as a dot, which needs a line, however short, to have round ends
export function inkPath(points) {
  if (!points?.length) {
    return "";
  }
  const [first, ...rest] = points;
  return `M${first.x} ${first.y}` + (rest.length ? rest.map(p => `L${p.x} ${p.y}`).join("") : `L${first.x + 0.01} ${first.y}`);
}

// Gives each text item's box the size of its text, which can only be measured once the SVG is in the page
export function sizeTextBoxes(svg) {
  for (const box of svg.querySelectorAll(".pixata-blazor-pdfviewer-markup-text rect")) {
    const text = box.parentNode;
    let bounds = null;
    for (const line of text.querySelectorAll("text")) {
      const b = line.getBBox();
      bounds = bounds
        ? { x1: Math.min(bounds.x1, b.x), y1: Math.min(bounds.y1, b.y), x2: Math.max(bounds.x2, b.x + b.width), y2: Math.max(bounds.y2, b.y + b.height) }
        : { x1: b.x, y1: b.y, x2: b.x + b.width, y2: b.y + b.height };
    }
    if (bounds) {
      box.setAttribute("x", round(bounds.x1 - 2));
      box.setAttribute("y", round(bounds.y1 - 2));
      box.setAttribute("width", round(bounds.x2 - bounds.x1 + 4));
      box.setAttribute("height", round(bounds.y2 - bounds.y1 + 4));
    }
  }
}

// Joins the rectangles of a text selection that are on the same line, as a selection has one for each piece of text. The
// rectangles are the page's, in CSS pixels, and lines run across (horizontal) or down (vertical) the screen
export function joinLines(rects, horizontal) {
  const joined = [];
  const start = r => (horizontal ? r.top : r.left);
  const end = r => (horizontal ? r.bottom : r.right);
  for (const rect of [...rects].sort((a, b) => start(a) - start(b) || (horizontal ? a.left - b.left : a.top - b.top))) {
    const line = joined.find(l => {
      const overlap = Math.min(end(l), end(rect)) - Math.max(start(l), start(rect));
      return overlap > Math.min(end(l) - start(l), end(rect) - start(rect)) * 0.5;
    });
    if (line) {
      line.left = Math.min(line.left, rect.left);
      line.top = Math.min(line.top, rect.top);
      line.right = Math.max(line.right, rect.right);
      line.bottom = Math.max(line.bottom, rect.bottom);
    } else {
      joined.push({ left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom });
    }
  }
  return joined;
}
