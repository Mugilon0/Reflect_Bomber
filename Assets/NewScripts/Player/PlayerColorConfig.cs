using UnityEngine;

/// <summary>
/// 1P～4Pのプレイヤーカラーを管理するScriptableObject
/// Assets/Configs/PlayerColorConfig.asset として作成する
/// </summary>
[CreateAssetMenu(fileName = "PlayerColorConfig", menuName = "Config/PlayerColorConfig")]
public class PlayerColorConfig : ScriptableObject
{
    public static PlayerColorConfig Instance { get; private set; }

    [Header("PlayerColor")]
    public Color[] PlayerColors = new Color[4]
    {
        new Color(0.2f, 0.6f, 1.0f),   // 1P: 青
        new Color(1.0f, 0.3f, 0.3f),   // 2P: 赤
        new Color(0.3f, 1.0f, 0.3f),   // 3P: 緑
        new Color(1.0f, 0.9f, 0.2f),   // 4P: 黄
    };

    private void OnEnable()
    {
        Instance = this;
    }

    /// <summary>
    /// インデックスからプレイヤーカラーを取得する
    /// インデックスが範囲外の場合はwhiteを返す
    /// </summary>
    public Color GetColor(int index)
    {
        if (index >= 0 && index < PlayerColors.Length)
            return PlayerColors[index];

        Debug.LogWarning($"PlayerColorConfig: index {index} is out of range");
        return Color.white;
    }
}