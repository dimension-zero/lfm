# Domain Onboarding Checklist

Use this checklist to track progress implementing a new domain adapter. Check off each item as you complete it.

**Domain Name**: ________________
**Start Date**: ________________
**Target Completion**: ________________

---

## Phase 1: Project Setup

- [ ] Created domain project folder: `src/Lfm.{DomainName}/`
- [ ] Created `.csproj` file with correct references
- [ ] Added project to solution file
- [ ] Created directory structure:
  - [ ] `Models/`
  - [ ] `Data/`
  - [ ] `Providers/`
  - [ ] `Algorithms/`
  - [ ] `Actions/`
- [ ] Project compiles without errors

---

## Phase 2: Domain Model

- [ ] Created `Models/{DomainName}Item.cs`
- [ ] `{DomainName}Item` implements `IDomainItem`
- [ ] `Id` property is required and unique
- [ ] `Name` property describes the item
- [ ] `ImageUrl` is optional (nullable)
- [ ] Added 4+ domain-specific properties
- [ ] All properties have XML documentation (`/// <summary>`)
- [ ] File compiles without errors

**Example Properties** (choose 3+):
- [ ] ProducerName / CreatorName / AuthorName
- [ ] UserInteractionCount / UserPlayCount / UserReadCount
- [ ] UserRating (float, 1-5 scale)
- [ ] Category / Genre / Type
- [ ] AverageRating (community rating)
- [ ] Price / Cost
- [ ] Description / Summary
- [ ] Ranking (for list displays)
- [ ] Custom domain property 1: ________________
- [ ] Custom domain property 2: ________________

---

## Phase 3: Data Provider

### 3.1 Interface (Optional but Recommended)

- [ ] Created `Data/I{DomainName}DataProvider.cs`
- [ ] Interface defines required methods:
  - [ ] `GetAllItems()`
  - [ ] `SearchItems(string query)`
  - [ ] `GetSimilarItems(string itemId)`

### 3.2 Implementation (Mock Data)

- [ ] Created `Data/Mock{DomainName}DataProvider.cs`
- [ ] Implements `I{DomainName}DataProvider`
- [ ] `SampleItems` list has 5+ items
- [ ] Each sample item has all required properties
- [ ] Sample items include realistic data
- [ ] `SearchItems()` is case-insensitive
- [ ] `GetSimilarItems()` returns similar items by category
- [ ] No null references (all nulls are explicit)

### 3.3 Alternative: Real API

- [ ] Created concrete API provider (if needed)
- [ ] Uses `IHttpClientFactory`
- [ ] Implements all interface methods
- [ ] Has proper error handling

### 3.4 Validation

- [ ] `GetAllItems()` returns non-null list
- [ ] `SearchItems()` works with empty/null query
- [ ] `GetSimilarItems()` returns appropriate results
- [ ] No exceptions thrown
- [ ] Data provider compiles

---

## Phase 4: History Provider

- [ ] Created `Providers/{DomainName}HistoryProvider.cs`
- [ ] Implements `IUserHistoryProvider<{DomainName}Item>`
- [ ] Constructor validates data provider (not null)
- [ ] **Method: `GetUserHistoryAsync()`**
  - [ ] Filters by `UserInteractionCount > 0`
  - [ ] Sorts by `UserInteractionCount` descending
  - [ ] Respects `limit` parameter
  - [ ] Returns `Result<List<{DomainName}Item>>`
  - [ ] Has try/catch with proper error handling
- [ ] **Method: `GetTopItemsAsync()`**
  - [ ] Supports "overall" period (default)
  - [ ] Supports "recent" period (optional)
  - [ ] Sorts by rating, then interaction count
  - [ ] Adds ranking to results ("#{number}")
  - [ ] Respects `count` parameter
  - [ ] Returns ranked items
- [ ] **Method: `GetUserHistoryForDateRangeAsync()`**
  - [ ] Accepts `from` and `to` date parameters
  - [ ] Returns top items (POC can ignore dates initially)
  - [ ] Adds ranking to results
