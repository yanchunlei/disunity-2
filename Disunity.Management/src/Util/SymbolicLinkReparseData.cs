using System.Runtime.InteropServices;


namespace Disunity.Management.Util {

    /// 
    /// Refer to http://msdn.microsoft.com/en-us/library/windows/hardware/ff552012%28v=vs.85%29.aspx
    /// 
    [StructLayout(LayoutKind.Sequential)]
    public struct SymbolicLinkReparseData
    {
        // Not certain about this!
        private const int maxUnicodePathLength = 260 * 2;

        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = maxUnicodePathLength)]
        public byte[] PathBuffer;
    }

}