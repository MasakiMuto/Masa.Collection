using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static Masa.Collection.BinarySearchTreeHelper;
using Ptr = System.Int32;
// ReSharper disable BuiltInTypeReferenceStyle

namespace Masa.Collection
{
    public class BinarySearchTree<T> : IReadOnlyCollection<T>
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

        public int Count => _BlobList.LivingCount - 2; //_S, _Root

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

        public bool Remove(T key)
        {
            Ptr temp;
            
            while (true)
            {
                Seek(key, out var parent, out var node, out _);
                ref var nNode = ref _BlobList.Read(node);
                var nKey = nNode.Key;
                if (key.CompareTo(nKey) != 0)
                {
                    return false;
                }

                ref var pNode = ref _BlobList.Read(parent);
                var pKey = pNode.Key;
                temp = nNode.Left;
                var lChild = GetAddress(temp);
                var ln = IsNull(temp);
                temp = nNode.Right;
                var rChild = GetAddress(temp);
                var rn = IsNull(temp);
                var pWhich = (parent == _S || nKey.CompareTo(pKey) < 0) ? LEFT : RIGHT;

                if (ln || rn) //simple delete
                {
                    if (LockEdge(parent, node, pWhich, false))
                    {
                        if (LockEdge(node, lChild, LEFT, ln))
                        {
                            if (LockEdge(node, rChild, RIGHT, rn))
                            {
                                if (nKey.CompareTo(nNode.Key) != 0)
                                {
                                    UnlockEdge(parent, pWhich);
                                    UnlockEdge(node, LEFT);
                                    UnlockEdge(node, RIGHT);
                                    continue;
                                }

                                if (ln && rn)
                                {
                                    pNode.Set(pWhich, SetNull(node));
                                }
                                else if (ln)
                                {
                                    pNode.Set(pWhich, rChild);
                                }
                                else
                                {
                                    pNode.Set(pWhich, lChild);
                                }
                                _BlobList.Free(node);

                                return true;
                            }
                            else
                            {
                                UnlockEdge(parent, pWhich);
                                UnlockEdge(node, LEFT);
                            }
                        }
                        else
                        {
                            UnlockEdge(parent, pWhich);
                        }
                    }
                    continue;
                }
                
                //complex delete
                var isSplCase = FindSmallest(node, rChild, out var succNode, out var succParent);
                var isSplCaseN = isSplCase ? 1 : 0;
                ref var sNode = ref _BlobList.Read(succNode);
                var succNodeLChild = GetAddress(sNode.Left);
                temp = sNode.Right;
                var srn = IsNull(temp);
                var succNodeRChild = GetAddress(temp);
                if (!isSplCase)
                {
                    if (!LockEdge(node, rChild, RIGHT, false))
                    {
                        continue;
                    }
                }

                if (LockEdge(succParent, succNode, isSplCaseN, false))
                {
                    if (LockEdge(succNode, succNodeLChild, LEFT, true))
                    {
                        if (LockEdge(succNode, succNodeRChild, RIGHT, srn))
                        {
                            if (nKey.CompareTo(nNode.Key) != 0)
                            {
                                if (!isSplCase)
                                {
                                    UnlockEdge(node, RIGHT);
                                }
                                UnlockEdge(succParent, isSplCaseN);
                                UnlockEdge(succNode, LEFT);
                                UnlockEdge(succNode, RIGHT);
                                continue;
                            }

                            nNode.Key = sNode.Key;
                            ref var spNode = ref _BlobList.Read(succParent);
                            if (srn)
                            {
                                spNode.Set(isSplCaseN, SetNull(succNode));
                            }
                            else
                            {
                                spNode.Set(isSplCaseN, succNodeRChild);
                            }

                            if (!isSplCase)
                            {
                                UnlockEdge(node, RIGHT);
                            }
                            _BlobList.Free(node);

                            return true;
                        }
                        else
                        {
                            UnlockEdge(succParent, isSplCaseN);
                            UnlockEdge(succNode, LEFT);
                        }
                    }
                    else
                    {
                        UnlockEdge(succNode, isSplCaseN);
                    }
                }

                if (!isSplCase)
                {
                    UnlockEdge(node, RIGHT);
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

        private bool FindSmallest(Ptr node, Ptr rChild, out Ptr found, out Ptr foundParent)
        {
            var prev = node;
            var curr = rChild;
            while (true)
            {
                var temp = _BlobList.Read(curr).Left;
                if (IsNull(temp))
                {
                    break;
                }

                prev = curr;
                curr = GetAddress(temp);
            }

            found = curr;
            foundParent = prev;
            return prev == node;
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

        public IEnumerator<T> GetEnumerator()
        {
            //the results may contain duplication
            
            var stack = new Stack<int>();
            var sNode = _BlobList.Read(_S);
            var l = sNode.Left;
            if (IsNull(l))
            {
                yield break;
            }

            stack.Push(GetAddress(l));
            while (stack.TryPop(out var current))
            {
                var node = _BlobList.Read(current);
                yield return node.Key;
                var left = node.Left;
                var right = node.Right;
                if (!IsNull(left))
                {
                    stack.Push(GetAddress(left));
                }

                if (!IsNull(right))
                {
                    stack.Push(GetAddress(right));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}