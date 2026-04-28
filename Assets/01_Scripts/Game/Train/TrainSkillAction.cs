using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public abstract class TrainSkillAction : TrainSkill
    {
        protected Train owner => Owner;
        protected TrainSkillData trainSkillData;
        protected float lastUseTime;

        public bool CanUse
        {
            get
            {
                return Owner != null
                    && trainSkillData != null
                    && trainSkillData.HasActiveSkill
                    && !Owner.IsDead
                    && !Owner.IsMainTrain
                    && GetRemainingCooldown() <= 0f;
            }
        }

        public virtual void Initialize(Train owner, TrainSkillData trainSkillData)
        {
            Owner = owner;
            this.trainSkillData = trainSkillData;
            lastUseTime = float.NegativeInfinity;
            Subscribe();
        }

        public bool TryUse()
        {
            if (!CanUse)
                return false;

            if (!OnUse())
                return false;

            lastUseTime = Time.time;
            return true;
        }

        public float GetCooldownRatio()
        {
            if (trainSkillData == null || trainSkillData.SkillCooldown <= 0f)
                return 0f;

            return Mathf.Clamp01(GetRemainingCooldown() / trainSkillData.SkillCooldown);
        }

        protected float GetRemainingCooldown()
        {
            if (trainSkillData == null || trainSkillData.SkillCooldown <= 0f)
                return 0f;

            return Mathf.Max(0f, trainSkillData.SkillCooldown - (Time.time - lastUseTime));
        }

        protected abstract bool OnUse();
    }
}
