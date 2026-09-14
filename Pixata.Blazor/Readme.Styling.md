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

The full list is at the top of [pixata.css](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/wwwroot/pixata.css), and covers colours, borders, spacing, the focus ring and the grid gutter.

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