- [ ] **Method: `GetUserInteractionCountsAsync()`**
  - [ ] Returns `Dictionary<string, int>`
  - [ ] Maps item name → interaction count
  - [ ] Only includes items with count > 0
- [ ] Helper method: `CloneWithRanking()`
  - [ ] Copies all item properties
  - [ ] Sets ranking without mutation
- [ ] All methods are `async` and return `Task<Result<T>>`
- [ ] No exceptions thrown (all errors return Result)
- [ ] Compiles without errors

---

## Phase 5: Item Provider

- [ ] Created `Providers/{DomainName}ItemProvider.cs`
- [ ] Implements `IItemProvider<{DomainName}Item>`
- [ ] Constructor validates data provider
- [ ] **Method: `GetItemDetailsAsync()`**
  - [ ] Takes `itemId` parameter
  - [ ] Returns found item or DataError
  - [ ] Case-insensitive ID comparison
  - [ ] Proper error handling
- [ ] **Method: `SearchItemsAsync()`**
  - [ ] Takes `query` and `limit` parameters
  - [ ] Case-insensitive search
  - [ ] Respects limit
  - [ ] Returns empty list if no matches (not error)
- [ ] **Method: `GetSimilarItemsAsync()`**
  - [ ] Takes `sourceItemId` and `limit` parameters
  - [ ] Returns DataError if source not found
  - [ ] Calls data provider's similarity method
  - [ ] Respects limit parameter
  - [ ] Excludes source item from results
- [ ] **Method: `ItemExistsAsync()`**
  - [ ] Returns boolean Result
  - [ ] Case-insensitive ID check
- [ ] All methods use `Result<T>` pattern
- [ ] No exceptions thrown
- [ ] Compiles without errors

---

## Phase 6: Recommendation Algorithm

- [ ] Created `Algorithms/{DomainName}RecommendationAlgorithm.cs`
- [ ] Implements `IRecommendationAlgorithm<{DomainName}Item>`
- [ ] Constructor validates data provider
- [ ] **Method: `GenerateRecommendationsAsync()`**
  - [ ] Gets user's top items (by `analysisLimit`)
  - [ ] Filters items by `filterThreshold`
  - [ ] Finds similar items user hasn't interacted with
  - [ ] Calculates relevance score
  - [ ] Aggregates multiple source matches
  - [ ] Returns top N recommendations (by `recommendationLimit`)
  - [ ] Results sorted by score descending
  - [ ] Includes `SourceItems` list
  - [ ] Returns `Result<List<RecommendationResult<>>>`
- [ ] **Method: `GenerateRecommendationsForDateRangeAsync()`**
  - [ ] Accepts `from` and `to` date parameters
  - [ ] For POC: same as overall recommendations
  - [ ] Returns `Result<List<RecommendationResult<>>>`
- [ ] **Score Calculation**
  - [ ] Formula: `(userRating * itemRating) / 5`
  - [ ] Reasonable range (0-25 typically)
  - [ ] Handles null ratings (use default like 3.5)
- [ ] All methods are `async`
- [ ] Proper error handling with try/catch
- [ ] Returns Result<T>, not exceptions
- [ ] Compiles without errors

---

## Phase 7: Action Executor

- [ ] Created `Actions/{DomainName}ActionExecutor.cs`
- [ ] Implements `IActionExecutor<{DomainName}Item>`
- [ ] **Static: `AvailableActions` list**
  - [ ] Includes 4-6 domain-relevant actions
  - [ ] Example actions:
    - [ ] "add" - Add to collection
    - [ ] "rate" - Rate the item
    - [ ] "purchase" - Mark for purchase
    - [ ] "viewdetails" - View full details
    - [ ] "viewsimilar" - View similar items
    - [ ] "export" - Export collection
