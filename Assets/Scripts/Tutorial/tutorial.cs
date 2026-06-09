using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class tutorial : MonoBehaviour
{
    private enum TutorialStep
    {
        Intro,
        SelectPiece,
        MovePiece,
        TurnChangeNotice,
        CaptureEnemySelect,
        CaptureEnemyTarget,
        BuyItem,
        UseItem,
        UseItemTarget,
        UseItemDone,
        CaptureKingSelect,
        CaptureKingTarget,
        Complete
    }

    private enum TutorialPieceType
    {
        Pawn,
        King,
        Queen
    }

    private sealed class TutorialPiece
    {
        public TutorialPieceType type;
        public bool white;
        public Vector2Int board;
        public GameObject root;
        public SpriteRenderer baseRenderer;
        public UnitData data;
        public Animator animator;
        public SpriteRenderer ringRenderer;
        public GameObject arrestIcon;
        public bool bound;
        public int defaultSortingOrder;
    }

    private const int BoardSize = 8;
    private const float TileSizeX = 0.982f;
    private const float TileSizeY = 0.962f;
    private static readonly Vector3 BoardCenter = new Vector3(-0.025f, 0.78f, 0f);
    private static readonly Vector3 BoardRootPosition = new Vector3(0f, -0.4f, 0f);
    private static readonly Vector3 BoardRootScale = new Vector3(0.7f, 0.7f, 0f);

    private readonly Dictionary<Vector2Int, TutorialPiece> pieces = new Dictionary<Vector2Int, TutorialPiece>();
    private readonly List<GameObject> highlights = new List<GameObject>();
    private readonly Color purple = new Color(0.66f, 0.22f, 1f, 0.86f);
    private readonly Color movePlateGray = new Color(0.45f, 0.45f, 0.45f, 0.78f);
    private readonly Color attackPurple = new Color(1f, 0.08f, 0.48f, 0.52f);
    private readonly Color boundRed = new Color(0.46f, 0.04f, 0.09f, 1f);

    private TutorialStep step;
    private TMP_FontAsset hbiosFont;
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Sprite arrestSprite;
    private Sprite arrestItemSprite;
    private Sprite nemoSprite;
    private Sprite goldSprite;
    private UnitData pawnData;
    private UnitData kingData;
    private UnitData queenData;
    private GameObject pawnPrefab;
    private GameObject kingPrefab;
    private GameObject queenPrefab;
    private GameObject movePlatePrefab;
    private RuntimeAnimatorController pawnAnimatorController;
    private RuntimeAnimatorController kingAnimatorController;
    private RuntimeAnimatorController queenAnimatorController;

    private Camera mainCamera;
    private Transform boardRoot;
    private Transform pieceRoot;
    private Transform highlightRoot;
    private Canvas canvas;
    private TextMeshProUGUI tutorialText;
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI goldText;
    private Button nextButton;
    private Button itemSlot;
    private Image itemSlotImage;
    private GameObject itemSlotBorder;
    private Button shopItemSlot;
    private Image shopItemImage;
    private GameObject shopItemBorder;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;

    private TutorialPiece selectedPiece;
    private TutorialPiece tutorialPawn;
    private TutorialPiece capturePawn;
    private TutorialPiece itemTarget;
    private TutorialPiece kingTarget;
    private TutorialPiece draggingPiece;
    private Vector3 dragStartLocalPosition;
    private Vector2Int moveTarget = new Vector2Int(2, 4);
    private int gold;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "Tutorial_Scene") return;
        if (FindFirstObjectByType<tutorial>() != null) return;

        new GameObject("tutorial").AddComponent<tutorial>();
    }

    private void Awake()
    {
        LoadAssets();
        SetupCamera();
        SetupEventSystem();
        SetupWorld();
        SetupUI();
    }

    private void Start()
    {
        EnterIntro();
    }

    private void Update()
    {
        PulseHighlights();
    }

    private void LoadAssets()
    {
        hbiosFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/HBIOS-SYS SDF");
        PrepareTutorialFont();
        squareSprite = CreateSolidSprite("TutorialSquare", 16, 16, Color.white);
        circleSprite = CreateCircleSprite("TutorialCircle", 64, Color.white);
        arrestSprite = LoadSpriteFromResources("Tutorial/ef_arrest");
        arrestItemSprite = LoadSpriteFromResources("Tutorial/item_arrest");
        LoadPieceSprites();
    }

    private void LoadPieceSprites()
    {
#if UNITY_EDITOR
        pawnData = AssetDatabase.LoadAssetAtPath<UnitData>("Assets/Scripts/Pieces/data/Pawn.asset");
        kingData = AssetDatabase.LoadAssetAtPath<UnitData>("Assets/Scripts/Pieces/data/King.asset");
        queenData = AssetDatabase.LoadAssetAtPath<UnitData>("Assets/Scripts/Pieces/data/Queen.asset");
        pawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChessDeffense/Prefabs/Pawn.prefab");
        kingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChessDeffense/Prefabs/King.prefab");
        queenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChessDeffense/Prefabs/Queen.prefab");
        movePlatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChessDeffense/Prefabs/MovePlate.prefab");
        arrestItemSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ChessDeffense/Sprite/UI/item/item_arrest.png") ?? arrestItemSprite;
        nemoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ChessDeffense/Sprite/UI/nemo1.png");
        goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ChessDeffense/Sprite/UI/gold.png");
        pawnAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ChessDeffense/Animation/_controller/Pawn Anim Controller.controller");
        kingAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ChessDeffense/Animation/_controller/King Anim Controller.controller");
        queenAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ChessDeffense/Animation/_controller/Queen Anim Controller.controller");
#endif
    }

    private void PrepareTutorialFont()
    {
        if (hbiosFont == null) return;

        hbiosFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        hbiosFont.isMultiAtlasTexturesEnabled = true;

        const string tutorialGlyphs =
            "이 게임은 유령 체스말을 움직여 상대 킹을 쓰러뜨리는 2인 턴제 전략 게임입니다." +
            "자신의 턴에는 말 하나를 움직이거나 아이템 하나를 사용할 수 있습니다." +
            "시작하기 다음 Player Turn Tutorial Start Complete Gold" +
            "빛나는 아군 말을 선택하세요 보라색 칸은 이동할 수 있는 위치입니다 표시된 칸을 클릭해 이동하세요" +
            "말을 움직이면 턴이 상대에게 넘어갑니다 이제 상대 플레이어의 차례입니다" +
            "붉은 칸에는 공격 가능한 상대 말이 있습니다 잡아보세요 골드 획득" +
            "아이템은 내 턴에 사용할 수 있습니다 이번에는 속박의 사슬을 사용해보세요" +
            "빛나는 상대 말을 선택하세요 선택한 말은 다음 턴에 움직일 수 없습니다" +
            "상대 말이 속박되었습니다 자금이 되는 만큼 자유롭게 사용할 수 있고 자신의 기물을 움직이면 턴이 끝납니다" +
            "다른 아이템의 효과는 아이템 위에 마우스를 올려 확인할 수 있습니다" +
            "기물을 죽인 골드로 아이템을 구매할 수 있습니다 하지만 상점은 공동으로 사용합니다 구매해 선점하거나 상대를 방해하세요" +
            "킹은 가장 중요한 말입니다 상대 킹을 잡으면 승리합니다 공격하세요" +
            "튜토리얼 완료 이제 말의 이동과 아이템을 활용해 상대 킹을 노려보세요";

        hbiosFont.TryAddCharacters(tutorialGlyphs, out _);
    }

    private static Sprite LoadSpriteFromResources(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateSolidSprite(string name, int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite CreateCircleSprite(string name, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        float inner = size * 0.36f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float alpha = d <= radius && d >= inner ? 1f : d < inner ? 0.16f : 0f;
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
            }
        }

        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera != null) return;

        GameObject cameraObject = new GameObject("Main Camera");
        mainCamera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
    }

    private void SetupEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void SetupWorld()
    {
        boardRoot = new GameObject("TutorialBoard").transform;
        pieceRoot = new GameObject("TutorialPieces").transform;
        highlightRoot = new GameObject("HighlightObject").transform;
        ApplyBoardRootTransform(boardRoot);
        ApplyBoardRootTransform(pieceRoot);
        ApplyBoardRootTransform(highlightRoot);

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                GameObject tile = new GameObject($"Tile {x},{y}");
                tile.transform.SetParent(boardRoot);
                tile.transform.localPosition = BoardToWorld(new Vector2Int(x, y));
                tile.transform.localScale = new Vector3(TileSizeX, TileSizeY, 1f);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = (x + y) % 2 == 0 ? new Color(0.22f, 0.20f, 0.26f) : new Color(0.13f, 0.12f, 0.18f);
                sr.sortingOrder = -4;

                BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                TutorialTile tileClick = tile.AddComponent<TutorialTile>();
                tileClick.Init(this, new Vector2Int(x, y));
            }
        }
    }

    private static void ApplyBoardRootTransform(Transform root)
    {
        root.position = BoardRootPosition;
        root.localScale = BoardRootScale;
    }

    private void SetupUI()
    {
        GameObject canvasObject = new GameObject("TutorialCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        turnText = CreateText("TurnText", canvas.transform, "Player 1 Turn", 46, TextAlignmentOptions.Center);
        SetRect(turnText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(520f, 72f));

        goldText = CreateText("GoldText", canvas.transform, "0", 36, TextAlignmentOptions.Center);
        goldText.color = Color.white;
        SetRect(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(212f, -177f), new Vector2(96f, 54f));

        GameObject panel = CreatePanel("TutorialPanel", canvas.transform, new Color(0.05f, 0.04f, 0.07f, 0.92f));
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(1260f, 220f));

        tutorialText = CreateText("TutorialText", panel.transform, "", 26, TextAlignmentOptions.Center);
        tutorialText.lineSpacing = 40f;
        tutorialText.fontSizeMin = 20f;
        SetRect(tutorialText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-145f, 14f), new Vector2(960f, 172f));

        nextButton = CreateButton("NextButton", panel.transform, "다음", 32);
        SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, -48f), new Vector2(210f, 72f));
        nextButton.onClick.AddListener(OnNextButton);

        itemSlot = CreateButton("ItemSlot", canvas.transform, "", 24);
        SetRect(itemSlot.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(245f, 0f), new Vector2(82f, 82f));
        itemSlotImage = itemSlot.GetComponent<Image>();
        itemSlotImage.sprite = arrestItemSprite;
        itemSlotImage.preserveAspect = true;
        itemSlotImage.color = Color.white;
        SetButtonGraphicColors(itemSlot, Color.white, new Color(1f, 1f, 1f, 0.92f), new Color(0.9f, 0.82f, 1f, 1f));
        itemSlot.onClick.AddListener(OnItemSlotClicked);
        itemSlotBorder = CreateItemSlotBorder(itemSlot.transform);

        TutorialTooltipTrigger trigger = itemSlot.gameObject.AddComponent<TutorialTooltipTrigger>();
        trigger.Init(this);
        TutorialItemClick itemClick = itemSlot.gameObject.AddComponent<TutorialItemClick>();
        itemClick.Init(this);

        shopItemSlot = CreateButton("ShopItemSlot", canvas.transform, "", 24);
        SetRect(shopItemSlot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-199f, -185f), new Vector2(82f, 82f));
        shopItemImage = shopItemSlot.GetComponent<Image>();
        shopItemImage.sprite = arrestItemSprite;
        shopItemImage.preserveAspect = true;
        shopItemImage.color = Color.white;
        SetButtonGraphicColors(shopItemSlot, Color.white, new Color(1f, 1f, 1f, 0.92f), new Color(0.9f, 0.82f, 1f, 1f));
        shopItemSlot.onClick.AddListener(OnShopItemClicked);
        shopItemBorder = CreateItemSlotBorder(shopItemSlot.transform);

        tooltipPanel = CreatePanel("ItemTooltip", canvas.transform, new Color(0.04f, 0.035f, 0.06f, 0.96f));
        SetRect(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(520f, 180f), new Vector2(470f, 150f));
        Image tooltipImage = tooltipPanel.GetComponent<Image>();
        if (nemoSprite != null)
        {
            tooltipImage.sprite = nemoSprite;
            tooltipImage.type = Image.Type.Simple;
            tooltipImage.color = Color.white;
        }
        tooltipImage.raycastTarget = false;
        tooltipText = CreateText("TooltipText", tooltipPanel.transform, "속박의 사슬\n상대 말 하나를 1턴 동안 움직이지 못하게 합니다.", 25, TextAlignmentOptions.Left);
        SetRect(tooltipText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 120f));
        tooltipText.raycastTarget = false;

        itemSlot.gameObject.SetActive(false);
        shopItemSlot.gameObject.SetActive(false);
        tooltipPanel.SetActive(false);
    }

    private static void SetButtonGraphicColors(Button button, Color normal, Color highlighted, Color pressed)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        button.colors = colors;
    }

    private GameObject CreateItemSlotBorder(Transform parent)
    {
        GameObject border = new GameObject("ItemSlotBorder");
        border.transform.SetParent(parent, false);
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(98f, 98f);
        AddBorderLine(border.transform, "Top", new Vector2(0f, 47f), new Vector2(98f, 5f));
        AddBorderLine(border.transform, "Bottom", new Vector2(0f, -47f), new Vector2(98f, 5f));
        AddBorderLine(border.transform, "Left", new Vector2(-47f, 0f), new Vector2(5f, 98f));
        AddBorderLine(border.transform, "Right", new Vector2(47f, 0f), new Vector2(5f, 98f));
        border.SetActive(false);
        return border;
    }

    private void AddBorderLine(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent, false);
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = line.AddComponent<Image>();
        image.sprite = squareSprite;
        image.color = new Color(0.92f, 0.56f, 1f, 0.92f);
        image.raycastTarget = false;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.sprite = squareSprite;
        image.color = color;
        return panel;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = hbiosFont;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = size;
        tmp.fontSizeMin = Mathf.Max(16f, size * 0.58f);
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.characterSpacing = 0f;
        tmp.wordSpacing = 0f;
        tmp.lineSpacing = 40f;
        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, float labelSize)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = squareSprite;
        image.color = new Color(0.42f, 0.18f, 0.64f, 0.94f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.62f, 0.31f, 0.86f, 1f);
        colors.pressedColor = new Color(0.30f, 0.11f, 0.46f, 1f);
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = CreateText($"{name}Text", buttonObject.transform, label, labelSize, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 60f));
        }

        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void EnterIntro()
    {
        ClearHighlights();
        ClearPieces();
        itemSlot.gameObject.SetActive(false);
        shopItemSlot.gameObject.SetActive(false);
        tooltipPanel.SetActive(false);
        step = TutorialStep.Intro;
        turnText.text = "Tutorial Start";
        SetTutorialText("이 게임은 유령 체스말을 움직여\n상대 킹을 쓰러뜨리는 2인 턴제 전략 게임입니다.\n자신의 턴에는 말 하나를 움직이거나\n아이템 하나를 사용할 수 있습니다.");
        SetButton(true, "시작하기");
    }

    private void EnterSelectPiece()
    {
        ClearPieces();
        gold = 0;
        UpdateGold();
        SpawnOpeningBoard();
        step = TutorialStep.SelectPiece;
        turnText.text = "Player 1 Turn";
        SetTutorialText("자신의 턴입니다.\n빛나는 아군 말을 선택하세요.");
        SetButton(false);
        HighlightPieceAfterDelay(tutorialPawn, purple, TutorialStep.SelectPiece);
    }

    private void EnterMovePiece()
    {
        step = TutorialStep.MovePiece;
        SetTutorialText("보라색 칸은 이동할 수 있는 위치입니다.\n표시된 칸을 클릭해 말을 이동하세요.");
        ClearHighlights();
        HighlightTile(moveTarget, movePlateGray);
        HighlightPiece(tutorialPawn, purple);
    }

    private void EnterTurnChangeNotice()
    {
        step = TutorialStep.TurnChangeNotice;
        ClearHighlights();
        StartCoroutine(ShowTurnChange());
        SetTutorialText("말을 움직이면 턴이 상대에게 넘어갑니다.\n이제 상대 플레이어의 차례입니다.");
        SetButton(true, "다음");
    }

    private IEnumerator ShowTurnChange()
    {
        turnText.text = "Player 1 Turn";
        yield return new WaitForSeconds(0.35f);
        turnText.text = "Player 2 Turn";
    }

    private void EnterCaptureEnemy()
    {
        ClearPieces();
        SpawnCaptureBoard();
        step = TutorialStep.CaptureEnemySelect;
        turnText.text = "Player 1 Turn";
        SetTutorialText("붉은 칸에는 공격 가능한 상대 말이 있습니다.\n상대 말을 클릭해 잡아보세요.");
        SetButton(false);
        HighlightPieceAfterDelay(tutorialPawn, purple, TutorialStep.CaptureEnemySelect);
        HighlightTile(capturePawn.board, attackPurple);
    }

    private IEnumerator EnterUseItemAfterGold()
    {
        AddGold(3);
        yield return new WaitForSeconds(1f);
        EnterBuyItem();
    }

    private void EnterBuyItem()
    {
        ClearHighlights();
        step = TutorialStep.BuyItem;
        SetTutorialText("기물을 죽인 골드로, 아이템을 구매할 수 있습니다.\n상점은 공동으로 사용합니다.\n아이템을 구매해 선점하거나 상대를 방해하세요.\n아이템은 자신의 턴에\n자금이 되는 만큼 자유롭게 구매, 사용 가능합니다.");
        SetButton(false);
        itemSlot.gameObject.SetActive(false);
        shopItemSlot.gameObject.SetActive(true);
        HighlightShopItemSlot(true);
    }

    private void EnterUseItem()
    {
        ClearHighlights();
        if (itemTarget == null)
        {
            itemTarget = SpawnPiece(TutorialPieceType.Pawn, false, new Vector2Int(4, 4));
        }

        step = TutorialStep.UseItem;
        SetTutorialText("아이템은 내 턴에 사용할 수 있습니다.\n이번에는 속박의 사슬을 사용해보세요.");
        SetButton(false);
        itemSlot.gameObject.SetActive(true);
        shopItemSlot.gameObject.SetActive(false);
        HighlightItemSlot(true);
    }

    private void EnterUseItemTarget()
    {
        step = TutorialStep.UseItemTarget;
        ClearHighlights();
        tooltipPanel.SetActive(false);
        HighlightItemSlot(false);
        SetTutorialText("빛나는 상대 말을 선택하세요.\n선택한 말은 다음 턴에 움직일 수 없습니다.");
        HighlightPiece(itemTarget, purple);
    }

    private void EnterUseItemDone()
    {
        step = TutorialStep.UseItemDone;
        ClearHighlights();
        SetTutorialText("상대 말이 속박되었습니다.\n아이템을 사용하고 자신의 기물을 움직이면 턴이 끝납니다.\n다른 아이템의 효과는 아이템 위에 마우스를 올려 확인할 수 있습니다.");
        SetButton(true, "다음");
    }

    private void EnterCaptureKing()
    {
        ClearPieces();
        SpawnKingCaptureBoard();
        itemSlot.gameObject.SetActive(false);
        shopItemSlot.gameObject.SetActive(false);
        tooltipPanel.SetActive(false);
        step = TutorialStep.CaptureKingSelect;
        turnText.text = "Player 1 Turn";
        SetTutorialText("킹은 가장 중요한 말입니다.\n상대 킹을 잡으면 승리합니다.\n상대 킹을 공격하세요.");
        SetButton(false);
        HighlightPieceAfterDelay(tutorialPawn, purple, TutorialStep.CaptureKingSelect);
        HighlightTile(kingTarget.board, attackPurple);
    }

    private void EnterComplete()
    {
        step = TutorialStep.Complete;
        ClearHighlights();
        SetTutorialText("튜토리얼 완료!\n이제 말의 이동과 아이템을 활용해 상대 킹을 노려보세요.");
        SetButton(true, "게임 시작");
        turnText.text = "Tutorial Complete";
    }

    private void SpawnOpeningBoard()
    {
        tutorialPawn = SpawnPiece(TutorialPieceType.Pawn, true, new Vector2Int(2, 3));
        SpawnPiece(TutorialPieceType.King, true, new Vector2Int(3, 1));
        SpawnPiece(TutorialPieceType.Pawn, false, new Vector2Int(3, 4));
        SpawnPiece(TutorialPieceType.King, false, new Vector2Int(3, 5));
    }

    private void SpawnCaptureBoard()
    {
        tutorialPawn = SpawnPiece(TutorialPieceType.Pawn, true, new Vector2Int(2, 3));
        SpawnPiece(TutorialPieceType.King, true, new Vector2Int(3, 1));
        capturePawn = SpawnPiece(TutorialPieceType.Pawn, false, new Vector2Int(3, 4));
        itemTarget = SpawnPiece(TutorialPieceType.Pawn, false, new Vector2Int(4, 4));
        SpawnPiece(TutorialPieceType.King, false, new Vector2Int(3, 5));
    }

    private void SpawnKingCaptureBoard()
    {
        tutorialPawn = SpawnPiece(TutorialPieceType.Queen, true, new Vector2Int(2, 4));
        SpawnPiece(TutorialPieceType.King, true, new Vector2Int(3, 1));
        kingTarget = SpawnPiece(TutorialPieceType.King, false, new Vector2Int(3, 5));
    }

    private TutorialPiece SpawnPiece(TutorialPieceType type, bool white, Vector2Int board)
    {
        GameObject prefab = GetPiecePrefab(type);
        GameObject root = prefab != null ? Instantiate(prefab, pieceRoot) : new GameObject($"{(white ? "P1" : "P2")} {type}");
        root.name = $"{(white ? "P1" : "P2")} {type}";
        if (prefab == null) root.transform.SetParent(pieceRoot);
        root.transform.localPosition = GetPieceCenter(board);
        root.transform.localScale = Vector3.one;

        UnitData data = GetPieceData(type);
        Units unit = root.GetComponent<Units>();
        if (data == null && unit != null) data = unit.data;
        if (unit != null)
        {
            unit.color = white ? 0 : 1;
            unit.currentX = board.x;
            unit.currentY = board.y;
            if (data != null) unit.data = data;
            unit.enabled = false;
        }

        SpriteRenderer baseRenderer = root.GetComponent<SpriteRenderer>();
        if (baseRenderer == null) baseRenderer = root.AddComponent<SpriteRenderer>();
        Sprite pieceSprite = data != null ? (white ? data.WhiteSprite : data.BlackSprite) : null;
        baseRenderer.sprite = pieceSprite != null ? pieceSprite : circleSprite;
        baseRenderer.color = pieceSprite != null ? Color.white : (white ? new Color(0.86f, 0.82f, 1f) : new Color(0.38f, 0.22f, 0.54f));
        baseRenderer.sortingOrder = 4;

        Animator animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        if (animator.runtimeAnimatorController == null)
            animator.runtimeAnimatorController = GetAnimatorController(type);
        if (animator.runtimeAnimatorController != null)
        {
            SetAnimatorInteger(animator, "color", white ? 0 : 1);
            SetAnimatorTrigger(animator, "Place");
        }

        CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
        if (collider == null) collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.86f;

        TutorialPieceClick click = root.AddComponent<TutorialPieceClick>();

        TutorialPiece piece = new TutorialPiece
        {
            type = type,
            white = white,
            board = board,
            root = root,
            baseRenderer = baseRenderer,
            data = data,
            animator = animator,
            defaultSortingOrder = baseRenderer.sortingOrder
        };
        click.Init(this, piece);
        pieces[board] = piece;
        return piece;
    }

    private GameObject GetPiecePrefab(TutorialPieceType type)
    {
        return type switch
        {
            TutorialPieceType.Pawn => pawnPrefab,
            TutorialPieceType.King => kingPrefab,
            TutorialPieceType.Queen => queenPrefab,
            _ => null
        };
    }

    private UnitData GetPieceData(TutorialPieceType type)
    {
        return type switch
        {
            TutorialPieceType.Pawn => pawnData,
            TutorialPieceType.King => kingData,
            TutorialPieceType.Queen => queenData,
            _ => null
        };
    }

    private RuntimeAnimatorController GetAnimatorController(TutorialPieceType type)
    {
        return type switch
        {
            TutorialPieceType.Pawn => pawnAnimatorController,
            TutorialPieceType.King => kingAnimatorController,
            TutorialPieceType.Queen => queenAnimatorController,
            _ => null
        };
    }

    private void MovePiece(TutorialPiece piece, Vector2Int target)
    {
        pieces.Remove(piece.board);
        piece.board = target;
        pieces[target] = piece;
        SetAnimatorTrigger(piece.animator, "move");
        StartCoroutine(MovePieceRoutine(piece, GetPieceCenter(target)));
    }

    private IEnumerator MovePieceRoutine(TutorialPiece piece, Vector3 target)
    {
        Vector3 start = piece.root.transform.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            piece.root.transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        piece.root.transform.localPosition = target;
    }

    private void CapturePiece(TutorialPiece attacker, TutorialPiece target, int rewardGold = 0)
    {
        StartCoroutine(CapturePieceRoutine(attacker, target, rewardGold));
    }

    private IEnumerator CapturePieceRoutine(TutorialPiece attacker, TutorialPiece target, int rewardGold)
    {
        if (attacker == null || target == null) yield break;

        Vector2Int targetBoard = target.board;
        Vector3 targetWorldPosition = target.root != null ? target.root.transform.position : BoardToWorld(target.board);
        pieces.Remove(target.board);

        if (attacker.animator != null && attacker.animator.runtimeAnimatorController != null)
            SetAnimatorTrigger(attacker.animator, "move");

        if (target.animator != null && target.animator.runtimeAnimatorController != null)
        {
            SetAnimatorInteger(target.animator, "color", 1);
            SetAnimatorTrigger(target.animator, "Die");
        }

        SpawnSmoke(target.root.transform.position);
        if (rewardGold > 0) StartCoroutine(FlyGoldToUi(targetWorldPosition, rewardGold));
        yield return new WaitForSeconds(0.55f);

        if (target.root != null) Destroy(target.root);
        MovePiece(attacker, targetBoard);
    }

    private void ApplyArrest(TutorialPiece target)
    {
        target.bound = true;
        SpawnPieceOutline(target, boundRed, "ArrestBorder", false);
        if (target.arrestIcon != null) Destroy(target.arrestIcon);

        GameObject icon = new GameObject("ef_arrest");
        icon.transform.SetParent(target.root.transform);
        icon.transform.localPosition = new Vector3(0f, 0.78f, -0.04f);
        icon.transform.localScale = Vector3.one * 0.85f;
        SpriteRenderer sr = icon.AddComponent<SpriteRenderer>();
        sr.sprite = arrestSprite != null ? arrestSprite : circleSprite;
        sr.color = Color.white;
        sr.sortingOrder = 10;
        target.arrestIcon = icon;
    }

    private void SpawnSmoke(Vector3 position)
    {
        StartCoroutine(SmokeRoutine(position));
    }

    private IEnumerator SmokeRoutine(Vector3 position)
    {
        GameObject smoke = new GameObject("CaptureSoulEffect");
        smoke.transform.position = position + new Vector3(0f, 0f, -0.8f);
        SpriteRenderer sr = smoke.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(0.72f, 0.22f, 1f, 0.62f);
        sr.sortingOrder = 20;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.8f;
            smoke.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.8f, t);
            sr.color = new Color(0.72f, 0.22f, 1f, Mathf.Lerp(0.62f, 0f, t));
            yield return null;
        }
        Destroy(smoke);
    }

    private void AddGold(int amount)
    {
        gold += amount;
        UpdateGold();
        StartCoroutine(GoldPopup(amount));
    }

    private void UpdateGold()
    {
        goldText.text = $"{gold}";
        goldText.color = Color.white;
    }

    private IEnumerator GoldPopup(int amount)
    {
        goldText.color = new Color(1f, 0.82f, 0.2f);
        goldText.text = $"+{amount}  {gold}";
        yield return new WaitForSeconds(1f);
        goldText.color = Color.white;
        UpdateGold();
    }

    private IEnumerator FlyGoldToUi(Vector3 startWorldPosition, int amount)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 start = WorldToCanvasPoint(startWorldPosition);
        Vector2 end = ScreenToCanvasPoint(RectTransformUtility.WorldToScreenPoint(null, goldText.rectTransform.position));

        GameObject flyObject = new GameObject("GoldFly");
        flyObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = flyObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(110f, 110f);

        CanvasGroup group = flyObject.AddComponent<CanvasGroup>();

        Image coin = flyObject.AddComponent<Image>();
        coin.sprite = goldSprite != null ? goldSprite : circleSprite;
        coin.preserveAspect = true;
        coin.color = Color.white;
        coin.raycastTarget = false;

        float elapsed = 0f;
        const float duration = 1.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.anchoredPosition = Vector2.Lerp(start, end, eased);
            rect.localScale = Vector3.one * Mathf.Lerp(1.45f, 0.92f, t);
            group.alpha = Mathf.Lerp(1f, 0.18f, Mathf.Max(0f, (t - 0.82f) / 0.18f));
            yield return null;
        }

        Destroy(flyObject);

        Vector2 WorldToCanvasPoint(Vector3 worldPosition)
        {
            Vector2 screen = mainCamera.WorldToScreenPoint(worldPosition);
            return ScreenToCanvasPoint(screen);
        }

        Vector2 ScreenToCanvasPoint(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local);
            return local;
        }
    }

    private void HighlightPiece(TutorialPiece piece, Color color)
    {
        if (piece == null || piece.root == null) return;
        bool interactiveTarget =
            piece != tutorialPawn &&
            (step == TutorialStep.CaptureEnemyTarget ||
             step == TutorialStep.UseItemTarget ||
             step == TutorialStep.CaptureKingTarget);
        piece.ringRenderer = SpawnPieceOutline(piece, color, $"Highlight {piece.root.name}", true, interactiveTarget);
    }

    private void HighlightPieceAfterDelay(TutorialPiece piece, Color color, TutorialStep expectedStep)
    {
        StartCoroutine(HighlightPieceAfterDelayRoutine(piece, color, expectedStep));
    }

    private IEnumerator HighlightPieceAfterDelayRoutine(TutorialPiece piece, Color color, TutorialStep expectedStep)
    {
        yield return new WaitForSeconds(1.5f);
        if (step != expectedStep || piece == null || piece.root == null) yield break;
        HighlightPiece(piece, color);
    }

    private void HighlightTile(Vector2Int board, Color color)
    {
        CreateMovePlateHighlight($"Highlight Tile {board.x},{board.y}", BoardToWorld(board) + new Vector3(0f, 0f, 0.2f), color);
    }

    private SpriteRenderer SpawnPieceOutline(TutorialPiece piece, Color color, string name, bool temporary, bool interactive = false)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(piece.root.transform, false);
        border.transform.localPosition = new Vector3(0f, GetPieceEffectY(piece.type), 0.001f);
        border.transform.localScale = Vector3.one;

        SpriteRenderer sr = border.AddComponent<SpriteRenderer>();
        sr.sprite = piece.data != null && piece.data.BackgroundSprite != null ? piece.data.BackgroundSprite : circleSprite;
        sr.color = color;
        sr.sortingOrder = piece.baseRenderer != null ? piece.baseRenderer.sortingOrder - 1 : 3;
        if (temporary)
        {
            if (interactive)
            {
                CircleCollider2D collider = border.AddComponent<CircleCollider2D>();
                collider.radius = 0.84f;
                collider.isTrigger = true;
                TutorialPieceClick click = border.AddComponent<TutorialPieceClick>();
                click.Init(this, piece);
            }
            highlights.Add(border);
        }
        return sr;
    }

    private static float GetPieceEffectY(TutorialPieceType type)
    {
        return type switch
        {
            TutorialPieceType.Pawn => 0.383f,
            TutorialPieceType.Queen => 0.3125f,
            TutorialPieceType.King => 0.3125f,
            _ => 0f
        };
    }

    private GameObject CreateMovePlateHighlight(string name, Vector3 position, Color color)
    {
        GameObject highlight = movePlatePrefab != null ? Instantiate(movePlatePrefab, highlightRoot) : new GameObject(name);
        highlight.name = name;
        highlight.transform.SetParent(highlightRoot);
        highlight.transform.localPosition = position;
        highlight.transform.localScale = Vector3.one;
        SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
        if (sr == null) sr = highlight.AddComponent<SpriteRenderer>();
        if (sr.sprite == null) sr.sprite = circleSprite;
        sr.color = color;
        sr.sortingOrder = 2;
        highlights.Add(highlight);
        return highlight;
    }

    private void HighlightItemSlot(bool active)
    {
        itemSlotImage.color = Color.white;
        if (itemSlotBorder != null) itemSlotBorder.SetActive(active);
    }

    private void HighlightShopItemSlot(bool active)
    {
        shopItemImage.color = Color.white;
        if (shopItemBorder != null) shopItemBorder.SetActive(active);
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < highlights.Count; i++)
            if (highlights[i] != null) Destroy(highlights[i]);
        highlights.Clear();
    }

    private void ClearPieces()
    {
        foreach (var piece in pieces.Values)
            if (piece.root != null) Destroy(piece.root);
        pieces.Clear();
        selectedPiece = null;
        tutorialPawn = null;
        capturePawn = null;
        itemTarget = null;
        kingTarget = null;
        ClearHighlights();
    }

    private void PulseHighlights()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.08f;
        for (int i = 0; i < highlights.Count; i++)
        {
            if (highlights[i] != null)
            {
                float baseScale = highlights[i].name.Contains("Tile") ? 0.92f : 1.05f;
                highlights[i].transform.localScale = Vector3.one * baseScale * pulse;
            }
        }
    }

    private Vector3 BoardToWorld(Vector2Int board)
    {
        Vector3 bounds = new Vector3((BoardSize / 2f) * TileSizeX, (BoardSize / 2f) * TileSizeY, 0f) + BoardCenter;
        return new Vector3(board.x * TileSizeX, board.y * TileSizeY, 0f) - bounds + new Vector3(TileSizeX / 2f, TileSizeY / 2f, 0f);
    }

    private Vector3 GetPieceCenter(Vector2Int board)
    {
        Vector3 center = BoardToWorld(board);
        center.z = -0.01f + board.y * 0.001f;
        return center;
    }

    private void SetTutorialText(string text)
    {
        tutorialText.text = text;
    }

    private void SetButton(bool visible, string label = "")
    {
        nextButton.gameObject.SetActive(visible);
        if (!visible) return;

        TextMeshProUGUI labelText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null) labelText.text = label;
    }

    private void OnNextButton()
    {
        switch (step)
        {
            case TutorialStep.Intro:
                EnterSelectPiece();
                break;
            case TutorialStep.TurnChangeNotice:
                EnterCaptureEnemy();
                break;
            case TutorialStep.UseItemDone:
                EnterCaptureKing();
                break;
            case TutorialStep.Complete:
                SceneManager.LoadScene("GameScene");
                break;
        }
    }

    private void OnItemSlotClicked()
    {
        if (step != TutorialStep.UseItem) return;
        tooltipPanel.SetActive(false);
        ClearHighlights();
        EnterUseItemTarget();
    }

    private void OnItemSlotPressed()
    {
        OnItemSlotClicked();
    }

    private void OnShopItemClicked()
    {
        if (step != TutorialStep.BuyItem) return;

        gold = Mathf.Max(0, gold - 3);
        UpdateGold();
        HighlightShopItemSlot(false);
        shopItemSlot.gameObject.SetActive(false);
        itemSlot.gameObject.SetActive(true);
        EnterUseItem();
    }

    public void ShowTooltip(bool visible)
    {
        tooltipPanel.SetActive(visible && step == TutorialStep.UseItem && itemSlot.gameObject.activeSelf);
    }

    private void OnPieceClicked(TutorialPiece piece)
    {
        if (piece == null) return;

        if (step == TutorialStep.SelectPiece && piece == tutorialPawn)
        {
            selectedPiece = piece;
            EnterMovePiece();
            return;
        }

        if (step == TutorialStep.CaptureEnemySelect && piece == tutorialPawn)
        {
            selectedPiece = piece;
            step = TutorialStep.CaptureEnemyTarget;
            ClearHighlights();
            HighlightTile(capturePawn.board, attackPurple);
            HighlightPiece(capturePawn, attackPurple);
            return;
        }

        if (step == TutorialStep.CaptureEnemyTarget && selectedPiece == tutorialPawn && piece == capturePawn)
        {
            CapturePiece(tutorialPawn, capturePawn, 3);
            capturePawn = null;
            StartCoroutine(EnterUseItemAfterGold());
            return;
        }

        if (step == TutorialStep.UseItemTarget && piece == itemTarget)
        {
            ApplyArrest(piece);
            EnterUseItemDone();
            return;
        }

        if (step == TutorialStep.CaptureKingSelect && piece == tutorialPawn)
        {
            selectedPiece = piece;
            step = TutorialStep.CaptureKingTarget;
            ClearHighlights();
            HighlightTile(kingTarget.board, attackPurple);
            HighlightPiece(kingTarget, attackPurple);
            return;
        }

        if (step == TutorialStep.CaptureKingTarget && selectedPiece == tutorialPawn && piece == kingTarget)
        {
            CapturePiece(tutorialPawn, kingTarget);
            kingTarget = null;
            StartCoroutine(CompleteAfterWinEffect());
        }
    }

    private bool TryHandleInstantPieceClick(TutorialPiece piece)
    {
        if (piece == null) return false;

        if (step == TutorialStep.UseItemTarget)
        {
            if (piece == itemTarget)
            {
                ApplyArrest(piece);
                EnterUseItemDone();
            }

            return true;
        }

        return false;
    }

    private bool BeginPieceDrag(TutorialPiece piece)
    {
        if (piece == null) return false;

        if (step == TutorialStep.SelectPiece && piece == tutorialPawn)
        {
            selectedPiece = piece;
            EnterMovePiece();
        }
        else if (step == TutorialStep.CaptureEnemySelect && piece == tutorialPawn)
        {
            selectedPiece = piece;
            step = TutorialStep.CaptureEnemyTarget;
            ClearHighlights();
            HighlightTile(capturePawn.board, attackPurple);
            HighlightPiece(capturePawn, attackPurple);
        }
        else if (step == TutorialStep.CaptureKingSelect && piece == tutorialPawn)
        {
            selectedPiece = piece;
            step = TutorialStep.CaptureKingTarget;
            ClearHighlights();
            HighlightTile(kingTarget.board, attackPurple);
            HighlightPiece(kingTarget, attackPurple);
        }

        bool canDrag =
            (step == TutorialStep.MovePiece && selectedPiece == tutorialPawn && piece == tutorialPawn) ||
            (step == TutorialStep.CaptureEnemyTarget && selectedPiece == tutorialPawn && piece == tutorialPawn) ||
            (step == TutorialStep.CaptureKingTarget && selectedPiece == tutorialPawn && piece == tutorialPawn);

        if (!canDrag) return false;

        draggingPiece = piece;
        dragStartLocalPosition = piece.root.transform.localPosition;
        SetPieceSortingOrder(piece, 30);
        SetAnimatorBool(piece.animator, "isDragging", true);
        return true;
    }

    private void DragPieceToMouse(TutorialPiece piece)
    {
        if (draggingPiece == null || piece != draggingPiece) return;
        if (piece == null || mainCamera == null) return;

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(piece.root.transform.position);
        screenPoint.x = Input.mousePosition.x;
        screenPoint.y = Input.mousePosition.y;
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPoint);
        Vector3 local = WorldToBoardLocal(world);
        local.z = piece.root.transform.localPosition.z;
        piece.root.transform.localPosition = local;
    }

    private void EndPieceDrag(TutorialPiece piece)
    {
        if (draggingPiece == null || piece != draggingPiece) return;

        Vector2Int dropBoard = GetNearestBoard(piece.root.transform.localPosition);
        bool accepted = TryCompleteDrag(piece, dropBoard);
        if (!accepted)
        {
            piece.root.transform.localPosition = dragStartLocalPosition;
        }

        SetPieceSortingOrder(piece, piece.defaultSortingOrder);
        SetAnimatorBool(piece.animator, "isDragging", false);
        draggingPiece = null;
    }

    private void SetPieceSortingOrder(TutorialPiece piece, int sortingOrder)
    {
        if (piece == null || piece.baseRenderer == null) return;
        piece.baseRenderer.sortingOrder = sortingOrder;
        if (piece.ringRenderer != null) piece.ringRenderer.sortingOrder = sortingOrder - 1;

        SpriteRenderer[] children = piece.root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == piece.baseRenderer) continue;
            if (piece.ringRenderer != null && children[i] == piece.ringRenderer) continue;
            children[i].sortingOrder = Mathf.Max(children[i].sortingOrder, sortingOrder + 1);
        }
    }

    private static void SetAnimatorTrigger(Animator animator, string parameterName)
    {
        if (animator == null || !HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Trigger)) return;
        animator.ResetTrigger(parameterName);
        animator.SetTrigger(parameterName);
    }

    private static void SetAnimatorBool(Animator animator, string parameterName, bool value)
    {
        if (animator == null || !HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Bool)) return;
        animator.SetBool(parameterName, value);
    }

    private static void SetAnimatorInteger(Animator animator, string parameterName, int value)
    {
        if (animator == null || !HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Int)) return;
        animator.SetInteger(parameterName, value);
    }

    private static bool HasAnimatorParameter(Animator animator, string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].name == parameterName && parameters[i].type == type)
                return true;

        return false;
    }

    private bool TryCompleteDrag(TutorialPiece piece, Vector2Int dropBoard)
    {
        if (step == TutorialStep.MovePiece && piece == tutorialPawn && dropBoard == moveTarget)
        {
            MovePiece(tutorialPawn, moveTarget);
            EnterTurnChangeNotice();
            return true;
        }

        if (step == TutorialStep.CaptureEnemyTarget && piece == tutorialPawn && capturePawn != null && dropBoard == capturePawn.board)
        {
            CapturePiece(tutorialPawn, capturePawn, 3);
            capturePawn = null;
            StartCoroutine(EnterUseItemAfterGold());
            return true;
        }

        if (step == TutorialStep.CaptureKingTarget && piece == tutorialPawn && kingTarget != null && dropBoard == kingTarget.board)
        {
            CapturePiece(tutorialPawn, kingTarget);
            kingTarget = null;
            StartCoroutine(CompleteAfterWinEffect());
            return true;
        }

        return false;
    }

    private Vector3 WorldToBoardLocal(Vector3 world)
    {
        Vector3 rootPosition = pieceRoot != null ? pieceRoot.position : Vector3.zero;
        Vector3 rootScale = pieceRoot != null ? pieceRoot.localScale : Vector3.one;
        float scaleX = Mathf.Approximately(rootScale.x, 0f) ? 1f : rootScale.x;
        float scaleY = Mathf.Approximately(rootScale.y, 0f) ? 1f : rootScale.y;
        return new Vector3((world.x - rootPosition.x) / scaleX, (world.y - rootPosition.y) / scaleY, world.z);
    }

    private Vector2Int GetNearestBoard(Vector3 localPosition)
    {
        Vector2Int nearest = Vector2Int.zero;
        float nearestDistance = float.MaxValue;
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int board = new Vector2Int(x, y);
                float distance = ((Vector2)BoardToWorld(board) - (Vector2)localPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = board;
                }
            }
        }

        return nearest;
    }

    private IEnumerator CompleteAfterWinEffect()
    {
        turnText.text = "Player 1 Wins";
        yield return new WaitForSeconds(0.8f);
        EnterComplete();
    }

    private void OnTileClicked(Vector2Int board)
    {
        // Movement in the tutorial uses the same drag-and-drop gesture as the main game.
    }

    private sealed class TutorialPieceClick : MonoBehaviour
    {
        private tutorial controller;
        private TutorialPiece piece;

        public void Init(tutorial owner, TutorialPiece targetPiece)
        {
            controller = owner;
            piece = targetPiece;
        }

        private void OnMouseDown()
        {
            if (controller.TryHandleInstantPieceClick(piece)) return;

            if (controller.BeginPieceDrag(piece))
                controller.DragPieceToMouse(piece);
            else
                controller.OnPieceClicked(piece);
        }

        private void OnMouseDrag()
        {
            controller.DragPieceToMouse(piece);
        }

        private void OnMouseUp()
        {
            controller.EndPieceDrag(piece);
        }
    }

    private sealed class TutorialTile : MonoBehaviour
    {
        private tutorial controller;
        private Vector2Int board;

        public void Init(tutorial owner, Vector2Int boardPosition)
        {
            controller = owner;
            board = boardPosition;
        }

        private void OnMouseDown()
        {
            controller.OnTileClicked(board);
        }
    }

    private sealed class TutorialTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private tutorial controller;

        public void Init(tutorial owner)
        {
            controller = owner;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller.ShowTooltip(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            controller.ShowTooltip(false);
        }
    }

    private sealed class TutorialItemClick : MonoBehaviour, IPointerDownHandler
    {
        private tutorial controller;

        public void Init(tutorial owner)
        {
            controller = owner;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            controller.OnItemSlotPressed();
        }
    }
}

