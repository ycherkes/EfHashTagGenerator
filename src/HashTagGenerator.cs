using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace EfHashTagGenerator;

[Generator]
public class HashTagGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationInfoFromProvider = context.CompilationProvider
            .Select((c, _) => CompilationHelper.LoadEfCoreContext(c));

        var calls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.Text: "TagWithCallSiteHash"
                    }
                },
                transform: static (ctx, _) => (InvocationExpressionSyntax)ctx.Node)
            .Collect();

        var combined = compilationInfoFromProvider.Combine(calls);

        context.RegisterImplementationSourceOutput(combined, GenerateExtension);
    }

    private static void GenerateExtension(SourceProductionContext context, (CompilationContext compilationContext, ImmutableArray<InvocationExpressionSyntax> calls) arg)
    {
        if (arg.compilationContext?.EfQueryableExtensionsType == null)
            return;

        var entries = new HashSet<(string file, string method, int line)>();

        foreach (var call in arg.calls)
        {
            var location = call.GetLocation().GetLineSpan();
            var file = location.Path;
            var line = location.EndLinePosition.Line + 1;

            var methodDecl = call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var methodName = methodDecl?.Identifier.Text ?? "<Main>$";

            entries.Add((file, methodName, line));
        }

        var switchCases = new StringBuilder();

        var locations = entries
            .Select(e => $"{System.IO.Path.GetFileNameWithoutExtension(e.file)}.{e.method}:L{e.line}")
            .Distinct()
            .ToArray();

        Array.Sort(locations, StringComparer.InvariantCulture);

        for (var index = 0; index < locations.Length; index++)
        {
            var location = locations[index];
            var hash = GetDeterministicHashCode(location).ToString("x8");
            if (index > 0)
            {
                switchCases.AppendLine();
            }
            switchCases.Append($"            case \"{location}\": return \"#{hash}\";");
        }

        var source = $$"""
                  using System;
                  using System.IO;
                  using System.Runtime.CompilerServices;
                  using Microsoft.EntityFrameworkCore;
                  using System.Linq;

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
                  {{switchCases}}
                              default: return location;
                          }
                      }
                  }
                  """;

        context.AddSource("EfHashTagExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
    private static int GetDeterministicHashCode(string input)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < input.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ input[i];
                if (i == input.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
            }

            return hash1 + hash2 * 1566083941;
        }
    }
}
