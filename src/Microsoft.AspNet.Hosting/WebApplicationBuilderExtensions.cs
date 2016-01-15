// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplicationBuilderExtensions
    {
        private static readonly string ServerUrlsSeparator = ";";

        public static IWebApplicationBuilder UseCaptureStartupErrors(this IWebApplicationBuilder applicationBuilder, bool captureStartupError)
        {
            return applicationBuilder.UseSetting(WebApplicationDefaults.CaptureStartupErrorsKey, captureStartupError ? "true" : "false");
        }

        public static IWebApplicationBuilder UseStartup<TStartup>(this IWebApplicationBuilder applicationBuilder) where TStartup : class
        {
            return applicationBuilder.UseStartup(typeof(TStartup));
        }

        public static IWebApplicationBuilder UseServer(this IWebApplicationBuilder applicationBuilder, string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.ServerKey, assemblyName);
        }

        public static IWebApplicationBuilder UseServer(this IWebApplicationBuilder applicationBuilder, IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            return applicationBuilder.UseServer(new ServerFactory(server));
        }

        public static IWebApplicationBuilder UseApplicationBasePath(this IWebApplicationBuilder applicationBuilder, string applicationBasePath)
        {
            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.ApplicationBaseKey, applicationBasePath);
        }

        public static IWebApplicationBuilder UseEnvironment(this IWebApplicationBuilder applicationBuilder, string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.EnvironmentKey, environment);
        }

        public static IWebApplicationBuilder UseWebRoot(this IWebApplicationBuilder applicationBuilder, string webRoot)
        {
            if (webRoot == null)
            {
                throw new ArgumentNullException(nameof(webRoot));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.WebRootKey, webRoot);
        }

        public static IWebApplicationBuilder UseUrls(this IWebApplicationBuilder applicationBuilder, params string[] urls)
        {
            if (urls == null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.ServerUrlsKey, string.Join(ServerUrlsSeparator, urls));
        }

        public static IWebApplicationBuilder UseStartup(this IWebApplicationBuilder applicationBuilder, string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }

            return applicationBuilder.UseSetting(WebApplicationDefaults.ApplicationKey, startupAssemblyName);
        }

        public static IWebApplication Start(this IWebApplicationBuilder applicationBuilder, params string[] urls)
        {
            var application = applicationBuilder.UseUrls(urls).Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="application"></param>
        public static void Run(this IWebApplication application)
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    cts.Cancel();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                application.Run(cts.Token, "Application started. Press Ctrl+C to shut down.");
            }
        }

        /// <summary>
        /// Runs a web application and block the calling thread until token is triggered or shutdown is triggered
        /// </summary>
        /// <param name="application"></param>
        /// <param name="token">The token to trigger shutdown</param>
        public static void Run(this IWebApplication application, CancellationToken token)
        {
            application.Run(token, shutdownMessage: null);
        }

        private static void Run(this IWebApplication application, CancellationToken token, string shutdownMessage)
        {
            using (application)
            {
                application.Start();

                var hostingEnvironment = application.Services.GetService<IHostingEnvironment>();
                var applicationLifetime = application.Services.GetService<IApplicationLifetime>();

                Console.WriteLine("Hosting environment: " + hostingEnvironment.EnvironmentName);

                var serverAddresses = application.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                if (serverAddresses != null)
                {
                    foreach (var address in serverAddresses)
                    {
                        Console.WriteLine("Now listening on: " + address);
                    }
                }

                if (!string.IsNullOrEmpty(shutdownMessage))
                {
                    Console.WriteLine(shutdownMessage);
                }

                token.Register(state =>
                {
                    ((IApplicationLifetime)state).StopApplication();
                },
                applicationLifetime);

                applicationLifetime.ApplicationStopping.WaitHandle.WaitOne();
            }
        }

        private class ServerFactory : IServerFactory
        {
            private readonly IServer _server;

            public ServerFactory(IServer server)
            {
                _server = server;
            }

            public IServer CreateServer(IConfiguration configuration) => _server;
        }
    }
}