using System.Data.Common;
using Unity.AI.Navigation; // Âèêîðèñòîâóºìî ïðîñòîðè ³ìåí äëÿ ðîáîòè ç íàâ³ãàö³ºþ
using UnityEngine; // Îñíîâíèé ïðîñò³ð ³ìåí Unity
using UnityEngine.AI; // Ïðîñò³ð ³ìåí äëÿ ðîáîòè ç AI òà íàâ³ãàö³ºþ

public class WallGenerator : MonoBehaviour // Êëàñ äëÿ ãåíåðàö³¿ ñò³í
{
    [Header("Wall Settings")] // Çàãîëîâîê äëÿ íàëàøòóâàíü ìîñòó
    public GameObject wallPrefab; // Ïðåôàá ñò³íè
    public GameObject wallConectorPrefab; // Ïðåôàá ñò³íè

    public float maxSlopeAngle = 15f; // Ìàêñèìàëüíèé êóò íàõèëó
    public Color validPlacementColor = new Color(0, 1, 0, 0.5f); // Êîë³ð äëÿ âàë³äíîãî ðîçì³ùåííÿ
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f); // Êîë³ð äëÿ íåâàë³äíîãî ðîçì³ùåííÿ
    public Color cursorFollowColor = new Color(0, 0.5f, 1f, 0.7f); // Êîë³ð äëÿ ïðåâ'þ ï³ä êóðñîðîì

    public float radius = 10f;
    public LayerMask wallLayer;


    [Header("Terrain Connection")] // Çàãîëîâîê äëÿ íàëàøòóâàíü ç'ºäíàííÿ ç òåððåéíîì
    public float connectionSearchDistance = 20f; // Äèñòàíö³ÿ ïîøóêó ç'ºäíàííÿ
    public LayerMask groundLayerMask; // Ìàñêà øàðó çåìë³
    public float waterCheckDistance = 10f; // Äèñòàíö³ÿ ïåðåâ³ðêè âîäè
    public int waterCheckPoints = 1; // Ê³ëüê³ñòü òî÷îê äëÿ ïåðåâ³ðêè âîäè

    private Vector3 firstPoint; // Ïåðøà òî÷êà ðîçì³ùåííÿ
    private Vector3 secondPoint; // Äðóãà òî÷êà ðîçì³ùåííÿ
    private bool isFirstPointSelected = false; // ×è îáðàíà ïåðøà òî÷êà
    private bool isPlacingWall = false; // ×è éäå ïðîöåñ ðîçì³ùåííÿ
    private bool isValidPlacement = true; // ×è âàë³äíå ðîçì³ùåííÿ
    private bool isWall = false;
    private bool isWallSecondPoint = false;


    private int wallCount = 0;
    private bool priceValid = false;

    private GameObject[] WallParts; // Ìàñèâ ÷àñòèí ìîñòó
    private GameObject[] previewParts; // Ìàñèâ ÷àñòèí ïðåâ'þ
    private GameObject cursorFollowPreview; // Ïðåâ'þ, ùî ñë³äóº çà êóðñîðîì

    public float maxAllowedSlopeAngle = 45f;
    public float maxHeightDifference = 0.5f;

    private int costOfTreeWall, costOfRockWall, costOfGoldWall;

    private bool needUpdateFirst = true;

    void Update()
    {
        if (!isPlacingWall)
        {
            ClearPreview();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check for wall under cursor (for both preview and placement)
            bool isOverWall = hit.collider.CompareTag("Wall");

            if (!isFirstPointSelected)
            {
                // Set wall flag if starting on a wall
                isWall = isOverWall;
                UpdateCursorFollowPreview(hit.point);
            }
            else
            {
                // Set second wall flag if ending on a wall
                isWallSecondPoint = isOverWall;
                secondPoint = hit.point;
                isValidPlacement = CheckPlacementValidity(firstPoint, secondPoint);
                UpdateWallPreview();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsValidLocation(hit) || isOverWall)
                {
                    priceValid =
                        UnityTcpClient.Instance.goldAmount < wallCount * costOfGoldWall ||
                        UnityTcpClient.Instance.rockAmount < wallCount * costOfRockWall ||
                        UnityTcpClient.Instance.woodAmount < wallCount * costOfTreeWall;

                    if (!isFirstPointSelected)
                    {
                        firstPoint = hit.point;
                        isFirstPointSelected = true;
                        isWall = isOverWall; // Confirm wall placement
                        Destroy(cursorFollowPreview);
                        cursorFollowPreview = null;
                    }
                    else if ((isValidPlacement || isWallSecondPoint) && !priceValid)
                    {
                        ClearPreview();
                        PlaceWall();
                        isPlacingWall = false;
                        isWall = false;
                        isWallSecondPoint = false;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }


    private void UpdateCursorFollowPreview(Vector3 position) // Îíîâëåííÿ ïðåâ'þ ï³ä êóðñîðîì
    {
        if (cursorFollowPreview == null) // ßêùî ïðåâ'þ ùå íå ñòâîðåíå
        {
            cursorFollowPreview = Instantiate(wallPrefab, position, Quaternion.identity); // Ñòâîðèòè ïðåâ'þ
            cursorFollowPreview.tag = "Untagged"; // Âñòàíîâèòè òåã

            foreach (var collider in cursorFollowPreview.GetComponentsInChildren<Collider>()) // Äëÿ âñ³õ êîëàéäåð³â
            {
                collider.enabled = false; // Âèìêíóòè êîëàéäåðè
            }

            foreach (var renderer in cursorFollowPreview.GetComponentsInChildren<Renderer>()) // Äëÿ âñ³õ ðåíäåðåð³â
            {
                renderer.material.color = cursorFollowColor; // Âñòàíîâèòè êîë³ð
            }
        }
        else // ßêùî ïðåâ'þ âæå ³ñíóº
        {
            cursorFollowPreview.transform.position = position; // Îíîâèòè ïîçèö³þ
        }
    }

    private bool IsValidLocation(RaycastHit hit) // Ïåðåâ³ðêà âàë³äíîñò³ ì³ñöÿ
    {
        return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") && // ×è öå çåìëÿ
               !hit.collider.CompareTag("Water") && // ×è íå âîäà
               !hit.collider.CompareTag("Wall"); // ×è íå ì³ñò
    }

    private bool CheckPlacementValidity(Vector3 start, Vector3 end) // Ïåðåâ³ðêà âàë³äíîñò³ ðîçì³ùåííÿ
    {
        Vector3 direction = end - start; // Íàïðÿìîê ì³æ òî÷êàìè
        float horizontalDistance = new Vector2(direction.x, direction.z).magnitude; // Ãîðèçîíòàëüíà äèñòàíö³ÿ
        float verticalDistance = Mathf.Abs(direction.y); // Âåðòèêàëüíà äèñòàíö³ÿ
        float angle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg; // Êóò íàõèëó

        if (angle > maxSlopeAngle) // ßêùî êóò ïåðåâèùóº ìàêñèìàëüíèé
        {
            Debug.LogWarning($"Wall angle {angle:F1}° exceeds maximum {maxSlopeAngle}°"); // Ïîïåðåäæåííÿ
            return false; // Ïîâåðíóòè false
        }
        return true; // Ïîâåðíóòè true
    }

    private void UpdateWallPreview()
    {
        ClearPreview();

        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1);
        this.wallCount = wallCount;
        float offset = (wallCount * wallLength - distance) / 2;

        Vector3 currentPosition = firstPoint - direction * (offset - 20);
        Vector3 firstConnectorPosition = firstPoint - direction * offset;
        previewParts = new GameObject[wallCount];

        // Перевірка ціни
        bool priceValid =
            UnityTcpClient.Instance.goldAmount < wallCount * costOfGoldWall ||
            UnityTcpClient.Instance.rockAmount < wallCount * costOfRockWall ||
            UnityTcpClient.Instance.woodAmount < wallCount * costOfTreeWall;

        // Визначаємо, чи показувати другий конектор у прев'ю
        bool showSecondConnector = false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            showSecondConnector = hit.collider.CompareTag("Wall");
        }

        Color previewColor = isValidPlacement ? validPlacementColor : invalidPlacementColor;
        if (priceValid) previewColor = invalidPlacementColor;

        for (int i = 0; i < wallCount; i++)
        {
            // Перший конектор
            if (i == 0 && isWall && !nearConector(firstConnectorPosition))
            {
                previewParts[i] = Instantiate(wallConectorPrefab, firstConnectorPosition, wallConectorPrefab.transform.rotation);
                previewParts[i].tag = "Untagged";

                foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.material;
                    material.color = new Color(previewColor.r, previewColor.g, previewColor.b, 0.7f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }

                currentPosition += direction * wallLength;
                continue;
            }

            // Останній конектор (прев'ю)
            if (i == wallCount - 1 && (isWallSecondPoint || showSecondConnector) && !nearConector(currentPosition))
            {
                previewParts[i] = Instantiate(wallConectorPrefab, currentPosition, wallConectorPrefab.transform.rotation);
                previewParts[i].tag = "Untagged";

                foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
                {
                    var material = renderer.material;
                    material.color = new Color(previewColor.r, previewColor.g, previewColor.b, 0.7f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }

                continue;
            }

            // Звичайні сегменти стіни
            Vector3 groundPosition = GetPositionOnTerrain(currentPosition);
            previewParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
            previewParts[i].tag = "Untagged";

            bool segmentValid = CheckSegmentPlacement(groundPosition, rotation);
            if (!segmentValid) isValidPlacement = false;

            Color segmentColor = segmentValid ? validPlacementColor : invalidPlacementColor;
            if (priceValid) segmentColor = invalidPlacementColor;

            foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
                collider.enabled = false;

            foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
            {
                var material = renderer.material;
                material.color = new Color(segmentColor.r, segmentColor.g, segmentColor.b, 0.7f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }

            currentPosition += direction * wallLength;
        }
    }
    public bool nearConector(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, wallLayer);
        return hits.Length > 0;
    }
    private void PlaceWall()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1);
        float offset = (wallCount * wallLength - distance) / 2;
        bool hasStartConn = false;
        bool hasEndConn = false;
        Vector3 currentPosition = firstPoint - direction * (offset - 20);
        Vector3 currentPosition1 = firstPoint - direction * offset;
        Vector3 endConnectorPosition = currentPosition + direction * (wallLength * (wallCount - 1)) + direction * 20f;

        WallParts = new GameObject[wallCount];

        for (int i = 0; i < wallCount; i++)
        {
            // First connector placement
            if (i == 0 && isWall && !nearConector(currentPosition1))
            {
                if (!ConnectorExistsInRadius(currentPosition1))
                {
                    WallParts[i] = Instantiate(wallConectorPrefab, currentPosition1, wallConectorPrefab.transform.rotation);
                    WallParts[i].tag = "Wall";
                    hasStartConn=true;
                    currentPosition += direction * wallLength;
                    continue;
                }
                else
                {
                    // If connector exists nearby, place normal wall segment instead
                    Debug.Log("Connector already exists nearby - placing normal wall");
                }
            }

            // Regular wall segment placement
            Vector3 groundPosition = GetPositionOnTerrain(currentPosition);
            WallParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
            WallParts[i].tag = "Wall";

            // Last connector placement
            if (i == (wallCount - 1) && isWallSecondPoint)
            {
                if (!ConnectorExistsInRadius(endConnectorPosition))
                {
                    WallParts[i] = Instantiate(wallConectorPrefab, endConnectorPosition, wallConectorPrefab.transform.rotation);
                    WallParts[i].tag = "Wall";
                    hasEndConn=true;
                    continue;
                }
                else
                {
                    // If connector exists nearby, keep the normal wall segment we already placed
                    Debug.Log("Connector already exists nearby - keeping normal wall");
                }
            }

            currentPosition += direction * wallLength;
        }

        // Update resources
        UnityTcpClient.Instance.goldAmount -= costOfGoldWall * wallCount;
        UnityTcpClient.Instance.woodAmount -= costOfTreeWall * wallCount;
        UnityTcpClient.Instance.rockAmount -= costOfRockWall * wallCount;
        UnityTcpClient.Instance.uIresource.UpdateAmoundOfResource();
        SendWallConstructionMessage(firstPoint, secondPoint, hasStartConn, hasEndConn);

    }
    private void SendWallConstructionMessage(Vector3 startPoint, Vector3 endPoint, bool hasStartConn, bool hasEndConn)
    {
        // Формат: WALL startX startY startZ endX endY endZ hasStartConn hasEndConn\n
        string message = $"WALL {startPoint.x} {startPoint.y} {startPoint.z} {endPoint.x} {endPoint.y} {endPoint.z} {(hasStartConn ? 1 : 0)} {(hasEndConn ? 1 : 0)}\n";
        UnityTcpClient.Instance.SendMessage(message);
    }

    public void HandleWallConstructionMessage(string message)
    {
        try
        {
            // Прибираємо зайві пробіли і розбиваємо повідомлення
            message = message.Trim();
            string[] parts = message.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 9 || !parts[0].Equals("WALL"))
            {
                Debug.LogError($"Невірний формат повідомлення про стіну: {message}");
                return;
            }

            // Парсимо координати
            Vector3 startPos = new Vector3(
                float.Parse(parts[1]),
                float.Parse(parts[2]),
                float.Parse(parts[3]));

            Vector3 endPos = new Vector3(
                float.Parse(parts[4]),
                float.Parse(parts[5]),
                float.Parse(parts[6]));

            // Парсимо прапорці конекторів
            bool hasStartConn = parts[7] == "1";
            bool hasEndConn = parts[8] == "1";

            Debug.Log($"Отримано стіну: {startPos} -> {endPos}, конектори: старт={hasStartConn}, кінець={hasEndConn}");

            BuildWallFromNetwork(startPos, endPos, hasStartConn, hasEndConn);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Помилка обробки повідомлення '{message}': {e.Message}");
        }
    }

    private void BuildWallFromNetwork(Vector3 startPos, Vector3 endPos, bool hasStartConn, bool hasEndConn)
    {
        Vector3 direction = (endPos - startPos).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z;
        float distance = Vector3.Distance(startPos, endPos);

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1);
        float offset = (wallCount * wallLength - distance) / 2;

        Vector3 currentPos = startPos - direction * (offset - 20);
        Vector3 startConnectorPos = startPos - direction * offset;
        Vector3 endConnectorPos = currentPos + direction * (wallLength * (wallCount - 1)) + direction * 20f;

        for (int i = 0; i < wallCount; i++)
        {
            // Початковий конектор
            if (i == 0 && hasStartConn && !ConnectorExistsInRadius(startConnectorPos))
            {
                Instantiate(wallConectorPrefab, startConnectorPos, wallConectorPrefab.transform.rotation).tag = "Wall";
                currentPos += direction * wallLength;
                continue;
            }

            // Звичайний сегмент стіни
            Instantiate(wallPrefab, GetPositionOnTerrain(currentPos), rotation).tag = "Wall";

            // Кінцевий конектор
            if (i == wallCount - 1 && hasEndConn && !ConnectorExistsInRadius(endConnectorPos))
            {
                Instantiate(wallConectorPrefab, endConnectorPos, wallConectorPrefab.transform.rotation).tag = "Wall";
                continue;
            }

            currentPos += direction * wallLength;
        }
    }

    private bool ConnectorExistsInRadius(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, wallLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Wall") && hit.gameObject.name.Contains(wallConectorPrefab.name))
            {
                return true;
            }
        }
        return false;
    }
    private Vector3 GetPositionOnTerrain(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 100f, Vector3.down); // Ïóñêàºìî ïðîì³íü çâåðõó
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point;
        }

        // ßêùî ïðîì³íü íå âëó÷èâ (íà âñÿêèé âèïàäîê)
        return position;
    }
    private bool CheckSegmentPlacement(Vector3 position, Quaternion rotation)
    {
        // 1. Ïåðåâ³ðêà íàÿâíîñò³ îïîðè ï³ä ñåãìåíòîì (÷è íå "âèñèòü" ó ïîâ³òð³)
        Ray groundCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        if (!Physics.Raycast(groundCheckRay, 1f, LayerMask.GetMask("Ground")))
        {
            return false;
        }

        // 2. Ïåðåâ³ðêà íà ïåðåòèí ç ³íøèìè îá'ºêòàìè
        Collider[] wallColliders = wallPrefab.GetComponentsInChildren<Collider>();
        foreach (var collider in wallColliders)
        {
            // Ñòâîðþºìî ô³çè÷íó êîï³þ êîëàéäåðà äëÿ ïåðåâ³ðêè
            Vector3 colliderCenter = position + rotation * collider.bounds.center;
            if (Physics.CheckBox(colliderCenter,
                               collider.bounds.extents,
                               rotation,
                               LayerMask.GetMask("Default"))) // Øàð ç ïåðåøêîäàìè
            {
                return false;
            }
        }



        // 3. Ïåðåâ³ðêà êóòà íàõèëó ïîâåðõí³ (÷è íå íàäòî êðóòèé ñõèë)
        Ray slopeCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit slopeHit;
        if (Physics.Raycast(slopeCheckRay, out slopeHit, 1f, LayerMask.GetMask("Ground")))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > maxAllowedSlopeAngle) // maxAllowedSlopeAngle - çàäàíèé ë³ì³ò êóòà
            {
                return false;
            }
        }

        // 4. Ïåðåâ³ðêà äîñòàòíüîãî ïðîñòîðó äëÿ ñåãìåíòà
        float requiredSpace = 0.2f; // Äîäàòêîâèé ïðîñò³ð íàâêîëî
        Bounds wallBounds = wallPrefab.GetComponentInChildren<Renderer>().bounds;
        Vector3 halfExtents = wallBounds.extents + Vector3.one * requiredSpace;
        if (Physics.CheckBox(position, halfExtents, rotation, LayerMask.GetMask("Obstacles")))
        {
            return false;
        }

        // 5. Ïåðåâ³ðêà âèñîòè (÷è íå çàíàäòî âèñîêî/íèçüêî)
        float terrainHeight = Terrain.activeTerrain.SampleHeight(position);
        if (Mathf.Abs(position.y - terrainHeight) > maxHeightDifference) // maxHeightDifference - äîïóñòèìà ð³çíèöÿ
        {
            return false;
        }

        // 6. Äîäàòêîâà ïåðåâ³ðêà íà íàÿâí³ñòü âîäè (ÿêùî ïîòð³áíî)
        if (CheckWaterPresence(position))
        {
            return false;
        }

        // Âñ³ ïåðåâ³ðêè ïðîéäåíî - ïîçèö³ÿ âàë³äíà
        return true;
    }

    // Äîïîì³æíèé ìåòîä äëÿ ïåðåâ³ðêè âîäè
    private bool CheckWaterPresence(Vector3 position)
    {
        // Ðåàë³çàö³ÿ çàëåæèòü â³ä òîãî, ÿê ó âàøîìó ïðîåêò³ ðåàë³çîâàíà âîäà
        // Íàïðèêëàä, ïåðåâ³ðêà ïî øàðàõ àáî ïî âèñîò³
        return false;
    }
    private void ClearPreview() // Î÷èùåííÿ ïðåâ'þ
    {
        if (previewParts != null) // ßêùî º ÷àñòèíè ïðåâ'þ
        {
            foreach (GameObject part in previewParts) // Äëÿ êîæíî¿ ÷àñòèíè
            {
                if (part != null) Destroy(part); // Çíèùèòè îá'ºêò
            }
            previewParts = null; // Î÷èñòèòè ìàñèâ
        }

        if (cursorFollowPreview != null) // ßêùî º ïðåâ'þ ï³ä êóðñîðîì
        {
            Destroy(cursorFollowPreview); // Çíèùèòè îá'ºêò
            cursorFollowPreview = null; // Î÷èñòèòè ïîñèëàííÿ
        }
    }

    private void CancelPlacement() // Ñêàñóâàííÿ ðîçì³ùåííÿ
    {
        isPlacingWall = false; // Âèéòè ç ðåæèìó ðîçì³ùåííÿ
        isFirstPointSelected = false; // Ñêèíóòè âèá³ð òî÷êè
        ClearPreview(); // Î÷èñòèòè ïðåâ'þ
        Debug.Log("Wall placement canceled"); // Ëîãóâàííÿ
    }

    public void StartPlacingWall(int costOfTreeWall, int costOfRockWall, int costOfGoldWall) // Ïî÷àòîê ðîçì³ùåííÿ ìîñòó

    {
        this.costOfTreeWall = costOfTreeWall;
        this.costOfRockWall = costOfRockWall;
        this.costOfGoldWall = costOfGoldWall;
        isPlacingWall = !isPlacingWall; // Ïåðåìêíóòè ðåæèì ðîçì³ùåííÿ
        isFirstPointSelected = false; // Ñêèíóòè âèá³ð òî÷êè
        ClearPreview(); // Î÷èñòèòè ïðåâ'þ
        Debug.Log("Wall placement mode: " + (isPlacingWall ? "ON" : "OFF")); // Ëîãóâàííÿ
    }
}