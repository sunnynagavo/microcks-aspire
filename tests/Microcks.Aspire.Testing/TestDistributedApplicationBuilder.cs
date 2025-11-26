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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microcks.Aspire.Testing;

/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public sealed class TestDistributedApplicationBuilder : IDistributedApplicationBuilder, IDisposable
{
    private readonly DistributedApplicationBuilder _innerBuilder;
    private bool _disposedValue;
    private DistributedApplication? _app;

    /// <summary>
    /// Creates a new instance of <see cref="TestDistributedApplicationBuilder"/> with the specified operation.
    /// </summary>
    /// <param name="operation">The distributed application operation (Run or Publish).</param>
    /// <returns>A new <see cref="TestDistributedApplicationBuilder"/> instance.</returns>
    public static TestDistributedApplicationBuilder Create(DistributedApplicationOperation operation)
    {
        var args = operation switch
        {
            DistributedApplicationOperation.Run => (string[])[],
            DistributedApplicationOperation.Publish => ["Publishing:Publisher=manifest"],
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        return Create(args);
    }

    /// <summary>
    /// Creates a new instance of <see cref="TestDistributedApplicationBuilder"/> with the specified command-line arguments.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A new <see cref="TestDistributedApplicationBuilder"/> instance.</returns>
    public static TestDistributedApplicationBuilder Create(params string[] args)
    {
        return new TestDistributedApplicationBuilder(options => options.Args = args);
    }

    /// <summary>
    /// Creates a new instance of <see cref="TestDistributedApplicationBuilder"/> with the specified test output helper and command-line arguments.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging.</param>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A new <see cref="TestDistributedApplicationBuilder"/> instance.</returns>
    public static TestDistributedApplicationBuilder Create(ITestOutputHelper testOutputHelper, params string[] args)
    {
        return new TestDistributedApplicationBuilder(options => options.Args = args, testOutputHelper);
    }

    /// <summary>
    /// Creates a new instance of <see cref="TestDistributedApplicationBuilder"/> with optional configuration and test output helper.
    /// </summary>
    /// <param name="configureOptions">An optional action to configure the distributed application options.</param>
    /// <param name="testOutputHelper">An optional xUnit test output helper for logging.</param>
    /// <returns>A new <see cref="TestDistributedApplicationBuilder"/> instance.</returns>
    public static TestDistributedApplicationBuilder Create(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        return new TestDistributedApplicationBuilder(configureOptions, testOutputHelper);
    }

    private TestDistributedApplicationBuilder(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        var appAssembly = typeof(TestDistributedApplicationBuilder).Assembly;
        var assemblyName = appAssembly.FullName;

        _innerBuilder = BuilderInterceptor.CreateBuilder(Configure);

        _innerBuilder.Services.AddHttpClient();
        _innerBuilder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        if (testOutputHelper is not null)
        {
            WithTestAndResourceLogging(testOutputHelper);
        }

        void Configure(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostBuilderOptions)
        {
            hostBuilderOptions.EnvironmentName = Environments.Development;
            hostBuilderOptions.ApplicationName = appAssembly.GetName().Name;
            applicationOptions.AssemblyName = assemblyName;
            applicationOptions.DisableDashboard = true;
            var cfg = hostBuilderOptions.Configuration ??= new();
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DcpPublisher:RandomizePorts"] = "true",
                ["DcpPublisher:DeleteResourcesOnShutdown"] = "true",
                ["DcpPublisher:ResourceNameSuffix"] = $"{Random.Shared.Next():x}",
            });

            configureOptions?.Invoke(applicationOptions);
        }
    }

    /// <summary>
    /// Configures the builder to forward logs from tests and resources to the specified test output helper.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TestDistributedApplicationBuilder WithTestAndResourceLogging(ITestOutputHelper testOutputHelper)
    {
        Services.AddHostedService<ResourceLoggerForwarderService>();
        Services.AddLogging(builder =>
        {
            builder.AddXUnit(testOutputHelper);
            builder.AddFilter("Aspire.Hosting", LogLevel.Trace);
            builder.AddFilter("Aspire.CommunityToolkit.Hosting", LogLevel.Trace);
        });
        return this;
    }

    /// <summary>
    /// Gets the configuration manager for the distributed application.
    /// </summary>
    public ConfigurationManager Configuration => _innerBuilder.Configuration;

    /// <summary>
    /// Gets the directory path of the app host project.
    /// </summary>
    public string AppHostDirectory => _innerBuilder.AppHostDirectory;

    /// <summary>
    /// Gets the assembly of the app host project.
    /// </summary>
    public Assembly? AppHostAssembly => _innerBuilder.AppHostAssembly;

    /// <summary>
    /// Gets the host environment information.
    /// </summary>
    public IHostEnvironment Environment => _innerBuilder.Environment;

    /// <summary>
    /// Gets the service collection for dependency injection.
    /// </summary>
    public IServiceCollection Services => _innerBuilder.Services;

    /// <summary>
    /// Gets the execution context for the distributed application.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext => _innerBuilder.ExecutionContext;

    /// <summary>
    /// Gets the collection of resources in the distributed application.
    /// </summary>
    public IResourceCollection Resources => _innerBuilder.Resources;

    /// <summary>
    /// Gets the eventing system for the distributed application.
    /// </summary>
    public IDistributedApplicationEventing Eventing => _innerBuilder.Eventing;

    /// <summary>
    /// Adds a resource to the distributed application.
    /// </summary>
    /// <typeparam name="T">The type of resource to add.</typeparam>
    /// <param name="resource">The resource instance to add.</param>
    /// <returns>A resource builder for further configuration of the resource.</returns>
    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource => _innerBuilder.AddResource(resource);

    [MemberNotNull(nameof(_app))]
    /// <summary>
    /// Builds the distributed application.
    /// </summary>
    /// <returns>The built <see cref="DistributedApplication"/> instance.</returns>
    public DistributedApplication Build() => _app = _innerBuilder.Build();

    /// <summary>
    /// Asynchronously builds the distributed application.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the build operation.</param>
    /// <returns>A task that represents the asynchronous build operation, containing the built <see cref="DistributedApplication"/> instance.</returns>
    public Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken = default) => Task.FromResult(Build());

    /// <summary>
    /// Creates a resource builder for the specified resource.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="resource">The resource instance.</param>
    /// <returns>A resource builder for the specified resource.</returns>
    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return _innerBuilder.CreateResourceBuilder(resource);
    }

    /// <summary>
    /// Disposes the test distributed application builder and any built application.
    /// </summary>
    public void Dispose()
    {
        if (!_disposedValue)
        {
            _disposedValue = true;
            if (_app is null)
            {
                try
                {
                    Build();
                }
                catch
                {
                }
            }

            _app?.Dispose();
        }
    }

    private sealed class BuilderInterceptor : IObserver<DiagnosticListener>
    {
        private static readonly ThreadLocal<BuilderInterceptor?> s_currentListener = new();
        private readonly ApplicationBuilderDiagnosticListener _applicationBuilderListener;
        private readonly Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? _onConstructing;

        private BuilderInterceptor(Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing)
        {
            _onConstructing = onConstructing;
            _applicationBuilderListener = new(this);
        }

        public static DistributedApplicationBuilder CreateBuilder(Action<DistributedApplicationOptions, HostApplicationBuilderSettings> onConstructing)
        {
            var interceptor = new BuilderInterceptor(onConstructing);
            var original = s_currentListener.Value;
            s_currentListener.Value = interceptor;
            try
            {
                using var subscription = DiagnosticListener.AllListeners.Subscribe(interceptor);
                return new DistributedApplicationBuilder([]);
            }
            finally
            {
                s_currentListener.Value = original;
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(DiagnosticListener value)
        {
            if (s_currentListener.Value != this)
            {
                // Ignore events that aren't for this listener
                return;
            }

            if (value.Name == "Aspire.Hosting")
            {
                _applicationBuilderListener.Subscribe(value);
            }
        }

        private sealed class ApplicationBuilderDiagnosticListener(BuilderInterceptor owner) : IObserver<KeyValuePair<string, object?>>
        {
            private IDisposable? _disposable;

            public void Subscribe(DiagnosticListener listener)
            {
                _disposable = listener.Subscribe(this);
            }

            public void OnCompleted()
            {
                _disposable?.Dispose();
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (s_currentListener.Value != owner)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Key == "DistributedApplicationBuilderConstructing")
                {
                    var (options, innerBuilderOptions) = ((DistributedApplicationOptions, HostApplicationBuilderSettings))value.Value!;
                    owner._onConstructing?.Invoke(options, innerBuilderOptions);
                }
            }
        }
    }
}
