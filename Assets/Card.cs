using UnityEngine;

public class Card : MonoBehaviour
{
    public enum Type
    {
        Warrior,
        Archer,
        Rider
    }

    public int x;
    public int y;
    public int player;
    public Type type;
}