- [ ] **Method: `ExecuteActionAsync()`**
  - [ ] Takes `item`, `actionType`, optional `parameters`
  - [ ] Validates action is in available list
  - [ ] Validates item is not null
  - [ ] Uses switch statement for actions
  - [ ] Action "rate" validates rating (1-5 range)
  - [ ] Returns `Result` (non-generic)
  - [ ] Has proper error handling
  - [ ] No exceptions thrown
- [ ] **Method: `GetAvailableActionsAsync()`**
  - [ ] Returns list of action strings
  - [ ] Is async (returns Task<Result<List<string>>>)
- [ ] **Method: `IsActionAvailableAsync()`**
  - [ ] Takes `actionType` parameter
  - [ ] Returns boolean Result
  - [ ] Case-insensitive action matching
  - [ ] Is async
- [ ] Helper method: `ExecuteRateAction()` (if needed)
  - [ ] Validates rating parameter
  - [ ] Returns Result with validation error if invalid
- [ ] All methods properly marked `async`
- [ ] Returns `Result<T>`, not exceptions
- [ ] Compiles without errors

---

## Phase 8: Unit Tests

### 8.1 Test Project Setup

- [ ] Added `Lfm.Wine` project reference to `Lfm.Tests.csproj`
- [ ] Created test directory: `src/Lfm.Tests/Unit/{DomainName}/`

### 8.2 History Provider Tests

- [ ] Created `{DomainName}HistoryProviderTests.cs`
- [ ] **Initialization (2 tests)**
  - [ ] Initializes with valid data provider
  - [ ] Throws ArgumentNullException with null provider
- [ ] **GetUserHistoryAsync Tests (4 tests)**
  - [ ] Returns all tasted items
  - [ ] Respects size limit
  - [ ] Orders by tasting count descending
  - [ ] Excludes untasted items (count = 0)
