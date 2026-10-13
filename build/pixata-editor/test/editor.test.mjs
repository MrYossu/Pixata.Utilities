// Tests for the browser half of PixataEditor, run with "npm test" (Node's built-in test runner, with jsdom standing in for
// the browser). They cover what the .NET tests can't: the schema's sanitising, paste cleaning and the editing commands
import { test } from "node:test";
import assert from "node:assert/strict";
import { JSDOM } from "jsdom";

const dom = new JSDOM("<!DOCTYPE html><html><body></body></html>");
globalThis.window = dom.window;
globalThis.document = dom.window.document;

const { sanitiseHtml, schema, docFromHtml, docToHtml, safeUrl } = await import("../src/schema.js");
const { commands, linkInfo } = await import("../src/commands.js");
const { cleanPastedHtml } = await import("../src/paste.js");
const { formatHtml } = await import("../src/format.js");
const { EditorState, TextSelection } = await import("prosemirror-state");
const { history } = await import("prosemirror-history");

// Sanitising

test("script elements are dropped", () => {
  assert.equal(sanitiseHtml("<p>Hello</p><script>alert(1)</script>"), "<p>Hello</p>");
});

test("event handler attributes are dropped", () => {
  assert.equal(sanitiseHtml("<p onclick=\"alert(1)\">Hi <img src=\"a.png\" onerror=\"alert(1)\"></p>"), "<p>Hi <img src=\"a.png\"></p>");
});

test("javascript: links are dropped but their text is kept", () => {
  assert.equal(sanitiseHtml("<p><a href=\"javascript:alert(1)\">Click</a></p>"), "<p>Click</p>");
  assert.equal(sanitiseHtml("<p><a href=\" java&#x09;script:alert(1)\">Click</a></p>"), "<p>Click</p>");
  assert.equal(sanitiseHtml("<p><a href=\"JaVaScRiPt:alert(1)\">Click</a></p>"), "<p>Click</p>");
});

test("data: URLs are only allowed for raster images", () => {
  assert.equal(sanitiseHtml("<p><a href=\"data:text/html,<script>alert(1)</script>\">x</a></p>"), "<p>x</p>");
  assert.equal(sanitiseHtml("<p><img src=\"data:image/svg+xml;base64,PHN2Zz4=\"></p>"), "");
  assert.equal(sanitiseHtml("<p><img src=\"data:image/png;base64,iVBORw0=\"></p>"), "<p><img src=\"data:image/png;base64,iVBORw0=\"></p>");
});

test("safe links keep their attributes, and new-window links get rel", () => {
  assert.equal(sanitiseHtml("<p><a href=\"https://example.com\" title=\"Ex\" target=\"_blank\" class=\"x\" id=\"y\">Ex</a></p>"),
    "<p><a href=\"https://example.com\" title=\"Ex\" target=\"_blank\" rel=\"noopener noreferrer\">Ex</a></p>");
  assert.equal(sanitiseHtml("<p><a href=\"/relative\" target=\"evil\">R</a></p>"), "<p><a href=\"/relative\">R</a></p>");
});

test("dangerous elements are dropped with their content", () => {
  assert.equal(sanitiseHtml("<p>a</p><iframe src=\"https://evil\"></iframe><style>p{}</style><object></object><form><input></form><p>b</p>"),
    "<p>a</p><p>b</p>");
});

test("unknown style properties and unsafe values are dropped", () => {
  assert.equal(sanitiseHtml("<p><span style=\"color: red; position: fixed; background-image: url(x)\">a</span></p>"),
    "<p><span style=\"color: red;\">a</span></p>");
  assert.equal(sanitiseHtml("<p><span style=\"color: expression(alert(1))\">a</span></p>"), "<p>a</p>");
  assert.equal(sanitiseHtml("<p><span style=\"font-family: url(javascript:alert(1))\">a</span></p>"), "<p>a</p>");
});

test("formatting the editor supports is kept", () => {
  const html = "<h2 style=\"text-align: center;\">Title</h2><p style=\"margin-left: 60px;\"><strong>b</strong> <em>i</em> <u>u</u> <s>s</s> x<sub>2</sub> y<sup>3</sup></p>"
    + "<ul><li><p>one</p></li></ul><ol start=\"3\"><li><p>three</p></li></ol>"
    + "<table><tbody><tr><td><p>cell</p></td><td colspan=\"2\"><p>wide</p></td></tr></tbody></table>";
  assert.equal(sanitiseHtml(html), html);
});

test("legacy and pasted markup is converted", () => {
  assert.equal(sanitiseHtml("<p><b>b</b><i>i</i><strike>s</strike><font color=\"#ff0000\" face=\"Arial\">f</font></p>"),
    "<p><strong>b</strong><em>i</em><s>s</s><span style=\"color: rgb(255, 0, 0); font-family: Arial;\">f</span></p>");
  assert.equal(sanitiseHtml("<ul><li>bare</li></ul>"), "<ul><li><p>bare</p></li></ul>");
});

test("an empty document is the empty string", () => {
  assert.equal(sanitiseHtml(""), "");
  assert.equal(sanitiseHtml("<p></p>"), "");
  assert.equal(sanitiseHtml("<script>x</script>"), "");
});

