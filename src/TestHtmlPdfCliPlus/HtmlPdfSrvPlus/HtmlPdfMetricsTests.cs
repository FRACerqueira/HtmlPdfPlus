// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics.Metrics;
using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    /// <summary>
    /// Exercises the <see cref="HtmlPdfMetrics"/> instruments via <see cref="MeterListener"/>,
    /// the same mechanism a real OpenTelemetry exporter would use. Every test tags its own
    /// builder with a unique sourcealias and filters captured measurements by it, so
    /// measurements from other tests' builders running concurrently against the same
    /// process-wide instruments cannot cause cross-test pollution.
    /// </summary>
    public class HtmlPdfMetricsTests
    {
        [Fact]
        public async Task Run_RecordsRequestDuration_ForSuccessfulRender()
        {
            var sourcealias = $"metrics-render-{Guid.NewGuid():N}";
            var measurements = new List<(double Value, bool Success)>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.request.duration")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                bool success = false;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "success") success = (bool)tag.Value!;
                }
                if (alias == sourcealias)
                {
                    measurements.Add((measurement, success));
                }
            });
            listener.Start();

            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync(sourcealias);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .Run(requestHtmlPdf, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var recorded = Assert.Single(measurements);
            Assert.True(recorded.Value > 0);
            Assert.True(recorded.Success);
        }

        [Fact]
        public async Task Run_RecordsRequestDuration_ForFailedRender()
        {
            var sourcealias = $"metrics-render-fail-{Guid.NewGuid():N}";
            var measurements = new List<bool>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.request.duration")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                bool success = false;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "success") success = (bool)tag.Value!;
                }
                if (alias == sourcealias)
                {
                    measurements.Add(success);
                }
            });
            listener.Start();

            // A validation failure (bad payload, decompression limit) returns before RunServer
            // is ever reached, so it deliberately does not record a request-duration measurement
            // - duration is meaningless for a request that was never actually attempted. Pool
            // exhaustion, by contrast, runs through RunServer/RecordRequestDuration like any
            // other outcome, so it is the failure mode used here.
            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.PagesBuffer(1);
            await objbuilder.BuildAsync(sourcealias);
            var heldPage = await objbuilder.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 200).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .Run(requestHtmlPdf, CancellationToken.None);

            Assert.False(result.IsSuccess);
            var recorded = Assert.Single(measurements);
            Assert.False(recorded);
        }

        [Fact]
        public async Task Run_ForValidationFailure_IncrementsErrorsButNotRequestDuration()
        {
            // A request-level validation failure (here: the decompressed payload exceeds the
            // configured limit) returns before RunServer is ever reached. It must still count
            // toward htmlpdfplus.errors (an error-rate alert must not go blind under a flood of
            // malformed requests), but htmlpdfplus.request.duration stays deliberately silent -
            // duration is meaningless for a request that was never actually attempted.
            var sourcealias = $"metrics-validation-{Guid.NewGuid():N}";
            var errorCodes = new List<string>();
            var durations = new List<double>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName &&
                    (instrument.Name == "htmlpdfplus.errors" || instrument.Name == "htmlpdfplus.request.duration"))
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                string? errorCode = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "error_code") errorCode = (string?)tag.Value;
                }
                if (alias == sourcealias && errorCode is not null)
                {
                    errorCodes.Add(errorCode);
                }
            });
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias" && (string?)tag.Value == sourcealias)
                    {
                        durations.Add(measurement);
                    }
                }
            });
            listener.Start();

            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.MaxDecompressedRequestSize(10);
            await objbuilder.BuildAsync(sourcealias);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .Run(requestHtmlPdf, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.InvalidRequest, result.Error!.Code);
            var recorded = Assert.Single(errorCodes);
            Assert.Equal(nameof(ErrorCode.InvalidRequest), recorded);
            Assert.Empty(durations);
        }

        [Fact]
        public async Task ScopeRequestRun_RecordsRequestDuration_ExactlyOnce()
        {
            // The ScopeRequest(...).Run(token) path - the one MapHtmlPdfEndpoints actually uses
            // in production - goes through HtmlPdfServerContext.Run, a separate entry point from
            // HtmlPdfServer.Run(byte[], token) exercised by the other tests in this file. Both
            // entry points call RecordRequestDuration, so this proves they don't nest into a
            // double-record for the path real HTTP hosts take.
            var sourcealias = $"metrics-scoperequest-{Guid.NewGuid():N}";
            var measurements = new List<bool>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.request.duration")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                bool success = false;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "success") success = (bool)tag.Value!;
                }
                if (alias == sourcealias)
                {
                    measurements.Add(success);
                }
            });
            listener.Start();

            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync(sourcealias);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .ScopeRequest(requestHtmlPdf)
                .Run(CancellationToken.None);

            Assert.True(result.IsSuccess);
            var recorded = Assert.Single(measurements);
            Assert.True(recorded);
        }

        [Fact]
        public async Task Run_IncrementsErrorsCounter_TaggedWithErrorCode_ForFailedRender()
        {
            var sourcealias = $"metrics-errors-{Guid.NewGuid():N}";
            var errorCodes = new List<string>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.errors")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                string? errorCode = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "error_code") errorCode = (string?)tag.Value;
                }
                if (alias == sourcealias && errorCode is not null)
                {
                    errorCodes.Add(errorCode);
                }
            });
            listener.Start();

            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.PagesBuffer(1);
            objbuilder.AcquireTimeout(20);
            await objbuilder.BuildAsync(sourcealias);
            var heldPage = await objbuilder.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .Run(requestHtmlPdf, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.PoolExhausted, result.Error!.Code);
            var recorded = Assert.Single(errorCodes);
            Assert.Equal(nameof(ErrorCode.PoolExhausted), recorded);
        }

        [Fact]
        public async Task Run_DoesNotIncrementErrorsCounter_ForSuccessfulRender()
        {
            var sourcealias = $"metrics-errors-success-{Guid.NewGuid():N}";
            var errorCodes = new List<string>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.errors")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias" && (string?)tag.Value == sourcealias)
                    {
                        errorCodes.Add("recorded");
                    }
                }
            });
            listener.Start();

            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync(sourcealias);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, sourcealias)
                .Run(requestHtmlPdf, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(errorCodes);
        }

        [Fact]
        public async Task AcquireAsync_RecordsAcquireWait_TaggedAcquired_OnSuccess()
        {
            var sourcealias = $"metrics-wait-acquired-{Guid.NewGuid():N}";
            var outcomes = new List<string>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.pool.acquire_wait")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                string? outcome = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "outcome") outcome = (string?)tag.Value;
                }
                if (alias == sourcealias && outcome is not null)
                {
                    outcomes.Add(outcome);
                }
            });
            listener.Start();

            using var obj = new HtmlPdfBuilder();
            await obj.BuildAsync(sourcealias);
            var page = await obj.AcquireAsync(CancellationToken.None);

            Assert.NotNull(page);
            var recorded = Assert.Single(outcomes);
            Assert.Equal("acquired", recorded);
        }

        [Fact]
        public async Task AcquireAsync_RecordsAcquireWait_TaggedPoolExhausted_WhenOwnTimeoutElapses()
        {
            var sourcealias = $"metrics-wait-exhausted-{Guid.NewGuid():N}";
            var outcomes = new List<string>();

            // Given: the pool's only page already held, before the listener starts - so the
            // measurement from acquiring it doesn't get captured alongside the one under test.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(1);
            obj.AcquireTimeout(20);
            await obj.BuildAsync(sourcealias);
            var heldPage = await obj.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.pool.acquire_wait")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                string? outcome = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "outcome") outcome = (string?)tag.Value;
                }
                if (alias == sourcealias && outcome is not null)
                {
                    outcomes.Add(outcome);
                }
            });
            listener.Start();

            var page = await obj.AcquireAsync(CancellationToken.None);

            Assert.Null(page);
            var recorded = Assert.Single(outcomes);
            Assert.Equal("pool_exhausted", recorded);
        }

        [Fact]
        public async Task AcquireAsync_RecordsAcquireWait_TaggedCanceled_WhenCallersOwnTokenFires()
        {
            var sourcealias = $"metrics-wait-canceled-{Guid.NewGuid():N}";
            var outcomes = new List<string>();

            // Given: the pool's only page already held, before the listener starts - same
            // reason as the pool_exhausted test above.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(1);
            await obj.BuildAsync(sourcealias);
            var heldPage = await obj.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.pool.acquire_wait")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                string? alias = null;
                string? outcome = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias") alias = (string?)tag.Value;
                    if (tag.Key == "outcome") outcome = (string?)tag.Value;
                }
                if (alias == sourcealias && outcome is not null)
                {
                    outcomes.Add(outcome);
                }
            });
            listener.Start();

            // Caller's own token (200ms) fires well before the pool's default AcquireTimeoutMs
            // (5000ms) - same scenario as HtmlPdfBuilderTest's
            // Ensure_buid_With_NotBufferExternalTimeout, exercised here for the metric it emits.
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(200);
            await Assert.ThrowsAsync<OperationCanceledException>(() => obj.AcquireAsync(cts.Token));

            var recorded = Assert.Single(outcomes);
            Assert.Equal("canceled", recorded);
        }

        [Fact]
        public async Task RecoverBrowserAsync_IncrementsBrowserRestartsCounter()
        {
            var sourcealias = $"metrics-restart-{Guid.NewGuid():N}";
            var restarts = new List<long>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.browser.restarts")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias" && (string?)tag.Value == sourcealias)
                    {
                        restarts.Add(measurement);
                    }
                }
            });
            listener.Start();

            // Same faithful CloseAsync crash simulation used elsewhere in this suite.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(1);
            await obj.BuildAsync(sourcealias);

            await obj.CurrentBrowser!.CloseAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 1)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }

            var recorded = Assert.Single(restarts);
            Assert.Equal(1, recorded);
        }

        [Fact]
        public async Task PoolAvailablePages_ObservableGauge_ReportsCurrentBufferLength()
        {
            var sourcealias = $"metrics-gauge-{Guid.NewGuid():N}";
            var gaugeValues = new List<int>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == HtmlPdfMetrics.MeterName && instrument.Name == "htmlpdfplus.pool.available_pages")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "sourcealias" && (string?)tag.Value == sourcealias)
                    {
                        gaugeValues.Add(measurement);
                    }
                }
            });
            listener.Start();

            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(3);
            await obj.BuildAsync(sourcealias);

            listener.RecordObservableInstruments();

            Assert.Contains(3, gaugeValues);
        }
    }
}
