using System.Collections.Generic;
using System.Text.Json;

namespace Pixata.Blazor.Components;

// What the browser reports about the selection, so the toolbar can show which tools are on (Active), which can't be used
// (Disabled), and the current format, font and colours
internal sealed class PixataEditorSelectionState {
  public HashSet<string> Active { get; init; } = [];
  public HashSet<string> Disabled { get; init; } = [];
  public string? Format { get; init; }
  public string? FontFamily { get; init; }
  public string? FontSize { get; init; }
  public string? ForeColor { get; init; }
  public string? BackColor { get; init; }

  public static PixataEditorSelectionState Parse(string json) {
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;
    return new() {
      Active = Strings(root, "active"),
      Disabled = Strings(root, "disabled"),
      Format = Text(root, "format"),
      FontFamily = Text(root, "fontFamily"),
      FontSize = Text(root, "fontSize"),
      ForeColor = Text(root, "foreColor"),
      BackColor = Text(root, "backColor")
    };
  }

  private static HashSet<string> Strings(JsonElement root, string name) {
    HashSet<string> result = [];
    if (root.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array) {
      foreach (JsonElement item in array.EnumerateArray()) {
        if (item.ValueKind == JsonValueKind.String) {
          result.Add(item.GetString()!);
        }
      }
    }
    return result;
  }

  private static string? Text(JsonElement root, string name) =>
    root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
