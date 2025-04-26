using Microsoft.CodeAnalysis;

namespace EfHashTagGenerator;

internal class CompilationContext
{
    public INamedTypeSymbol EfQueryableExtensionsType { get; set; }
}