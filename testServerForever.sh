#!/bin/sh
dotnet test --filter DisplayName=PurpleSofa.Tests.TestServer.TestForever --framework "net7.0"

# exclude
# dotnet test --filter "FullyQualifiedName!=PurpleSofa.Tests.TestServer.TestForever" --collect:"XPlat Code Coverage"