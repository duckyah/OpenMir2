namespace SelGate.Package
{
    public struct TMessageData
    {
        public int SessionId;

        public byte[] Body;
        public int MsgLen => Body.Length;
    }
}