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

using System;

namespace Microcks.Aspire.MainRemoteArtifacts;

/// <summary>
/// Represents a remote artifact with an optional secret for authentication.
/// This class supports the backward compatible approach where remote artifacts
/// can be specified either as simple URL strings or as objects with URL and secret.
/// </summary>
public class RemoteArtifact
{
    /// <summary>
    /// Gets or sets the URL of the remote artifact.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the optional name of the secret to use for authentication
    /// when downloading the artifact from the remote URL.
    /// </summary>
    public string? SecretName { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteArtifact"/> from a URL string.
    /// </summary>
    /// <param name="url">The URL of the remote artifact.</param>
    /// <returns>A new <see cref="RemoteArtifact"/> instance.</returns>
    public static implicit operator RemoteArtifact(string url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        return new RemoteArtifact { Url = url };
    }
}
