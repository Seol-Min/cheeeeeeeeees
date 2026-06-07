using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView; //for editor
using UnityEngine;
using UnityEngine.SceneManagement;

public class Control : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private float tileSizeX = 1.0f;
    [SerializeField] private float tileSizeY = 1.0f;
    [SerializeField] private Vector3 MapCenter = Vector3.zero;
    private float zOffset = -0.1f;

    [Header("Objects")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private GameObject movePlate;
    [SerializeField] private GameObject gold;
    [SerializeField] private GameObject turnChanger;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject statePanel;
    [SerializeField] private Texture2D whiteCursor;
    [SerializeField] private Texture2D blackCursor;
    [SerializeField] public Sprite[] itemEfSprite;

    [Header("Player Gold")]
    [SerializeField] private PlayerGold p1Gold;
    [SerializeField] private PlayerGold p2Gold;

    [Header("Gold VFX -> UI targets")]
    [SerializeField] private RectTransform p1GoldUI;
    [SerializeField] private RectTransform p2GoldUI;
    [SerializeField] private float goldMoveDuration = 0.6f;
    [SerializeField] private AnimationCurve goldMoveCurve;
    [SerializeField] private int maxGoldParticles = 20;
    [SerializeField] private float goldSpawnSpread = 0.3f;
    [SerializeField] private float goldStaggerDelay = 0.06f;


    // Logic
    private Units[,] units;
    public static Control Instance { get; private set; }
    private Units currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private TextMeshProUGUI stateText;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    public bool isWhiteTurn;
    private bool isSummonMode = false;
    private int summonColor = -1;
    private List<UnitType> unitsToPlace;
    //private bool isPlacingPhase = true;
    //private int placingColor = 0;
    //private int startPlaceCount = 0;
    public bool isTargetMode = false;
    private int targetColor = -1;
    private bool targetEnemy = true;
    private bool targetAll = false;
    private Func<Units, bool> targetCondition = null;
    public System.Action<Units> onTargetSelected;
    private List<UnitType> targetFilter = null;
    private ItemData pendingItem = null;
    private int pendingSlotIndex = -1;
    private Inventory pendingInventory = null;
    private int win = -1; // 0: white wins, 1: black wins, -1: ongoing

    public bool IsGameOver => win != -1;
    public bool IsPaused => pausePanel.activeSelf;
    public bool IsChangingTurn { get; private set; }

    private Ray ray;
    private RaycastHit hit;

    private void Awake()
    {
        Instance = this;
        currentCamera = Camera.main;
        isWhiteTurn = true;
        GenerateAllTiles(tileSizeX, tileSizeY, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAll();
        PositionAll();
        stateText = statePanel.GetComponentInChildren<TextMeshProUGUI>();
        //units = new Units[TILE_COUNT_X, TILE_COUNT_Y];
        unitsToPlace = new List<UnitType>();
        //StartPlacingPhase();
        StartCoroutine(WriteStateText("백 턴 시작"));
        UpdateCursor();
        pausePanel.SetActive(false);
        turnChanger.SetActive(false);

        currentHover = -Vector2Int.one;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pausePanel.SetActive(!IsPaused);
        }
        if (IsGameOver) return;
        if (IsPaused) return;
        if (IsChangingTurn) return;
        if (Input.GetMouseButtonDown(0))
        {
            ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
            {
                currentHover = LookupTileIndex(hit.collider.gameObject);
                if (isSummonMode)
                {
                    int minY = summonColor == 0 ? 0 : TILE_COUNT_Y - 2;
                    int maxY = summonColor == 0 ? 1 : TILE_COUNT_Y - 1;
                    if (currentHover.y >= minY && currentHover.y <= maxY && units[currentHover.x, currentHover.y] == null)
                    {
                        Units unit = SpawnSingleUnit(unitsToPlace[0], summonColor);
                        units[currentHover.x, currentHover.y] = unit;

                        PositionSingle(currentHover.x, currentHover.y);
                        unit.GetComponent<Animator>().SetTrigger("Place");
                        unitsToPlace.RemoveAt(0);

                        RemoveMovePlate();

                        if (unitsToPlace.Count > 0)
                        {
                            ShowSummonPlate(summonColor);
                        }
                        else
                        {
                            isSummonMode = false;
                            UpdateCheckState();
                            //if (isPlacingPhase)
                            //{
                            //    if (startPlaceCount < 2)
                            //    {
                            //        placingColor = 1 - placingColor;
                            //        RemoveMovePlate();
                            //        StartPlacingPhase();
                            //    }
                            //    else
                            //    {
                            //        RemoveMovePlate();
                            //        isPlacingPhase = false;
                            //    }
                            //}
                        }
                    }
                    return;
                }
                if (isTargetMode)
                {
                    if (units[currentHover.x, currentHover.y] != null)
                    {
                        Units clicked = units[currentHover.x, currentHover.y];
                        bool isEnemy = clicked.color != targetColor;
                        bool isAlly = clicked.color == targetColor;
                        bool filterMatch = targetFilter == null || targetFilter.Contains(clicked.data.Type);
                        bool conditionMatch = targetCondition == null || targetCondition(clicked);

                        if ((targetAll || (targetEnemy && isEnemy) || (!targetEnemy && isAlly)) && filterMatch && conditionMatch)
                        {
                            onTargetSelected?.Invoke(clicked);
                            RemoveMovePlate();
                            isTargetMode = false;
                            UpdateCheckState();
                        }
                    }
                    else
                    {
                        RemoveMovePlate();
                        isTargetMode = false;
                        pendingInventory?.ReturnItem(pendingItem, pendingSlotIndex);
                        pendingItem = null;
                        pendingSlotIndex = -1;
                        pendingInventory = null;
                    }
                    return;
                }

                if (currentHover.x >= 0 && currentHover.y >= 0 && units[currentHover.x, currentHover.y] != null)
                {
                    if ((isWhiteTurn && units[currentHover.x, currentHover.y].color == 0) || (!isWhiteTurn && units[currentHover.x, currentHover.y].color == 1))
                    {
                        if (units[currentHover.x, currentHover.y].isArrested > -1)
                        {
                            StartCoroutine(WriteStateText("해당 기물은 속박되었습니다!"));
                            return;
                        }
                        currentlyDragging = units[currentHover.x, currentHover.y];
                        currentlyDragging.GetComponent<Animator>().SetBool("isDragging", true);
                        availableMoves = currentlyDragging.GetAvailableMoves(ref units, TILE_COUNT_X, TILE_COUNT_Y);
                        currentlyDragging.SetScale(new Vector3(1.5f, 1.5f, 1f), true);
                        ShowMovePlate();
                    }
                }
            }
        }
        // Hold: Move the unit with the mouse
        if (Input.GetMouseButton(0) && currentlyDragging != null)
        {
            Vector3 mouseWorld = GetMouseWorldPosition(currentlyDragging.transform.position);
            mouseWorld.x += 0.2f;
            mouseWorld.y -= 0.3f;
            mouseWorld.z -= 0.01f;
            currentlyDragging.transform.position = mouseWorld;
        }

        // Button Up: Release the unit and attempt to move it to the new position
        if (Input.GetMouseButtonUp(0) && currentlyDragging != null)
        {
            ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            bool moved = false;
            if (Physics.Raycast(ray, out hit, 100))
            {
                Vector2Int target = LookupTileIndex(hit.collider.gameObject);
                if (target != -Vector2Int.one)
                {
                    if (MoveTo(currentlyDragging, target.x, target.y))
                    {
                        moved = true;   
                        currentlyDragging.GetComponent<Animator>().SetTrigger("move");

                        PlayerGold nextGold = isWhiteTurn ? p1Gold : p2Gold;
                        nextGold.AddGold(2);
                    }
                }
            }
            if (!moved)
            {
                PositionSingle(currentlyDragging.currentX, currentlyDragging.currentY, false, currentlyDragging.previousPosition.x, currentlyDragging.previousPosition.y);
            }
            currentlyDragging.SetScale(new Vector3(1f, 1f, 1f), true);
            currentlyDragging.GetComponent<Animator>().SetBool("isDragging", false);
            RemoveMovePlate();
            currentlyDragging = null;
        }
    }

    private Vector3 GetMouseWorldPosition(Vector3 referenceWorldPosition)
    {
        Vector3 screenPoint = currentCamera.WorldToScreenPoint(referenceWorldPosition);
        screenPoint.x = Input.mousePosition.x;
        screenPoint.y = Input.mousePosition.y;
        return currentCamera.ScreenToWorldPoint(screenPoint);
    }
    private void UpdateCursor()
    {
        if (isWhiteTurn)
            Cursor.SetCursor(whiteCursor, Vector2.zero, CursorMode.Auto);
        else
            Cursor.SetCursor(blackCursor, Vector2.zero, CursorMode.Auto);
    }
    public void UpdateCheckState()
    {
        Units whiteKing = null, blackKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (units[x, y] == null) continue;
                if (units[x, y].data.Type == UnitType.King)
                {
                    if (units[x, y].color == 0) whiteKing = units[x, y];
                    else blackKing = units[x, y];
                }
            }

        bool whiteInCheck = false, blackInCheck = false;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (units[x, y] == null) continue;
                if (units[x, y].isArrested >= 0) continue;
                List<Vector2Int> moves = units[x, y].GetAvailableMoves(ref units, TILE_COUNT_X, TILE_COUNT_Y);

                foreach (var move in moves)
                {
                    if (whiteKing != null && move == new Vector2Int(whiteKing.currentX, whiteKing.currentY) && units[x, y].color == 1)
                        whiteInCheck = true;
                    if (blackKing != null && move == new Vector2Int(blackKing.currentX, blackKing.currentY) && units[x, y].color == 0)
                        blackInCheck = true;
                }
            }

        if (whiteKing != null && HasParameter(whiteKing.GetComponent<Animator>(), "Check"))
            whiteKing.GetComponent<Animator>().SetBool("Check", whiteInCheck);
        if (blackKing != null && HasParameter(blackKing.GetComponent<Animator>(), "Check"))
            blackKing.GetComponent<Animator>().SetBool("Check", blackInCheck);
    }

    // Generate Tiles
    private void GenerateAllTiles(float tileSizeX, float tileSizeY, int tileCountX, int tileCountY)
    {
        zOffset += transform.position.z;
        bounds = new Vector3((tileCountX / 2) * tileSizeX, (tileCountY / 2) * tileSizeY, 0) + MapCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSizeX, tileSizeY, x, y);
    }
    private GameObject GenerateSingleTile(float tileSizeX, float tileSizeY, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format($"X:{x}, Y:{y}"));
        tileObject.transform.parent = transform;

        Vector3 center = new Vector3(x * tileSizeX, y * tileSizeY, zOffset) - bounds + new Vector3(tileSizeX / 2f, tileSizeY / 2f, 0.01f);
        tileObject.transform.position = center;
        tileObject.transform.localScale = new Vector3(tileSizeX, tileSizeY, 1f);

        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(1f, 1f, 0.01f);
        collider.center = Vector3.zero;
            
        return tileObject;
    }

    // Spawn Units
    //private void StartPlacingPhase()
    //{
    //    startPlaceCount++;
    //    unitsToPlace.AddRange(new List<UnitType> {
    //        UnitType.Pawn, UnitType.Pawn,
    //        UnitType.Knight,
    //        UnitType.Bishop,
    //        UnitType.Rook,
    //        UnitType.King
    //    });
    //    isSummonMode = true;
    //    summonColor = placingColor;
    //    ShowSummonPlate(summonColor);
    //}
    private void SpawnAll()
    {
        units = new Units[TILE_COUNT_X, TILE_COUNT_Y];

        int white = 0, black = 1;

        units[0, 0] = SpawnSingleUnit(UnitType.Rook, white);
        units[1, 0] = SpawnSingleUnit(UnitType.Bishop, white);
        units[3, 0] = SpawnSingleUnit(UnitType.King, white);
        units[6, 0] = SpawnSingleUnit(UnitType.Knight, white);
        units[0, 1] = SpawnSingleUnit(UnitType.Pawn, white);
        units[2, 1] = SpawnSingleUnit(UnitType.Pawn, white);

        units[7, 7] = SpawnSingleUnit(UnitType.Rook, black);
        units[6, 7] = SpawnSingleUnit(UnitType.Bishop, black);
        units[4, 7] = SpawnSingleUnit(UnitType.King, black);
        units[1, 7] = SpawnSingleUnit(UnitType.Knight, black);
        units[7, 6] = SpawnSingleUnit(UnitType.Pawn, black);
        units[5, 6] = SpawnSingleUnit(UnitType.Pawn, black);
    }
    private Units SpawnSingleUnit(UnitType type, int color)
    {
        Units unit = Instantiate(prefabs[(int)type - 1], transform).GetComponent<Units>();
        unit.color = color;
        unit.GetComponent<Animator>().SetInteger("color", color);
        return unit;
    }
    public bool HasEmptyCallTile(bool isWhite)
    {
        int color = isWhite ? 0 : 1;
        int minY = color == 0 ? 0 : TILE_COUNT_Y - 2;
        int maxY = color == 0 ? 1 : TILE_COUNT_Y - 1;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = minY; y <= maxY; y++)
                if (units[x, y] == null) return true;

        return false;
    }
    // 위치 선정
    private void PositionAll()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (units[x, y] != null)
                {
                    PositionSingle(x, y, true);
                    units[x, y].GetComponent<Animator>().SetTrigger("Place");
                }
    }
    private void PositionSingle(int x, int y, bool force = false, int prX = -1, int prY = -1)
    {
        units[x, y].previousPosition = new Vector2Int(prX, prY);
        units[x, y].currentX = x;
        units[x, y].currentY = y;
        units[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSizeX, y * tileSizeY, zOffset) - bounds + new Vector3(tileSizeX / 2, tileSizeY / 2, -0.01f + (y * 0.001f));
    }
    private void ShowMovePlate()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            Vector2Int move = availableMoves[i];
            GameObject movePlateInstance = Instantiate(movePlate, transform);
            if (units[move.x, move.y] != null)
            {
                movePlateInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f);
                movePlateInstance.transform.localScale = new Vector3(2f, 2f, 1f);
            }
            Vector3 movePos = GetTileCenter(move.x, move.y);
            movePos.z = -0.09f;
            movePlateInstance.transform.position = movePos;
        }
    }
    private void ShowSummonPlate(int color)
    {
        int minY = color == 0 ? 0 : TILE_COUNT_Y - 2;
        int maxY = color == 0 ? 1 : TILE_COUNT_Y - 1;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (units[x, y] != null) continue;

                GameObject plate = Instantiate(movePlate, transform);
                plate.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0f, 0.5f);
                Vector3 pos = GetTileCenter(x, y);
                pos.z = -0.09f;
                plate.transform.position = pos;
            }
        }
    }
    private void ShowTargetPlate(int color, bool targetEnemy, List<UnitType> filter = null, bool targetAll = false, Func<Units, bool> condition = null)
    {
        List<Units> targets = new List<Units>();

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (units[x, y] != null)
                {
                    bool colorMatch = targetAll || (targetEnemy ? units[x, y].color != color : units[x, y].color == color);
                    bool filterMatch = filter == null || filter.Contains(units[x, y].data.Type);
                    bool conditionMatch = condition == null || condition(units[x, y]);
                    if (colorMatch && filterMatch && conditionMatch)
                        targets.Add(units[x, y]);
                }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject plate = Instantiate(movePlate, transform);
            plate.GetComponent<SpriteRenderer>().color = targetEnemy
                ? new Color(1f, 0f, 0f, 0.9f)
                : new Color(0f, 1f, 0f, 0.9f);
            Vector3 pos = GetTileCenter(targets[i].currentX, targets[i].currentY);
            pos.z = -0.09f;
            plate.transform.position = pos;
        }
    }
    private void RemoveMovePlate()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
            Destroy(movePlates[i]);
    }
    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    public bool HasValidTarget(bool isWhite, bool targetEnemy, List<UnitType> filter = null, Func<Units, bool> condition = null, bool targetAll = false)
    {
        int color = isWhite ? 0 : 1;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (units[x, y] == null) continue;
                bool colorMatch = targetAll || (targetEnemy ? units[x, y].color != color : units[x, y].color == color);
                bool filterMatch = filter == null || filter.Contains(units[x, y].data.Type);
                bool conditionMatch = condition == null || condition(units[x, y]);
                if (colorMatch && filterMatch && conditionMatch) return true;
            }
        return false;
    }
    public bool MoveTo(Units un, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)) && !isTargetMode)
            return false;

        Vector2Int previousPosition = new Vector2Int(un.currentX, un.currentY);
        bool hasEnemy = units[x, y] != null;
        if (units[x, y] != null)
        {
            StartCoroutine(PauseAnimation(un.GetComponent<Animator>()));
            Units other = units[x, y];

            if (other.data.Type == UnitType.King)
            {
                win = un.color;
                string winText = win == 0 ? "백 팀 승리!" : "흑 팀 승리!";
                StartCoroutine(WriteStateText(winText, 10f, true));
            }

            PlayerGold attackerGold = isWhiteTurn ? p1Gold : p2Gold;
            if (!other.poop)
                attackerGold.AddGold(other.data.CaptureGold + other.bonusCaptureGold);

            int gain = (!other.poop) ? (other.data.CaptureGold + other.bonusCaptureGold) : 0;
            if (gain > 0)
                StartCoroutine(SpawnGoldEffect(other.transform.position, isWhiteTurn ? 0 : 1, gain));

            StartCoroutine(DestroyAfterAnim(other));
        }
        units[x, y] = un;
        units[previousPosition.x, previousPosition.y] = null;
        PositionSingle(x, y, false, previousPosition.x, previousPosition.y);
        
        if (un.color == 0 && un.hasCrown && y == TILE_COUNT_Y - 1)
        {
            win = 0;
            StartCoroutine(WriteStateText("백 팀 승리!", 10f, true));
            return true;
        }
        else if (un.color == 1 && un.hasCrown && y == 0)
        {
            win = 1;
            StartCoroutine(WriteStateText("흑 팀 승리!", 10f, true));
            return true;
        }

        if (!isTargetMode)
        {
            StartCoroutine(TurnChange(hasEnemy ? 1.2f : 0.9f));
        }
        UpdateCheckState();
        return true;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }
    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }

    private IEnumerator DestroyAfterAnim(Units unit, bool prom = false, UnitType type = UnitType.None)
    {
        Animator anim = unit.GetComponent<Animator>();
        if (prom)
        {
            anim.SetTrigger("Promote");
        }
        else
        {
            if (unit.currentBubble != null)
                unit.currentBubble.SetActive(false);
            anim.SetTrigger("Die");
        }
        yield return null;
        float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);
        if (prom)
        {
            int x = unit.currentX;
            int y = unit.currentY;
            Vector2Int previousPosition = unit.previousPosition;

            int color = unit.color;
            int arrestScore = unit.isArrested;
            Destroy(unit.gameObject);

            Units newUnit = SpawnSingleUnit(type, color);
            units[x, y] = newUnit;
            newUnit.isArrested = arrestScore;
            newUnit.UpdateState();
            PositionSingle(x, y, true, previousPosition.x, previousPosition.y);
            yield return null;
            newUnit.GetComponent<Animator>().SetTrigger("Create");
        }
        else
        {
            Destroy(unit.gameObject);
        }
        UpdateCheckState();
    }

    private IEnumerator PauseAnimation(Animator anim)
    {
        yield return new WaitForSeconds(0.1f);
        anim.speed = 0f;
        yield return new WaitForSeconds(0.35f);
        anim.speed = 1f;
    }
    private IEnumerator TurnChange(float delay)
    {
        IsChangingTurn = true;
        yield return new WaitForSeconds(delay);
        turnChanger.SetActive(true);
        turnChanger.GetComponent<Animator>().SetTrigger("change");
        yield return new WaitForSeconds(1.2f);
        isWhiteTurn = !isWhiteTurn;
        UpdateCursor();
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (units[x, y] == null) continue;
                if (units[x, y].isArrested >= 0)
                    units[x, y].isArrested--;
                units[x, y].UpdateState();
            }
        turnChanger.SetActive(false);
        IsChangingTurn = false;
    }

    private IEnumerator SpawnGoldEffect(Vector3 startWorldPos, int playerIndex, int amount)
    {
        if (gold == null) yield break;
        RectTransform targetRect = playerIndex == 0 ? p1GoldUI : p2GoldUI;
        if (targetRect == null) yield break;

        Vector3 targetWorld = targetRect.position;
        targetWorld.z = -0.111f;

        int pieces = Mathf.Clamp(amount, 1, maxGoldParticles);
        Vector3 start = startWorldPos;

        for (int i = 0; i < pieces; i++)
        {
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-goldSpawnSpread, goldSpawnSpread),
                UnityEngine.Random.Range(-goldSpawnSpread, goldSpawnSpread),
                0f);
            GameObject instance = Instantiate(gold, transform);
            instance.transform.position = start + offset;

            StartCoroutine(MoveSingleGold(instance.transform, start + offset, targetWorld, i * goldStaggerDelay));
        }

        yield return new WaitForSeconds(goldMoveDuration + pieces * goldStaggerDelay);
    }

    private IEnumerator MoveSingleGold(Transform tr, Vector3 from, Vector3 to, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < goldMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / goldMoveDuration);
            float eval = goldMoveCurve != null ? goldMoveCurve.Evaluate(t) : t;

            tr.position = Vector3.Lerp(from, to, eval);
            yield return null;
        }

        Destroy(tr.gameObject);
    }
    public IEnumerator WriteStateText(string text, float duration = 2f, bool isWin = false)
    {
        statePanel.SetActive(true);
        stateText.text = text;
        yield return new WaitForSeconds(duration);
        stateText.text = "";
        statePanel.SetActive(false);
        if (isWin)
        {
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Items
    public void Call(bool isWhite)
    {
        unitsToPlace.Add(UnitType.Pawn);
        isSummonMode = true;
        summonColor = isWhite ? 0 : 1;
        ShowSummonPlate(summonColor);
    }
    public void TargetItem(bool isWhite, bool targetEnemy = true, List<UnitType> filter = null, bool targetAll = false, Func<Units, bool> condition = null, ItemData item = null, int slotIndex = -1, Inventory inventory = null)
    {
        isTargetMode = true;
        targetColor = isWhite ? 0 : 1;
        this.targetEnemy = targetEnemy;
        this.targetAll = targetAll;
        this.targetFilter = filter;
        this.targetCondition = condition;
        this.pendingItem = item;
        this.pendingSlotIndex = slotIndex;
        this.pendingInventory = inventory;
        ShowTargetPlate(targetColor, targetEnemy, filter, targetAll, condition);
    }
    public void PromoteUnit(Units unit, UnitType type)
    {
        StartCoroutine(DestroyAfterAnim(unit, true, type));
    }

}
