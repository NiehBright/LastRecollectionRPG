namespace RPG.Combat
{
    public enum ElementType
    {
        Fire,
        Ice,
        Lightning,
        Nature,
        Physical,
        Ether
    }

    public enum SkillType
    {
        BASIC,
        SPECIAL,
        ULTIMATE
    }

    public enum TargetType
    {
        SINGLE,
        AOE,
        SELF,
        ALL_ALLIES,
        ALL_ENEMIES
    }

    public enum CharacterRole
    {
        BASTION,
        VANGUARD,
        ECHO,
        WARDEN,
        PHANTOM
    }

    public enum EffectType
    {
        DAMAGE_OVER_TIME,
        SPEED_CHANGE,
        ATK_BUFF,
        DEF_BUFF,
        FREEZE,
        STUN,
        BURN,
        POISON,
        SHIELD,
        LIFESTEAL,
        REFLECT,
        COMBO_TRIGGER
    }

    public enum SkillRangeType
    {
        MELEE,
        RANGED
    }

    public enum RangedVfxType
    {
        SPAWN_AT_TARGET,
        PROJECTILE
    }
}
