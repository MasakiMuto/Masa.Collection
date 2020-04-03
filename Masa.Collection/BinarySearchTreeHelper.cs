namespace Masa.Collection
{
    public static class BinarySearchTreeHelper
    {
        private const int NullBit = (0b10 << 30);
        private const int LockBit = (0b01 << 30);
        private const int MaskBits = (0b11 << 30);
        
        public static int GetAddress(int rawValue)
        {
            unchecked
            {
                return rawValue & (~MaskBits);
            }
        }

        public static bool IsNull(int rawValue)
        {
            unchecked
            {
                return (rawValue & NullBit) != 0;
            }
        }

        public static bool IsLocked(int rawValue)
        {
            unchecked
            {
                return (rawValue & LockBit) != 0;
            }
        }

        public static int SetLock(int rawValue)
        {
            unchecked
            {
                return rawValue | LockBit;
            }
        }

        public static int SetUnlock(int rawValue)
        {
            unchecked
            {
                return rawValue & (~LockBit);
            }
        }

        public static int SetNull(int rawValue)
        {
            unchecked
            {
                return rawValue | NullBit;
            }
        }
    }
}