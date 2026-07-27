# Pattern: Unit Test (Handler)

> Isolated handler tests with inline fakes/stubs. No mocking frameworks.

## When to use

- Every command handler: happy path + at least one failure path
- Every query handler: found + not found (if applicable)
- Validators: see `.agents/patterns/validator.md`

Integration tests (HTTP pipeline, auth) are a separate pattern — see Planned `integration-test-auth.md`.

## File location

```
tests/UnitTests/{Feature}/{HandlerName}Tests.cs
```

Some modules use `tests/IntegrationTests/` with `HandlerTestFixture` for handler tests against real DI — prefer **UnitTests** for pure handler logic with inline fakes.

## Invariants (non-negotiable)

- ❌ No `Moq`, `NSubstitute`, or similar
- ✅ Inline `sealed class` fakes/stubs at bottom of test file (or shared inner class)
- ✅ `NullLogger<T>.Instance` for loggers
- ✅ Naming: `{Method}_{Scenario}_{ExpectedResult}`
- ✅ Forward `CancellationToken.None` in unit tests

## Structure checklist

- [ ] Happy path asserts on Output fields
- [ ] Failure path asserts exception type (`BusinessRuleException`, `NotFoundException`)
- [ ] Fake repository returns controlled data
- [ ] Fake `IUnitOfWork` captures `Commit()` calls (commands only)
- [ ] No database, no `WebApplicationFactory` in unit tests

## Annotated template — command handler

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.{Module}.Application.Handlers.{Feature};
using Product.Template.Core.{Module}.Application.Handlers.{Feature}.Commands;
using Product.Template.Core.{Module}.Domain.Entities;
using Product.Template.Core.{Module}.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.{Feature};

public class {Verb}{Noun}CommandHandlerTests
{
    private readonly Fake{Aggregate}Repository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new(WellKnownTenants.Public);

    private {Verb}{Noun}CommandHandler CreateHandler() => new(
        _repository,
        _unitOfWork,
        _tenantContext,
        NullLogger<{Verb}{Noun}CommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnOutput_WhenInputIsValid()
    {
        var command = new {Verb}{Noun}Command(/* valid */);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(_unitOfWork.CommitCalled);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenDuplicateExists()
    {
        _repository.SeedExisting(/* duplicate key */);
        var command = new {Verb}{Noun}Command(/* duplicate */);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        Assert.False(_unitOfWork.CommitCalled);
    }

    // ── Fakes (bottom of file) ──

    private sealed class Fake{Aggregate}Repository : I{Aggregate}Repository
    {
        private readonly List<{Aggregate}> _data = [];

        public void SeedExisting({Aggregate} entity) => _data.Add(entity);

        public Task<{Aggregate}?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_data.FirstOrDefault(e => e.Id == id));

        public Task AddAsync({Aggregate} entity, CancellationToken ct)
        {
            _data.Add(entity);
            return Task.CompletedTask;
        }

        // implement remaining interface members as no-ops or NotImplementedException
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool CommitCalled { get; private set; }

        public Task Commit(CancellationToken cancellationToken = default)
        {
            CommitCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTenantContext : ITenantContext
    {
        private readonly Guid? _tenantId;

        public StubTenantContext(Guid? tenantId) => _tenantId = tenantId;

        public Guid? TenantId => _tenantId;
        public string? TenantKey => "test";
        public TenantConfig? Tenant => null;
        public bool IsResolved => _tenantId.HasValue;
        public void SetTenant(TenantConfig tenant) => throw new NotSupportedException();
    }
}
```

## Annotated template — query handler

```csharp
[Fact]
public async Task Handle_ShouldThrowNotFoundException_WhenEntityMissing()
{
    await Assert.ThrowsAsync<NotFoundException>(() =>
        CreateHandler().Handle(new Get{Noun}ByIdQuery(Guid.NewGuid()), CancellationToken.None));
}
```

## Scenarios to cover by handler type

| Handler type | Minimum tests |
|---|---|
| Command (create) | valid → output + commit; duplicate → `BusinessRuleException` |
| Command (update) | valid → output; not found → `NotFoundException` |
| Command (delete) | valid → commit; not found → `NotFoundException` |
| Query (get by ID) | found → correct output; missing → `NotFoundException` |
| Query (list) | returns page with correct count |

## Verification

```bash
dotnet test tests/UnitTests --filter "FullyQualifiedName~{HandlerName}Tests"
```

## Reference

- Live (inline fakes): `tests/UnitTests/Ai/ChatCommandHandlerTests.cs`
- Live (fixture-based): `tests/IntegrationTests/Identity/RegisterUserCommandHandlerTests.cs`
- Rules: `.cursor/rules/tests.mdc`
- Skills: `/test-writer`
- Checklist: `.agents/checklists/new-feature.md` § Tests
