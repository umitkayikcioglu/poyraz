# Poyraz

Poyraz is a suite of libraries designed to streamline and enhance your .NET development experience. It includes the following packages:

- **Poyraz.EntityFramework**: Provides additional abstractions such as Repository and UnitOfWork patterns, along with support for the Specification Pattern.

  [![](https://img.shields.io/nuget/v/Poyraz.EntityFramework.svg?logo=nuget)](https://www.nuget.org/packages/Poyraz.EntityFramework)
![](https://img.shields.io/nuget/dt/Poyraz.EntityFramework.svg?logo=nuget&color=yellow)
![](https://github.com/umitkayikcioglu/poyraz/workflows/Poyraz.EntityFramework/badge.svg)
- **Poyraz.Helpers.Primitives**: Contains helper classes and patterns such as `Result`, `Error`, and `ResultList` for functional programming and error handling.

  [![](https://img.shields.io/nuget/v/Poyraz.Helpers.Primitives.svg?logo=nuget)](https://www.nuget.org/packages/Poyraz.Helpers.Primitives)
![](https://img.shields.io/nuget/dt/Poyraz.Helpers.Primitives.svg?logo=nuget&color=yellow)
![](https://github.com/umitkayikcioglu/poyraz/workflows/Poyraz.Helpers.Primitives/badge.svg)

---

# Poyraz.EntityFramework

Poyraz.EntityFramework is a library designed to enhance your Entity Framework Core experience by providing additional abstractions such as Repository and UnitOfWork patterns, along with support for Specification Pattern.

---

## Features

- **Repository Pattern**: Simplifies CRUD operations with a generic repository interface and implementation.
- **UnitOfWork Pattern**: Manages transactions and ensures atomic operations.
- **Specification Pattern**: Provides a clean way to build and execute complex queries.
- **Dependency Injection Support**: Easily integrate with ASP.NET Core's DI container.

---

## Installation

Add the NuGet package to your project:

```bash
Install-Package Poyraz.EntityFramework
```

---

## Usage

Refer to the combined usage section for examples.

---

# Poyraz.Helpers.Primitives

Poyraz.Helpers.Primitives provides functional programming tools and error handling mechanisms for .NET applications. It simplifies common patterns such as error handling, validation, and method chaining.

---

## Features

- **Result Pattern**: Encapsulates operation results with success/failure states and error details.
- **Error Handling**: Provides a standardized way to define and handle errors using the `Error` class.
- **ResultList**: A convenient wrapper for paginated results with metadata.
- **Extension Methods**: Enhances the usability of `Result` with chaining and transformation methods.
- **Serialization**: Supports JSON serialization for errors and results.

---

## Installation

Add the NuGet package to your project:

```bash
Install-Package Poyraz.Helpers.Primitives
```

---

## Usage

Refer to the combined usage section for examples.

---

## Contributing

Contributions are welcome! Please submit issues and pull requests to help improve these libraries.

---

## License

These projects are licensed under the MIT License.

