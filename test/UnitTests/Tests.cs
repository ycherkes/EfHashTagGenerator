using EfHashTagGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using VerifyCS = UnitTests.CSharpSourceGeneratorVerifier<EfHashTagGenerator.HashTagGenerator>;

namespace UnitTests;

public class Tests
{
    [Fact]
    public async Task GeneratesReadOnlyEntitiesAndDbContext()
    {
        // Input source code
        var inputSource = """
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class User
                              {
                                  public int Id { get; set; }
                                  public string Name { get; set; }
                                  public ICollection<Order> Orders { get; set; }
                                  
                                  public static User Create(int id, string name, ICollection<Order> orders)
                                  {
                                      return new User
                                      {
                                          Id = id,
                                          Name = name,
                                          Orders = orders
                                      };
                                  }
                              }
                          
                              public class Order
                              {
                                  public int Id { get; set; }
                                  public string Description { get; set; }
                              }
                          
                              public class MyDbContext : DbContext
                              {
                                  public DbSet<User> Users { get; set; }
                              }
                              
                              public static class Program
                              {
                                  public static void Main(MyDbContext dbContext)
                                  {
                                      var usrs = dbContext.Users.TagWithCallSiteHash();
                                      foreach(var usr in usrs)
                                      {
                                          System.Console.WriteLine(usr.Name);
                                      }
                                  }
                              }
                          }
                          """;

        var expectedGeneratedExtensionMethodSource =
            """
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
                        case "Test0.Main:L38": return "#c9c32584";
                        default: return location;
                    }
                }
            }
            """;

        // Configure the test
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    // Verify the generated sources
                    (typeof(HashTagGenerator), "EfHashTagExtensions.g.cs", expectedGeneratedExtensionMethodSource)
                }
            },
        };

        // Run the test
        await test.RunAsync();
    }
}