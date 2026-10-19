# Styling

>Part of [Pixata.Blazor](Readme.md)

The components used to expect the host app to have Bootstrap loaded, as they used a handful of its layout, form, button and utility classes. That was fine when I wrote them (I used Bootstrap in every app), but it meant that this package quietly depended on something it doesn't reference, and that apps which use a different CSS framework (or none) got components that looked broken. Worse, the classes it used (`form-group`, `input-group-prepend`, `pl-2`) were Bootstrap 4 ones that were removed in Bootstrap 5, so even Bootstrap apps were only half getting what they should.

As of v3.0.0, the package brings its own styles. Add this line to your `App.razor` (or `_Host.cshtml`, or `index.html`, depending on your project type)...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and you no longer need Bootstrap for these components. If you do use Bootstrap (or Tailwind, or anything else), you can carry on doing so. Every class in the file is prefixed with `pixata-`, so nothing in it can clash with your own styles.

The sizes and colours are copied from Bootstrap 4.3.1 (MIT licensed), which is what the components were written against, so they look the same as they always did.

## Making them fit your theme

The stylesheet is written in terms of CSS custom properties, so you can restyle the components without having to fight the specificity of individual rules. Override whichever ones you want in your own CSS, after the link above...

```css
:root {
  --pixata-primary: #6f42c1;
  --pixata-primary-hover: #59359a;
  --pixata-border-radius: 0;
  --pixata-danger: #b02a37;
}
```

The full list (including `--pixata-label-font-size`, `--pixata-label-font-weight` and `--pixata-label-color` for the form labels) is at the top of [pixata.css](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/wwwroot/pixata.css), and covers colours, borders, spacing, the focus ring and the grid gutter. The `--pixata-grid-*` properties are for `PixataGrid`, and have their own defaults rather than Bootstrap's. See [Grid](Readme.Grid.md#styling).

The [audit viewer](Readme.AuditViewer.md#styling), [`Confirm` and `Inform`](Readme.Containers.md#styling-confirm-and-inform) and the [notifications](Readme.Notifications.md#icons-and-colours) use these variables too, along with a few of their own, which are listed on their pages. Prior to v5.4.0, their colours were hard-coded.

The classes used by those components all start with `pixata-blazor-`, followed by the component name where they're specific to one component (such as `pixata-blazor-grid-pager`), so you can target them in your own CSS without clashing with anything else.

## The newer components

The components added in v5.0.0 (the drop-downs, tab strip, editor, file upload, dialogs, window, wizard and stepper, radio group, switch, chart, drop-down and split buttons, and scheduler) have their own look, and their own set of custom properties, all starting with `--pixata-blazor-`. Their classes start with `pixata-blazor-`, and the component-specific ones include the component's name, eg `pixata-blazor-window-titlebar`.

They are restyled the same way. Override the variables after the link above, either on `:root`, or on any element to restyle just the components inside it...

```css
:root {
  --pixata-blazor-primary: #6f42c1;
  --pixata-blazor-primary-hover: #59359a;
  --pixata-blazor-border-radius: 0;
}
```

The ones they all share (the font, the text and background colours, a set for each theme colour, borders, inputs, popups and the focus ring) are at the start of the components section of [pixata.css](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/wwwroot/pixata.css), and each component's documentation page lists the ones that are specific to that component.

Dialogs and windows are moved to the end of the page when they open, so variables set on a container don't reach them. Set theirs on `:root`, or on a class you give them through their `Class` parameter.

## If you were styling the components yourself

The class names in the rendered markup have changed, so if you had CSS of your own targetting them, you'll need to update your selectors. The mapping is a simple one...

| Was | Now |
| --- | --- |
| `row`, `col-lg-2`, `col-lg-10`, `col-1` | `pixata-row`, `pixata-col-2`, `pixata-col-10`, `pixata-col-1` |
| `form-group`, `form-control`, `col-form-label` | `pixata-form-group`, `pixata-form-control`, `pixata-col-form-label` |
| `input-group`, `input-group-prepend`, `input-group-append`, `input-group-text` | the same names, prefixed with `pixata-` |
| `btn`, `btn-primary`, `btn-secondary`, `btn-sm`, `btn-link`, `btn-close` | the same names, prefixed with `pixata-` |
| `card`, `card-header`, `card-body`, `card-footer`, `alert`, `badge` | the same names, prefixed with `pixata-` |
| Utilities (`d-flex`, `h-100`, `mt-3`, `text-danger`, and so on) | the same names, prefixed with `pixata-` |
| `invalid` (added to a form row's input when validation fails) | `pixata-invalid`, which now comes with a red border of its own |

The `SpinnerClass` parameters on `Busy`, `Confirm` and `LoadingOption` now default to `pixata-spinner pixata-spinner-sm` rather than the Bootstrap `spinner-border spinner-border-sm`. If you were passing your own value (a Font Awesome class, for example), nothing changes.
