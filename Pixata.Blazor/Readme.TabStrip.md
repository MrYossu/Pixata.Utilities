# Tab strip

>Part of [Pixata.Blazor](Readme.md)

`PixataTabStrip` shows a row (or column) of tabs, each with its own content, and shows the content of the tab the user selects. The tabs are declared with `PixataTab` components inside the tab strip.

It works in Blazor Server, WebAssembly and Auto apps, can be prerendered, and needs no registration: add `@using Pixata.Blazor.Components` to your `_Imports.razor` and make sure your host page includes `_content/Pixata.Blazor/pixata.css` (see [Styling](Readme.Styling.md)).

You can see it in action on the [tab strip sample page](https://test.pixata.co.uk/TabStripSample) ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TabStripSample.razor)).

## Basic use

```razor
<PixataTabStrip @bind-ActiveTabId="_activeTab">
  <PixataTab Id="general" Title="General">
    <p>The general settings go here.</p>
  </PixataTab>
  <PixataTab Id="advanced" Title="Advanced">
    <p>The advanced settings go here.</p>
  </PixataTab>
  <PixataTab Id="billing" Title="Billing" Disabled="true">
    <p>Not available yet.</p>
  </PixataTab>
</PixataTabStrip>

@code {
  private string _activeTab = "advanced";
}
```

If you don't set `ActiveTabId` (or it doesn't match a tab that can be shown), the first visible tab that isn't disabled is selected.

A tab's content can go straight inside the `PixataTab`, as above, or in a `<Content>` tag, which you need when you also use a `<HeaderTemplate>`...

```razor
<PixataTab Id="inbox">
  <HeaderTemplate>
    <PixataIcon Icon="PixataSvgIcons.Info" /> Inbox <span class="badge">3</span>
  </HeaderTemplate>
  <Content>
    <p>You have 3 new messages.</p>
  </Content>
</PixataTab>
```

## `PixataTabStrip` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `ActiveTabId` | `string?` | `null` | The `Id` of the selected tab. Use `@bind-ActiveTabId` for two-way binding. |
| `ActiveTabIdChanged` | `EventCallback<string>` | | Raised when the user selects another tab, or the selected tab is closed, hidden or removed (in which case its neighbour is selected). |
| `TabPosition` | `PixataTabPosition` | `Top` | Where the tabs go: `Top`, `Bottom`, `Left` or `Right` of the content. |
| `TabAlignment` | `PixataTabStripTabAlignment` | `Start` | How the tabs line up: `Start`, `End`, `Center`, `Justify` (spread out with space between them) or `Stretched` (sharing the whole width). |
| `PersistTabContent` | `bool` | `false` | When false, only the selected tab's content is rendered, so it is created afresh each time the tab is selected. When true, a tab's content is kept (hidden) once it has been shown, so anything the user typed survives switching tabs. |
| `Scrollable` | `bool` | `false` | When true, the tabs stay on one line and scroll when they don't fit, rather than wrapping. Recommended for anything used on phones (see below). |
| `OverflowMode` | `PixataTabStripOverflowMode` | `None` | Another way of setting `Scrollable`: `Scroll` is the same as `Scrollable="true"`. |
| `ScrollButtonsPosition` | `PixataTabStripScrollButtonsPosition` | `Split` | Where the scroll buttons go: `Split` (one at each end), `Start` or `End` (both together). |
| `ScrollButtonsVisibility` | `PixataTabStripScrollButtonsVisibility` | `Auto` | `Auto` shows the scroll buttons only when the tabs don't fit, `Visible` always shows them, and `Hidden` never does (the tabs can still be swiped or scrolled with the keyboard). |
| `Size` | `string?` | `"md"` | The size of the tabs: `"sm"`, `"md"` or `"lg"` (see `PixataThemeConstants.Size`). |
| `Width` | `string?` | | The width of the tab strip, as a CSS length. |
| `Height` | `string?` | | The height of the tab strip, as a CSS length. The content scrolls if it doesn't fit. |
| `Id` | `string?` | | The `id` of the tab strip's outer element. |
| `Class` | `string?` | | Extra CSS classes for the tab strip's outer element. |
| `TabStripSuffixTemplate` | `RenderFragment?` | | Content shown after the tabs, in the same row, eg a button that adds a tab. |
| `OnTabClose` | `EventCallback<PixataTabStripTabCloseEventArgs>` | | Raised when the user closes a tab (see below). |
| `CloseButtonLabel` | `string` | `"Close"` | The tooltip and accessible name of the close buttons. |

