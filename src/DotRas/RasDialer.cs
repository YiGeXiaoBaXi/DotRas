﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotRas.Internal.Abstractions.Primitives;
using DotRas.Internal.Abstractions.Services;
using DotRas.Internal.DependencyInjection;

namespace DotRas
{
    /// <summary>
    /// Provides a mechanism to dial a connection.
    /// </summary>
    public sealed class RasDialer : DisposableObject
    {
        #region Fields and Properties

        private readonly IRasDial api;
        private readonly IFileSystem fileSystem;
        private readonly IPhoneBookEntryValidator validator;

        /// <summary>
        /// Gets a value indicating whether this instance is currently dialing a connection.
        /// </summary>
        public bool IsBusy => api.IsBusy;

        /// <summary>
        /// Gets or sets the name of the entry within the phone book.
        /// </summary>
        public string EntryName { get; set; }

        /// <summary>
        /// Gets the full path (including filename) of the phone book containing the entry.
        /// </summary>
        public string PhoneBookPath { get; set; }

        /// <summary>
        /// Gets or sets the credentials to use while dialing the connection.
        /// </summary>
        public NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Gets the options configurable for a dial attempt.
        /// </summary>
        public RasDialerOptions Options { get; } = new RasDialerOptions();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the state changes while dialing a connection.
        /// </summary>
        /// <remarks>
        /// Please note, this event is only raised while a connection is being dialed. It will not be raised if
        /// an active connection has been disconnected outside of an attempt to dial.
        /// </remarks>
        public event EventHandler<DialStateChangedEventArgs> DialStateChanged;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="RasDialer"/> class.
        /// </summary>
        public RasDialer() : this(
                Container.Default.GetRequiredService<IRasDial>(), 
                Container.Default.GetRequiredService<IFileSystem>(), 
                Container.Default.GetRequiredService<IPhoneBookEntryValidator>())
        {
        }

        internal RasDialer(IRasDial api, IFileSystem fileSystem, IPhoneBookEntryValidator validator)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Dials the connection.
        /// </summary>
        /// <returns>The connection instance.</returns>
        public RasConnection Dial()
        {
            GuardMustNotBeDisposed();
            ValidateConfigurationPriorToDialAttempt();

            return Dial(CancellationToken.None);
        }

        /// <summary>
        /// Dials the connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests while dialing the connection.</param>
        /// <returns>The connection instance.</returns>
        public RasConnection Dial(CancellationToken cancellationToken)
        {
            GuardMustNotBeDisposed();
            ValidateConfigurationPriorToDialAttempt();

            using (var task = DialAsync(cancellationToken))
            {
                return task.Result;
            }
        }

        /// <summary>
        /// Dials the connection asynchronously.
        /// </summary>
        /// <returns>The connection instance.</returns>
        public Task<RasConnection> DialAsync()
        {
            GuardMustNotBeDisposed();
            ValidateConfigurationPriorToDialAttempt();

            return DialAsync(CancellationToken.None);
        }

        /// <summary>
        /// Dials the connection asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests while dialing the connection.</param>
        /// <returns>The connection instance.</returns>
        public Task<RasConnection> DialAsync(CancellationToken cancellationToken)
        {
            GuardMustNotBeDisposed();
            ValidateConfigurationPriorToDialAttempt();

            return api.DialAsync(new RasDialContext
            {
                PhoneBookPath = PhoneBookPath,
                EntryName = EntryName,
                Credentials = Credentials,
                CancellationToken = cancellationToken,
                Options = Options,
                OnStateChangedCallback = RaiseDialStateChanged
            });
        }        

        private void ValidateConfigurationPriorToDialAttempt()
        {
            if (!string.IsNullOrWhiteSpace(PhoneBookPath) && !fileSystem.VerifyFileExists(PhoneBookPath))
            {
                throw new FileNotFoundException("The file does not exist.", PhoneBookPath);
            }

            if (string.IsNullOrWhiteSpace(EntryName) || !validator.VerifyEntryExists(EntryName, PhoneBookPath))
            {
                throw new RasEntryNotFoundException($"The entry does not exist within the phone book specified.", EntryName, PhoneBookPath);
            }
        }

        private void RaiseDialStateChanged(DialStateChangedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            DialStateChanged?.Invoke(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                api.DisposeIfNecessary();
            }

            base.Dispose(disposing);
        }
    }
}