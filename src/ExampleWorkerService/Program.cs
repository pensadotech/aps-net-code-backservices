using ExampleWorkerService;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService<Worker>();   // <<<--- Registers the background process 
	})
	.Build();

await host.RunAsync();
