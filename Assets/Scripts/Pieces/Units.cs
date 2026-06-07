using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;

public class Units : MonoBehaviour
{

    [Header("Unit Info")]
    public int color;
    public int currentX;
    public int currentY;
    public Vector2Int previousPosition = -Vector2Int.one;
    public int bonusCaptureGold = 0;
    public bool poop = false;
    public int isArrested = -1;
    public bool hasCrown = false;

    public UnitData data;

    private GameObject currentBorder;
    public GameObject currentBubble;

    protected Vector3 desiredPosition;
    protected Vector3 desiredScale = Vector3.one;

    private float itemEfPosY;

    [Header("Animation")]
    private Animator animator;

    public List<Vector2Int> GetAvailableMoves(ref Units[,] un, int tileCountX, int tileCountY)
    {
        return data.MoveType switch
        {
            MoveType.Slide => GetSlideMoves(ref un, tileCountX, tileCountY),
            MoveType.Step => GetStepMoves(ref un, tileCountX, tileCountY),
            MoveType.Pawn => GetPawnMoves(ref un, tileCountX, tileCountY),
            _ => new List<Vector2Int>()
        };
    }

    // Bishop, Rook, Queen
    private List<Vector2Int> GetSlideMoves(ref Units[,] un, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        foreach (var offset in data.MoveOffsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            while (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                if (un[x, y] == null)
                {
                    r.Add(new Vector2Int(x, y));
                }
                else
                {
                    if (un[x, y].color != color)
                        r.Add(new Vector2Int(x, y));
                    break;
                }
                x += offset.x;
                y += offset.y;
            }
        }
        return r;
    }

    // Knight, King
    private List<Vector2Int> GetStepMoves(ref Units[,] un, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        foreach (var offset in data.MoveOffsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
                if (un[x, y] == null || un[x, y].color != color)
                {
                    if (data.Type == UnitType.King && IsTileUnderAttack(x, y, ref un, tileCountX, tileCountY))
                        continue;
                    r.Add(new Vector2Int(x, y));
                }
        }
        return r;
    }

    private bool IsTileUnderAttack(int x, int y, ref Units[,] un, int tileCountX, int tileCountY)
    {
        for (int tx = 0; tx < tileCountX; tx++)
            for (int ty = 0; ty < tileCountY; ty++)
            {
                if (un[tx, ty] == null) continue;
                if (un[tx, ty].color == color) continue;
                if (un[tx, ty].isArrested >= 0) continue;
                if (un[tx, ty].data.Type == UnitType.King)
                {
                    int dx = Mathf.Abs(tx - x);
                    int dy = Mathf.Abs(ty - y);
                    if (dx <= 1 && dy <= 1) return true;
                    continue;
                }

                List<Vector2Int> moves = un[tx, ty].GetAvailableMoves(ref un, tileCountX, tileCountY);
                foreach (var move in moves)
                    if (move.x == x && move.y == y) return true;
            }
        return false;
    }

    // Pawn
    private List<Vector2Int> GetPawnMoves(ref Units[,] un, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        int dir = color == 0 ? 1 : -1;

        foreach (var offset in data.MoveOffsets)
        {
            int x = currentX + offset.x;
            int y = currentY + (offset.y * dir);

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
                if (un[x, y] == null)
                    r.Add(new Vector2Int(x, y));
        }

        foreach (var offset in data.AttackOffsets)
        {
            int x = currentX + offset.x;
            int y = currentY + (offset.y * dir);

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
                if (un[x, y] != null && un[x, y].color != color)
                    r.Add(new Vector2Int(x, y));
        }
        return r;
    }

    void Awake()
    {
        desiredScale = new Vector3(1f, 1f, 1f);
        animator = GetComponent<Animator>();
        itemEfPosY = data.Type switch
        {
            UnitType.Pawn => 0.383f,
            UnitType.Knight => 0.328f,
            UnitType.Bishop => 0.3125f,
            UnitType.Rook => 0.3516f,
            UnitType.Queen => 0.3125f,
            UnitType.King => 0.3125f,
            _ => 0f
        };
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }
    public void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }

    private void LateUpdate()
    {
        bool isIdle = transform.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle");
        if (isIdle)
        {
            if (color == 0)
                GetComponent<SpriteRenderer>().sprite = data.WhiteSprite;
            else if (color == 1)
                GetComponent<SpriteRenderer>().sprite = data.BlackSprite;
        }
        if (currentBorder != null)
        {
            currentBorder.SetActive(isIdle);
        }
    }

    public void UpdateState()
    {
        if (currentBorder != null) Destroy(currentBorder);
        if (currentBubble != null) Destroy(currentBubble);

        // Reward
        if (bonusCaptureGold > 0)
        {
            SpawnBorder(new Color32(209, 161, 50, 255));
            SpawnBubble(0);
            return;
        }

        // Poop
        if (poop)
        {
            SpawnBorder(new Color32(114, 53, 133, 255));
            SpawnBubble(1);
            return;
        }

        // Arrest
        if (isArrested >= 0)
        {
            SpawnBorder(new Color32(130, 60, 60, 255));
            SpawnBubble(2);
            return;
        }

        // Crown
        if (hasCrown)
        {
            SpawnBorder(new Color32(127, 66, 77, 255));
            SpawnBubble(3);
            return;
        }
    }

    private void SpawnBorder(Color32 color)
    {
        currentBorder = new GameObject("Border");
        currentBorder.transform.parent = transform;
        currentBorder.transform.localPosition = new Vector3(0f, itemEfPosY, 0.001f);
        currentBorder.transform.localScale = Vector3.one * 1f;
        SpriteRenderer sr = currentBorder.AddComponent<SpriteRenderer>();
        sr.sprite = data.BackgroundSprite;
        sr.color = color;
    }

    private void SpawnBubble(int index)
    {
        currentBubble = new GameObject("Bubble");
        currentBubble.transform.parent = transform;
        currentBubble.transform.localPosition = new Vector3(0.5f, 0.89f, 0.002f);
        currentBubble.transform.localScale = Vector3.one;
        SpriteRenderer sr = currentBubble.AddComponent<SpriteRenderer>();
        sr.sprite = Control.Instance.itemEfSprite[index];
    }
}