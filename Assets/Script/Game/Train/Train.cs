using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Train : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private float moveSpeed;
        [SerializeField]
        private int maxTrainCount;
        #endregion

        private readonly List<ITrainable> _trainables = new();

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        private void AddTrain(ITrainable trainable)
        {
            if (_trainables.Count >= maxTrainCount) return;

            _trainables.Add(trainable);
        }
    }
}