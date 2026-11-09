using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    [TestMethod]
    public void PersistentStateHelper_RegistersForBothServerAndWebAssembly() {
      // With InteractiveWebAssembly, an app that prerenders InteractiveServer never persisted anything, so every page loaded its
      // data twice. Auto is accepted by both the server's and WebAssembly's stores
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> helper = new(harness.State, new TestNavigationManager());

      Assert.AreEqual(1, harness.RenderModes.Count);
      Assert.IsInstanceOfType<InteractiveAutoRenderMode>(harness.RenderModes[0]);
    }

    [TestMethod]
    public async Task PersistentStateHelper_Persist_TwoHelpersWithTheSameKey_PersistsNeither() {
      // Whichever was persisted could be read back by the other, which would then show the wrong data
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> first = new(harness.State, new TestNavigationManager());
      using PersistentStateHelper<ApiResponse<TestDto>> second = new(harness.State, new TestNavigationManager());
      using PersistentStateHelper<ApiResponse<TestDto>> other = new(harness.State, new TestNavigationManager());

      await first.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("First"))));
      await second.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Second"))));
      await other.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Other"))), "other");
      await harness.RunPersistCallbacks();

      Assert.IsFalse(harness.Persisted.ContainsKey(Key));
      Assert.IsTrue(harness.Persisted.ContainsKey("other"));
    }

    [TestMethod]
    public async Task PersistentStateHelper_Persist_AfterTheOtherHelperWithTheKeyIsDisposed_PersistsTheData() {
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> first = new(harness.State, new TestNavigationManager());
      PersistentStateHelper<ApiResponse<TestDto>> second = new(harness.State, new TestNavigationManager());

      await first.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("First"))));
      await second.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Second"))));
      second.Dispose();
      await harness.RunPersistCallbacks();

      Assert.AreEqual(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("First")),
        JsonSerializer.Deserialize<ApiResponse<TestDto>>(harness.Persisted[Key], new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [TestMethod]
    public async Task PersistentStateHelper_Get_WithAnotherKey_ReleasesTheFirstKey() {
      PersistentComponentStateHarness harness = new();
      using PersistentStateHelper<ApiResponse<TestDto>> first = new(harness.State, new TestNavigationManager());
      using PersistentStateHelper<ApiResponse<TestDto>> second = new(harness.State, new TestNavigationManager());

      await first.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("First"))));
      await second.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Second"))));
      await second.Get(() => Task.FromResult(new ApiResponse<TestDto>(ApiResponseStates.Success, new TestDto("Second again"))), "second");
      await harness.RunPersistCallbacks();

      Assert.IsTrue(harness.Persisted.ContainsKey(Key));
      Assert.IsTrue(harness.Persisted.ContainsKey("second"));
    }

    #endregion

    private class TestNavigationManager : NavigationManager {
      public TestNavigationManager() =>
        Initialize("https://localhost/", "https://localhost/some-page");
    }
  }
}