Any other attributes (eg `aria-label` or `data-*`) are rendered on the tab strip's outer element.

### Methods

| Method | Description |
|---|---|
| `Refresh()` | Redraws the tab strip and its tabs, for when you change something the tabs show without the tab strip's parent re-rendering. |

## `PixataTab` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | | The text shown in the tab. Ignored if there is a `HeaderTemplate`. |
| `HeaderTemplate` | `RenderFragment?` | | Custom content for the tab itself, such as an icon or a badge. |
| `ChildContent` / `Content` | `RenderFragment?` | | What the tab shows when it is selected. Use a `<Content>` tag when you also have a `<HeaderTemplate>`. |
| `Id` | `string?` | generated | Identifies the tab for `ActiveTabId`. If you don't set one, a unique id is generated, but you will want your own if you bind `ActiveTabId`. |
| `Disabled` | `bool` | `false` | Greys the tab out, so it can't be selected with the mouse or the keyboard. |
| `Visible` | `bool` | `true` | When false, the tab isn't shown at all. |
| `VisibleChanged` | `EventCallback<bool>` | | Raised with false when the user closes the tab. Use `@bind-Visible` to know when a tab has been closed, and to show it again. |
| `Closeable` | `bool` | `false` | Gives the tab a close button. |
| `Class` | `string?` | | Extra CSS classes for the tab itself (not its content). |

`PixataTab` also has a `Refresh()` method, which redraws the tab.

A `PixataTab` must be inside a `PixataTabStrip`, and throws an exception if it isn't.

## Closing tabs

Set `Closeable="true"` on a tab to give it a close button. When the user clicks it (or presses Delete while the tab has the focus), the tab strip raises `OnTabClose`, which receives a `PixataTabStripTabCloseEventArgs` with the closing tab's `TabId`. Set `IsCancelled` to true to keep the tab open. Otherwise the tab is hidden, its `VisibleChanged` is raised with false, and if it was the selected tab, the next tab (or failing that, the previous one) is selected.

If your tabs come from a collection, remove the closed one from the collection in `OnTabClose`...

```razor
<PixataTabStrip @bind-ActiveTabId="_activeTab" OnTabClose="CloseDocument">
  @foreach (Document document in _documents) {
    <PixataTab @key="document.Id" Id="@document.Id" Title="@document.Name" Closeable="@(!document.Pinned)">
      <DocumentEditor Document="document" />
    </PixataTab>
  }
  <TabStripSuffixTemplate>
    <button type="button" class="@PixataCss.Button(null, PixataThemeConstants.Button.FillMode.Flat, null, null, true)"
            title="New document" @onclick="AddDocument">
      <PixataIcon Icon="PixataSvgIcons.Plus" />
    </button>
  </TabStripSuffixTemplate>
</PixataTabStrip>

@code {
  private void CloseDocument(PixataTabStripTabCloseEventArgs args) {
    Document? document = _documents.FirstOrDefault(d => d.Id == args.TabId);
    if (document?.HasUnsavedChanges == true) {
      args.IsCancelled = true;
      return;
    }
    _documents.Remove(document!);
  }
}
```

## Adding and removing tabs

Tabs can be added, removed and moved at any time, with `@if`, `@foreach` or by changing `Visible`. Tabs are always shown in the order they are declared, even when one is added between existing ones, or the collection a `@foreach` loops over is sorted. Give tabs generated in a loop a `@key`, so Blazor keeps each tab's content (and, with `PersistTabContent`, its state) with the right tab.

If the selected tab is removed or hidden, its neighbour is selected and `ActiveTabIdChanged` is raised.

## Phones and scrolling tabs

