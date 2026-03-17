using System.Reflection;
using NetArchTest.Rules;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace ArchitectureTests;

/// <summary>
/// Ensures every ICommand/IQuery has a corresponding handler.
/// </summary>
public class CqrsConventionTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(Product.Template.Kernel.Application.DependencyInjection).Assembly,
        typeof(Product.Template.Core.Identity.Application.Handlers.Auth.LoginCommandHandler).Assembly,
        typeof(Product.Template.Core.Authorization.Application.Permissions.AuthorizationPermissions).Assembly,
        typeof(Product.Template.Core.Tenants.Application.Permissions.TenantsPermissions).Assembly,
    ];

    [Fact]
    public void Commands_ShouldEndWith_Command()
    {
        var result = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Commands should end with 'Command'. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void Queries_ShouldEndWith_Query()
    {
        var result = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Queries should end with 'Query'. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void EveryCommand_ShouldHave_AHandler()
    {
        var commandTypes = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlerTypes = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlerCommandTypes = handlerTypes
            .SelectMany(h => h.GetInterfaces())
            .Where(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                 i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct()
            .ToList();

        var orphanCommands = commandTypes
            .Where(c => !handlerCommandTypes.Contains(c))
            .Select(c => c.FullName)
            .ToList();

        Assert.True(orphanCommands.Count == 0,
            $"Commands without handlers: {string.Join(", ", orphanCommands)}");
    }

    [Fact]
    public void EveryQuery_ShouldHave_AHandler()
    {
        var queryTypes = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlerTypes = Types.InAssemblies(ApplicationAssemblies)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlerQueryTypes = handlerTypes
            .SelectMany(h => h.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct()
            .ToList();

        var orphanQueries = queryTypes
            .Where(q => !handlerQueryTypes.Contains(q))
            .Select(q => q.FullName)
            .ToList();

        Assert.True(orphanQueries.Count == 0,
            $"Queries without handlers: {string.Join(", ", orphanQueries)}");
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypes is null || !result.FailingTypes.Any())
            return "(none)";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}

