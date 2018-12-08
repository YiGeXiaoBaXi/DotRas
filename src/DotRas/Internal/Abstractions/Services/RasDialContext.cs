﻿using System;
using System.Net;
using System.Threading;

namespace DotRas.Internal.Abstractions.Services
{
    internal class RasDialContext
    {
        public string PhoneBookPath { get; }
        public string EntryName { get; }
        public NetworkCredential Credentials { get; }
        public CancellationToken CancellationToken { get; }
        public Action<DialerStateChangedEventArgs> OnStateChangedCallback { get; }

        public RasDialContext(string phoneBookPath, string entryName, NetworkCredential credentials, CancellationToken cancellationToken, Action<DialerStateChangedEventArgs> onStateChangedCallback)
        {
            PhoneBookPath = phoneBookPath;
            EntryName = entryName;
            Credentials = credentials;
            CancellationToken = cancellationToken;
            OnStateChangedCallback = onStateChangedCallback;
        }
    }
}