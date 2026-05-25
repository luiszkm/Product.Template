.PHONY: build verify test test-unit test-arch test-integration test-e2e format

build:
	dotnet build

format:
	dotnet format --verify-no-changes

test-arch:
	dotnet test tests/ArchitectureTests

test-unit:
	dotnet test tests/UnitTests

test-integration:
	dotnet test tests/IntegrationTests

test-e2e:
	dotnet test tests/E2ETests

test:
	dotnet test

verify: build test-arch test-unit format
	@echo "Verification gate passed."

health:
	curl -f http://localhost:5000/health
