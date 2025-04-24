using Unity.AI.Navigation; // ������������� �������� ���� ��� ������ � ���������
using UnityEngine; // �������� ������ ���� Unity
using UnityEngine.AI; // ������ ���� ��� ������ � AI �� ���������

public class WallGenerator : MonoBehaviour // ���� ��� ��������� ���
{
    [Header("Wall Settings")] // ��������� ��� ����������� �����
    public GameObject wallPrefab; // ������ ����
    public GameObject wallConectorPrefab; // ������ ����
    public float navMeshLinkWidth = 5f; // ������ ��������� ���������� ����
    public bool navMeshLinkBidirectional = true; // �� � ��������� ��������������
    public float maxSlopeAngle = 15f; // ������������ ��� ������
    public Color validPlacementColor = new Color(0, 1, 0, 0.5f); // ���� ��� �������� ���������
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f); // ���� ��� ���������� ���������
    public Color cursorFollowColor = new Color(0, 0.5f, 1f, 0.7f); // ���� ��� ����'� �� ��������

    [Header("Terrain Connection")] // ��������� ��� ����������� �'������� � ���������
    public float connectionSearchDistance = 20f; // ��������� ������ �'�������
    public LayerMask groundLayerMask; // ����� ���� ����
    public float waterCheckDistance = 10f; // ��������� �������� ����
    public int waterCheckPoints = 1; // ʳ������ ����� ��� �������� ����

    private Vector3 firstPoint; // ����� ����� ���������
    private Vector3 secondPoint; // ����� ����� ���������
    private bool isFirstPointSelected = false; // �� ������ ����� �����
    private bool isPlacingWall = false; // �� ��� ������ ���������
    private bool isValidPlacement = true; // �� ������ ���������
    private bool isWall = false;
    private bool isWallSecondPoint = false;

    
    private int wallCount = 0;
    private bool priceValid = false;

    private GameObject[] WallParts; // ����� ������ �����
    private GameObject[] previewParts; // ����� ������ ����'�
    private GameObject cursorFollowPreview; // ����'�, �� ���� �� ��������

    public float maxAllowedSlopeAngle = 45f;
    public float maxHeightDifference = 0.5f;

    private int costOfTreeWall, costOfRockWall, costOfGoldWall;


    void Update() // ��������� ������� �����
    {
        if (!isPlacingWall) // ���� �� � ����� ���������
        {
            ClearPreview(); // �������� ����'�
            return; // ����� � ������
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // �������� ������ �� ������ �� �������
        if (Physics.Raycast(ray, out RaycastHit hit)) // ���� ������ �������� ��'���
        {

            if (!isFirstPointSelected) // ���� ����� ����� �� ������
            {

                UpdateCursorFollowPreview(hit.point); // ������� ����'� �� ��������

            }
            else // ���� ����� ����� ������
            {
                secondPoint = hit.point; // �����'����� ����� �����
                isValidPlacement = CheckPlacementValidity(firstPoint, secondPoint); // ��������� �������� ���������

                UpdateWallPreview(); // ������� ����'� �����

            }

            if (Input.GetMouseButtonDown(0)) // ��� ��������� ��� ������ ����
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    isWall = true;
                    Debug.Log("����");
                }

                if (hit.collider.CompareTag("Wall") && isFirstPointSelected)
                {
                    isWallSecondPoint = true;
                    Debug.Log("���_DWA�");
                }





                if (IsValidLocation(hit) || isWall) // ���� ���� ������
                {
                    priceValid = 
                        UnityTcpClient.Instance.goldAmount < wallCount * costOfGoldWall ||
                        UnityTcpClient.Instance.rockAmount < wallCount * costOfRockWall ||
                        UnityTcpClient.Instance.woodAmount < wallCount * costOfTreeWall;
                    Debug.Log("����_0");
                    if (!isFirstPointSelected) // ���� ����� ����� �� ������
                    {
                        Debug.Log("����_1");
                        firstPoint = hit.point; // �����'����� ����� �����
                        isFirstPointSelected = true; // ���������, �� ����� ����� ������
                        Destroy(cursorFollowPreview); // �������� ����'� �� ��������
                        cursorFollowPreview = null; // �������� ���������

                        Debug.Log("����_2");
                    }
                    else if ((isValidPlacement || isWallSecondPoint) && !priceValid) // ���� ��������� ������ � � ����
                    {

                        ClearPreview(); // �������� ����'�
                        PlaceWall(); // ��������� ���
                        isPlacingWall = false; // ����� � ������ ���������
                        isWall = false;
                        isWallSecondPoint = false;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1)) // ��� ��������� ����� ������ ����
        {
            CancelPlacement(); // ��������� ���������
        }
    }



    private void UpdateCursorFollowPreview(Vector3 position) // ��������� ����'� �� ��������
    {
        if (cursorFollowPreview == null) // ���� ����'� �� �� ��������
        {
            cursorFollowPreview = Instantiate(wallPrefab, position, Quaternion.identity); // �������� ����'�
            cursorFollowPreview.tag = "Untagged"; // ���������� ���

            foreach (var collider in cursorFollowPreview.GetComponentsInChildren<Collider>()) // ��� ��� ���������
            {
                collider.enabled = false; // �������� ���������
            }

            foreach (var renderer in cursorFollowPreview.GetComponentsInChildren<Renderer>()) // ��� ��� ���������
            {
                renderer.material.color = cursorFollowColor; // ���������� ����
            }
        }
        else // ���� ����'� ��� ����
        {
            cursorFollowPreview.transform.position = position; // ������� �������
        }
    }

    private bool IsValidLocation(RaycastHit hit) // �������� �������� ����
    {
        return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") && // �� �� �����
               !hit.collider.CompareTag("Water") && // �� �� ����
               !hit.collider.CompareTag("Wall"); // �� �� ���
    }

    private bool CheckPlacementValidity(Vector3 start, Vector3 end) // �������� �������� ���������
    {
        Vector3 direction = end - start; // �������� �� �������
        float horizontalDistance = new Vector2(direction.x, direction.z).magnitude; // ������������� ���������
        float verticalDistance = Mathf.Abs(direction.y); // ����������� ���������
        float angle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg; // ��� ������

        if (angle > maxSlopeAngle) // ���� ��� �������� ������������
        {
            Debug.LogWarning($"Wall angle {angle:F1}� exceeds maximum {maxSlopeAngle}�"); // ������������
            return false; // ��������� false
        }
        return true; // ��������� true
    }

    private void UpdateWallPreview() // ��������� ����'� ���� � ����������� �������
    {
        ClearPreview(); // �������� �������� ����'�

        Vector3 direction = (secondPoint - firstPoint).normalized; // �������� ����
        Quaternion rotation = Quaternion.LookRotation(direction); // ������� ����
        float distance = Vector3.Distance(firstPoint, secondPoint); // ��������� �� �������
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z; // ������� ��������

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1); // ʳ������ ��������
        this.wallCount = wallCount;
        float offset = (wallCount * wallLength - distance) / 2; // ³����� ��� �����������

        Vector3 currentPosition = firstPoint - direction * (offset - 20);
        Vector3 currentPosition1 = firstPoint - direction * (offset);// ��������� �������
        previewParts = new GameObject[wallCount]; // ����������� ������ ����'�

        Color previewColor = isValidPlacement ? validPlacementColor : invalidPlacementColor; // ���� �������
        bool allSegmentsValid = true; // �� �� �������� ����� ������ ���������



        bool priceValid = 
            UnityTcpClient.Instance.goldAmount < wallCount * costOfGoldWall ||
            UnityTcpClient.Instance.rockAmount < wallCount * costOfRockWall || 
            UnityTcpClient.Instance.woodAmount < wallCount * costOfTreeWall;

        if (priceValid) previewColor = invalidPlacementColor;

        for (int i = 0; i < wallCount; i++) // ��� ������� ��������
        {
            if (i == 0 && isWall)
            {
                previewParts[i] = Instantiate(wallConectorPrefab, currentPosition1, wallConectorPrefab.transform.rotation);
                previewParts[i].tag = "Untagged";
                continue;
            }

            // ��������� ������� �� �������
            Vector3 groundPosition = GetPositionOnTerrain(currentPosition);



            // ��������� ����'� ��������
            previewParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
            previewParts[i].tag = "Untagged";

            // ���������� �������� ������� ��� ��������� ��������
            bool segmentValid = CheckSegmentPlacement(groundPosition, rotation);
            if (!segmentValid) allSegmentsValid = false;

            // ������������ ���� (����� ������� ������������� ��� ������� ��������)
            Color segmentColor = segmentValid ? validPlacementColor : invalidPlacementColor;
            if (priceValid) segmentColor = invalidPlacementColor;

            // ������������ ����'�
            foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = segmentColor;
                // ������ ������������� ��� ����'�
                var material = renderer.material;
                material.color = new Color(segmentColor.r, segmentColor.g, segmentColor.b, 0.7f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }

            currentPosition += direction * wallLength;
        }

        // ��������� ��������� ������ ��������
        isValidPlacement = allSegmentsValid;
    }

    private void PlaceWall()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1);
        float offset = (wallCount * wallLength - distance) / 2;

        Vector3 currentPosition = firstPoint - direction * (offset - 20);
        Vector3 currentPosition1 = firstPoint - direction * (offset);

        


        WallParts = new GameObject[wallCount];

        for (int i = 0; i < wallCount; i++)
        {

                if (i == 0 && isWall)
                {
                    WallParts[i] = Instantiate(wallConectorPrefab, currentPosition1, wallConectorPrefab.transform.rotation);
                    WallParts[i].tag = "Wall";

                    continue;
                }




                // ��������� ������ ��� ��������� ��������
                Vector3 groundPosition = GetPositionOnTerrain(currentPosition);

                WallParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
                WallParts[i].tag = "Wall";
                if (i == (wallCount - 1) && isWallSecondPoint)
                {
                    currentPosition = currentPosition + direction * 20f;
                    WallParts[i] = Instantiate(wallConectorPrefab, currentPosition, wallConectorPrefab.transform.rotation);
                    WallParts[i].tag = "Wall";

                    continue;
                }

                currentPosition += direction * wallLength;

        }
        UnityTcpClient.Instance.goldAmount = UnityTcpClient.Instance.goldAmount - costOfGoldWall * wallCount;
        UnityTcpClient.Instance.woodAmount = UnityTcpClient.Instance.woodAmount - costOfTreeWall * wallCount;
        UnityTcpClient.Instance.rockAmount = UnityTcpClient.Instance.rockAmount - costOfRockWall * wallCount;
        UnityTcpClient.Instance.uIresource.UpdateAmoundOfResource();

    }

    private Vector3 GetPositionOnTerrain(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 100f, Vector3.down); // ������� ������ ������
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point;
        }

        // ���� ������ �� ������ (�� ������ �������)
        return position;
    }
    private bool CheckSegmentPlacement(Vector3 position, Quaternion rotation)
    {
        // 1. �������� �������� ����� �� ��������� (�� �� "������" � �����)
        Ray groundCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        if (!Physics.Raycast(groundCheckRay, 1f, LayerMask.GetMask("Ground")))
        {
            return false;
        }

        // 2. �������� �� ������� � ������ ��'������
        Collider[] wallColliders = wallPrefab.GetComponentsInChildren<Collider>();
        foreach (var collider in wallColliders)
        {
            // ��������� ������� ���� ��������� ��� ��������
            Vector3 colliderCenter = position + rotation * collider.bounds.center;
            if (Physics.CheckBox(colliderCenter,
                               collider.bounds.extents,
                               rotation,
                               LayerMask.GetMask("Default"))) // ��� � �����������
            {
                return false;
            }
        }



        // 3. �������� ���� ������ ������� (�� �� ����� ������ ����)
        Ray slopeCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit slopeHit;
        if (Physics.Raycast(slopeCheckRay, out slopeHit, 1f, LayerMask.GetMask("Ground")))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > maxAllowedSlopeAngle) // maxAllowedSlopeAngle - ������� ��� ����
            {
                return false;
            }
        }

        // 4. �������� ����������� �������� ��� ��������
        float requiredSpace = 0.2f; // ���������� ������ �������
        Bounds wallBounds = wallPrefab.GetComponentInChildren<Renderer>().bounds;
        Vector3 halfExtents = wallBounds.extents + Vector3.one * requiredSpace;
        if (Physics.CheckBox(position, halfExtents, rotation, LayerMask.GetMask("Obstacles")))
        {
            return false;
        }

        // 5. �������� ������ (�� �� ������� ������/������)
        float terrainHeight = Terrain.activeTerrain.SampleHeight(position);
        if (Mathf.Abs(position.y - terrainHeight) > maxHeightDifference) // maxHeightDifference - ��������� ������
        {
            return false;
        }

        // 6. ��������� �������� �� �������� ���� (���� �������)
        if (CheckWaterPresence(position))
        {
            return false;
        }

        // �� �������� �������� - ������� ������
        return true;
    }

    // ��������� ����� ��� �������� ����
    private bool CheckWaterPresence(Vector3 position)
    {
        // ��������� �������� �� ����, �� � ������ ������ ���������� ����
        // ���������, �������� �� ����� ��� �� �����
        return false;
    }
    private void ClearPreview() // �������� ����'�
    {
        if (previewParts != null) // ���� � ������� ����'�
        {
            foreach (GameObject part in previewParts) // ��� ����� �������
            {
                if (part != null) Destroy(part); // ������� ��'���
            }
            previewParts = null; // �������� �����
        }

        if (cursorFollowPreview != null) // ���� � ����'� �� ��������
        {
            Destroy(cursorFollowPreview); // ������� ��'���
            cursorFollowPreview = null; // �������� ���������
        }
    }

    private void CancelPlacement() // ���������� ���������
    {
        isPlacingWall = false; // ����� � ������ ���������
        isFirstPointSelected = false; // ������� ���� �����
        ClearPreview(); // �������� ����'�
        Debug.Log("Wall placement canceled"); // ���������
    }

    public void StartPlacingWall(int costOfTreeWall, int costOfRockWall, int costOfGoldWall) // ������� ��������� �����
        
    {
        this.costOfTreeWall = costOfTreeWall;
        this.costOfRockWall = costOfRockWall;
        this.costOfGoldWall = costOfGoldWall;
        isPlacingWall = !isPlacingWall; // ���������� ����� ���������
        isFirstPointSelected = false; // ������� ���� �����
        ClearPreview(); // �������� ����'�
        Debug.Log("Wall placement mode: " + (isPlacingWall ? "ON" : "OFF")); // ���������
    }
}