namespace Common
{
    public struct StylusUpdateMessage
    {
        const int TOUCHED_MASK = 0b00000001;

        const int ALTERNATE_MASK = 0b00000010;

        const int INVERTED_MASK = 0b00000100;

        public double PositionX;

        public double PositionY;

        byte _statusFlags;

        public const int BUFFER_SIZE = 2 * sizeof(double) + 1;

        public bool Touched
        {
            get => (_statusFlags & TOUCHED_MASK) > 0;
            set => _statusFlags = (byte)(value ? _statusFlags | TOUCHED_MASK : _statusFlags & ~TOUCHED_MASK);
        }

        public bool Alternate
        {
            get => (_statusFlags & ALTERNATE_MASK) > 0;
            set => _statusFlags = (byte)(value ? _statusFlags | ALTERNATE_MASK : _statusFlags & ~ALTERNATE_MASK);
        }

        public bool Inverted
        {
            get => (_statusFlags & INVERTED_MASK) > 0;
            set => _statusFlags = (byte)(value ? _statusFlags | INVERTED_MASK : _statusFlags & ~INVERTED_MASK);
        }

        public StylusUpdateMessage()
        { }

        public StylusUpdateMessage(byte[] data, int offset = 0)
        {
            DecodeNewData(data, offset);
        }

        public void DecodeNewData(byte[] data, int offset = 0)
        {
            if (data.Length < offset + BUFFER_SIZE)
                throw new ArgumentException("The array supplied is missing data.", nameof(data));
            PositionX = BitConverter.ToDouble(data, offset);
            PositionX = BitConverter.ToDouble(data, offset + sizeof(double));
            _statusFlags = data[offset + 2 * sizeof(double)];
        }

        public byte[] Encode(byte[] buffer, int offset = 0)
        {
            if (buffer.Length < offset + BUFFER_SIZE)
                throw new ArgumentException("The buffer is too small.", nameof(buffer));
            Array.Copy(BitConverter.GetBytes(PositionX), 0, buffer, offset, sizeof(double));
            Array.Copy(BitConverter.GetBytes(PositionY), 0, buffer, offset + sizeof(double), sizeof(double));
            buffer[2 * sizeof(double)] = _statusFlags;
            return buffer;
        }
    }
}
