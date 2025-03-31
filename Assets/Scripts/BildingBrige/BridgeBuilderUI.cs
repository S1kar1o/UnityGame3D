using UnityEngine;
using UnityEngine.UI;

public class BridgeBuilderUI : MonoBehaviour
{
    public Button bridgeButton;
    public BridgePlacer bridgePlacer;

    void Start()
    {
        bridgeButton.onClick.AddListener(bridgePlacer.StartPlacingBridge);
    }
}
