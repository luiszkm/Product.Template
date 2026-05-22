using E2ETests.Security;

namespace E2ETests.Common;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RbacE2ECollection : ICollectionFixture<RbacWebApplicationFactory>
{
    public const string Name = "RbacE2E";
}
