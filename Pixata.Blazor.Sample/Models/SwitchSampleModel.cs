using System.ComponentModel.DataAnnotations;

namespace Pixata.Blazor.Sample.Models;

public class SwitchSampleModel {
  [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the terms and conditions")]
  public bool AcceptTerms { get; set; }
}
