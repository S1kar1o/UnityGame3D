
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BildingButtonManager : MonoBehaviour
{




    public int costOfTreeBrige, costOfRockBrige, costOfGoldBrige, costOfTreeWall, costOfRockWall, costOfGoldWall;
    public TextMeshProUGUI textCostOfTreeBrige, textCostOfRockBrige, textCostOfGoldBrige, textCostOfTreeWall, textCostOfRockWall, textCostOfGoldWall;
    private Coroutine resourceCheckCoroutine;

    private void OnEnable()
    {
        resourceCheckCoroutine = StartCoroutine(UpdateResourceColorsRoutine());
    }

    private void OnDisable()
    {
        if (resourceCheckCoroutine != null)
            StopCoroutine(resourceCheckCoroutine);
    }

    private IEnumerator UpdateResourceColorsRoutine()
    {
        while (true)
        {
            UpdateResourceColors();
            yield return new WaitForSeconds(0.3f);
        }
    }
    private void UpdateResourceColors()
    {
        UpdateTextColor(textCostOfGoldWall, UnityTcpClient.Instance.goldAmount, costOfGoldWall);
        UpdateTextColor(textCostOfRockWall, UnityTcpClient.Instance.rockAmount, costOfRockWall);
        UpdateTextColor(textCostOfTreeWall, UnityTcpClient.Instance.woodAmount, costOfTreeWall);
        UpdateTextColor(textCostOfGoldBrige, UnityTcpClient.Instance.goldAmount, costOfGoldBrige);
        UpdateTextColor(textCostOfRockBrige, UnityTcpClient.Instance.rockAmount, costOfRockBrige);
        UpdateTextColor(textCostOfTreeBrige, UnityTcpClient.Instance.woodAmount, costOfTreeBrige);
    }

    private void UpdateTextColor(TMP_Text text, int currentAmount, int cost)
    {
        text.color = currentAmount >= cost ? Color.green : Color.red;
    }



    public void BrigeGenerator()
    {

        BridgePlacer bridgePlacer = FindObjectOfType<BridgePlacer>();
        bridgePlacer.StartPlacingBridge(costOfTreeBrige, costOfRockBrige, costOfGoldBrige);
    }

    public void WallGenerator()
    {
        WallGenerator wallGenerator = FindObjectOfType<WallGenerator>();
        wallGenerator.StartPlacingWall(costOfTreeWall, costOfRockWall, costOfGoldWall);
    }
}
