using System.Text.Json;
using Runner.Pipeline;

var manifestPath = args.Length >= 2 && string.Equals(args[0], "--manifest", StringComparison.OrdinalIgnoreCase)
    ? args[1]
    : null;

await new GradingPipeline().RunPipeline(manifestPath);
