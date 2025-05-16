[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://stand-with-ukraine.pp.ua)


 # EfHashTagGenerator

[![NuGet version (EfHashTagGenerator)](https://img.shields.io/nuget/v/EfHashTagGenerator.svg?style=flat-square)](https://www.nuget.org/packages/EfHashTagGenerator/)

**EfHashTagGenerator** is a Roslyn Incremental Source Generator that enhances EF Core LINQ query tracing by generating deterministic and consistent SQL query tags based on the call site. 


## 🛠 Usage

1. Reference this generator from your project (as an analyzer NuGet package or project reference).
2. In your LINQ queries, simply call:

```csharp
query.TagWithCallSiteHash();
```

3. The source generator will automatically emit:

```csharp
query.TagWith("#a1b2c3d4");
```

The tag `#a1b2c3d4` is generated from a stable hash of the call site like:

```
MyClass.GetUsers:L42
```

## 🔧 How It Works

1. Scans all invocations of `TagWithCallSiteHash()` in your codebase.
2. Extracts:
   - File path
   - Method name
   - Line number
3. Computes a **[deterministic hash code](https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/)**.
4. Emits a helper class:

```csharp

internal static class EfHashTagExtensions
{
    public static IQueryable<T> TagWithCallSiteHash<T>(
        this IQueryable<T> query,
        [CallerFilePath] string filePath = null,
        [CallerMemberName] string memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var location = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}:L{lineNumber}";
        var hashTag = GetHashTagByLocation(location);
        return query.TagWith(hashTag);
    }

    private static string GetHashTagByLocation(string location)
    {
        switch (location)
        {
            case "Test0.Main:L38": return "#c9c32584";
            // ...
            default: return location;
        }
    }
}
```

---

## 📝 Requirements

- EF Core (uses `.TagWith()` internally)

---

## 📂 [Output Location](https://andrewlock.net/creating-a-source-generator-part-6-saving-source-generator-output-in-source-control/)

Add the following code to your project file to add the generated code to the project folder to be able to find the source code by hashtag later:

```
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

And then exclude it from compilation

```
<ItemGroup>
    <!-- Exclude the output of source generators from the compilation -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

---
## Useful links:

- [Query tagging](https://www.danielmallott.com/posts/tag-your-queries-in-entity-framework-core)
- [Better Tagging of EF Core Queries with .NET 6](https://thirty25.blog/blog/2021/12/tagging-query-with-ef-core)
- [Automatic tagging with DbCommandInterceptor and StackTrace](https://stackoverflow.com/a/78550020/7901167)
- [Why is string.GetHashCode() different each time I run my program in .NET Core?](https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core)
- [Slow Sql Server queries tagged with callsite hash](https://gist.github.com/ycherkes/fe6688ea5be65df0c38e8948e0ffc464)
---

## 📃 License

Apache 2.0