test("safeUrl", () => {
  assert.equal(safeUrl("https://a.b/c?d=1"), "https://a.b/c?d=1");
  assert.equal(safeUrl("mailto:a@b.c"), "mailto:a@b.c");
  assert.equal(safeUrl("#top"), "#top");
  assert.equal(safeUrl("vbscript:x"), null);
  assert.equal(safeUrl("\u0000javascript:x"), null);
  assert.equal(safeUrl("data:image/png;base64,AAAA"), null);
  assert.equal(safeUrl("data:image/png;base64,AAAA", true), "data:image/png;base64,AAAA");
});

// Paste cleaning

const wordList = `
<p class=MsoListParagraphCxSpFirst style='text-indent:-18.0pt;mso-list:l0 level1 lfo1'><![if !supportLists]><span style='font-family:Symbol'><span style='mso-list:Ignore'>·<span>&nbsp;&nbsp; </span></span></span><![endif]>One<o:p></o:p></p>
<p class=MsoListParagraphCxSpMiddle style='margin-left:72.0pt;mso-list:l0 level2 lfo1'><![if !supportLists]><span style='mso-list:Ignore'>1.<span>&nbsp; </span></span><![endif]>One point one</p>
<p class=MsoListParagraphCxSpLast style='text-indent:-18.0pt;mso-list:l0 level1 lfo1'><![if !supportLists]><span style='mso-list:Ignore'>·<span>&nbsp; </span></span><![endif]>Two</p>
<p class=MsoNormal style='mso-margin-top-alt:auto;color:red'>After</p>`;

test("Word lists become real lists", () => {
  const html = sanitiseHtml(cleanPastedHtml(wordList));
  assert.equal(html, "<ul><li><p>One</p><ol><li><p>One point one</p></li></ol></li><li><p>Two</p></li></ul><p>After</p>");
});

test("Word classes and mso- styles are removed", () => {
  const cleaned = cleanPastedHtml("<p class=\"MsoNormal keep\" style=\"mso-line-height-rule:exactly; color: red\">x</p>");
  assert.equal(cleaned, "<p class=\"keep\" style=\"color: red\">x</p>");
});

test("Word lists are left alone when ConvertMsLists is off", () => {
  const cleaned = cleanPastedHtml(wordList, { convertMsLists: false });
  assert.ok(!cleaned.includes("<ul>"));
});

test("stripped tags keep their content, and attributes can be removed", () => {
  assert.equal(cleanPastedHtml("<p><span class=\"a\">x</span> <font>y</font></p>", { stripTags: ["span", "font"] }), "<p>x y</p>");
  assert.equal(cleanPastedHtml("<p id=\"a\" title=\"t\" class=\"c\">x</p>", { removeAttributes: ["id", "title"] }), "<p class=\"c\">x</p>");
  assert.equal(cleanPastedHtml("<p id=\"a\" style=\"color: red\"><a href=\"https://x\">x</a></p>", { removeAllAttributes: true }), "<p><a>x</a></p>");
});

test("comments are removed when asked", () => {
  assert.equal(cleanPastedHtml("<p>a<!-- note -->b</p>", { removeHtmlComments: true }), "<p>ab</p>");
  assert.equal(cleanPastedHtml("<p>a<!-- note -->b</p>"), "<p>a<!-- note -->b</p>");
});

// Formatting for the View HTML dialog

test("formatted HTML reads back as the same document", () => {
  const html = "<h1>T</h1><ul><li><p>a</p><ul><li><p>b</p></li></ul></li></ul><table><tbody><tr><td><p>c</p></td></tr></tbody></table><p>x <strong>y</strong></p>";
  const formatted = formatHtml(html);
  assert.ok(formatted.includes("\n  <li>"));
  assert.equal(sanitiseHtml(formatted), html);
});

// Commands

function stateFor(html, select) {
  let state = EditorState.create({ doc: docFromHtml(html), plugins: [history()] });
  if (select) {
    state = state.apply(state.tr.setSelection(select(state.doc)));
  }
  return state;
}

function run(state, name, args) {
  let result = state;
  const applied = commands[name](args || {})(state, tr => {
    result = state.apply(tr);
  });
  return { applied, state: result, html: docToHtml(result.doc) };
}

const selectAllText = doc => TextSelection.create(doc, 1, doc.firstChild.nodeSize - 1);

test("bold toggles strong", () => {
  const { html, state } = run(stateFor("<p>abc</p>", selectAllText), "bold");
  assert.equal(html, "<p><strong>abc</strong></p>");
  assert.equal(run(state, "bold").html, "<p>abc</p>");
});

test("format changes the block type and keeps the alignment", () => {
  const { html } = run(stateFor("<p style=\"text-align: right;\">abc</p>", selectAllText), "format", { value: "h3" });
  assert.equal(html, "<h3 style=\"text-align: right;\">abc</h3>");
  assert.equal(run(stateFor("<p>abc</p>"), "format", { value: "script" }).applied, false);
});

