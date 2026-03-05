# Poyraz.Helpers.Primitives

A .NET library implementing the **Result Pattern** for functional error handling. Instead of throwing exceptions for validation failures and domain rule violations, return a `Result` or `Result<T>` that explicitly represents success or failure.

## API Reference

### `Error`

Represents a domain or validation problem with a `Code` and optional `Values`.

```csharp
using Poyraz.Helpers.Primitives;

// Built-in errors
Error.None;                     // No error (used internally for success)
Error.NullValues;               // "Error.NullValue" - used when a value is null

// Custom errors — code only
var err = new Error("User.NotFound");

// Custom errors — code + descriptive values (params object[])
var err2 = new Error("User.NotFound", "The user with the specified ID was not found.");
var err3 = new Error("Validation.Range", "Age", "must be between 1 and 150");
// err3.Code   == "Validation.Range"
// err3.Values == ["Age", "must be between 1 and 150"]

// Implicit conversion to string returns the Code
string code = err; // "User.NotFound"
```

### `Result` (no data)

Use when an operation returns no value (like `void`).

```csharp
// Success
Result.Success();

// Failure — single error
Result.Failure(new Error("Order.InvalidState"));

// Failure — multiple errors
Result.Failure(new Error[] { error1, error2 });
```

### `Result<T>` (with data)

Use when an operation returns a value. Inherits from `Result`.

```csharp
// Explicit success
Result.Success(myUser);         // Result<User>

// Explicit failure
Result.Failure<User>(new Error("User.NotFound"));
Result.Failure<User>(new Error[] { error1, error2 });

// Create from nullable — returns Failure(Error.NullValues) if null
Result.Create<User>(maybeNull);

// IMPLICIT CONVERSION: any non-null T automatically becomes Result<T> success
public Result<User> GetUser()
{
    var user = _repo.Find(1);
    return user; // implicitly wraps in Result.Success(user), or Failure if null
}
```

#### Consuming a Result

**Important:** Always check `IsSuccess` or `IsFailure` before accessing `Data`. Accessing `Data` on a failure throws `InvalidOperationException`.

```csharp
var result = GetUser(1);

if (result.IsFailure)
{
    // result.Errors is Error[] — iterate or serialize
    return BadRequest(result.Errors);
}

User user = result.Data; // safe here
```

### `Result.Ensure` — Validation

Validate a value against one or more predicates, collecting all errors.

```csharp
// Single predicate, single error
Result<int> validated = Result.Ensure(
    age,
    v => v >= 0,
    new Error("Age.Negative", "Age cannot be negative.")
);

// Multiple predicates — all are evaluated and errors are combined
Result<string> validated = Result.Ensure(
    email,
    (v => !string.IsNullOrEmpty(v),   new Error("Email.Empty")),
    (v => v.Contains('@'),            new Error("Email.InvalidFormat"))
);
// If both fail, result.Errors contains both Error objects
```

### `Result.Combine`

Merge multiple `Result<T>` into one. If any is a failure, all distinct errors are aggregated.

```csharp
var r1 = Result.Ensure(name, v => v.Length > 0, new Error("Name.Empty"));
var r2 = Result.Ensure(name, v => v.Length < 100, new Error("Name.TooLong"));

Result<string> combined = Result.Combine(r1, r2);
// combined.IsFailure == true if either r1 or r2 failed
// combined.Errors contains all distinct errors from both
```

### Extension Methods (`ResultExtensions`)

#### `Ensure` (extension)

Chain additional validations on an existing `Result<T>`:

```csharp
Result<User> result = GetUser(id)
    .Ensure(u => u.IsActive, new Error("User.Inactive"))
    .Ensure(u => u.Balance >= 0, new Error("User.NegativeBalance"));
```

#### `Map`

Transform the data inside a successful result:

```csharp
Result<UserDto> dto = GetUser(id)
    .Map(user => new UserDto { Name = user.Name, Email = user.Email });
// If GetUser failed, the failure propagates — Map is skipped
```

#### `OnSuccess`

Execute logic only when the result is successful. Multiple overloads exist:

```csharp
// Action on success (returns Result)
Result r = GetUser(id).OnSuccess(user => SendWelcomeEmail(user));

// Func<Result> chaining
Result r = CheckBalance(user).OnSuccess(() => DeductAmount(user, 100));

// Func<T> — wrap return in success
Result<Token> r = ValidateCredentials(creds).OnSuccess(() => GenerateToken());

// Func<T, Result> — chain dependent operations
Result r = GetUser(id).OnSuccess(user => DeactivateUser(user));

// Func<Result<TIn>, Result<TOut>> — full transformation
Result<OrderDto> r = GetOrder(id).OnSuccess(orderResult => MapToDto(orderResult));
```

#### `OnFailure`

Execute logic only when the result is a failure:

```csharp
var result = GetUser(id)
    .OnFailure(() => _logger.LogWarning("User not found"));
```

#### `OnBoth`

Execute logic regardless of success or failure:

```csharp
// Action<Result> — side effects
GetUser(id).OnBoth(r => _logger.LogInformation($"Success: {r.IsSuccess}"));

// Func<Result, T> — transform to any type
string message = GetUser(id).OnBoth(r => r.IsSuccess ? "Found" : "Not found");

// Func<Result<TIn>, TOut>
ApiResponse response = GetUser(id).OnBoth(r =>
    r.IsSuccess
        ? new ApiResponse(200, r.Data)
        : new ApiResponse(404, r.Errors));
```

#### `GetResultWithoutData`

Strip the data from a `Result<T>`, returning a plain `Result` while preserving success/failure state:

```csharp
Result<User> typed = GetUser(id);
Result plain = typed.GetResultWithoutData(); // Result without Data property
```

### Error Serialization

```csharp
// Serialize errors to JSON string
string json = result.ErrorSerialize();
// e.g. [{"Code":"User.NotFound","Values":["..."]}]

// Deserialize back
Error[] errors = Result.ErrorDeserialize(json);
```

### `ResultList<TItem>`

Wraps a collection with pagination metadata. Properties: `Items` (TItem[]), `TotalResults` (int), `ResultsPerPage` (int).

```csharp
// Implicit conversion from array — TotalResults & ResultsPerPage = array.Length
ResultList<UserDto> list = usersArray;

// Manual — when total differs from page size (pagination)
var list = new ResultList<UserDto>(pageItems, totalCount: 500);
// list.Items         = pageItems
// list.ResultsPerPage = pageItems.Length (e.g. 20)
// list.TotalResults   = 500
```

#### Common Pattern: Paginated API Response

```csharp
public Result<ResultList<ProductDto>> GetProducts(int page, int pageSize)
{
    var totalCount = _repository.Count();
    var items = _repository.GetPage(page, pageSize).ToArray();
    return new ResultList<ProductDto>(items, totalCount);
    // Implicit: ResultList<T> → Result<ResultList<T>> via Result<T> implicit operator
}
```
