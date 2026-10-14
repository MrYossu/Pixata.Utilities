# Wizard and stepper

>Part of [Pixata.Blazor](Readme.md)

`PixataWizard` takes the user through a task one step at a time, with a stepper showing where they are, and Previous, Next and Done buttons. It can validate each step before the user moves on.

`PixataStepper` is the stepper on its own: a row (or column) of numbered steps showing how far the user has got through a process, which they can click to move between steps. The wizard uses it, but you can use it anywhere, eg above a series of pages, or to show the progress of an order.

Both work in Blazor Server, WebAssembly and Auto apps, can be prerendered, and need no registration: add `@using Pixata.Blazor.Components` to your `_Imports.razor` and make sure your host page includes `_content/Pixata.Blazor/pixata.css` (see [Styling](Readme.Styling.md)).

You can see them in action on the [wizard sample page](https://test.pixata.co.uk/WizardSample) ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/WizardSample.razor)).

## The wizard

```razor
<PixataWizard @bind-Value="_step" OnFinish="PlaceOrder">
  <WizardSteps>
    <PixataWizardStep Label="Basket" Icon="PixataSvgIcons.File">
      <Content>
        <BasketEditor />
      </Content>
    </PixataWizardStep>
    <PixataWizardStep Label="Gift message" Optional="true">
      <Content>
        <GiftMessageEditor />
      </Content>
    </PixataWizardStep>
    <PixataWizardStep Label="Confirm" Icon="PixataSvgIcons.Check">
      <Content>
        <OrderSummary />
      </Content>
    </PixataWizardStep>
  </WizardSteps>
</PixataWizard>

@code {
  private int _step;

  private async Task PlaceOrder() {
    // ...
  }
}
```

The wizard shows the current step's content, with the stepper above it, and "Step 1 of 3" and the buttons below it. The first step has no Previous button, and the last step has Done instead of Next. The user can also move between steps by clicking them in the stepper.

Only the current step's content is rendered, so it is created afresh each time the user moves to its step. Keep anything the user enters in a model held by the page (as in the validation examples below), not in the step's components.

### `PixataWizard` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `WizardSteps` | `RenderFragment?` | | The `PixataWizardStep` components. |
| `Value` | `int` | `0` | The index of the current step, starting at 0. Use `@bind-Value` for two-way binding. |
| `ValueChanged` | `EventCallback<int>` | | Raised when the user moves to another step. |
| `OnFinish` | `EventCallback` | | Raised when the user clicks Done, once every step that is validated is valid (see below). |
| `StepperPosition` | `PixataWizardStepperPosition` | `Top` | Where the stepper goes: `Top`, `Bottom`, `Left` or `Right` of the step. On the left or right, the stepper is vertical. |
| `ShowPager` | `bool` | `true` | Whether "Step 1 of 3" is shown next to the buttons. |
| `WizardButtons` | `RenderFragment<PixataWizard>?` | | Replaces the Previous, Next and Done buttons (see below). |
| `WizardSettings` | `RenderFragment?` | | Holds a `PixataWizardStepperSettings`, to configure the stepper (see below). |
| `PreviousText`, `NextText`, `DoneText` | `string` | `"Previous"`, `"Next"`, `"Done"` | The text of the buttons. |
| `PagerTextFormat` | `string` | `"Step {0} of {1}"` | The pager's text, where `{0}` is the current step's number and `{1}` the number of steps. |
| `OptionalText` | `string` | `"(Optional)"` | What follows the label of an optional step in the stepper. |
| `Width`, `Height` | `string?` | | The size of the wizard, as CSS lengths. The step's content scrolls if it doesn't fit. |
| `Id` | `string?` | | The `id` of the wizard's outer element. |
| `Class` | `string?` | | Extra CSS classes for the wizard's outer element. |

Any other attributes are rendered on the wizard's outer element.

### Methods and properties

These are mostly for use in a `WizardButtons` template, or from code via `@ref`.

| Member | Description |
|---|---|
| `Task Next()` | Moves to the next step, if the current step is valid and its `OnChange` doesn't cancel the move. |
| `Task Previous()` | Moves to the previous step, if the current step's `OnChange` doesn't cancel the move. |
| `Task<bool> ChangeStep(int index)` | Moves to the step with the given index, with the same checks (moving forward validates the current step). Returns whether it moved. |
| `Task Finish()` | Does what the Done button does: validates the steps and then raises `OnFinish`. |
| `int CurrentStepIndex` | The index of the current step. |
| `bool IsFirstStep`, `bool IsLastStep` | Whether there is no step before, or after, the current one that the user can move to. |
| `int StepCount` | The number of visible steps. |

### `PixataWizardStep` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | | The step's label in the stepper. |
| `Text` | `string?` | | The text in the step's indicator (the circle) in the stepper, instead of its number. |
| `Icon` | `object?` | | An icon for the step's indicator: a `PixataSvgIcon` (eg `PixataSvgIcons.Check`), or the CSS class(es) of an icon font. |
| `Content` | `RenderFragment?` | | What the wizard shows when this is the current step. |
| `StepTemplate` | `RenderFragment?` | | Custom content for the step in the stepper, replacing its indicator and label. |
| `Disabled` | `bool` | `false` | Greys the step out in the stepper. The user can't go to it, and Next and Previous skip it. |
| `Visible` | `bool` | `true` | When false, the step is left out of the wizard altogether. |
| `Optional` | `bool` | `false` | Adds `OptionalText` after the step's label in the stepper. |
| `Valid` | `bool?` | `null` | Shows whether the step is valid in the stepper: true shows a tick, false an exclamation mark. When null, the result of the step's own validation (if it has any) is shown. Setting this doesn't stop the user moving on. |
| `OnChange` | `EventCallback<PixataWizardStepChangeEventArgs>` | | Raised when the user tries to move from this step to another (but not when they click Done). The argument's `TargetIndex` is the step they are going to. Set `IsCancelled` to keep them on this step. |
| `Model` | `object?` | | A model to validate the step against (see below). |
| `EditContext` | `EditContext?` | | Instead of `Model`, an `EditContext` to validate the step against. |
| `Fields` | `IEnumerable<Expression<Func<object?>>>?` | | When the wizard is inside a form, the fields that belong to this step (see below). |
| `Class` | `string?` | | Extra CSS classes for the step in the stepper. |

A `PixataWizardStep` must be inside a `PixataWizard`, and throws an exception if it isn't.

### Step indexes and hidden steps

`Value`, `ChangeStep()` and `TargetIndex` count every step, including hidden ones, so a step keeps its index when an earlier step is hidden. The pager and the stepper only count the visible steps.

If the current step is hidden, the wizard moves to the visible step before it (or, if there isn't one, the one after it) and raises `ValueChanged`.

Steps can also be added and removed with `@if` and `@foreach`, and are always shown in the order they are declared, even when one is added between existing ones. Give steps generated in a loop a `@key`. Steps that are already there and are moved (eg by sorting the collection a keyed `@foreach` loops over) keep their old places, so if you need to change the order of the steps, give the wizard a `@key` that changes with it, so it is created afresh.

### Validating each step

Most wizards collect information, and shouldn't let the user move on until what they have entered on the current step is valid. The wizard can do this for you in two ways.

**Each step has its own model.** Set the step's `Model` (or `EditContext`), and put a validator (such as `<DataAnnotationsValidator />`) and your inputs in the step's content. The wizard wraps the content in a form, and...

- Next (or clicking a later step in the stepper) validates the step, and doesn't move on until it is valid. The validation messages are shown as usual.
- Pressing Enter in one of the step's inputs does the same as Next (or Done on the last step), and moves the focus into the next step.
- Previous (or clicking an earlier step) is always allowed, so the user can go back and check something without fixing the current step first.
- Once a step has been validated, the stepper shows a tick or an exclamation mark for it, which changes as soon as the user fixes (or breaks) the step.
- Done validates the current step, and then checks the other steps (except disabled ones, which the user can't go to). If the user skipped a step (by clicking a later step in the stepper), or left one that wasn't valid, the wizard goes back to it and shows its validation messages. Only when every step is valid is `OnFinish` raised.

```razor
<PixataWizard OnFinish="SignUp">
  <WizardSteps>
    <PixataWizardStep Label="Account" Model="_account">
      <Content>
        <DataAnnotationsValidator />
        <label>Email <InputText @bind-Value="_account.Email" /></label>
        <ValidationMessage For="() => _account.Email" />
        <label>Password <InputText type="password" @bind-Value="_account.Password" /></label>
        <ValidationMessage For="() => _account.Password" />
      </Content>
    </PixataWizardStep>
    <PixataWizardStep Label="About you" Model="_personal">
      <Content>
        <DataAnnotationsValidator />
        <label>Name <InputText @bind-Value="_personal.Name" /></label>
        <ValidationMessage For="() => _personal.Name" />
      </Content>
    </PixataWizardStep>
    <PixataWizardStep Label="Finish">
      <Content>
        <p>Click Done to sign up.</p>
      </Content>
    </PixataWizardStep>
  </WizardSteps>
</PixataWizard>

@code {
  private readonly AccountModel _account = new();
  private readonly PersonalModel _personal = new();
}
```

The wizard keeps one `EditContext` for each model, so the validation state of a step survives the user moving away from it and back. Two steps can share a model, but then each step's validator validates the whole model, so it is usually better to give each step its own.

**The whole wizard is one form.** If the wizard is inside an `EditForm` (eg because the steps edit one model, or the wizard is part of a bigger form), set each step's `Fields` to the fields it edits. Moving on from a step validates only those fields, so fields on later steps don't stop the user. Done validates all the steps' fields, and then the whole form.

```razor
<EditForm Model="_order">
  <DataAnnotationsValidator />
  <PixataWizard OnFinish="PlaceOrder">
    <WizardSteps>
      <PixataWizardStep Label="Product" Fields="@([() => _order.Product, () => _order.Quantity])">
        <Content>
          <InputText @bind-Value="_order.Product" />
          <InputNumber @bind-Value="_order.Quantity" />
          <ValidationMessage For="() => _order.Product" />
          <ValidationMessage For="() => _order.Quantity" />
        </Content>
      </PixataWizardStep>
      <PixataWizardStep Label="Contact" Fields="@([() => _order.Email])">
        <Content>
          <InputText @bind-Value="_order.Email" />
          <ValidationMessage For="() => _order.Email" />
        </Content>
      </PixataWizardStep>
    </WizardSteps>
  </PixataWizard>
</EditForm>
```

`Fields` needs a validator that validates individual fields when they change, as `DataAnnotationsValidator` does. As forms can't be nested, a wizard inside a form doesn't add forms of its own, so a step with its own `Model` there is validated in the same way but Enter doesn't move on.

If neither of these suits, handle the step's `OnChange`, validate however you like, set `IsCancelled` if the step isn't valid, and set the step's `Valid` to show the result in the stepper.

### Custom buttons

`WizardButtons` replaces the Previous, Next and Done buttons. Its context is the wizard, so your buttons can call its methods, which do the same checks as the built-in buttons...

```razor
<PixataWizard @bind-Value="_step" OnFinish="Save">
  <WizardSteps>
    ...
  </WizardSteps>
  <WizardButtons>
    <button type="button" class="@PixataCss.Button(null, PixataThemeConstants.Button.FillMode.Flat, null, null)" @onclick="Cancel">Cancel</button>
    @if (!context.IsFirstStep) {
      <button type="button" class="@PixataCss.Button(null, null, null, null)" @onclick="context.Previous">Back</button>
    }
    @if (context.IsLastStep) {
      <button type="button" class="@PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Success, null, null, null)" @onclick="context.Finish">Save</button>
    } else {
      <button type="button" class="@PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Primary, null, null, null)" @onclick="context.Next">Continue</button>
    }
  </WizardButtons>
</PixataWizard>
```

`PixataCss.Button()` gives the classes for a button in the same style as the built-in ones.

### Stepper settings

Put a `PixataWizardStepperSettings` in the wizard's `WizardSettings` to configure its stepper...

```razor
<PixataWizard>
  <WizardSettings>
    <PixataWizardStepperSettings Linear="true" StepType="PixataStepperStepType.Labels" />
  </WizardSettings>
  <WizardSteps>
    ...
  </WizardSteps>
</PixataWizard>
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Linear` | `bool` | `false` | When true, the user can only click their way to the steps either side of the current one in the stepper, so can't skip any. |
| `StepType` | `PixataStepperStepType` | `Steps` | `Steps` shows an indicator and a label for each step, `Labels` just the labels. |

### On phones

A wizard with its stepper on the left or right that is less than 360px wide (eg on a phone) puts the stepper above the step and collapses it to show the current step and a progress bar. A stepper at the top or bottom collapses when it is too narrow for its steps, as described for the stepper below.

### Keyboard and accessibility

The wizard's stepper is a `role="tablist"`, its steps are `role="tab"`, and the current step's content is the `role="tabpanel"` they control, labelled with the pager text. See the stepper's keyboard support below. When a step's form moves the user on (with Enter), the focus moves into the next step. When Next takes the user to the last step, the focus moves to Done, which replaces it.

## The stepper

```razor
<PixataStepper @bind-Value="_step">
  <StepperSteps>
    <PixataStepperStep Label="Cart" Valid="true" />
    <PixataStepperStep Label="Delivery" Icon="PixataSvgIcons.Calendar" Optional="true" />
    <PixataStepperStep Label="Payment" />
    <PixataStepperStep Label="Confirm" Icon="PixataSvgIcons.Check" />
  </StepperSteps>
</PixataStepper>

@code {
  private int _step = 1;
}
```

Each step has an indicator (a circle showing its number, `Text` or `Icon`) and a label. The steps before the current one, and the current one, are highlighted, as is the line joining them.

### `PixataStepper` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `StepperSteps` | `RenderFragment?` | | The `PixataStepperStep` components. |
| `Value` | `int` | `0` | The index of the current step, starting at 0. Use `@bind-Value` for two-way binding. |
| `ValueChanged` | `EventCallback<int>` | | Raised when the user moves to another step. |
| `Linear` | `bool` | `false` | When true, the user can only move to the steps either side of the current one, so can't skip any. The others are shown as unavailable. |
| `StepType` | `PixataStepperStepType` | `Steps` | `Steps` shows an indicator and a label for each step, `Labels` just the labels. |
| `Orientation` | `PixataStepperOrientation` | `Horizontal` | Whether the steps are in a row or a column. A vertical stepper's steps share its height equally, so give it a `Height`. |
| `Collapsible` | `bool` | `true` | Whether a horizontal stepper collapses when it is too narrow for its steps (see below). |
| `OptionalText` | `string` | `"(Optional)"` | What follows the label of an optional step. |
| `CollapsedTextFormat` | `string` | `"Step {0} of {1}"` | The text shown when the stepper is collapsed, where `{0}` is the current step's number and `{1}` the number of steps. |
| `PreviousStepLabel`, `NextStepLabel` | `string` | `"Previous step"`, `"Next step"` | The accessible names of the collapsed stepper's buttons. |
| `Width`, `Height` | `string?` | | The size of the stepper, as CSS lengths. |
| `Id` | `string?` | | The `id` of the stepper's outer element. |
| `Class` | `string?` | | Extra CSS classes for the stepper's outer element. |

Any other attributes (eg `aria-label`) are rendered on the stepper's outer element, which is a `<nav>`.

### `PixataStepperStep` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | | The step's label. |
| `Text` | `string?` | | The text in the step's indicator, instead of its number. |
| `Icon` | `object?` | | An icon for the step's indicator: a `PixataSvgIcon`, or the CSS class(es) of an icon font. |
| `Disabled` | `bool` | `false` | Greys the step out, so it can't be selected. |
| `Optional` | `bool` | `false` | Adds `OptionalText` after the step's label. |
| `Valid` | `bool?` | `null` | Shows whether the step is valid: true shows a tick in the indicator, false an exclamation mark and the label in the error colour. In `Labels` mode, the tick or exclamation mark follows the label. This is only an indication: it doesn't stop the user moving between steps. |
| `OnChange` | `EventCallback<PixataStepperStepChangeEventArgs>` | | Raised when the user tries to move from this step to another. The argument's `TargetIndex` is the step they are going to. Set `IsCancelled` to keep them on this step. |
| `Template` | `RenderFragment?` | | Custom content for the step, replacing its indicator and label. |
| `AriaControls` | `string?` | | The `id` of the element the step controls, rendered as its `aria-controls` attribute. |
| `Class` | `string?` | | Extra CSS classes for the step. |

A `PixataStepperStep` must be inside a `PixataStepper`, and throws an exception if it isn't. Steps can be added and removed with `@if` and `@foreach`, and are always shown in the order they are declared, even when one is added between existing ones. As with the wizard, steps that are moved in a keyed `@foreach` keep their old places, so give the stepper a `@key` that changes with the order of the steps if you need to move them. If the current step is removed from the end, the new last step becomes the current one, and `ValueChanged` is raised.

### On phones

A horizontal stepper with less than 80px for each of its steps (eg less than 400px wide with five steps) collapses to show the current step's indicator and label, "Step 2 of 5", a progress bar, and buttons to move to the previous and next steps. This happens whatever the screen size, as it depends on the space the stepper has, so it also works in narrow columns. Set `Collapsible="false"` to stop it.

On touch screens, the steps are made big enough to tap easily.

### Keyboard and accessibility

Each step is a button, and only the current step is in the Tab key order. The current step has `aria-current="step"` (or, in a wizard, `aria-selected="true"`), invalid steps have `aria-invalid`, and steps a linear stepper won't let the user reach have `aria-disabled`.

| Key | Action |
|---|---|
| Left or Up arrow | Move the focus to the previous step, skipping disabled ones. |
| Right or Down arrow | Move the focus to the next step, skipping disabled ones. |
| Home / End | Move the focus to the first or last step. |
| Enter / Space | Move to the step with the focus. |

## Styling

The wizard and stepper use the shared theme variables (see [Styling](Readme.Styling.md)), plus these, which you can override on `:root` or on any element containing them...

| Variable | Default | Used for |
|---|---|---|
| `--pixata-blazor-stepper-color` | `var(--pixata-blazor-text-color)` | The steps' text. |
| `--pixata-blazor-stepper-accent` | `var(--pixata-blazor-primary)` | The indicators of the current and earlier steps, and the line joining them. |
| `--pixata-blazor-stepper-accent-hover` | `var(--pixata-blazor-primary-hover)` | Those indicators when the mouse is over them. |
| `--pixata-blazor-stepper-accent-color` | `var(--pixata-blazor-primary-color)` | The text and icons in those indicators. |
| `--pixata-blazor-stepper-indicator-size` | `1.75rem` | The size of the indicators. |
| `--pixata-blazor-stepper-indicator-margin` | `3px` | The space around the indicators, where the focus ring goes. |
| `--pixata-blazor-stepper-indicator-bg` | `var(--pixata-blazor-surface-bg)` | The background of the later steps' indicators, and of the labels in `Labels` mode. |
| `--pixata-blazor-stepper-indicator-border-color` | `var(--pixata-blazor-border-color)` | The border of the later steps' indicators. |
| `--pixata-blazor-stepper-indicator-hover-bg` | `var(--pixata-blazor-base-hover-bg)` | Their background when the mouse is over them. |
| `--pixata-blazor-stepper-line-width` | `2px` | The thickness of the line joining the steps. |
| `--pixata-blazor-stepper-line-color` | `var(--pixata-blazor-base-hover-bg)` | The line joining the later steps, and the collapsed stepper's progress bar. |
| `--pixata-blazor-stepper-label-max-width` | `10em` | How wide a label can get before it is cut short with an ellipsis. |
| `--pixata-blazor-stepper-label-radius` | `16px` | The rounding of the labels in `Labels` mode. |
| `--pixata-blazor-stepper-success-color` | `var(--pixata-blazor-success)` | The tick after a valid step's label in `Labels` mode. |
| `--pixata-blazor-stepper-error-color` | `var(--pixata-blazor-error)` | An invalid step's label. |
| `--pixata-blazor-stepper-optional-color` | `var(--pixata-blazor-subtle-color)` | The optional text. |
| `--pixata-blazor-stepper-disabled-color` | the text colour at 60% | Disabled steps. |
| `--pixata-blazor-wizard-padding` | `20px` | The padding around the wizard. |
| `--pixata-blazor-wizard-gap` | `32px` | The space between the stepper and the step, and above the buttons. |

The main CSS classes are...

| Class | Element |
|---|---|
| `pixata-blazor-stepper` | The stepper's outer element, which also has `pixata-blazor-stepper-{horizontal,vertical}`, and `pixata-blazor-stepper-labels`, `pixata-blazor-stepper-linear` and `pixata-blazor-stepper-collapsible` when they apply. |
| `pixata-blazor-stepper-list` | The list of steps. |
| `pixata-blazor-stepper-step` | A step, which also has `-first`, `-last`, `-done` (before the current step), `-current`, `-optional`, `-disabled`, `-unreachable` (in a linear stepper), `-success` and `-error`, all prefixed with `pixata-blazor-stepper-step`. |
| `pixata-blazor-stepper-step-link` | The button inside a step. |
| `pixata-blazor-stepper-step-indicator` | A step's circle. |
| `pixata-blazor-stepper-step-label`, `pixata-blazor-stepper-step-text` | A step's label, and its text. |
| `pixata-blazor-stepper-step-label-optional` | The optional text. |
| `pixata-blazor-stepper-progress`, `pixata-blazor-stepper-progress-value` | The line joining the steps, and the highlighted part of it. |
| `pixata-blazor-stepper-compact` | The collapsed stepper, with its `pixata-blazor-stepper-compact-button`s, `-compact-count`, `-compact-label` and `-compact-progress`. |
| `pixata-blazor-wizard` | The wizard's outer element, which also has `pixata-blazor-wizard-{horizontal,vertical}` and `pixata-blazor-wizard-{top,bottom,left,right}`. |
| `pixata-blazor-wizard-stepper` | The wizard's stepper. |
| `pixata-blazor-wizard-step` | The current step: its content and buttons. |
| `pixata-blazor-wizard-content` | The step's content. |
| `pixata-blazor-wizard-form` | The form around a step that has a `Model` or `EditContext`. |
| `pixata-blazor-wizard-buttons` | The row with the pager (`pixata-blazor-wizard-pager`) and the buttons (`pixata-blazor-wizard-previous`, `-next` and `-done`). |