test("alignment toggles", () => {
  const { html, state } = run(stateFor("<p>abc</p>"), "alignCenter");
  assert.equal(html, "<p style=\"text-align: center;\">abc</p>");
  assert.equal(run(state, "alignCenter").html, "<p>abc</p>");
});

test("text styles merge into one span", () => {
  let { state } = run(stateFor("<p>abc</p>", selectAllText), "foreColor", { value: "#ff0000" });
  ({ state } = run(state, "fontSize", { value: "large" }));
  assert.equal(docToHtml(state.doc), "<p><span style=\"color: rgb(255, 0, 0); font-size: large;\">abc</span></p>");
  assert.equal(run(state, "foreColor", { value: "url(x)" }).applied, false);
  assert.equal(run(state, "foreColor", { value: "" }).html, "<p><span style=\"font-size: large;\">abc</span></p>");
});

test("lists toggle and switch type", () => {
  let { html, state } = run(stateFor("<p>abc</p>"), "insertUnorderedList");
  assert.equal(html, "<ul><li><p>abc</p></li></ul>");
  ({ html, state } = run(state, "insertOrderedList"));
  assert.equal(html, "<ol><li><p>abc</p></li></ol>");
  assert.equal(run(state, "insertOrderedList").html, "<p>abc</p>");
});

test("indent and outdent change the margin outside lists", () => {
  const { html, state } = run(stateFor("<p>abc</p>"), "indent");
  assert.equal(html, "<p style=\"margin-left: 30px;\">abc</p>");
  assert.equal(run(state, "outdent").html, "<p>abc</p>");
  assert.equal(run(stateFor("<p>abc</p>"), "outdent").applied, false);
});

test("createLink refuses unsafe URLs and wraps the selection", () => {
  assert.equal(run(stateFor("<p>abc</p>", selectAllText), "createLink", { href: "javascript:alert(1)" }).applied, false);
  const { html, state } = run(stateFor("<p>abc</p>", selectAllText), "createLink", { href: "https://x.y", target: "_blank", title: "T" });
  assert.equal(html, "<p><a href=\"https://x.y\" title=\"T\" target=\"_blank\" rel=\"noopener noreferrer\">abc</a></p>");
  const cursor = state.apply(state.tr.setSelection(TextSelection.create(state.doc, 2)));
  assert.deepEqual(linkInfo(cursor), { href: "https://x.y", title: "T", target: "_blank", text: "abc" });
  assert.equal(run(cursor, "unlink").html, "<p>abc</p>");
});

test("createLink inserts the text when nothing is selected", () => {
  const { html } = run(stateFor("<p>ab</p>", doc => TextSelection.create(doc, 2)), "createLink", { href: "https://x.y", text: "link" });
  assert.equal(html, "<p>a<a href=\"https://x.y\">link</a>b</p>");
});

test("insertImage refuses unsafe sources", () => {
  assert.equal(run(stateFor("<p>a</p>"), "insertImage", { src: "javascript:alert(1)" }).applied, false);
  assert.equal(run(stateFor("<p>a</p>", doc => TextSelection.create(doc, 2)), "insertImage", { src: "/a.png", alt: "A", width: "100px" }).html,
    "<p>a<img src=\"/a.png\" alt=\"A\" width=\"100\"></p>");
});

test("insertTable inserts a table and puts the cursor in it", () => {
  const { html, state } = run(stateFor("<p></p>"), "insertTable", { rows: 2, cols: 3 });
  assert.equal((html.match(/<tr>/g) || []).length, 2);
  assert.equal((html.match(/<td>/g) || []).length, 6);
  assert.equal(state.selection.$from.parent.type, schema.nodes.paragraph);
  assert.equal(state.selection.$from.node(-1).type, schema.nodes.table_cell);
  const added = run(state, "addColumnAfter");
  assert.equal((added.html.match(/<td>/g) || []).length, 8);
  assert.equal(run(added.state, "deleteTable").html, "");
});

test("table commands don't apply outside a table", () => {
  assert.equal(run(stateFor("<p>a</p>"), "addRowAfter").applied, false);
  assert.equal(run(stateFor("<p>a</p>"), "mergeCells").applied, false);
});

test("setHtml replaces the document through the schema, and can be undone", () => {
  const { html, state } = run(stateFor("<p>old</p>"), "setHtml", { value: "<h1>new</h1><script>alert(1)</script>" });
  assert.equal(html, "<h1>new</h1>");
  assert.equal(run(state, "undo").html, "<p>old</p>");
});

test("insertHtml inserts at the cursor", () => {
  const { html } = run(stateFor("<p>ab</p>", doc => TextSelection.create(doc, 2)), "insertHtml", { value: "<strong>X</strong>", inline: true });
  assert.equal(html, "<p>a<strong>X</strong>b</p>");
});

test("cleanFormatting removes marks and alignment but keeps links", () => {
  const { html } = run(stateFor("<p style=\"text-align: center\"><strong><a href=\"/x\">ab</a></strong><em>c</em></p>"), "cleanFormatting");
  assert.equal(html, "<p><a href=\"/x\">ab</a>c</p>");
});
