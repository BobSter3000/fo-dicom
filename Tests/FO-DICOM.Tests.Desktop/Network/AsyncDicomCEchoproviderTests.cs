﻿// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using FellowOakDicom.Tests.Helpers;
using FellowOakDicom.Tests.Network;
using Xunit;
using Xunit.Abstractions;

namespace FellowOakDicom.Tests.Desktop.Network
{
    [Collection("Network")]
    public class AsyncDicomCEchoProviderTests
    {
        private readonly XUnitDicomLogger _logger;

        public AsyncDicomCEchoProviderTests(ITestOutputHelper testOutputHelper)
        {
            _logger = new XUnitDicomLogger(testOutputHelper)
                .IncludeTimestamps()
                .IncludeThreadId()
                .WithMinimumLevel(LogLevel.Debug);
        }

        [Fact]
        public async Task OnCEchoRequestAsync_ShouldRespond()
        {
            var port = Ports.GetNext();

            using (DicomServer.Create<AsyncDicomCEchoProvider>(port, logger: _logger.IncludePrefix("DicomServer")))
            {
                var client = new FellowOakDicom.Network.Client.DicomClient("127.0.0.1", port, false, "SCU", "ANY-SCP")
                {
                    Logger = _logger.IncludePrefix(typeof(FellowOakDicom.Network.Client.DicomClient).Name)
                };

                DicomCEchoResponse response = null;
                DicomRequest.OnTimeoutEventArgs timeout = null;
                var request = new DicomCEchoRequest
                {
                    OnResponseReceived = (req, res) => response = res,
                    OnTimeout = (sender, args) => timeout = args
                };

                await client.AddRequestAsync(request).ConfigureAwait(false);
                await client.SendAsync().ConfigureAwait(false);

                Assert.NotNull(response);
                Assert.Equal(DicomStatus.Success, response.Status);
                Assert.Null(timeout);
            }
        }
    }

    #region helper classes

    public class AsyncDicomCEchoProvider : DicomService, IDicomServiceProvider, IDicomCEchoProvider
    {
        public AsyncDicomCEchoProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log)
            : base(stream, fallbackEncoding, log)
        {
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            foreach (var pc in association.PresentationContexts)
            {
                pc.SetResult(DicomPresentationContextResult.Accept);
            }

            await SendAssociationAcceptAsync(association);
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationReleaseRequestAsync()
            => await SendAssociationReleaseResponseAsync().ConfigureAwait(false);

        /// <inheritdoc />
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            // do nothing, ignore
        }

        /// <inheritdoc />
        public void OnConnectionClosed(Exception exception)
        {
            // do nothing here
        }

        public async Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }
    }


    #endregion
}
