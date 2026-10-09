namespace EvTrainer.Web.Domain.Model;

/// <summary>Game limits and helpers for 6-stat vectors [HP, Ataque, Defesa, SpAtk, SpDef, Speed].</summary>
public static class StatVector
{
    public const int Count = 6;

    public const int Hp = 0;

    public const int MaxEvPerStat = 255;

    public const int MaxEvTotal = 510;

    public const int VitaminCap = 100;

    public const int MaxIv = 31;

    extension(int[] vector)
    {
        /// <summary>Copy resized to exactly 6 positions (padding with 0, truncating extras).</summary>
        public int[] ToSix()
        {
            var result = new int[Count];
            Array.Copy(vector, result, Math.Min(vector.Length, Count));
            return result;
        }
    }
}