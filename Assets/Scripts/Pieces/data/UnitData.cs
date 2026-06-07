using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Unit/UnitData")]
public class UnitData : ScriptableObject
{
    [SerializeField] private UnitType mType;
    [SerializeField] private Sprite mWhiteSprite;
    [SerializeField] private Sprite mBlackSprite;
    [SerializeField] private Sprite mBackgroundSprite;
    [SerializeField] private int mMaxHp;
    [SerializeField] private int mSpeed;
    [SerializeField] private int mPower;
    [SerializeField] private MoveType mMoveType;
    [SerializeField] private List<Vector2Int> mMoveOffsets;
    [SerializeField] private List<Vector2Int> mAttackOffsets;
    [SerializeField] private int mCaptureGold;

    public UnitType Type { get { return mType; } }
    public Sprite WhiteSprite { get { return mWhiteSprite; } }
    public Sprite BlackSprite { get { return mBlackSprite; } }
    public Sprite BackgroundSprite { get { return mBackgroundSprite; } }
    public int MaxHp { get { return mMaxHp; } }
    public int Speed { get { return mSpeed; } }
    public int Power { get { return mPower; } }
    public MoveType MoveType { get { return mMoveType; } }
    public List<Vector2Int> MoveOffsets { get { return mMoveOffsets; } }
    public List<Vector2Int> AttackOffsets { get { return mAttackOffsets; } }
    public int CaptureGold { get { return mCaptureGold; } }
}
public enum UnitType
{
    None = 0,
    Pawn = 1,
    Knight = 2,
    Bishop = 3,
    Rook = 4,
    Queen = 5,
    King = 6
}
public enum MoveType
{
    Slide,
    Step,
    Pawn
}
