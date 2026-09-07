using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Extensions;
using Pixata.Extensions;

namespace Pixata.Blazor.Tests {
  [TestClass]
  public class PersistentStateHelper_Tests {
    private const string Key = "/some-page";

    private record TestDto(string Name);

    #region Get

    [TestMethod]
    public async Task PersistentStateHelper_Get_PersistedValueCannotBeRead_FetchesTheData() {
      // The persisted value is valid JSON, but data is a string where a TestDto is expected, so TryTakeFromJson throws
      PersistentComponentStateHarness harness = new(new Dictionary<string, byte[]> {
        [Key] = Encoding.UTF8.GetBytes("""{"state":1,"data":"not-a-dto"}""")
      });
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());
      ApiResponse<TestDto> expected = new(ApiResponseStates.Success, new TestDto("Jim Spriggs"));
      bool fetched = false;

      ApiResponse<TestDto> result = await helper.Get(() => {
        fetched = true;
        return Task.FromResult(expected);
      }, Key);

      Assert.IsTrue(fetched);
      Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public async Task PersistentStateHelper_Get_PersistedValueCanBeRead_DoesNotFetchTheData() {
      PersistentComponentStateHarness harness = new(new Dictionary<string, byte[]> {
        [Key] = Encoding.UTF8.GetBytes("""{"state":1,"data":{"name":"Jim Spriggs"}}""")
      });
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());
      bool fetched = false;

      ApiResponse<TestDto> result = await helper.Get(() => {
        fetched = true;
        return Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Failure));
      }, Key);

      Assert.IsFalse(fetched);
      Assert.AreEqual(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Jim Spriggs")), result);
    }

    #endregion

    #region Persist

    [TestMethod]
    public async Task PersistentStateHelper_Persist_PersistWhenIsNotMet_PersistsNothing() {
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());
      ApiResponse<TestDto> failure = new(ApiResponseStates.Failure, Message: "The API had a bad minute");

      await helper.Get(() => Task.FromResult(failure), Key, response => response.State == ApiResponseStates.Success);
      await harness.RunPersistCallbacks();

      Assert.IsFalse(harness.Persisted.ContainsKey(Key));
    }

    [TestMethod]
    public async Task PersistentStateHelper_Persist_PersistWhenIsMet_PersistsTheData() {
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());
      ApiResponse<TestDto> success = new(ApiResponseStates.Success, new TestDto("Jim Spriggs"));

      await helper.Get(() => Task.FromResult(success), Key, response => response.State == ApiResponseStates.Success);
      await harness.RunPersistCallbacks();

      Assert.IsTrue(harness.Persisted.ContainsKey(Key));
      // What was persisted has to be readable, otherwise we're back to the bug the first test covers
      Assert.AreEqual(success, JsonSerializer.Deserialize<ApiResponse<TestDto>>(harness.Persisted[Key], new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [TestMethod]
    public async Task PersistentStateHelper_Persist_NoPersistWhenGiven_PersistsTheData() {
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());
      ApiResponse<TestDto> failure = new(ApiResponseStates.Failure, Message: "The API had a bad minute");

      await helper.Get(() => Task.FromResult(failure), Key);
      await harness.RunPersistCallbacks();

      Assert.IsTrue(harness.Persisted.ContainsKey(Key));
    }

    #endregion

    private class TestNavigationManager : NavigationManager {
      public TestNavigationManager() =>
        Initialize("https://localhost/", "https://localhost/some-page");
    }
  }
}
