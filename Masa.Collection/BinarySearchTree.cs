using System;
using System.Buffers;
using System.Threading;
using static Masa.Collection.BinarySearchTreeHelper;
using Ptr = System.Int32;
// ReSharper disable BuiltInTypeReferenceStyle

namespace Masa.Collection
{
    public class BinarySearchTree<T>
        where T : IComparable<T>
    {
        private const int LEFT = 0;
        private const int RIGHT = 1;
        
        private struct Node
        {
            public T Key;
            
            public Ptr Left;
            public Ptr Right;

            public Node(T key)
            {
                Key = key;
                Left = SetNull(0);
                Right = SetNull(0);
            }

            public Ptr Get(int i) => i == LEFT ? Left : Right;
            public int Set(int i, Ptr value) => i == LEFT ? Left = value : Right = value;
        }

        private readonly BlobList<Node> _BlobList;

        private readonly Ptr _Root; //minimum
        private readonly Ptr _S; //maximum

        public BinarySearchTree(int blobSize = 256)
        {
            _BlobList = new BlobList<Node>(blobSize);
            _Root = _BlobList.Allocate();
            _S = _BlobList.Allocate();
            ref var root = ref _BlobList.Read(_Root);
            _BlobList.Write(_Root, new Node(default));
            root.Right = _S;
            _BlobList.Write(_S, new Node(default));
        }

        public bool Contains(T key)
        {
            this.Seek(key, out _, out var node, out _);
            var n = _BlobList.Read(node).Key;
            return key.CompareTo(n) == 0;
        }

        public bool Add(T key)
        {
            Ptr newNode = default;
            var created = false;
            
            while (true)
            {
                this.Seek(key, out var parent, out var node, out var address);
                ref var nNode = ref _BlobList.Read(node);
                var compare = key.CompareTo(nNode.Key);
                if (node == _S)
                {
                    compare = -1;
                }
                if (compare == 0)
                {
                    return false;
                }

                if (!created)
                {
                    newNode = _BlobList.Allocate();
                    var tmp = new Node(key);
                    _BlobList.Write(newNode, in tmp);
                    created = true;
                }
                var which = compare > 0 ? RIGHT : LEFT;
                var result = CompareAndSwap(node, which, SetNull(address), newNode);
                if (result)
                {
                    return true;
                }
            }
        }

        private void Seek(T value, out Ptr parent, out Ptr node, out Ptr injectionPoint)
        {
            Span<Ptr> prev = stackalloc Ptr[2];
            Span<Ptr> curr = stackalloc Ptr[2];
            Span<Ptr> lastRNode = stackalloc Ptr[2];
            Span<Ptr> address = stackalloc Ptr[2];
            var lastRKey = ArrayPool<T>.Shared.Rent(2);
            
            var pSeek = 0;
            var cSeek = 1;
            int index;
            
            BEGIN:
            {
                
                prev[cSeek] = _Root;
                curr[cSeek] = _S;
                lastRNode[cSeek] = _Root;
                lastRKey[cSeek] = default;
                address[cSeek] = 0;

                while (true)
                {
                    int which;

                    var cNode = _BlobList.Read(curr[cSeek]);
                    var cKey = cNode.Key;
                    var compare = value.CompareTo(cKey);
                    if (curr[cSeek] == _S)
                    {
                        compare = -1;
                    }
                    if (compare < 0)
                    {
                        which = LEFT;
                    }
                    else if (compare > 0)
                    {
                        which = RIGHT;
                    }
                    else
                    {
                        //key found
                        index = cSeek;
                        goto END;
                    }

                    var temp = cNode.Get(which);
                    address[cSeek] = GetAddress(temp);
                    if (IsNull(temp))
                    {
                        var rNode = _BlobList.Read(lastRNode[cSeek]);
                        if (rNode.Key.CompareTo(lastRKey[cSeek]) != 0)
                        {
                            goto BEGIN;
                        }

                        if (!IsLocked(rNode.Right))
                        {
                            index = cSeek;
                            goto END;
                        }

                        if (lastRNode[cSeek] == lastRNode[pSeek] && lastRKey[cSeek].CompareTo(lastRKey[pSeek]) == 0)
                        {
                            index = pSeek;
                            goto END;
                        }

                        pSeek = 1 - pSeek;
                        cSeek = 1 - cSeek;
                        goto BEGIN;
                    }

                    if (which == RIGHT)
                    {
                        lastRNode[cSeek] = curr[cSeek];
                        lastRKey[cSeek] = cKey;
                    }

                    prev[cSeek] = curr[cSeek];
                    curr[cSeek] = address[cSeek];
                }
            }
            
            END:
            {
                parent = prev[index];
                node = curr[index];
                injectionPoint = address[index];
                ArrayPool<T>.Shared.Return(lastRKey);
            }
        }

        private bool CompareAndSwap(Ptr parent, int which, Ptr oldChild, Ptr newChild)
        {
            ref var pNode = ref _BlobList.Read(parent);
            if (which == LEFT)
            {
                return Interlocked.CompareExchange(ref pNode.Left, newChild, oldChild) == oldChild;
            }

            return Interlocked.CompareExchange(ref pNode.Right, newChild, oldChild) == oldChild;
        }

        private bool LockEdge(Ptr parent, Ptr oldChild, int which, bool isNull)
        {
            ref var pNode = ref _BlobList.Read(parent);
            if (IsLocked(pNode.Get(which)))
            {
                return false;
            }

            var newChild = SetLock(oldChild);
            if (isNull)
            {
                return CompareAndSwap(parent, which, SetNull(oldChild),
                    SetNull(newChild));
            }

            return CompareAndSwap(parent, which, oldChild, newChild);
        }

        private void UnlockEdge(Ptr parent, int which)
        {
            ref var pNode = ref _BlobList.Read(parent);
            pNode.Set(which, SetUnlock(pNode.Get(which)));
        }
    }
}