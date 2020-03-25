using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Masa.Collection
{
    public class BlobList<T>
        where T : struct
    {
        private class Blob
        {
            public int StartIndex { get; }
            public T[] Nodes { get; }

            internal Blob(int startIndex, int size)
            {
                this.StartIndex = startIndex;
                this.Nodes = new T[size];
            }
        }
        private readonly int _BlobSize;
        private readonly object _BlobCreateLock = new object();
        
        private Blob[] _Blobs;

        private int _LastIndex;
        private readonly ConcurrentStack<int> _FreeAddress = new ConcurrentStack<int>();

        public BlobList(int blobSize)
        {
            _BlobSize = blobSize;
            CreateBlob(0);
        }

        private Blob CreateBlob(int blobIndex)
        {
            lock (_BlobCreateLock)
            {
                var lastBlobs = _Blobs ?? Array.Empty<Blob>();
                if (lastBlobs.Length > blobIndex)
                {
                    return lastBlobs[blobIndex];
                }
                var newBlobs = new Blob[lastBlobs.Length + 1];
                Array.Copy(lastBlobs, newBlobs, lastBlobs.Length);
                
                newBlobs[lastBlobs.Length] = new Blob(lastBlobs.Length * _BlobSize, _BlobSize);
                _Blobs = newBlobs;
                return newBlobs[lastBlobs.Length];
            }
        }

        public T Read(int address)
        {
            var blob = _Blobs[address / _BlobSize];
            return blob.Nodes[address - blob.StartIndex];
        }

        public int Allocate()
        {
            if (_FreeAddress.Count > 100 && _FreeAddress.TryPop(out var address))
            {
                return address;
            }

            address = Interlocked.Increment(ref _LastIndex);
            var blobIndex = address / _BlobSize;
            if (blobIndex >= _Blobs.Length)
            {
                CreateBlob(blobIndex);
            }
            return address;
        }

        public void Write(int address, in T value)
        {
            var blob = _Blobs[address / _BlobSize];
            blob.Nodes[address - blob.StartIndex] = value;
        }

        public void Free(int address)
        {
            _FreeAddress.Push(address);
        }
    }
}