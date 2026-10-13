# pixata-editor

The source of `Pixata.Blazor/wwwroot/pixata-editor.js`, the browser half of `PixataEditor`. It is built on
[ProseMirror](https://prosemirror.net) (MIT licensed), bundled with [esbuild](https://esbuild.github.io) into one minified
ES module. The built file is committed, so you only need this folder when you change the editor's JavaScript or update
ProseMirror. The `.csproj` files don't build it, and nothing here is packed into the NuGet package except the built file.

| File | What's in it |
| --- | --- |
| `src/schema.js` | The document schema, which is also what sanitises the HTML (see below) |
| `src/commands.js` | The commands the toolbar and `ExecuteAsync()` run, keyed by command name |
| `src/paste.js` | The paste cleaning that `PixataEditorPasteSettings` controls |
| `src/format.js` | Lays out the HTML for the View HTML dialog |
| `src/index.js` | The functions the .NET component calls: creating the editor, setting the value, running commands, the toolbar's keyboard handling and overflow |
| `build.mjs` | Builds the bundle, with the licence notices of the bundled packages in its header |
| `test/` | Tests for the schema, paste cleaning and commands, run in Node with jsdom |

## Rebuilding

You need Node.js 18 or later.

```bash
cd build/pixata-editor
npm ci
npm test
npm run build
```

`npm run build` writes `Pixata.Blazor/wwwroot/pixata-editor.js`. Commit it along with your changes to `src/`.

The package versions in `package.json` are pinned exactly, and `package-lock.json` is committed, so a rebuild gives the same
bundle. To update ProseMirror, install the new versions with `npm install --save-exact <package>@<version>`, run the tests,
rebuild, and check the editor on the sample site's `/EditorSample` page.

## Sanitising

Every piece of HTML that reaches the editor (the bound value, pasted or dropped content, `setHtml` and `insertHtml`) is
parsed against the schema in `src/schema.js`, and the value the editor emits is serialised from it. Anything the schema
doesn't describe is dropped: `script`, `style`, `iframe`, `object`, forms and so on, every attribute other than the few the
schema reads (so no `on*` event handlers, `id` or `class`), and every style property other than text alignment,
indentation, colour, background colour, font family and font size. Links and images are only kept when their URL is
relative or uses `http`, `https`, `mailto` or `tel` (and, for images, a base64 `data:image/...` URL), and CSS values
containing `url()`, `expression()` and the like are refused. The tests in `test/editor.test.mjs` check this.

The .NET component also runs the value through `PixataHtmlSanitiser` in both directions, as a browser can't be trusted to
do this on the server's behalf.
