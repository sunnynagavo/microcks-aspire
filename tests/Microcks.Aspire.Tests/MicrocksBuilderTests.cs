//
// Copyright The Microcks Authors.
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//

using Xunit;

using FakeItEasy;

using System;
using System.Linq;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using Microcks.Aspire.MainRemoteArtifacts;

namespace Microcks.Aspire.Tests;

/// <summary>
/// Tests that validate the behavior of the <c>AddMicrocks</c> builder extension
/// without starting any external resources. These tests run fast and only
/// exercise builder configuration logic.
/// </summary>
public class MicrocksBuilderTests
{
    /// <summary>
    /// Ensures that passing null or whitespace to the builder extension
    /// <c>AddMicrocks</c> results in an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="name">Input name to validate.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddMicrocks_WithNullOrWhitespaceName_ShouldThrowsException(string name)
    {
        IDistributedApplicationBuilder builder = A.Fake<IDistributedApplicationBuilder>();

        Assert.Throws<ArgumentException>(() => builder.AddMicrocks(name!));
    }

    /// <summary>
    /// Verifies that calling <c>AddMicrocks</c> with valid arguments registers
    /// a resource and sets its default container image and registry annotations.
    /// </summary>
    [Fact]
    public void AddMicrocks_WithValidParameters_ShouldConfigureResourceCorrectly()
    {
        var builder = DistributedApplication.CreateBuilder();

        var name = $"microcks{Guid.NewGuid()}";
        var microcks = builder.AddMicrocks(name);

        Assert.NotNull(microcks.Resource);
        Assert.Equal(name, microcks.Resource.Name);

        var containerImageAnnotation = microcks.Resource
            .Annotations
            .OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        Assert.Equal(MicrocksContainerImageTags.Image, containerImageAnnotation?.Image);
        Assert.Equal(MicrocksContainerImageTags.Tag, containerImageAnnotation?.Tag);
        Assert.Equal(MicrocksContainerImageTags.Registry, containerImageAnnotation?.Registry);
    }

    /// <summary>
    /// Verifies that calling <c>WithMainRemoteArtifacts</c> with string URLs
    /// adds the correct annotations to the resource.
    /// </summary>
    [Fact]
    public void WithMainRemoteArtifacts_WithStringUrls_ShouldAddAnnotations()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = $"microcks{Guid.NewGuid()}";

        var microcks = builder.AddMicrocks(name)
            .WithMainRemoteArtifacts(
                "https://example.com/artifact1.yaml",
                "https://example.com/artifact2.yaml");

        var annotations = microcks.Resource.Annotations
            .OfType<MainRemoteArtifactAnnotation>()
            .ToList();

        Assert.Equal(2, annotations.Count);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://example.com/artifact1.yaml" && a.SecretName == null);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://example.com/artifact2.yaml" && a.SecretName == null);
    }

    /// <summary>
    /// Verifies that calling <c>WithMainRemoteArtifacts</c> with RemoteArtifact objects
    /// adds the correct annotations with secret names to the resource.
    /// </summary>
    [Fact]
    public void WithMainRemoteArtifacts_WithRemoteArtifactObjects_ShouldAddAnnotationsWithSecrets()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = $"microcks{Guid.NewGuid()}";

        var microcks = builder.AddMicrocks(name)
            .WithMainRemoteArtifacts(
                new RemoteArtifact { Url = "https://gitlab.com/repo/artifact.yaml", SecretName = "gl-secret" },
                "https://github.com/repo/artifact2.yaml" // backward compatible string format via implicit conversion
            );

        var annotations = microcks.Resource.Annotations
            .OfType<MainRemoteArtifactAnnotation>()
            .ToList();

        Assert.Equal(2, annotations.Count);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://gitlab.com/repo/artifact.yaml" && a.SecretName == "gl-secret");
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://github.com/repo/artifact2.yaml" && a.SecretName == null);
    }

    /// <summary>
    /// Verifies that calling <c>WithSecondaryRemoteArtifacts</c> with string URLs
    /// adds the correct annotations to the resource.
    /// </summary>
    [Fact]
    public void WithSecondaryRemoteArtifacts_WithStringUrls_ShouldAddAnnotations()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = $"microcks{Guid.NewGuid()}";

        var microcks = builder.AddMicrocks(name)
            .WithSecondaryRemoteArtifacts(
                "https://example.com/secondary1.yaml",
                "https://example.com/secondary2.yaml");

        var annotations = microcks.Resource.Annotations
            .OfType<SecondaryRemoteArtifactAnnotation>()
            .ToList();

        Assert.Equal(2, annotations.Count);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://example.com/secondary1.yaml" && a.SecretName == null);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://example.com/secondary2.yaml" && a.SecretName == null);
    }

    /// <summary>
    /// Verifies that calling <c>WithSecondaryRemoteArtifacts</c> with RemoteArtifact objects
    /// adds the correct annotations with secret names to the resource.
    /// </summary>
    [Fact]
    public void WithSecondaryRemoteArtifacts_WithRemoteArtifactObjects_ShouldAddAnnotationsWithSecrets()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = $"microcks{Guid.NewGuid()}";

        var microcks = builder.AddMicrocks(name)
            .WithSecondaryRemoteArtifacts(
                new RemoteArtifact { Url = "https://gitlab.com/repo/examples.yaml", SecretName = "gl-secret" },
                "https://github.com/repo/examples2.yaml"
            );

        var annotations = microcks.Resource.Annotations
            .OfType<SecondaryRemoteArtifactAnnotation>()
            .ToList();

        Assert.Equal(2, annotations.Count);
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://gitlab.com/repo/examples.yaml" && a.SecretName == "gl-secret");
        Assert.Contains(annotations, a => a.RemoteArtifactUrl == "https://github.com/repo/examples2.yaml" && a.SecretName == null);
    }

    /// <summary>
    /// Verifies that RemoteArtifact implicit conversion from string works correctly.
    /// </summary>
    [Fact]
    public void RemoteArtifact_ImplicitConversionFromString_ShouldWork()
    {
        RemoteArtifact artifact = "https://example.com/artifact.yaml";

        Assert.Equal("https://example.com/artifact.yaml", artifact.Url);
        Assert.Null(artifact.SecretName);
    }

    /// <summary>
    /// Verifies that mixing main and secondary remote artifacts with and without secrets works.
    /// </summary>
    [Fact]
    public void WithMixedRemoteArtifacts_ShouldAddAllAnnotationsCorrectly()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = $"microcks{Guid.NewGuid()}";

        var microcks = builder.AddMicrocks(name)
            .WithMainRemoteArtifacts(
                new RemoteArtifact { Url = "https://gitlab.com/repo/main.yaml", SecretName = "main-secret" }
            )
            .WithSecondaryRemoteArtifacts(
                new RemoteArtifact { Url = "https://gitlab.com/repo/secondary.yaml", SecretName = "sec-secret" }
            );

        var mainAnnotations = microcks.Resource.Annotations
            .OfType<MainRemoteArtifactAnnotation>()
            .ToList();
        var secondaryAnnotations = microcks.Resource.Annotations
            .OfType<SecondaryRemoteArtifactAnnotation>()
            .ToList();

        Assert.Single(mainAnnotations);
        Assert.Single(secondaryAnnotations);
        Assert.Equal("main-secret", mainAnnotations[0].SecretName);
        Assert.Equal("sec-secret", secondaryAnnotations[0].SecretName);
    }
}
