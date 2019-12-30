using System.Collections;

namespace RTSLockstep.Abilities.Essential
{
    public class Experience : Ability
    {

        public int experience;
        public int level;

        private const int xpMax = 300;

        public void AddExperience(int xp)
        {
            experience += xp;
            if (experience > xpMax) experience = xpMax;


            if (experience >= 100 && level == 1)
            {
                StartCoroutine(LevelUpSequence());
            }
            else if (experience >= 300 && level == 2)
            {
                StartCoroutine(LevelUpSequence());
            }
        }

        private void Start()
        {
            experience = 0;
            level = 1;
        }

        private IEnumerator LevelUpSequence()
        {
            level++;
            yield return null;
            //  GetComponent<Unit>().UpdateAttributesForLevel(level);
        }

        //    public virtual void UpdateAttributesForLevel(int level) { }

        //public override void UpdateAttributesForLevel(int level)
        //{
        //    switch (level)
        //    {
        //        case 2:
        //            speedMultiplier = 1.5f;
        //            turnSpeedMultiplier = 3f;
        //            break;
        //        case 3:
        //            speedMultiplier = 2f;
        //            turnSpeedMultiplier = 4.5f;
        //            break;
        //    }
        //}

        public int GetNextLevelXP()
        {
            switch (level)
            {
                case 1:
                    return 100;
                case 2:
                    return 300;
                case 3:
                    return 300;
                default:
                    return 9999;
            }
        }
    }
}