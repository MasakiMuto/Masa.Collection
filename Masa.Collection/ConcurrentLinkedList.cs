using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Masa.Collection
{
    public class ConcurrentLinkedList<T> : IEnumerable<T>
    {
        private struct Node
        {
            public int NextAddress;
            public readonly T Value;

            public Node(int nextAddress, in T value)
            {
                NextAddress = nextAddress;
                Value = value;
            }
        }

        private readonly BlobList<Node> _Blob;
        private readonly int _HeadAddress;

        public ConcurrentLinkedList()
        {
            _Blob = new BlobList<Node>(1024);
            _HeadAddress = _Blob.Allocate();
        }

        public int AddAfter(int prevNodeAddress, in T value)
        {
            var newNode = _Blob.Allocate();
            ref var prevNode = ref _Blob.Read(prevNodeAddress);
            int lastAddress;
            do
            {
                lastAddress = prevNode.NextAddress;
                _Blob.Write(newNode, new Node(lastAddress, value));
            } while (Interlocked.CompareExchange(ref prevNode.NextAddress, newNode, lastAddress) != lastAddress);
            return newNode;
        }

        public int AddFirst(in T value)
        {
            return AddAfter(_HeadAddress, value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private struct Enumerator : IEnumerator<T>
        {
            private readonly ConcurrentLinkedList<T> _List;
            
            private int _CurrentAddress;

            public Enumerator(ConcurrentLinkedList<T> list)
            {
                _List = list;
                _Current = default;
                _CurrentAddress = list._HeadAddress;
                MoveNext();
            }
            
            public bool MoveNext()
            {
                if (_CurrentAddress == 0)
                {
                    return false;
                }
                var node = _List._Blob.Read(_CurrentAddress);
                
                _Current = node.Value;
                _CurrentAddress = node.NextAddress;
                return true;
            }

            public void Reset()
            {
                _CurrentAddress = _List._HeadAddress;
                MoveNext();
            }

            private T _Current;
            public T Current => _Current;

            object IEnumerator.Current => _Current;

            public void Dispose()
            {
            }
        }
    }
}