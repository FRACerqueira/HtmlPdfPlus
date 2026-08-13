// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using Xunit;

// Several behavioral tests spawn real Chromium processes (browser build/crash-recovery)
// while others race a tight timing margin (a near-zero Timeout against a blocking sleep,
// or a sub-2s HTTP deadline). Under xUnit's default parallel execution these compete for
// the thread pool, and the tight margins occasionally lose the race under that CPU
// contention - not because of a product bug, but because the test itself has no slack.
// Disabling parallelization removes the contention instead of loosening the margins,
// which would weaken what those tests are actually asserting.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
