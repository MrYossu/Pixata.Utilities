using System.ComponentModel.DataAnnotations;

namespace Pixata.Blazor.Sample.Models;

public class RadioGroupSampleModel {
  [Required(ErrorMessage = "Please choose how we should contact you")]
  public int? ContactMethodId { get; set; }
}

public class RadioGroupSampleContactMethod {
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public string Description { get; set; } = "";
  public bool Unavailable { get; set; }
}
