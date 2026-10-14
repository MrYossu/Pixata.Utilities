// The editor's commands, keyed by the command names the .NET side uses. Each takes the arguments object sent from .NET and
// returns a ProseMirror command, (state, dispatch) => boolean, so the same function can be used both to run a command and,
// without a dispatch, to find out whether it can run at the moment

import { Slice } from "prosemirror-model";
import { TextSelection, NodeSelection } from "prosemirror-state";
import { toggleMark, selectAll } from "prosemirror-commands";
import { undo, redo } from "prosemirror-history";
import { wrapInList, liftListItem, sinkListItem } from "prosemirror-schema-list";
import {
  addColumnBefore, addColumnAfter, addRowBefore, addRowAfter, deleteColumn, deleteRow, deleteTable, mergeCells, splitCell
} from "prosemirror-tables";
import { schema, safeUrl, safeCssValue, maxIndent, parseHtml, parser } from "./schema.js";

const { nodes, marks } = schema;

// Selection helpers

// The from/to range of the mark of the given type around a position, eg the whole of the link the cursor is in
export function markRange($pos, type) {
  const parent = $pos.parent;
  const start = $pos.start();
  let index = $pos.index();
  let mark = type.isInSet(parent.maybeChild(index)?.marks || []);
  if (!mark && index > 0) {
    index--;
    mark = type.isInSet(parent.child(index).marks);
  }
  if (!mark) {
    return null;
  }
  let from = start;
  for (let i = 0; i < index; i++) {
    from += parent.child(i).nodeSize;
  }
  let to = from + parent.child(index).nodeSize;
  let first = index;
  while (first > 0 && mark.isInSet(parent.child(first - 1).marks)) {
    first--;
    from -= parent.child(first).nodeSize;
  }
  let last = index;
  while (last < parent.childCount - 1 && mark.isInSet(parent.child(last + 1).marks)) {
    last++;
    to += parent.child(last).nodeSize;
  }
  return { from, to, mark };
}

// The ranges a command applies to: the selection's ranges, or the whole of the text block holding an empty selection
function rangesOrBlock(state) {
  const { selection } = state;
  if (!selection.empty) {
    return selection.ranges.map(r => ({ from: r.$from.pos, to: r.$to.pos }));
  }
  const $from = selection.$from;
  return [{ from: $from.start(), to: $from.end() }];
}

// Calls fn(node, pos) for each text block (paragraph or heading) touched by the selection
function eachTextBlock(state, fn) {
  const { from, to } = state.selection;
  state.doc.nodesBetween(from, to, (node, pos) => {
    if (node.type === nodes.paragraph || node.type === nodes.heading) {
      fn(node, pos);
      return false;
    }
    return true;
  });
}

function findAncestor($pos, predicate) {
  for (let depth = $pos.depth; depth > 0; depth--) {
    const node = $pos.node(depth);
    if (predicate(node)) {
      return { node, depth, pos: $pos.before(depth) };
    }
  }
  return null;
}

function isList(node) {
  return node.type === nodes.bullet_list || node.type === nodes.ordered_list;
}

export function inList(state) {
  return !!findAncestor(state.selection.$from, isList);
}

export function inTable(state) {
  return !!findAncestor(state.selection.$from, node => node.type.spec.tableRole === "table");
}

// Text style (colour, background colour, font family and size)

function currentTextStyle(state) {
  const { selection, storedMarks } = state;
  const markSet = storedMarks || (selection.empty ? selection.$from.marks() : marksAt(state, selection.from));
  const mark = marks.textStyle.isInSet(markSet);
  return mark ? mark.attrs : {};
}

function marksAt(state, pos) {
  const $pos = state.doc.resolve(pos);
  const after = $pos.nodeAfter;
  return after ? after.marks : $pos.marks();
}

export function textStyleAt(state) {
  return currentTextStyle(state);
}

function hasValues(attrs) {
  return Object.values(attrs).some(v => v);
}

function setTextStyle(changes) {
  return (state, dispatch) => {
    const type = marks.textStyle;
    if (dispatch) {
      const tr = state.tr;
      const { selection } = state;
      if (selection.empty) {
        const existing = type.isInSet(state.storedMarks || selection.$from.marks());
        const attrs = { ...(existing ? existing.attrs : {}), ...changes };
        tr.removeStoredMark(type);
        if (hasValues(attrs)) {
          tr.addStoredMark(type.create(attrs));
        }
      } else {
        for (const range of selection.ranges) {
          const from = range.$from.pos;
          const to = range.$to.pos;
          state.doc.nodesBetween(from, to, (node, pos) => {
            if (!node.isInline) {
              return true;
            }
            const start = Math.max(pos, from);
            const end = Math.min(pos + node.nodeSize, to);
            const existing = type.isInSet(node.marks);
            const attrs = { ...(existing ? existing.attrs : {}), ...changes };
            tr.removeMark(start, end, type);
            if (hasValues(attrs)) {
              tr.addMark(start, end, type.create(attrs));
            }
            return false;
          });
        }
      }
      dispatch(tr.scrollIntoView());
    }
    return true;
  };
}

