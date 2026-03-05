# Poyraz.EntityFramework

Clean abstractions over Entity Framework Core: generic Repository, UnitOfWork, Specification pattern, dynamic query-string-based sorting/searching/pagination, and date range filtering.

## Features

- **`IEntity`** / **`IEntityWithExternalId`** / **`ISimpleAuditable`** — Entity marker interfaces (`long Id`, `Guid RowGuid`, `DateTime CreatedAt`).
- **`IRepository<TEntity>`** — Generic CRUD (`Add`, `AddRange`, `Remove`, `RemoveRange`, `Update`), querying (`Find`, `FindAsync`, `FindByIdAsync`, `FindByRowGuidAsync`, `SingleAsync`, `AsQueryable`), and counting (`Count`, `CountExcludePagingParameter`, `Contains`).
- **`IUnitOfWork`** — Scoped transaction management (`SaveChangesAsync`), centralized repository access (`Repository<TEntity>()`), and `GetId<TEntity>(Guid?)` for RowGuid-to-Id resolution.
- **`Specification<TEntity>`** — Encapsulate query criteria, includes, ordering, grouping, and paging in reusable classes.
- **`QueryableExtensions`** — `ApplyQueryStringParametersAsync` for dynamic search+sort+page returning `ResultList<TDto>`, `ApplyDateRangeFilter` for date bounds, and `ApplySpecification` for manual spec evaluation.
- **`ResilientTransaction`** — EF Core resiliency-aware explicit transaction wrapper.
- **`SortEntityFieldAttribute`** / **`NonSearchAttribute`** — Attributes to control DTO-to-Entity property mapping for dynamic sorting and searching.

## Usage Guide

### 1. Dependency Injection Setup

Register UnitOfWork for your `DbContext` in `Program.cs` or `Startup.cs`:

```csharp
using Poyraz.EntityFramework;

builder.Services.AddUnitOfWork<MyDbContext>();
```

This registers `IUnitOfWork` as scoped, backed by `UnitOfWork<MyDbContext>`.

### 2. Defining Entities

Entities must implement `IEntity` (requires `long Id { get; }`). Optional interfaces:
- `IEntityWithExternalId` adds `Guid RowGuid { get; }` for external/public identifiers.
- `ISimpleAuditable` adds `DateTime CreatedAt { get; }` (used as default sort when no ordering is specified).

```csharp
using Poyraz.EntityFramework.Abstractions;

public class Product : IEntity, ISimpleAuditable
{
    public long Id { get; set; }
    public Guid RowGuid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Category Category { get; set; }
}
```

### 3. Specifications

Encapsulate query logic in reusable classes. The `Criteria` (Where expression) goes into the base constructor. Use protected methods for ordering, includes, and paging.

```csharp
using Poyraz.EntityFramework.Specifications;

// Simple specification — filter + order
public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec()
        : base(p => p.IsActive)      // Criteria = Where clause
    {
        ApplyOrderBy(p => p.Name);   // ORDER BY Name ASC
        AddInclude(p => p.Category); // Include navigation property
    }
}

// Specification with paging
public class ProductPageSpec : Specification<Product>
{
    public ProductPageSpec(int page, int pageSize)
        : base(p => p.IsActive)
    {
        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyPaging(skip: (page - 1) * pageSize, take: pageSize);
    }
}

// Specification with dynamic QueryString parameters (sorting, searching, paging from API request)
public class ProductQuerySpec : Specification<Product>
{
    public ProductQuerySpec(QueryStringParameters queryParams)
        : base(null)  // null criteria = no Where filter
    {
        // Automatically maps DTO property names to Entity properties for sort/search,
        // applies Skip/Take from queryParams.PageNumber & PageSize,
        // and builds full-text search expressions from queryParams.Search/FullTextSearch
        ApplyQueryStringParameters<ProductDto>(queryParams);
    }
}
```

#### Available Specification Methods

| Method | Description |
|---|---|
| `base(criteria)` | Constructor — sets the Where expression (`Expression<Func<TEntity, bool>>`) |
| `ApplyOrderBy(expr)` | ORDER BY ascending |
| `ApplyOrderByDescending(expr)` | ORDER BY descending |
| `AddInclude(expr)` | Eager-load a navigation property (expression) |
| `AddInclude(string)` | Eager-load via string path (for nested includes) |
| `ApplyPaging(skip, take)` | Enable Skip/Take pagination |
| `UndoPaging()` | Disable pagination (useful with `CountExcludePagingParameter`) |
| `ApplyGroupBy(expr)` | GROUP BY |
| `ApplyQueryStringParameters<TDto>(params)` | Dynamic sort/search/page from a `QueryStringParameters` object |

### 4. UnitOfWork and Repository Usage

Inject `IUnitOfWork`. It provides `Repository<TEntity>()` to get a repository for any entity and `SaveChangesAsync()` to persist all tracked changes.

