namespace TrainDefense.Game.Datas
{
    public enum SoundType
    {
        None = 0,

        // BGM
        BGM_Lobby = 100,
        BGM_Stage = 101,

        // SFX - UI
        SFX_UI_ButtonClick = 200,
        SFX_UI_WindowOpen = 201,
        SFX_UI_WindowClose = 202,
        SFX_UI_ShopOpen = 203,
        SFX_UI_TriChoiceSelect = 204,
        SFX_UI_ItemBuy = 205,
        SFX_UI_LevelUp = 206,

        // SFX - Game
        SFX_Game_Hit = 300,
        NormalTurretTrainAttack = 301,
        FireTurretTrainAttack = 302,
        MissileTurretTrainAttack = 303,
        LaserTurretTrainAttack = 304,
        ElectrickTurretTrainAttack = 305,
        CannonTurretTrainAttack = 306,
        SniperTurretTrainAttack = 307,
        ExplosionRangeTrainAttack = 308,
        ColdAirRangeTrainAttack = 309,
        SFX_Game_NewMonster = 310,
        SFX_Game_Heal = 311,
        SFX_Game_LightningStrike = 312,

        CannonTriggerSound = 401,
        MissleTriggerSound = 402,

        SFX_Game_Explosion = 501,
        SFX_Game_Upgrade = 502
    }
}