function textStyleCommand(name) {
  return args => {
    const raw = args && args.value;
    const value = raw ? safeCssValue(raw) : null;
    if (raw && !value) {
      return () => false;
    }
    return setTextStyle({ [name]: value });
  };
}

// Blocks: format, alignment and indentation

function setFormat(value) {
  const match = /^(p|h([1-6]))$/i.exec(value || "");
  if (!match) {
    return () => false;
  }
  const type = match[2] ? nodes.heading : nodes.paragraph;
  const level = match[2] ? parseInt(match[2], 10) : null;
  return (state, dispatch) => {
    let applicable = false;
    const tr = state.tr;
    eachTextBlock(state, (node, pos) => {
      applicable = true;
      const attrs = { textAlign: node.attrs.textAlign, indent: node.attrs.indent };
      if (level) {
        attrs.level = level;
      }
      tr.setNodeMarkup(pos, type, attrs);
    });
    if (applicable && dispatch) {
      dispatch(tr.scrollIntoView());
    }
    return applicable;
  };
}

export function currentFormat(state) {
  const { $from, $to } = state.selection;
  const block = $from.parent;
  if (!$from.sameParent($to) && !state.selection.empty) {
    let format = undefined;
    let mixed = false;
    eachTextBlock(state, node => {
      const value = blockFormat(node);
      if (format === undefined) {
        format = value;
      } else if (format !== value) {
        mixed = true;
      }
    });
    return mixed ? null : format || null;
  }
  return blockFormat(block);
}

function blockFormat(node) {
  if (node.type === nodes.paragraph) {
    return "p";
  }
  if (node.type === nodes.heading) {
    return `h${node.attrs.level}`;
  }
  return null;
}

function setAlignment(alignment) {
  return (state, dispatch) => {
    let applicable = false;
    let allSet = true;
    eachTextBlock(state, node => {
      applicable = true;
      allSet = allSet && node.attrs.textAlign === alignment;
    });
    if (applicable && dispatch) {
      const tr = state.tr;
      eachTextBlock(state, (node, pos) => {
        tr.setNodeMarkup(pos, null, { ...node.attrs, textAlign: allSet ? null : alignment });
      });
      dispatch(tr);
    }
    return applicable;
  };
}

export function currentAlignment(state) {
  let alignment;
  let mixed = false;
  eachTextBlock(state, node => {
    if (alignment === undefined) {
      alignment = node.attrs.textAlign;
    } else if (alignment !== node.attrs.textAlign) {
      mixed = true;
    }
  });
  return mixed ? null : alignment || null;
}

function changeIndent(delta) {
  return (state, dispatch) => {
    let applicable = false;
    const tr = state.tr;
    eachTextBlock(state, (node, pos) => {
      const indent = Math.max(0, Math.min(maxIndent, (node.attrs.indent || 0) + delta));
      if (indent !== (node.attrs.indent || 0)) {
        applicable = true;
        tr.setNodeMarkup(pos, null, { ...node.attrs, indent });
      }
    });
    if (applicable && dispatch) {
      dispatch(tr);
    }
    return applicable;
  };
}

const indent = (state, dispatch) =>
  inList(state) ? sinkListItem(nodes.list_item)(state, dispatch) : changeIndent(1)(state, dispatch);

const outdent = (state, dispatch) =>
  inList(state) ? liftListItem(nodes.list_item)(state, dispatch) : changeIndent(-1)(state, dispatch);

// Lists

function toggleList(listType) {
  return (state, dispatch) => {
    const { $from, $to } = state.selection;
    const range = $from.blockRange($to, isList);
    const list = range && range.depth >= 0 ? range.parent : null;
    if (list && isList(list) && range.depth >= 1) {
      if (list.type === listType) {
        return liftListItem(nodes.list_item)(state, dispatch);
      }
      if (dispatch) {
        dispatch(state.tr.setNodeMarkup($from.before(range.depth), listType));
      }
      return true;
    }
    return wrapInList(listType)(state, dispatch);
  };
}

export function currentList(state) {
  const { $from, $to } = state.selection;
  const range = $from.blockRange($to, isList);
  if (!range || !isList(range.parent)) {
    return null;
  }
  return range.parent.type === nodes.bullet_list ? "bullet" : "ordered";
}

// Links

