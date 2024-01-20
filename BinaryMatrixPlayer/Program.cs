using System.Net.WebSockets;
using BinaryMatrixPlayer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

/* app.MapGet("/", () => "Hello World!"); */

app.UseWebSockets();

app.Use(async (context, next) => {
	if(context.Request.Path == "/connect-socket") {
		if(context.WebSockets.IsWebSocketRequest) {
			WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
			try {
				await BackgroundRunner.AddSocket(socket, context.RequestAborted).CompleteTask;
			} catch(OperationCanceledException) {}
		} else {
			context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
		}

		return;
	}

	await next(context);
});

PhysicalFileProvider fileProvider = new(Path.Join(app.Environment.ContentRootPath, "static"));

app.UseRewriter(new RewriteOptions().Add(context => {

	HttpRequest request = context.HttpContext.Request;

	if(!(request.Path.Value?.EndsWith("/") ?? false)) {
		/* ignore anything that doesn't end in a slash */
		return;
	}

	string indexPath = request.Path.Value + "index.html";
	if(fileProvider.GetFileInfo(indexPath).Exists)
		request.Path = new PathString(indexPath);
}));

app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

Thread backgroundRunnerThread = new(BackgroundRunner.Run);

app.Lifetime.ApplicationStarted.Register(() => {
	backgroundRunnerThread.Start();
}, useSynchronizationContext: false);

app.Lifetime.ApplicationStopping.Register(() => {
	BackgroundRunner.Stop();
	backgroundRunnerThread.Join();
}, useSynchronizationContext: false);

app.Run();
