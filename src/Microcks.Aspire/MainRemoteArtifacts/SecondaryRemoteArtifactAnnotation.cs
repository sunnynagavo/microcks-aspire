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

internal sealed class SecondaryRemoteArtifactAnnotation : IRemoteArtifactAnnotation
{
    public string RemoteArtifactUrl { get; }

    /// <summary>
    /// Gets the optional name of the secret to use for authentication
    /// when downloading the artifact from the remote URL.
    /// </summary>
    public string? SecretName { get; }

    public SecondaryRemoteArtifactAnnotation(string remoteArtifactUrl, string? secretName = null)
    {
        ArgumentNullException.ThrowIfNull(remoteArtifactUrl, nameof(remoteArtifactUrl));
        RemoteArtifactUrl = remoteArtifactUrl;
        SecretName = secretName;
    }
}