export function linkInfo(state) {
  const { selection, doc } = state;
  const range = markRange(selection.$from, marks.link);
  if (range && (selection.empty || (range.from <= selection.from && range.to >= selection.to))) {
    const { href, title, target } = range.mark.attrs;
    return { href, title: title || "", target: target || "", text: doc.textBetween(range.from, range.to, " ") };
  }
  return { href: "", title: "", target: "", text: doc.textBetween(selection.from, selection.to, " ") };
}

function createLink(args) {
  const href = safeUrl(args && args.href, false);
  if (!href) {
    return () => false;
  }
  const target = args.target && ["_blank", "_self", "_parent", "_top"].includes(args.target) ? args.target : null;
  const attrs = { href, title: args.title || null, target };
  return (state, dispatch) => {
    if (!dispatch) {
      return true;
    }
    const { selection, doc } = state;
    let { from, to, empty } = selection;
    const range = markRange(selection.$from, marks.link);
    if (range && range.from <= from && range.to >= to) {
      from = range.from;
      to = range.to;
      empty = false;
    }
    const tr = state.tr;
    const text = args.text || "";
    const sameBlock = doc.resolve(from).sameParent(doc.resolve(to));
    if (empty || (text && sameBlock && text !== doc.textBetween(from, to, " "))) {
      const insert = text || href;
      const inherited = (empty ? state.storedMarks || selection.$from.marks() : doc.resolve(from).marksAcross(doc.resolve(to)) || [])
        .filter(m => m.type !== marks.link);
      tr.replaceWith(from, to, schema.text(insert, inherited));
      to = from + insert.length;
    }
    tr.removeMark(from, to, marks.link).addMark(from, to, marks.link.create(attrs));
    tr.setSelection(TextSelection.create(tr.doc, to));
    dispatch(tr.scrollIntoView());
    return true;
  };
}

function unlink(state, dispatch) {
  const { selection } = state;
  let { from, to } = selection;
  if (selection.empty) {
    const range = markRange(selection.$from, marks.link);
    if (!range) {
      return false;
    }
    from = range.from;
    to = range.to;
  } else if (!state.doc.rangeHasMark(from, to, marks.link)) {
    return false;
  }
  if (dispatch) {
    dispatch(state.tr.removeMark(from, to, marks.link));
  }
  return true;
}

// Images

export function imageInfo(state) {
  const { selection } = state;
  if (selection instanceof NodeSelection && selection.node.type === nodes.image) {
    const { src, alt, width, height } = selection.node.attrs;
    return { src, alt: alt || "", width: width || "", height: height || "" };
  }
  return { src: "", alt: "", width: "", height: "" };
}

function dimension(value) {
  const match = /^\s*(\d{1,5}(?:\.\d+)?)\s*(px|%)?\s*$/i.exec(value === null || value === undefined ? "" : String(value));
  if (!match) {
    return null;
  }
  return match[2] === "%" ? `${match[1]}%` : match[1];
}

function insertImage(args) {
  const src = safeUrl(args && args.src, true);
  if (!src) {
    return () => false;
  }
  return (state, dispatch) => {
    if (dispatch) {
      const node = nodes.image.create({
        src,
        alt: args.alt || null,
        title: args.title || null,
        width: dimension(args.width),
        height: dimension(args.height)
      });
      dispatch(state.tr.replaceSelectionWith(node).scrollIntoView());
    }
    return true;
  };
}

// Tables

function insertTable(args) {
  const requestedRows = parseInt(args && args.rows, 10);
  const requestedCols = parseInt(args && args.cols, 10);
  if (!(requestedRows > 0 && requestedCols > 0)) {
    return () => false;
  }
  const rows = Math.min(50, requestedRows);
  const cols = Math.min(20, requestedCols);
  return (state, dispatch) => {
    if (dispatch) {
      const cells = [];
      for (let c = 0; c < cols; c++) {
        cells.push(nodes.table_cell.createAndFill());
      }
      const tableRows = [];
      for (let r = 0; r < rows; r++) {
        tableRows.push(nodes.table_row.create(null, cells));
      }
      const table = nodes.table.create(null, tableRows);
      const tr = state.tr;
      const start = state.selection.from;
      tr.replaceSelectionWith(table);
      const after = tr.mapping.map(start, -1) - 1;
      let tablePos = null;
      tr.doc.descendants((node, pos) => {
        if (tablePos !== null) {
          return false;
        }
        if (node.type === nodes.table && pos >= after) {
          tablePos = pos;
          return false;
        }
        return true;
      });
      if (tablePos !== null) {
        tr.setSelection(TextSelection.near(tr.doc.resolve(tablePos + 4)));
      }
      dispatch(tr.scrollIntoView());
    }
    return true;
  };
}

// HTML

function setHtml(args) {
  return (state, dispatch) => {
    if (dispatch) {
      const doc = parser.parse(parseHtml(args && args.value));
      const tr = state.tr.replaceWith(0, state.doc.content.size, doc.content);
      tr.setSelection(TextSelection.atStart(tr.doc));
      dispatch(tr);
    }
    return true;
  };
}

