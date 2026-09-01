using Xunit;

// Each test class gets its own ApiTestFactory (own WebApplicationFactory<Program> host, own InMemory
// database — see ApiTestFactory). Every host build re-runs Program.cs's top-level statements,
// including `Log.Logger = new LoggerConfiguration()...CreateBootstrapLogger();`. Serilog's
// reloadable bootstrap logger only supports being "frozen" (finalized by UseSerilog) once per
// process; xUnit's default cross-class parallelism can start two hosts at once and race two threads
// over that single static Serilog.Log.Logger slot ("the logger is already frozen"). Serializing test
// classes avoids that race — it costs a little wall-clock time on a ~20-test suite, not correctness.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
