using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Test34869
{
    public class Program
    {
        private static readonly X509Certificate2 _testCert = new("testCert.pfx", "testPassword");

        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            await host.StartAsync();

            using var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                ClientCertificates = { _testCert, },
            };
            using var httpClient = new HttpClient(httpClientHandler);

            var response = await httpClient.GetStringAsync("https://localhost:5001/");
            Console.WriteLine($"Response: {response}");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ListenLocalhost(5001, listenOptions =>
                        {
                            listenOptions.UseHttps(async (SslStream stream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken) =>
                            {
                                var ops = new SslServerAuthenticationOptions
                                {
                                    ClientCertificateRequired = true
                                };

                                ops.ServerCertificate = await GetCertificateAsync();

                                ops.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                                {
                                    Console.WriteLine("EXPECT TO HIT HERE FOR CUSTOM VALIDATION"); // Is called?
                                    return true;
                                };

                                ops.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
                                return ops;
                            }, null);
                        });
                    });
                });

        private static async Task<X509Certificate2> GetCertificateAsync()
        {
            // Await to keep test case similar to what was reported in https://github.com/dotnet/aspnetcore/issues/34869
            await Task.Yield();
            return _testCert;
        }
    }
}
