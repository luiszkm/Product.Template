using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Controllers.v1;

namespace UnitTests.Security;

public class RbacMatrixConsistencyTests
{
    [Fact]
    public void IdentityProtectedEndpoints_ShouldBeMappedInRbacMatrix_WithMatchingPolicy()
    {
        var matrixEntries = LoadMatrixEntries()
            .Where(x => x.Route.StartsWith("/api/v1/identity", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(matrixEntries);

        var controller = typeof(IdentityController);
        var routePrefix = "/api/v1/identity";

        var protectedEndpoints = controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => new
            {
                Method = m,
                Authorize = m.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                    .Cast<AuthorizeAttribute>()
                    .FirstOrDefault(),
                Http = m.GetCustomAttributes().OfType<HttpMethodAttribute>().FirstOrDefault()
            })
            .Where(x => x.Authorize is not null && x.Http is not null)
            .Select(x => new
            {
                HttpMethod = x.Http!.HttpMethods.Single().ToUpperInvariant(),
                Route = BuildRoute(routePrefix, x.Http!.Template),
                Policy = x.Authorize!.Policy,
                MethodName = x.Method.Name
            })
            .ToList();

        Assert.NotEmpty(protectedEndpoints);

        foreach (var endpoint in protectedEndpoints)
        {
            var matrix = matrixEntries.FirstOrDefault(x =>
                string.Equals(x.HttpMethod, endpoint.HttpMethod, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Route, endpoint.Route, StringComparison.OrdinalIgnoreCase));

            Assert.True(matrix is not null,
                $"Endpoint {endpoint.HttpMethod} {endpoint.Route} ({endpoint.MethodName}) is missing in docs/security/RBAC_MATRIX.md");

            Assert.Equal(endpoint.Policy, matrix!.Policy);
        }
    }

    private static string BuildRoute(string prefix, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return prefix;

        return $"{prefix}/{template.TrimStart('/')}";
    }

    private static List<RbacMatrixEntry> LoadMatrixEntries()
    {
        var root = FindRepositoryRoot();
        var matrixPath = Path.Combine(root, "docs", "security", "RBAC_MATRIX.md");

        var lines = File.ReadAllLines(matrixPath);
        var entries = new List<RbacMatrixEntry>();

        foreach (var line in lines)
        {
            if (!line.StartsWith("| "))
                continue;

            var columns = line.Split('|', StringSplitOptions.TrimEntries);
            if (columns.Length < 6)
                continue;

            var method = columns[1];
            var route = columns[2].Trim('`');
            var policy = columns[4].Trim('`');

            if (string.Equals(method, "Método", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "---", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries.Add(new RbacMatrixEntry(method.ToUpperInvariant(), route, policy));
        }

        return entries;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Product.Template.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed record RbacMatrixEntry(string HttpMethod, string Route, string Policy);
}
