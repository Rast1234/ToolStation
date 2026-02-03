using PkgMaker.Services;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Main.Log("Canceled!");
    cts.Cancel();
    e.Cancel = true;
};
await new Main().Run(args, cts.Token);