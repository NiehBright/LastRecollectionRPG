namespace RPG.Combat
{
    public enum ElementType
    {
        Fire,
        Ice,
        Lightning,
        Nature,
        Physical
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

    public enum EffectType
    {
        DAMAGE_OVER_TIME,
        SPEED_CHANGE,
        ATK_BUFF,
        DEF_BUFF,
        FREEZE,
        STUN
    }
}
