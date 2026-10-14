namespace Pixata.Blazor.Sample.Pages;

public partial class PixataGridSample {
  // The markup for the TemplateHelper example, shown on the page
  private const string TemplateHelperCode = """
    @inject TemplateHelper TemplateHelper

    <PixataGrid TItem="GridSampleEmployee" Data="Data">
      <GridColumns>
        <PixataGridColumn Field="@nameof(GridSampleEmployee.FirstName)" />
        <PixataGridColumn Field="@nameof(GridSampleEmployee.Surname)" />
        <PixataGridColumn Field="@nameof(GridSampleEmployee.Email)"
                          Template="@(TemplateHelper.Link<GridSampleEmployee>(e => e.Email, e => $"mailto:{e.Email}", title: "Send an email"))" />
        <PixataGridColumn Field="@nameof(GridSampleEmployee.Salary)"
                          Template="@(TemplateHelper.Text<GridSampleEmployee>(e => e.Salary.ToString("C0"),
                                      e => e.Salary > 300000 ? "text-align: right; color: red" : "text-align: right"))" />
      </GridColumns>
    </PixataGrid>
    """;
}
