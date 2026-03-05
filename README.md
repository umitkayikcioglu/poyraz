# Poyraz

Poyraz is a suite of .NET libraries for **functional error handling** and **Entity Framework Core abstractions**. It provides the Result Pattern for safe error propagation without exceptions, and clean Repository/UnitOfWork/Specification patterns for data access.

## Packages

### Poyraz.Helpers.Primitives

[![](https://img.shields.io/nuget/v/Poyraz.Helpers.Primitives.svg?logo=nuget)](https://www.nuget.org/packages/Poyraz.Helpers.Primitives)
![](https://img.shields.io/nuget/dt/Poyraz.Helpers.Primitives.svg?logo=nuget&color=yellow)

Functional error handling via the Result Pattern. Return `Result` / `Result<T>` instead of throwing exceptions.

**Key types:** `Result`, `Result<T>`, `ResultList<TItem>`, `Error`

```bash
dotnet add package Poyraz.Helpers.Primitives
```

```csharp
using Poyraz.Helpers.Primitives;

// Return errors without exceptions
public Result<User> GetUser(int id)
{
    var user = _repo.FindById(id);
    if (user == null)
        return Result.Failure<User>(new Error("User.NotFound"));

    return user; // implicit conversion to Result<User>
}

// Validate with Ensure — collects all errors
var validated = Result.Ensure(email,
    (v => !string.IsNullOrEmpty(v), new Error("Email.Empty")),
    (v => v.Contains('@'),          new Error("Email.InvalidFormat"))
);

// Chain operations with OnSuccess / Map
Result<UserDto> dto = GetUser(id)
    .Ensure(u => u.IsActive, new Error("User.Inactive"))
    .Map(u => new UserDto { Name = u.Name });

// Paginated results
var list = new ResultList<UserDto>(pageItems, totalCount: 500);
```

📖 [Full documentation →](src/Poyraz.Helpers.Primitives/README.md)

---

### Poyraz.EntityFramework

[![](https://img.shields.io/nuget/v/Poyraz.EntityFramework.svg?logo=nuget)](https://www.nuget.org/packages/Poyraz.EntityFramework)
![](https://img.shields.io/nuget/dt/Poyraz.EntityFramework.svg?logo=nuget&color=yellow)

Repository, UnitOfWork, and Specification patterns for Entity Framework Core with built-in dynamic sorting, searching, and pagination from query strings.

**Key types:** `IUnitOfWork`, `IRepository<T>`, `Specification<T>`, `QueryStringParameters`, `QueryableExtensions`

```bash
dotnet add package Poyraz.EntityFramework
```

```csharp
using Poyraz.EntityFramework;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Specifications;

// 1. Register in DI
builder.Services.AddUnitOfWork<MyDbContext>();

// 2. Define a specification
public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec() : base(p => p.IsActive)
    {
        ApplyOrderBy(p => p.Name);
        AddInclude(p => p.Category);
    }
}

// 3. Use via UnitOfWork
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Product>> GetActiveAsync()
    {
        var repo = _unitOfWork.Repository<Product>();
        return await repo.Find(new ActiveProductsSpec()).ToListAsync();
    }

    public async Task CreateAsync(Product product)
    {
        _unitOfWork.Repository<Product>().Add(product);
        await _unitOfWork.SaveChangesAsync();
    }
}

// 4. Dynamic search + sort + page from API query strings
var results = await repo.AsQueryable()
    .ApplyQueryStringParametersAsync<Product, ProductDto>(
        queryParams,
        projection: p => new ProductDto { Id = p.Id, Name = p.Name },
        searchFields: new() { p => p.Name, p => p.Description }
    );
// Returns ResultList<ProductDto> with Items, TotalResults, ResultsPerPage
```

📖 [Full documentation →](src/Poyraz.EntityFramework/README.md)

---

## Contributing

Contributions are welcome! Please submit issues and pull requests to help improve these libraries.

## License

These projects are licensed under the [MIT License](LICENSE).
