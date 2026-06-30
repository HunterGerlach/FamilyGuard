using Shouldly;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;

namespace FamilyGuard.Application.Tests.UseCases;

public class CheckFirstRunUseCaseTests
{
    private readonly ISettingsRepository _settings = Substitute.For<ISettingsRepository>();
    private readonly IPolicyRepository _policyRepo = Substitute.For<IPolicyRepository>();
    private readonly CheckFirstRunUseCase _useCase;

    public CheckFirstRunUseCaseTests()
    {
        _useCase = new CheckFirstRunUseCase(_settings, _policyRepo);
    }

    [Fact]
    public void IsPinConfigured_ReturnsFalse_WhenNoPinSet()
    {
        _settings.Load().Returns(new ProtectedSettings { PinHash = string.Empty });

        _useCase.IsPinConfigured().ShouldBeFalse();
    }

    [Fact]
    public void IsPinConfigured_ReturnsTrue_WhenPinSet()
    {
        _settings.Load().Returns(new ProtectedSettings { PinHash = "somehash" });

        _useCase.IsPinConfigured().ShouldBeTrue();
    }

    [Fact]
    public void SetupInitialPin_DelegatesToRepository()
    {
        _useCase.SetupInitialPin("1234");

        _settings.Received(1).SetPin("1234");
    }

    [Fact]
    public void EnsureDefaults_CreatesDefaultRules()
    {
        _useCase.EnsureDefaults();

        _policyRepo.Received(1).EnsureDefaultRules();
    }
}
