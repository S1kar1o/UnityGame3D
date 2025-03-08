/*using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using System.Linq;
using System.Collections;

public class MovingObjects1 : MonoBehaviour
{
    private Vector2 startScreenPosition; // ��������� ������� ����� ������
    private Vector2 endScreenPosition;   // ������� ������� ����
    private bool isDragging = false;     // �� ����� ��������� �����

    private Camera mainCamera;           // ������� ������
    public LayerMask groundLayer;        // ��� ��� ���� (xz-�������)
    public LayerMask selectionLayer;     // ��� ��� ��'���� ������
    public List<GameObject> selectedUnits = new List<GameObject>();
    public List<CashObg> cashObg = new List<CashObg>();// ������ �������� ����
    public LayerMask riverLayer;
    public Animation anim;
    private float timer = 0f;
*//*    private float ProbabilityDrowningForTime = 0.5f;
    private float DrowningTimeForProbability = 5f;
    private float ProbabilityNoDrowningForSecond;*//*
    

    void Start()
    {
*//*        ProbabilityNoDrowningForSecond = Mathf.Pow(ProbabilityDrowningForTime, 1 / DrowningTimeForProbability);
*//*        //Debug.Log(ProbabilityNoDrowningForSecond);
         mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMouseInput();
        timer += Time.deltaTime;

        foreach (var cashobj in cashObg)
        {
            GameObject obj = cashobj.obj;
            Animator objAnimator = obj.GetComponent<Animator>();
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (objAnimator.GetBool("Run") && agent.remainingDistance != 0)
            {
               
                float distance = agent.remainingDistance;
                //Debug.Log(distance);
                if (distance < 10)
                {
                    //Debug.Log(distance + "RRR");
                    agent.isStopped = true;
                    agent.ResetPath();

                    string objectName = obj.name;
                    
                    objAnimator.SetBool("Run", false);
                    //Debug.Log("���������� ���� ���: " + objectName + " " + objAnimator.GetBool("Run"));
                    // ������������� ������� �����
                }
            }

            if (obj != null)
            {
                
                if (agent != null)
                {
                    Vector3 childPosition = new Vector3();
                    Transform childTransform = obj.transform.Find("Cube");  // �������� "ChildObjectName" �� ��� ������ ��������� �������
                    if (childTransform != null)
                    {

                        childPosition = childTransform.position;  // �������� ������� ������� ��������� �������
                        
                    }
                    
                    RaycastHit hit;
                    if (Physics.Raycast(childPosition, Vector3.down, out hit, Mathf.Infinity, riverLayer))
                    {

                        if (timer >= 1) 
                        {
                            float randomNumber = Random.Range(0f, 1f);
                            //Debug.Log(randomNumber);
                            //if (randomNumber > ProbabilityNoDrowningForSecond)
                            //{
                            //    Debug.Log("OOO");
                            //    objAnimator.SetBool("Drown", true);
                            //    agent.isStopped = true;
                            //    cashobj.isDrow = true;
                                
                            //    Destroy(obj, 5f);
                            //    StartCoroutine(RemoveFromListAfterDelay(cashobj, 5f));
                                

                            //}
                            
                        }

                       
                        if (cashobj.isDrow) 
                        {
                            cashobj.deep += 0.05f;
                        } 

                        Vector3 nextPosition = agent.nextPosition;
                        nextPosition.y = nextPosition.y - obj.transform.localScale.y - cashobj.deep;  // ������������� Y �� ������� ������

                        agent.transform.position = nextPosition;
                        if (!cashobj.isRiver)
                        {
                            objAnimator.SetBool("Water", true);
                            agent.speed = agent.speed/4;
                            cashobj.isRiver = true;
                        }

                    }
                    else
                    {
                        
                        if (cashobj.isRiver)
                        {
                            objAnimator.SetBool("Water", false);
                            agent.speed = agent.speed * 4;
                            cashobj.isRiver = false;
                        }
                    }
                }
            }
        }

        if (timer >= 1)
        { 
            timer = 0f;
        }
    }

    void HandleMouseInput()
    {
        if(Input.GetMouseButtonDown(1)&&selectedUnits.Count>0) {
            Ray ray= mainCamera.ScreenPointToRay( Input.mousePosition );
            if(Physics.Raycast(ray,out RaycastHit agentTarget,3000f,groundLayer) ) {
            foreach(var obj in selectedUnits)
                {
                    
                    bool containsSilver = cashObg.Any(repObj => repObj.obj == obj);
                    if (!containsSilver)
                    {
                        cashObg.Add(new CashObg(obj));
                    }

                    obj.GetComponent<NavMeshAgent>().SetDestination(agentTarget.point);
                    Animator objAnimator = obj.GetComponent<Animator>();
                    if (objAnimator != null)
                    {
                        string objectName = obj.name;
                       
                        obj.GetComponent<NavMeshAgent>().isStopped = false;
                        objAnimator.SetBool("Run",true);
                        //Debug.Log("��������� ���� ���: " + objectName + " " + objAnimator.GetBool("Run"));

                    }
                    else
                    {
                        //Debug.LogWarning("� ����� ��� Animator ����������!");
                    }
                }
            
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            startScreenPosition = Input.mousePosition;
            isDragging = true;
            //Debug.Log($"������� ������ (������ ����������): {startScreenPosition}");
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            endScreenPosition = Input.mousePosition;

            //Debug.Log($"ʳ���� ������ (������ ����������): {endScreenPosition}");
            SelectUnits();
        }

        if (isDragging)
        {
            endScreenPosition = Input.mousePosition;
        }
    }

    void OnGUI()
    {
        if (isDragging)
        {
            Rect rect = GetScreenRect(startScreenPosition, endScreenPosition);
            DrawSelectionRect(rect);
        }
    }

    void SelectUnits()
    {
        // ������������ �������� ��������� � �������� �����
        Vector3 worldStart = ScreenToWorldXZ(startScreenPosition);
        Vector3 worldEnd = ScreenToWorldXZ(endScreenPosition);

        //Debug.Log($"������� ������ � ��� (xz): {worldStart}");
        //Debug.Log($"ʳ���� ������ � ��� (xz): {worldEnd}");

        // ����������� ��� xz
        float minX = Mathf.Min(worldStart.x, worldEnd.x);
        float maxX = Mathf.Max(worldStart.x, worldEnd.x);
        float minZ = Mathf.Min(worldStart.z, worldEnd.z);
        float maxZ = Mathf.Max(worldStart.z, worldEnd.z);

        // �������� ������������ ������
        selectedUnits.Clear();
        foreach (GameObject unit in GameObject.FindGameObjectsWithTag("Unit"))
        {
            Vector3 unitPosition = unit.transform.position;
            // ��������, �� ��������� ��� � �����
            if (unitPosition.x >= minX && unitPosition.x <= maxX && unitPosition.z >= minZ && unitPosition.z <= maxZ)
            {
                selectedUnits.Add(unit);
                HighlightUnit(unit, true);
            }
            else
            {
                HighlightUnit(unit, false);
            }
        }

        // ��������� �������� ����
        //Debug.Log($"������� {selectedUnits.Count} ����.");
        foreach (GameObject unit in selectedUnits)
        {
            //Debug.Log($"�������� ���: {unit.name}, ������� � ��� (xz): ({unit.transform.position.x}, {unit.transform.position.z})");
        }
    }

    Vector3 ScreenToWorldXZ(Vector2 screenPosition)
    {
        // ��������� ������� � �������� ���������
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // ������������ � ����� ����������; y = 0 ��� ������� xz
            return new Vector3(hit.point.x, 0, hit.point.z);
        }

        // ���� ������ �� �������� �����, ��������� 0 (���� ���� ������)
        //Debug.LogWarning("������ �� �������� �������.");
        return Vector3.zero;
    }

    Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
    {
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        Vector2 topLeft = Vector2.Min(screenPosition1, screenPosition2);
        Vector2 bottomRight = Vector2.Max(screenPosition1, screenPosition2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    void DrawSelectionRect(Rect rect)
    {
        GUI.color = new Color(0, 1, 0, 0.25f); // ������������ �������
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.green; // ���������
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 2, rect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 2, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2, rect.yMin, 2, rect.height), Texture2D.whiteTexture);
    }

    void HighlightUnit(GameObject unit, bool highlight)
    {
        if (unit != null && unit.transform.childCount > 0)
        {
            unit.transform.GetChild(0).gameObject.SetActive(highlight);
        }
        else
        {
            //Debug.LogWarning($"��'��� {unit?.name ?? "null"} �� �� ���������� ��������.");
        }
    }

    private IEnumerator RemoveFromListAfterDelay(CashObg obj, float delay)
    {
        // ���� ��������� �����
        yield return new WaitForSeconds(delay);

        // ������� ������ �� ������, ���� �� ��� ��� ����
        if (cashObg.Contains(obj))
        {
            cashObg.Remove(obj);
            Debug.Log($"������ {obj.obj.name} ������ �� ������.");
        }
        
        if (selectedUnits.Contains(obj.obj))
        {
            selectedUnits.Remove(obj.obj);
            Debug.Log($"������ {obj.obj.name} ������ �� ������.");
        }
    }
}



public class CashObg
{
    public GameObject obj;
    public bool isRiver;
    public bool isDrow;
    public float deep;
    public CashObg(GameObject obj)
    {
        this.obj = obj;
        isRiver = false;
        isDrow = false;
        deep = 5f;
    }
}
*/