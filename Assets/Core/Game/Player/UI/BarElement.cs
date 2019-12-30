using UnityEngine;
using UnityEngine.UI;

namespace RTSLockstep.Player.UI
{
    public class BarElement : MonoBehaviour
    {
        [SerializeField]
        private Image _fillImage;
        public Image FillImage { get { return _fillImage; } }

        public void SetFill(float amount)
        {
            FillImage.fillAmount = amount;
        }
    }
}
