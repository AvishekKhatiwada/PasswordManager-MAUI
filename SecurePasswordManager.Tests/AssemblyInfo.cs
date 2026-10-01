using Xunit;

// DatabaseService.InitializeAsync/CloseAsync call the process-wide static
// SQLiteAsyncConnection.ResetPool(), which drops every test's pooled
// connection, not just its own. Serialize test execution so one test's
// lock/unlock cycle can't evict another test's open vault connection.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