- [ ] **GetTopItemsAsync Tests (4 tests)**
  - [ ] Returns top rated items (overall period)
  - [ ] Applies period filtering (overall vs recent)
  - [ ] Ranks items correctly (#1, #2, etc.)
  - [ ] Only includes tasted items
- [ ] **GetUserHistoryForDateRangeAsync Tests (2 tests)**
  - [ ] Returns most frequently tasted items
  - [ ] Ranks items correctly
  - [ ] Respects count parameter
- [ ] **GetUserInteractionCountsAsync Tests (2 tests)**
  - [ ] Returns name → count mapping
  - [ ] Only includes tasted items
  - [ ] Respects maxItems parameter
- [ ] **Edge Cases (3 tests)**
  - [ ] Handles zero limit
  - [ ] Handles large count parameter
  - [ ] Handles missing user (returns empty or error)
- [ ] **Total**: 17-25 tests, **All Passing**

### 8.3 Item Provider Tests

- [ ] Created `{DomainName}ItemProviderTests.cs`
- [ ] **Initialization (2 tests)**
  - [ ] Initializes with valid data provider
  - [ ] Throws ArgumentNullException with null provider
- [ ] **GetItemDetailsAsync Tests (3 tests)**
  - [ ] Returns item by valid ID
  - [ ] Returns all item properties
  - [ ] Fails with invalid ID
  - [ ] Case-insensitive lookup
- [ ] **SearchItemsAsync Tests (4 tests)**
  - [ ] Returns items matching query
  - [ ] Searches across multiple fields
  - [ ] Respects size limit
  - [ ] Returns empty list for no matches
  - [ ] Case-insensitive search
- [ ] **GetSimilarItemsAsync Tests (4 tests)**
  - [ ] Returns similar items
  - [ ] Excludes source item
  - [ ] Similar items match by category
  - [ ] Respects size limit
  - [ ] Fails with invalid source ID
- [ ] **ItemExistsAsync Tests (2 tests)**
  - [ ] Returns true for existing item
  - [ ] Returns false for non-existent item
  - [ ] Case-insensitive check
- [ ] **Integration Tests (2 tests)**
  - [ ] Search then get details flow
  - [ ] Check exists then get details flow
- [ ] **Error Handling (2 tests)**
  - [ ] Handles empty/bad query
  - [ ] Default limits work
- [ ] **Total**: 20-25 tests, **All Passing**

### 8.4 Recommendation Algorithm Tests

- [ ] Created `{DomainName}RecommendationAlgorithmTests.cs`
- [ ] **Initialization (2 tests)**
  - [ ] Initializes with valid data provider
  - [ ] Throws ArgumentNullException with null provider
- [ ] **GenerateRecommendationsAsync Tests (6 tests)**
  - [ ] Returns recommendations
  - [ ] Respects analysis limit
  - [ ] Respects recommendation limit
  - [ ] Returns items with scores
  - [ ] Sorts by score descending
  - [ ] Recommendations based on favorite categories
  - [ ] Excludes already-interacted items
  - [ ] Includes source items in result
  - [ ] Handles high filter threshold
- [ ] **GenerateRecommendationsForDateRangeAsync Tests (2 tests)**
  - [ ] Returns POC behavior (same as overall)
  - [ ] Respects period parameters
  - [ ] Returns correct count
- [ ] **Score Calculation Tests (2 tests)**
  - [ ] Calculates reasonable scores
  - [ ] Aggregates multiple source items
- [ ] **Edge Cases (4 tests)**
  - [ ] Handles zero analysis limit
  - [ ] Handles zero recommendation limit
  - [ ] Handles no tasted items
  - [ ] Handles null user ID
- [ ] **Period Parameter Tests (1 test)**
  - [ ] Period parameter works (even if POC ignores it)
- [ ] **Integration Tests (1 test)**
  - [ ] All fields in RecommendationResult populated
- [ ] **Error Handling (1 test)**
  - [ ] Handles bad limits gracefully
  - [ ] Handles inverted date range
- [ ] **Total**: 20-30 tests, **All Passing**

### 8.5 Action Executor Tests

- [ ] Created `{DomainName}ActionExecutorTests.cs`
- [ ] **Initialization (1 test)**
  - [ ] Executor initializes
- [ ] **GetAvailableActionsAsync Tests (2 tests)**
  - [ ] Returns actions list
  - [ ] Contains expected actions
- [ ] **IsActionAvailableAsync Tests (3 tests)**
  - [ ] Returns true for valid actions
  - [ ] Returns false for invalid actions
  - [ ] Case-insensitive action matching
- [ ] **ExecuteActionAsync Tests (3 tests)**
  - [ ] Succeeds with valid wine and action
  - [ ] Fails with null item
  - [ ] Fails with unknown action
- [ ] **Individual Action Tests (4+ tests)**
  - [ ] Each action type (add, rate, purchase, etc.)
  - [ ] Rating action validates parameters
  - [ ] Rating action checks range (1-5)
- [ ] **Parameter Handling Tests (3 tests)**
  - [ ] Handles empty parameters
  - [ ] Handles extra parameters
  - [ ] Handles null parameters
- [ ] **Integration Tests (1 test)**
  - [ ] All actions are executable
- [ ] **Total**: 10-15 tests, **All Passing**

### 8.6 Test Statistics

- [ ] History Provider: 17-25 tests
- [ ] Item Provider: 20-25 tests
- [ ] Recommendation Algorithm: 20-30 tests
- [ ] Action Executor: 10-15 tests
- [ ] **Total Domain Tests**: 80-95 tests
- [ ] **Pass Rate**: 100% (0 failures)
- [ ] **Build Status**: 0 errors, 0 warnings

---

## Phase 9: Dependency Injection & Integration

- [ ] Added data provider registration to `Program.cs`:
  ```csharp
  services.AddSingleton<I{DomainName}DataProvider, Mock{DomainName}DataProvider>();
  ```

- [ ] Added history provider registration:
  ```csharp
  services.AddScoped<IUserHistoryProvider<{DomainName}Item>, {DomainName}HistoryProvider>();
  ```

- [ ] Added item provider registration:
  ```csharp
  services.AddScoped<IItemProvider<{DomainName}Item>, {DomainName}ItemProvider>();
  ```

- [ ] Added recommendation algorithm registration:
  ```csharp
  services.AddScoped<IRecommendationAlgorithm<{DomainName}Item>, {DomainName}RecommendationAlgorithm>();
  ```

- [ ] Added action executor registration:
  ```csharp
  services.AddScoped<IActionExecutor<{DomainName}Item>, {DomainName}ActionExecutor>();
  ```

- [ ] Added project reference to `Lfm.Tests.csproj`:
  ```xml
  <ProjectReference Include="..\Lfm.{DomainName}\Lfm.{DomainName}.csproj" />
  ```

- [ ] Added using statements where needed
- [ ] Solution file includes new project
- [ ] No broken references

---

## Phase 10: Build & Validation

### 10.1 Build Verification

- [ ] Full solution builds: `dotnet build -c Release`
- [ ] **Result**: 0 Errors, 0 Warnings (or pre-existing only)
- [ ] No new compiler errors
- [ ] No new compiler warnings

### 10.2 Test Verification

- [ ] Domain tests pass:
  ```bash
  dotnet test --filter "FullyQualifiedName~Lfm.Tests.Unit.{DomainName}" -c Release
  ```
- [ ] **Result**: All 80-95 tests passing
- [ ] **Duration**: < 5 seconds

### 10.3 Integration Test

- [ ] Created and passing integration test demonstrating:
  - [ ] History provider works
  - [ ] Item provider works
  - [ ] Recommendation algorithm works
  - [ ] Action executor works
  - [ ] All components return successful results

### 10.4 Documentation

- [ ] Created/updated README for domain (if needed)
- [ ] Documented any special configuration
- [ ] Added examples of domain-specific usage

---

## Phase 11: Production Readiness

### 11.1 Code Quality

- [ ] All files have XML documentation comments
- [ ] Error messages are clear and specific
- [ ] No magic strings (use constants for actions, etc.)
- [ ] No hardcoded values
- [ ] Proper null checking
- [ ] Result<T> pattern used throughout
- [ ] No try/catch blocks that swallow errors silently

### 11.2 Testing

- [ ] All test categories covered:
  - [ ] Happy path (normal operation)
  - [ ] Error cases (invalid input)
  - [ ] Edge cases (boundaries, limits)
  - [ ] Integration (components together)
- [ ] 100% of test methods passing
- [ ] At least 80 unit tests written

### 11.3 Performance

- [ ] Tests complete in <5 seconds total
- [ ] No memory leaks in test runs
- [ ] Mock data provider is lightweight
- [ ] All methods are O(n) or better

### 11.4 Extensibility

- [ ] Data provider can be swapped for real API
- [ ] Recommendation algorithm can be customized
- [ ] Actions can be extended easily
- [ ] Framework interfaces not modified

---

## Completion Checklist

### Summary

- [ ] **Total Steps Completed**: ____ / 100+
- [ ] **Build Status**: ✅ Passing
- [ ] **Test Status**: ✅ All Passing
- [ ] **Code Quality**: ✅ Verified
- [ ] **Documentation**: ✅ Complete

### Sign-Off

**Developer Name**: ____________________
**Completion Date**: ____________________
**Code Review By**: ____________________
**Approved For Production**: ☐ Yes ☐ No

### Next Steps

- [ ] Deploy to production (if approved)
- [ ] Monitor for issues
- [ ] Gather user feedback
- [ ] Plan next domain implementation
- [ ] Share lessons learned with team

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails with namespace errors | Check project references in .csproj |
| Tests don't run | Verify test project references |
| Data provider not found | Check DI registration in Program.cs |
| Recommendations are empty | Verify mock data has appropriate categories |
| Action executor fails | Validate parameter types in ExecuteActionAsync |

---

**Questions?** Reference the Adapter Implementation Guide or existing Music/Wine adapters for examples.
