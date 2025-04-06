using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BildingButtonManager : MonoBehaviour
{

    public void BrigeGenerator()
    {

        BridgePlacer bridgePlacer = FindObjectOfType<BridgePlacer>();
        bridgePlacer.StartPlacingBridge();
    }

    public void WallGenerator()
    {
        WallGenerator wallGenerator = FindObjectOfType<WallGenerator>();
        wallGenerator.StartPlacingWall();
    }
}
