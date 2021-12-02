#!/bin/sh
dotnet test --filter DisplayName=PurpleSofa.Tests.TestServer.TestForever

# exclude
# dotnet test --filter "FullyQualifiedName!=PurpleSofa.Tests.TestServer.TestForever"