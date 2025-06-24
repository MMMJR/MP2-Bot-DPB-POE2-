using DreamPoeBot.Common;

namespace MP2.Helpers
{
    public static class MoveHelper
    {
        public static bool SafePath(Vector2i destination, int combatRange)
        {
            return MiscHelpers.NumberOfMobsNearPosition(destination, combatRange) <= 0;
        }
        
        public static int GetSkillMinRange(string skillName)
        {
            switch (skillName)
            {
                default:
                    return 1;
            }
        }
        public static int GetSkillMaxRange(string skillName)
        {
            switch (skillName)
            {
                default:
                    return 70;
            }
        }
    }
}
