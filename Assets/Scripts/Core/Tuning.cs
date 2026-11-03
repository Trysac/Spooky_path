namespace SpookyPath.Core
{
    /// <summary>Every gameplay number in one place, so balance changes stay in one file.</summary>
    public static class Tuning
    {
        // Grid
        public const float CellSize = 4f;
        /// <summary>Signs stand this far towards the row above the centre of their cell.</summary>
        public const float SignOffset = 1.3f;
        /// <summary>A sign's text shows while the explorer is within this ground distance of it.</summary>
        public const float SignReadRadius = 5.5f;

        // Explorer
        public const float MaxHealth = 100f;
        public const float MaxMana = 100f;
        public const float MaxStamina = 100f;
        public const float WalkSpeed = 5.5f;
        public const float SprintSpeed = 9f;
        public const float JumpSpeed = 8.5f;
        public const float Gravity = 24f;
        public const int MaxJumps = 2;
        public const float HurtGrace = 0.6f;
        public const float ManaRegen = 4f;
        public const float StaminaDrain = 30f;
        public const float StaminaRegen = 22f;
        public const float StaminaRegenDelay = 0.6f;
        /// <summary>Once empty, stamina must refill to this fraction before sprinting again.</summary>
        public const float StaminaRecoverThreshold = 0.25f;
        public const float PotionAmount = 40f;
        public const float ElixirBonus = 20f;
        public const float FallDeathDepth = -8f;

        // Staff strike
        public const float StrikeDamage = 25f;
        public const float StrikeRange = 2.4f;
        public const float StrikeArcDegrees = 110f;
        public const float StrikeCooldown = 0.5f;

        // Scoring
        public const int CoinScore = 10;
        public const int CandyScore = 25;
        public const int RelicScore = 250;
        public const int ArtefactScore = 2000;
        /// <summary>Seconds under the par time are worth this many points each.</summary>
        public const int ParSecondScore = 10;
    }
}
