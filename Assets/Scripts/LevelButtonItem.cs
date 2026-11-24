using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButtonItem : MonoBehaviour
{
    [Header("Ссылки на UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Превью уровня (скрывается, если у уровня нет previewIcon).")]
    [SerializeField] private Image previewImage;
    [Tooltip("Иконка замка: показывается только для закрытых уровней.")]
    [SerializeField] private GameObject lockIcon;

    [Header("Тексты статуса")]
    [SerializeField] private string lockedText = "Закрыт";
    [SerializeField] private string completedText = "Пройден";
    [SerializeField] private string notCompletedText = "Не пройден";

    [Header("Цвета")]
    [SerializeField] private Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color completedColor = new Color(0.45f, 0.9f, 0.45f, 1f);
    [SerializeField] private Color notCompletedColor = Color.white;
    [Tooltip("Затемнение превью закрытого уровня.")]
    [SerializeField] private Color lockedPreviewTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    private Action<int> clickCallback;
    private int levelIndex;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Setup(LevelData data, int index, bool unlocked, bool completed, Action<int> onClick)
    {
        levelIndex = index;
        clickCallback = onClick;

        button.interactable = unlocked;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(data.displayName)
                ? $"Уровень {index + 1}"
                : data.displayName;
        }

        if (statusText != null)
        {
            if (!unlocked)
            {
                statusText.text = lockedText;
                statusText.color = lockedColor;
            }
            else if (completed)
            {
                statusText.text = completedText;
                statusText.color = completedColor;
            }
            else
            {
                statusText.text = notCompletedText;
                statusText.color = notCompletedColor;
            }
        }

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        if (previewImage != null)
        {
            bool hasPreview = data.previewIcon != null;
            previewImage.gameObject.SetActive(hasPreview);

            if (hasPreview)
            {
                previewImage.sprite = data.previewIcon;
                previewImage.color = unlocked ? Color.white : lockedPreviewTint;
            }
        }
    }

    private void HandleClick() => clickCallback?.Invoke(levelIndex);
}