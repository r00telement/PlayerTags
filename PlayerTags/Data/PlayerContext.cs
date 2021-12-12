namespace PlayerTags.Data
{
    public enum PlayerContext
    {
        None = 0x0,
        Self = 0x1,
        Party = 0x2,
        Alliance = 0x4,
        Enemy = 0x8,
        Friend = 0x10
    }
}
