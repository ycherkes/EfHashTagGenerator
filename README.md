# EfHashTagGenerator

[![NuGet version (EfHashTagGenerator)](https://img.shields.io/nuget/v/EfHashTagGenerator.svg?style=flat-square)](https://www.nuget.org/packages/EfHashTagGenerator/)

**EfHashTagGenerator** is an incremental Roslyn source‑generator that injects a short, stable hash‑tag into every Entity Framework Core LINQ query **at compile time**.  
The tag is derived from the call‑site (file + member + line) and lets you instantly map a SQL trace back to the exact line of code—without leaking your project structure in production.

---

## Why do I need it?

| Common pain | EfHashTagGenerator gives you… |
|-------------|------------------------------|
| **Manual `TagWith("…")`** is easy to forget or duplicate | 100 % automatic coverage—every query is tagged |
| **`TagWithCallSite()`** embeds full paths & line numbers → long SQL comments and possible code disclosure | A short, opaque 8‑character hash (e.g. `#a1b2c3d4`) that is stable across machines |
| **DbCommandInterceptor + StackTrace** or similar runtime hacks | Near-Zero runtime overhead—everything happens during build |

---

## Installation

```bash
dotnet add package EfHashTagGenerator
```
---

## Quick Start

```csharp
// Add TagWithCallSiteHash call:
var users = _context.Users.Where(u => u.IsActive).TagWithCallSiteHash();

// The source generator will automatically emit the equivalent:
var users = _context.Users.Where(u => u.IsActive).TagWith("#c9c32584");
```

Result in SQL profiler:

```sql
-- #c9c32584
SELECT ...
```

The tag is calculated from `UsersRepository.GetActive:L42`.

---

## How it Works

1. Scans for `TagWithCallSiteHash()` calls
2. Builds the string `<File>.<Member>:L<Line>`
3. Hashes it
4. Emits `EfHashTagExtensions.g.cs` with a switch lookup:

   ```csharp
   case "UsersRepository.GetActive:L42": return "#c9c32584";
   ```

*## 📂 [Output Location](https://andrewlock.net/creating-a-source-generator-part-6-saving-source-generator-output-in-source-control/)

Add the following code to your ptoject file to add the generated code to project folder to be able to find the source code by hashtag later:

```
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

And then exlude it from compilation

```
<ItemGroup>
    <!-- Exclude the output of source generators from the compilation -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

---

## Comparison with Alternatives

| Approach | Pros | Cons |
|----------|------|------|
| **`TagWith("manual")`** | Built into EF Core | Manual work, inconsistent naming |
| **`TagWithCallSite()`** | Automatic tag, no strings to type | Long paths, machine‑dependent, exposes code structure, unstable after refactoring |
| **DbCommandInterceptor + StackTrace** | Works on any EF Core version | Runtime cost; JIT inlining may break line numbers |

---

## Limitations

* The hash changes when the **line number** changes (code added/removed above).  
* Possible collisions.

---

## Why choose EfHashTagGenerator?

* ⚡ **Fast tracing** – jump from SQL profiler to Visual Studio in seconds.  
* 🛡 **Safe** – short, opaque tags; no file paths or method names in production.  
* 🏎 **Near-Zero runtime cost** – everything is compile‑time.  

---

## Contributing

Issues, ideas, and pull requests are welcome!
---

## License

Apache‑2.0
