using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 按鈕 hover 顯示背景圖。
    /// 只負責顯隱，點擊音效由 TitleUI 統一處理。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TitleButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("按鈕的背景 Image，hover 才顯示")]
        [SerializeField] private Image backgroundImage;

        /// <summary>
        /// 程式碼初始化（由 TitleUI 呼叫）。
        /// </summary>
        public void Init(Image bgImage)
        {
            backgroundImage = bgImage;

            if (backgroundImage != null)
                SetBackgroundVisible(false);
        }

        private void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (backgroundImage != null)
                SetBackgroundVisible(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetBackgroundVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetBackgroundVisible(false);
        }

        private void SetBackgroundVisible(bool visible)
        {
            if (backgroundImage == null) return;

            var color = backgroundImage.color;
            color.a = visible ? 1f : 0f;
            backgroundImage.color = color;
        }
    }
}
