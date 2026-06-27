using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 포탑 선택창에 노출할 항목 목록. 에디터에서 이 에셋을 만들어(Create > TrainDefense > Turret Select Config)
    /// Resources/Data/TurretSelectConfig 로 두면 선택창이 자동으로 읽는다. 없으면 DB의 기본 터렛 목록을 fallback으로 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "TurretSelectConfig", menuName = "TrainDefense/Turret Select Config")]
    public class TurretSelectConfig : ScriptableObject
    {
        [SerializeField]
        private List<TurretSelectOption> options = new();

        public IReadOnlyList<TurretSelectOption> Options => options;
    }
}
