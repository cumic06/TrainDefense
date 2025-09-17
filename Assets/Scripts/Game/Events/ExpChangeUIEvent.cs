namespace TrainDefense.Game.Events
{
    public class ExpChangeUIEvent 
    {
        private int currentExp;
        private int maxExp;

        public int CurrentExp => currentExp;
        public int MaxExp => maxExp;
        public float CurrentExpRatio => currentExp / (float)maxExp;

        public ExpChangeUIEvent(int currentExp, int maxExp)
        {
            this.currentExp = currentExp;
            this.maxExp = maxExp;
        }
    }
}