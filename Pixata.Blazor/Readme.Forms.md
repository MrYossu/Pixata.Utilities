# Forms

>Part of [Pixata.Blazor](Readme.md)

A set of components for laying out forms. These come in two flavours, the classic label-beside-the-input style (which was originally built with Bootstrap's grid, but no longer needs Bootstrap - see [Styling](Readme.Styling.md)), and floating label style.

A live sample of the first style can be seen here - [live demo](https://test.pixata.co.uk/FormSample), [source code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor).

## Layout components

These wrap your own input controls, and lay out the label and the validation message for you. I hope to add a sample for these soon. The controls are...

- `FormSingle` - a single input control with label
- `FormDouble` - a row with two input controls and labels
- `FormTriple` - a row with three input controls and labels
- `FormQuad` - a row with four input controls and labels
- `FormName` - a row with title, first name and surname input controls and labels

Sample usage of these is as follows (Telerik input controls used, but you can use any input controls you like)...

```xml
<FormSingle Label="Email" Id="Email" Required="true">
  <TelerikTextBox @bind-Value="@user.Email" Id="Email" />
  <ValidationMessage For="@(() => user.Email)" />
</FormSingle>
```

This looks like this...

![FormSingle](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/FormSingle.png "FormSingle")

Note that the `Required` property merely adds a red asterisk to the label, it does not enforce any validation. You still need to do that yourself.

The `FormDouble`, `FormTriple` and `FormQuad` components work in a similar way, except that the properties for setting the Ids are named `FirstId`, `SecondId`, `ThirdId` and `FourthId`. The properties for the labels and required are named similarly.

The `FormName` component is very similar to `FormTriple`, except that the controls are sized more appropriately for names. Sample usage is as follows...

```xml
<FormName TitleLabel="Title" TitleId="Title" TitleRequired="true"
          FirstNameLabel="First name" FirstNameId="FirstName" FirstNameRequired="true"
          SurnameLabel="Surname" SurnameId="Surname" SurnameRequired="true">
  <Title>
    <TelerikTextBox @bind-Value="@user.Title" Id="Title" />
    <ValidationMessage For="@(() => user.Title)" />
  </Title>
  <Salutation>
    <div style="max-width: 300px">
      <TelerikTextBox @bind-Value="@user.FirstName" Id="FirstName" />
      <ValidationMessage For="@(() => user.FirstName)" />
    </div>
  </Salutation>
  <Surname>
    <div style="max-width: 300px">
      <TelerikTextBox @bind-Value="@user.Surname" Id="Surname" />
      <ValidationMessage For="@(() => user.Surname)" />
    </div>
  </Surname>
</FormName>
```

This looks like this...

![FormName](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/FormName.png "FormName")

## Complete form rows

The components named `FormRowXxx` go one step further, and include the input control as well, so a whole row is one line of markup. They render a label, an icon, the input and the validation message, and they hook into the cascaded `EditContext` so the input picks up the `pixata-invalid` class when validation fails.

```xml
<EditForm Model="@_model">
  <FluentValidationValidator />

  <FormRowText PropertyName="@nameof(_model.Name)" @bind-Value="_model.Name"
               Caption="Name" Icon="fas fa-user" />

  <FormRowTextArea PropertyName="@nameof(_model.Message)" @bind-Value="_model.Message"
                   Caption="Message" Icon="fas fa-comment" Rows="6" />

  <FormRowCheckbox PropertyName="@nameof(_model.Subscribe)" @bind-Checked="_model.Subscribe"
                   Caption="Subscribe?" />
</EditForm>
```

The set is...

| Component | For |
| --- | --- |
| `FormRowText` | Single-line text |
| `FormRowText2`, `FormRowText3` | Two or three single-line text inputs on one row, sharing a caption. The parameters are numbered, so `PropertyName1`, `Value1`, `Attributes1` and so on |
| `FormRowTextName` | Title, first name and surname on one row, using `ValueTitle`, `ValueFirstName` and `ValueSurname` |
| `FormRowTextArea` | Multi-line text. Takes a `Rows` parameter, default 4 |
| `FormRowPassword` | A password |
| `FormRowInt`, `FormRowDecimal`, `FormRowDouble` | Numbers |
| `FormRowCheckbox` | A `bool`. Binds `Checked` rather than `Value` |
| `FormRowDropdown`, `FormRowDropdownNullable` | A list of options, passed as `Values`, an `IEnumerable` of `(int val, string name)` tuples. The nullable version binds an `int?`, so it can have a "none selected" entry |
| `FormRowDropdownEnum` | The members of an `enum`, with their names split by camel case |
| `FormRowBlank` | A row with a label and an icon, but no input. Put whatever you like in its `ChildContent` and it lines up with the rest of the form |

Most of them take the same parameters...

| Parameter | What it does |
| --- | --- |
| `PropertyName` | The name of the property being edited. Used as the input's `id` and `name`, as the fallback caption, and to look up the validation messages |
| `Value` / `Checked` | The value. Supports `@bind-Value` (or `@bind-Checked`) |
| `Caption` | The label text. Defaults to `PropertyName` |
| `Icon` | CSS classes for the icon shown before the input, eg a Font Awesome class |
| `Attributes` | A dictionary of extra attributes splatted onto the input |

These must be inside an `EditForm`, as they take the `EditContext` as a cascading parameter.

You can see the full collection in [the Forms section of the source code](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor/Forms).

## Telerik versions

There are equivalent components built on the Telerik input controls in [Pixata.Blazor.TelerikComponents](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents). They follow the same layout, and use the stylesheet from this package.
