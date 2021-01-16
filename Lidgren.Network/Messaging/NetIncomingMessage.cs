﻿using System.Net;
using System.Diagnostics;
using System;
using System.Buffers;

namespace Lidgren.Network
{
    /// <summary>
    /// Incoming message either sent from a remote peer or generated within the library.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class NetIncomingMessage : NetBuffer
    {
        internal NetMessageType _baseMessageType;

        internal string DebuggerDisplay => $"Type = {MessageType}, BitLength = {BitLength}";

        /// <summary>
        /// Gets the type of this incoming message.
        /// </summary>
        public NetIncomingMessageType MessageType { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> of the sender, if any.
        /// </summary>
        public IPEndPoint? SenderEndPoint { get; internal set; }

        /// <summary>
        /// Gets the <see cref="NetConnection"/> of the sender, if any.
        /// </summary>
        public NetConnection? SenderConnection { get; internal set; }

        /// <summary>
        /// Gets at what local time the message was received from the network.
        /// </summary>
        public TimeSpan ReceiveTime { get; internal set; }

        public bool IsFragment { get; internal set; }

        public int SequenceNumber { get; internal set; }

        /// <summary>
        /// Gets the delivery method this message was sent with (if user data).
        /// </summary>
        public NetDeliveryMethod DeliveryMethod => NetUtility.GetDeliveryMethod(_baseMessageType);

        /// <summary>
        /// Gets the sequence channel this message was sent with (if user data).
        /// </summary>
        public int SequenceChannel => (int)_baseMessageType - (int)DeliveryMethod;

        public NetIncomingMessage(ArrayPool<byte> storagePool) : base(storagePool)
        {
        }

        internal void Reset()
        {
            _baseMessageType = NetMessageType.LibraryError;
            MessageType = NetIncomingMessageType.Error;
            BitLength = 0;
            SenderConnection = null;
            SenderEndPoint = null;
            IsFragment = false;
            SequenceNumber = 0;
        }

        // TODO: make Decrypt() and ReadLocalTime() into extension methods

        /// <summary>
        /// Try to decrypt the message with the specified encryption algorithm.
        /// </summary>
        /// <param name="encryption">The encryption algorithm used to encrypt the message.</param>
        /// <returns>Whether the decryption succeeded.</returns>
        public bool Decrypt(NetEncryption encryption)
        {
            if (encryption == null)
                throw new ArgumentNullException(nameof(encryption));

            return encryption.Decrypt(this);
        }

        /// <summary>
        /// Reads local time comparable to <see cref="NetTime.Now"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="SenderConnection"/> is null.</exception>
        public TimeSpan ReadLocalTime()
        {
            if (SenderConnection == null)
            {
                throw new InvalidOperationException(
                    "This message is not associated with a sender connection.");
            }
            return this.ReadLocalTime(SenderConnection);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this object.
        /// </summary>
        public override string ToString()
        {
            return "{NetIncomingMessage: #" + SequenceNumber + ", " + ByteLength + " bytes}";
        }
    }
}
