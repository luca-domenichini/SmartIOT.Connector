using BenchmarkDotNet.Running;

// Run all benchmarks in this assembly.
// Usage: dotnet run -c Release
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();