By default, tabs that don't fit wrap onto another line. On a phone that quickly takes up a lot of the screen, so set `Scrollable="true"`. The tabs then stay on one line, and the user can swipe them sideways, use the scroll buttons, or move through them with the keyboard (the selected tab is always scrolled into view). Scroll buttons are only shown when the tabs don't fit, unless you change `ScrollButtonsVisibility`.

```razor
<PixataTabStrip Scrollable="true" ScrollButtonsPosition="PixataTabStripScrollButtonsPosition.End">
  @foreach (string month in Months) {
    <PixataTab Id="@month" Title="@month">Figures for @month</PixataTab>
  }
</PixataTabStrip>
```

On touch screens, the tabs, close buttons and scroll buttons are made big enough to tap easily.

## Keyboard and accessibility

The tab strip follows the WAI-ARIA tabs pattern: the tabs have `role="tab"` inside a `role="tablist"`, and each tab's content is a `role="tabpanel"` linked to its tab with `aria-controls` and `aria-labelledby`. Only the selected tab is in the Tab key order.

| Key | Action |
|---|---|
| Left or Up arrow | Select the previous tab, skipping disabled ones, and wrapping round to the last tab from the first. |
| Right or Down arrow | Select the next tab, skipping disabled ones, and wrapping round to the first tab from the last. |
| Home / End | Select the first or last tab. |
| Delete | Close the selected tab, if it is closeable. |

## Styling

The tab strip uses the shared theme variables (see [Styling](Readme.Styling.md)), plus these, which you can override on `:root` or on any element containing the tab strip...

| Variable | Default | Used for |
|---|---|---|
| `--pixata-blazor-tabstrip-color` | `var(--pixata-blazor-text-color)` | The tabs' text. |
| `--pixata-blazor-tabstrip-hover-color` | `var(--pixata-blazor-primary)` | A tab's text when the mouse is over it. |
| `--pixata-blazor-tabstrip-active-color` | `var(--pixata-blazor-primary)` | The selected tab's text. |
| `--pixata-blazor-tabstrip-active-bg` | `var(--pixata-blazor-surface-bg)` | The selected tab's background. |
| `--pixata-blazor-tabstrip-border-color` | `var(--pixata-blazor-border-color)` | The line under the tabs, and the borders of the selected tab and the content. |
| `--pixata-blazor-tabstrip-content-bg` | `var(--pixata-blazor-surface-bg)` | The content's background. |
| `--pixata-blazor-tabstrip-content-padding` | `16px` | The padding around the content. |
| `--pixata-blazor-tabstrip-radius` | `var(--pixata-blazor-border-radius)` | The rounding of the selected tab's corners. |
| `--pixata-blazor-tabstrip-focus-ring` | an inset 2px shadow | The focus indicator on a tab focused with the keyboard. |

The main CSS classes are...

| Class | Element |
|---|---|
| `pixata-blazor-tabstrip` | The outer element, which also has `pixata-blazor-tabstrip-{top,bottom,left,right}`, `pixata-blazor-tabstrip-{sm,md,lg}` and, when scrollable, `pixata-blazor-tabstrip-scrollable`. |
| `pixata-blazor-tabstrip-items-wrapper` | The row holding the tabs, the scroll buttons and the suffix template. |
| `pixata-blazor-tabstrip-items` | The list of tabs, which also has `pixata-blazor-tabstrip-items-{start,end,center,justify,stretched}`. |
| `pixata-blazor-tabstrip-item` | A tab, with `pixata-blazor-active` when it is selected and `pixata-blazor-disabled` when it is disabled. |
| `pixata-blazor-tabstrip-link`, `pixata-blazor-tabstrip-link-text` | The clickable part of a tab, and its title. |
| `pixata-blazor-tabstrip-close` | A tab's close button. |
| `pixata-blazor-tabstrip-scroll-button` | The scroll buttons, with `pixata-blazor-tabstrip-prev` or `pixata-blazor-tabstrip-next`. |
| `pixata-blazor-tabstrip-actions` | The container of the `TabStripSuffixTemplate`. |
| `pixata-blazor-tabstrip-content` | A tab's content. |