```csharp
using Poyraz.EntityFramework.Abstractions;
using Microsoft.EntityFrameworkCore;

public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Query with specification — Find() returns IQueryable<T>
    public async Task<List<Product>> GetActiveProductsAsync()
    {
        var repo = _unitOfWork.Repository<Product>();
        var spec = new ActiveProductsSpec();
        return await repo.Find(spec).ToListAsync();
    }

    // Direct query with predicate
    public async Task<Product> GetByNameAsync(string name)
    {
        var repo = _unitOfWork.Repository<Product>();
        return await repo.FindAsync(p => p.Name == name);
    }

    // Find by Id
    public async Task<Product> GetByIdAsync(long id)
    {
        var repo = _unitOfWork.Repository<Product>();
        return await repo.FindByIdAsync(id);
    }

    // Find by external Guid (entity must implement IEntityWithExternalId)
    public async Task<Product> GetByRowGuidAsync(Guid rowGuid)
    {
        var repo = _unitOfWork.Repository<Product>();
        return await repo.FindByRowGuidAsync(rowGuid);
    }

    // CRUD operations — Add, Update, Remove + SaveChangesAsync
    public async Task CreateProductAsync(Product product)
    {
        var repo = _unitOfWork.Repository<Product>();
        repo.Add(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateProductAsync(Product product)
    {
        var repo = _unitOfWork.Repository<Product>();
        repo.Update(product);   // Attaches and marks as Modified
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(Product product)
    {
        var repo = _unitOfWork.Repository<Product>();
        repo.Remove(product);
        await _unitOfWork.SaveChangesAsync();
    }

    // Count with specification (excludes paging for total count)
    public int GetTotalActiveCount()
    {
        var repo = _unitOfWork.Repository<Product>();
        var spec = new ActiveProductsSpec();
        return repo.CountExcludePagingParameter(spec);
    }

    // Resolve long Id from Guid
    public long? ResolveProductId(Guid? rowGuid)
    {
        return _unitOfWork.GetId<Product>(rowGuid);
    }
}
```

### 5. QueryableExtensions — Dynamic Search, Sort, Page

#### `ApplyQueryStringParametersAsync`

Builds a full pipeline from an `IQueryable<TEntity>`: applies search via `EF.Functions.Like`, sorts, pages, projects to DTO, and returns a `ResultList<TDto>` with total count.

```csharp
public async Task<ResultList<ProductDto>> GetProductsAsync(QueryStringParameters queryParams)
{
    var query = _unitOfWork.Repository<Product>().AsQueryable();

    // Optional: specify which entity string properties to search on with EF.Functions.Like
    var searchFields = new List<Expression<Func<Product, string>>>
    {
        p => p.Name,
        p => p.Description
    };

    // Executes: search filter → CountAsync → sort → Skip/Take → Select projection → ToArrayAsync
    // Returns ResultList<ProductDto> with Items, TotalResults, ResultsPerPage
    return await query.ApplyQueryStringParametersAsync<Product, ProductDto>(
        queryStringParameters: queryParams,
        projection: p => new ProductDto { Id = p.Id, Name = p.Name, Description = p.Description },
        searchFields: searchFields
    );
}
```

`QueryStringParameters` properties:
- `PageNumber` (default 1), `PageSize` (default/max capped)
- `OrderBy` — e.g. `"name desc,createdAt"` (comma-separated, supports `asc`/`desc`)
- `Search` — simple search term
- `FullTextSearch` — full-text search term

#### `ApplyDateRangeFilter`

Filter an `IQueryable` by a date range. Supports `DateTime`, `DateTime?`, `DateOnly`, and `DateOnly?` properties.

```csharp
var range = new DateRange
{
    Start = new DateOnly(2025, 1, 1),
    End = new DateOnly(2025, 12, 31)
};

var query = _unitOfWork.Repository<Product>().AsQueryable();
query = query.ApplyDateRangeFilter(range, p => p.CreatedAt);
// Generates: WHERE CreatedAt >= '2025-01-01' AND CreatedAt <= '2025-12-31'
```

### 6. DTO Sort/Search Mapping Attributes

Use `SortEntityFieldAttribute` on DTO properties to map to different entity property names for sorting and searching. Use `NonSearchAttribute` to exclude a property from full-text search.

```csharp
public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; }

    // When API sends "?orderBy=categoryName", sort by Category.Name on the entity
    [SortEntityField<Category>(nameof(Category.Name))]
    public string CategoryName { get; set; }

    // Exclude from full-text search
    [NonSearch]
    public string InternalCode { get; set; }
}
```

### 7. ResilientTransaction

For explicit transactions with EF Core resiliency strategies:

```csharp
await ResilientTransaction.New(_dbContext).ExecuteAsync(async () =>
{
    _dbContext.Products.Add(product);
    await _dbContext.SaveChangesAsync();

    _dbContext.AuditLogs.Add(log);
    await _dbContext.SaveChangesAsync();
    // Both operations commit together or roll back together
});
```
