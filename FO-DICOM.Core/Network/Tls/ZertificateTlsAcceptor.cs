﻿using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FellowOakDicom.Network.Tls
{
    public class ZertificateTlsAcceptor : ITlsAcceptor
    {

        public X509Certificate Certificate { get; set; }

        public TimeSpan SslHandshakeTimeout { get; set; } = TimeSpan.FromMinutes(1);


        public ZertificateTlsAcceptor(string certificateName)
        {
            Certificate = GetX509Certificate(certificateName);
        }

        public Stream AcceptTls(Stream encryptedStream, string remoteAddress, int localPort)
        {
            var ssl = new SslStream(encryptedStream, false);

            var authenticationSucceeded = Task.Run(
                async () => await ssl.AuthenticateAsServerAsync(Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false).ConfigureAwait(false)
                ).Wait(SslHandshakeTimeout);

            if (!authenticationSucceeded)
            {
                throw new DicomNetworkException($"SSL server authentication took longer than {SslHandshakeTimeout.TotalSeconds}s");
            }

           return ssl;
        }

        /// <summary>
        /// Get X509 certificate from the certificate store.
        /// </summary>
        /// <param name="certificateName">Certificate name.</param>
        /// <returns>Certificate with the specified name.</returns>
        private static X509Certificate GetX509Certificate(string certificateName)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, false);
            store.Dispose();

            if (certs.Count == 0)
            {
                throw new DicomNetworkException("Unable to find certificate for " + certificateName);
            }

            return certs[0];
        }

    }
}
