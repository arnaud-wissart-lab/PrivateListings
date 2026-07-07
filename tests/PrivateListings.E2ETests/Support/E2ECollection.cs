namespace PrivateListings.E2ETests.Support;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class E2ETestGroup : ICollectionFixture<E2EApplicationFixture>
{
    public const string Name = "E2E";
}
