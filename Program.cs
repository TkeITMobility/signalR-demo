using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRDemo;

var host = Host.CreateDefaultBuilder(args)
               .ConfigureServices((context, services) => 
                {
                    services.AddSingleton(context.Configuration.GetRequiredSection("Settings").Get<Settings>()!);
                    services.AddHostedService<SignalRListener>();
                })
               .Build();
host.Run();
