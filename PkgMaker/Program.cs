using PkgMaker.Services;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Main.Log("Canceled!");
    cts.Cancel();
    e.Cancel = true;
};
await Main.Run(args, cts.Token);
