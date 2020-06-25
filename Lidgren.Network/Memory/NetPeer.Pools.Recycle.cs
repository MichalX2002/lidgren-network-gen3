﻿using System;
using System.Collections.Generic;

namespace Lidgren.Network
{
    public partial class NetPeer
    {
        internal void Recycle(byte[] storage)
        {
            if (_storagePool == null || storage == null)
                return;

            lock (_storagePool)
            {
                _bytesInPool += storage.Length;
                int cnt = _storagePool.Count;
                for (int i = 0; i < cnt; i++)
                {
                    if (_storagePool[i] == null)
                    {
                        _storagePool[i] = storage;
                        return;
                    }
                }
                _storagePool.Add(storage);
            }
        }

        /// <summary>
        /// Recycles a message for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(NetIncomingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (_incomingMessagePool == null)
                return;

            LidgrenException.Assert(
                !_incomingMessagePool.Contains(message), "Recyling already recycled message! Thread race?");

            byte[] storage = message.Data;
            message.Data = Array.Empty<byte>();
            Recycle(storage);
            message.Reset();
            _incomingMessagePool.Enqueue(message);
        }

        /// <summary>
        /// Recycles a list of messages for reuse.
        /// </summary>
        public void Recycle(IEnumerable<NetIncomingMessage> toRecycle)
        {
            if (toRecycle == null)
                throw new ArgumentNullException(nameof(toRecycle));

            if (_incomingMessagePool == null)
                return;

            // first recycle the storage of each message
            if (_storagePool != null)
            {
                lock (_storagePool)
                {
                    foreach (var msg in toRecycle)
                    {
                        var storage = msg.Data;
                        msg.Data = Array.Empty<byte>();
                        _bytesInPool += storage.Length;
                        for (int i = 0; i < _storagePool.Count; i++)
                        {
                            if (_storagePool[i] == null)
                            {
                                _storagePool[i] = storage;
                                return;
                            }
                        }
                        msg.Reset();
                        _storagePool.Add(storage);
                    }
                }
            }

            // then recycle the message objects
            _incomingMessagePool.Enqueue(toRecycle);
        }

        internal void Recycle(NetOutgoingMessage msg)
        {
            if (_outgoingMessagePool == null)
                return;

            LidgrenException.Assert(
                !_outgoingMessagePool.Contains(msg), "Recyling already recycled message! Thread race?");

            byte[] storage = msg.Data;
            msg.Data = Array.Empty<byte>();

            // message fragments cannot be recycled
            // TODO: find a way to recycle large message after all fragments has been acknowledged;
            //       or? possibly better just to garbage collect them
            if (msg._fragmentGroup == 0)
                Recycle(storage);

            msg.Reset();
            _outgoingMessagePool.Enqueue(msg);
        }
    }
}