function insertHtml(args) {
  return (state, dispatch) => {
    if (dispatch) {
      const body = parseHtml(args && args.value);
      const tr = state.tr;
      if (args && args.inline) {
        tr.replaceSelection(parser.parseSlice(body));
      } else {
        const doc = parser.parse(body);
        tr.replaceSelection(new Slice(doc.content, 0, 0));
      }
      dispatch(tr.scrollIntoView());
    }
    return true;
  };
}

// Removes the inline formatting (everything but links) and the alignment and indentation from the selection, or from the
// whole of the paragraph holding the cursor
function cleanFormatting(state, dispatch) {
  if (dispatch) {
    const tr = state.tr;
    for (const { from, to } of rangesOrBlock(state)) {
      for (const type of Object.values(marks)) {
        if (type !== marks.link) {
          tr.removeMark(from, to, type);
        }
      }
      state.doc.nodesBetween(from, to, (node, pos) => {
        if (node.type === nodes.paragraph || node.type === nodes.heading) {
          tr.setNodeMarkup(pos, null, { ...node.attrs, textAlign: null, indent: 0 });
          return false;
        }
        return true;
      });
    }
    tr.setStoredMarks([]);
    dispatch(tr);
  }
  return true;
}

const fixed = command => () => command;

// The commands, by name. Each entry takes the arguments sent from .NET and returns a ProseMirror command
export const commands = {
  bold: fixed(toggleMark(marks.strong)),
  italic: fixed(toggleMark(marks.em)),
  underline: fixed(toggleMark(marks.underline)),
  strikethrough: fixed(toggleMark(marks.strikethrough)),
  subscript: fixed(toggleMark(marks.subscript)),
  superscript: fixed(toggleMark(marks.superscript)),
  undo: fixed(undo),
  redo: fixed(redo),
  format: args => setFormat(args && args.value),
  fontFamily: textStyleCommand("fontFamily"),
  fontSize: textStyleCommand("fontSize"),
  foreColor: textStyleCommand("color"),
  backColor: textStyleCommand("backgroundColor"),
  alignLeft: fixed(setAlignment("left")),
  alignCenter: fixed(setAlignment("center")),
  alignRight: fixed(setAlignment("right")),
  alignJustify: fixed(setAlignment("justify")),
  insertUnorderedList: fixed(toggleList(nodes.bullet_list)),
  insertOrderedList: fixed(toggleList(nodes.ordered_list)),
  indent: fixed(indent),
  outdent: fixed(outdent),
  createLink,
  unlink: fixed(unlink),
  insertImage,
  insertTable,
  addColumnBefore: fixed(addColumnBefore),
  addColumnAfter: fixed(addColumnAfter),
  addRowBefore: fixed(addRowBefore),
  addRowAfter: fixed(addRowAfter),
  deleteColumn: fixed(deleteColumn),
  deleteRow: fixed(deleteRow),
  deleteTable: fixed(deleteTable),
  mergeCells: fixed(mergeCells),
  splitCell: fixed(splitCell),
  setHtml,
  insertHtml,
  cleanFormatting: fixed(cleanFormatting),
  selectAll: fixed(selectAll)
};

// The commands whose availability depends on the selection, so the toolbar can disable them
export const conditionalCommands = [
  "undo", "redo", "indent", "outdent", "unlink", "addColumnBefore", "addColumnAfter", "addRowBefore", "addRowAfter",
  "deleteColumn", "deleteRow", "deleteTable", "mergeCells", "splitCell"
];

// The commands that are toggled on and off, and so are shown as pressed when they apply to the selection
export function activeCommands(state) {
  const active = [];
  const { from, to, empty, $from } = state.selection;
  const markSet = state.storedMarks || $from.marks();
  const isActive = type => empty ? !!type.isInSet(markSet) : state.doc.rangeHasMark(from, to, type);
  const toggles = {
    bold: marks.strong,
    italic: marks.em,
    underline: marks.underline,
    strikethrough: marks.strikethrough,
    subscript: marks.subscript,
    superscript: marks.superscript
  };
  for (const [name, type] of Object.entries(toggles)) {
    if (isActive(type)) {
      active.push(name);
    }
  }
  const alignment = currentAlignment(state);
  if (alignment) {
    active.push({ left: "alignLeft", center: "alignCenter", right: "alignRight", justify: "alignJustify" }[alignment]);
  }
  const list = currentList(state);
  if (list) {
    active.push(list === "bullet" ? "insertUnorderedList" : "insertOrderedList");
  }
  if (markRange($from, marks.link) || (!empty && state.doc.rangeHasMark(from, to, marks.link))) {
    active.push("createLink");
  }
  return active;
}
