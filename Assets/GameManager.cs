using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

using Random = UnityEngine.Random;
using CardPair = System.Tuple<Card.Type, Card.Type>;
using ObjectPair = System.Tuple<UnityEngine.GameObject, UnityEngine.GameObject>;

[System.Serializable]
public struct CardSet
{
    public Sprite back;
    public Sprite[] cards;
}

public class GameManager : MonoBehaviour
{
    public GameObject columnPrefab;
    public GameObject cardPrefab;
    public Sprite[] icons;
    public CardSet[] factions = new CardSet[2];

    private class SelectedCard
    {
        public bool opened;
        public int player;
        public Card.Type type;
        public GameObject gameObject;
    }

    private enum Result
    {
        Tie,
        Win,
        Fail
    }

    private static Dictionary<CardPair, Result> resultMap;
    private bool playerTurn = true;
    private SelectedCard[] selectedCards;
    private readonly List<GameObject> playerCards = new List<GameObject>();
    private readonly List<GameObject> enemyCards = new List<GameObject>();
    private readonly List<GameObject> knownPlayerCards = new List<GameObject>();
    private readonly List<GameObject> knownEnemyCards = new List<GameObject>();

    static GameManager()
    {
        resultMap = new Dictionary<CardPair, Result>();
        resultMap.Add(new CardPair(Card.Type.Warrior, Card.Type.Warrior), Result.Tie);
        resultMap.Add(new CardPair(Card.Type.Warrior, Card.Type.Archer), Result.Fail);
        resultMap.Add(new CardPair(Card.Type.Warrior, Card.Type.Rider), Result.Win);
        resultMap.Add(new CardPair(Card.Type.Archer, Card.Type.Warrior), Result.Win);
        resultMap.Add(new CardPair(Card.Type.Archer, Card.Type.Archer), Result.Tie);
        resultMap.Add(new CardPair(Card.Type.Archer, Card.Type.Rider), Result.Fail);
        resultMap.Add(new CardPair(Card.Type.Rider, Card.Type.Warrior), Result.Fail);
        resultMap.Add(new CardPair(Card.Type.Rider, Card.Type.Archer), Result.Win);
        resultMap.Add(new CardPair(Card.Type.Rider, Card.Type.Rider), Result.Tie);
    }

    private void Awake()
    {
        selectedCards = new SelectedCard[2]
        {
            new SelectedCard(),
            new SelectedCard()
        };

        DOTween.Init();
    }

    private void Start()
    {
        PopulateCards(0, 6, 6);
        PopulateCards(1, 6, 6);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void PopulateCards(int playerId, int width, int height)
    {
        Card.Type[] types = new Card.Type[36];

        for (int i = 0; i < 12; ++i)
            types[i] = Card.Type.Warrior;

        for (int i = 12; i < 24; ++i)
            types[i] = Card.Type.Archer;

        for (int i = 24; i < 36; ++i)
            types[i] = Card.Type.Rider;

        for (int i = types.Length - 1; i > 0; i--)
        {
            int index = Random.Range(0, i + 1);
            Card.Type temp = types[i];
            types[i] = types[index];
            types[index] = temp;
        }

        string playerTag;
        List<GameObject> cards;

        if (playerId == 1)
        {
            playerTag = "Enemy Panel";
            cards = enemyCards;
        }
        else
        {
            playerTag = "Player Panel";
            cards = playerCards;
        }

        GameObject cardPanel = GameObject.FindGameObjectWithTag(playerTag);

        for (int y = 0; y < height; ++y)
        {
            GameObject columnObject = Instantiate(columnPrefab, cardPanel.transform);

            for (int x = 0; x < width; ++x)
            {
                GameObject cellObject = Instantiate(cardPrefab, columnObject.transform);
                GameObject cardObject = cellObject.transform.GetChild(0).gameObject;

                Image image = cardObject.GetComponent<Image>();
                image.sprite = factions[playerId].back;

                Button button = cardObject.GetComponent<Button>();
                button.onClick.AddListener(() => OnClicked(cardObject));

                Card card = cardObject.GetComponent<Card>();
                card.x = x;
                card.y = y;
                card.player = playerId;
                card.type = types[y * 6 + x];

                cards.Add(cardObject);
            }
        }
    }

    private void OnClicked(GameObject gameObject)
    {
        if (!playerTurn)
            return;

        Card card = gameObject.GetComponent<Card>();
        SelectedCard selectedCard = selectedCards[card.player];
        SelectedCard card1 = selectedCards[0];
        SelectedCard card2 = selectedCards[1];

        if (selectedCard.opened)
            return;

        selectedCard.opened = true;
        selectedCard.player = card.player;
        selectedCard.type = card.type;
        selectedCard.gameObject = gameObject;

        if (card1.opened && card2.opened)
        {
            playerTurn = false;
            card1.opened = false;
            card2.opened = false;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(OpenCard(gameObject));

            CardPair pair = new CardPair(card1.type, card2.type);
            Result result = resultMap[pair];

            switch (result)
            {
                case Result.Tie:
                    sequence.Insert(0.4f, CloseCard(card1.gameObject));
                    sequence.Insert(0.4f, CloseCard(card2.gameObject));
                    break;
                case Result.Win:
                    sequence.Insert(0.4f, CloseCard(card1.gameObject));
                    sequence.Insert(0.4f, RemoveCard(card2.gameObject));
                    break;
                case Result.Fail:
                    sequence.Insert(0.4f, RemoveCard(card1.gameObject));
                    sequence.Insert(0.4f, CloseCard(card2.gameObject));
                    break;
            }

            sequence.Append(EnemyTurn());
        }
        else
        {
            OpenCard(gameObject);
        }
    }

    private Tween OpenCard(GameObject gameObject)
    {
        RectTransform buttonTransform = gameObject.GetComponent<RectTransform>();
        Image image = gameObject.GetComponent<Image>();
        Card card = gameObject.GetComponent<Card>();

        if (card.player == 0 && !knownPlayerCards.Contains(gameObject))
        {
            knownPlayerCards.Add(gameObject);
            playerCards.Remove(gameObject);
        }
        else if (card.player == 1 && !knownEnemyCards.Contains(gameObject))
        {
            knownEnemyCards.Add(gameObject);
            enemyCards.Remove(gameObject);
        }

        Transform iconObject = gameObject.transform.GetChild(0);
        Image icon = iconObject.GetComponent<Image>();

        Sequence sequence = DOTween.Sequence();
        sequence.Append(gameObject.transform.DORotate(new Vector3(0, 90, 0), 0.2f).SetEase(Ease.OutQuint));
        sequence.AppendCallback(() =>
        {
            image.sprite = factions[card.player].cards[(int)card.type];
            icon.sprite = icons[(int)card.type];
            icon.enabled = true;
        });
        sequence.Append(gameObject.transform.DORotate(new Vector3(0, 180, 0), 0.2f).SetEase(Ease.OutQuint));
        return sequence;
    }

    private Tween CloseCard(GameObject gameObject)
    {
        RectTransform buttonTransform = gameObject.GetComponent<RectTransform>();
        Image image = gameObject.GetComponent<Image>();
        Card card = gameObject.GetComponent<Card>();

        Transform iconObject = gameObject.transform.GetChild(0);
        Image icon = iconObject.GetComponent<Image>();

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(1.0f);
        sequence.Append(gameObject.transform.DORotate(new Vector3(0, 90, 0), 0.2f).SetEase(Ease.OutQuint));
        sequence.AppendCallback(() =>
        {
            icon.enabled = false;
            image.sprite = factions[card.player].back;
        });
        sequence.Append(gameObject.transform.DORotate(new Vector3(0, 0, 0), 0.2f).SetEase(Ease.OutQuint));
        return sequence;
    }

    private Tween RemoveCard(GameObject gameObject)
    {
        RectTransform buttonTransform = gameObject.GetComponent<RectTransform>();
        Image image = gameObject.GetComponent<Image>();

        playerCards.Remove(gameObject);
        enemyCards.Remove(gameObject);
        knownPlayerCards.Remove(gameObject);
        knownEnemyCards.Remove(gameObject);

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(1.0f);
        sequence.Append(gameObject.transform.DOMove(new Vector3(-100, -100, 0), 0.6f).SetEase(Ease.OutQuint));
        sequence.AppendCallback(() =>
        {
            Destroy(gameObject);
        });
        return sequence;
    }

    private Tween EnemyTurn()
    {
        Sequence sequence = DOTween.Sequence();

        GameObject cardPanel = GameObject.FindGameObjectWithTag("Info Panel");
        Text infoText = cardPanel.GetComponent<Text>();

        sequence.AppendCallback(() => infoText.text = "Enemy turn");
        sequence.AppendInterval(1.0f);

        GameObject playerObject = null;
        GameObject enemyObject = null;

        if (knownPlayerCards.Count > 0 && knownEnemyCards.Count > 0)
        {
            ObjectPair objects = FindPair();

            if (objects == null)
            {
                enemyObject = knownEnemyCards[0];
                int playerIndex = Random.Range(0, playerCards.Count);
                playerObject = playerCards[playerIndex];
            }
            else
            {
                playerObject = objects.Item1;
                enemyObject = objects.Item2;
            }
        }
        else if (knownPlayerCards.Count > 0)
        {
            playerObject = knownPlayerCards[0];
            int enemyIndex = Random.Range(0, enemyCards.Count);
            enemyObject = enemyCards[enemyIndex];
        }
        else if (knownEnemyCards.Count > 0)
        {
            enemyObject = knownEnemyCards[0];
            int playerIndex = Random.Range(0, playerCards.Count);
            playerObject = playerCards[playerIndex];
        }
        else
        {
            int playerIndex = Random.Range(0, playerCards.Count);
            int enemyIndex = Random.Range(0, enemyCards.Count);
            playerObject = playerCards[playerIndex];
            enemyObject = enemyCards[enemyIndex];
        }

        Card playerCard = playerObject.GetComponent<Card>();
        Card enemyCard = enemyObject.GetComponent<Card>();

        sequence.Append(OpenCard(enemyObject));
        sequence.Append(OpenCard(playerObject));

        CardPair pair = new CardPair(playerCard.type, enemyCard.type);
        Result result = resultMap[pair];

        switch (result)
        {
            case Result.Tie:
                sequence.Insert(1.8f, CloseCard(playerCard.gameObject));
                sequence.Insert(1.8f, CloseCard(enemyCard.gameObject));
                break;
            case Result.Win:
                sequence.Insert(1.8f, CloseCard(playerCard.gameObject));
                sequence.Insert(1.8f, RemoveCard(enemyCard.gameObject));
                break;
            case Result.Fail:
                sequence.Insert(1.8f, RemoveCard(playerCard.gameObject));
                sequence.Insert(1.8f, CloseCard(enemyCard.gameObject));
                break;
        }

        sequence.AppendCallback(() => 
        {
            infoText.text = "Player turn";
            playerTurn = true;
        });

        return sequence;
    }

    private ObjectPair FindPair()
    {
        foreach (GameObject enemyObject in knownEnemyCards)
        {
            foreach (GameObject playerObject in knownPlayerCards)
            {
                Card playerCard = playerObject.GetComponent<Card>();
                Card enemyCard = enemyObject.GetComponent<Card>();

                CardPair pair = new CardPair(playerCard.type, enemyCard.type);
                Result result = resultMap[pair];

                if (result == Result.Fail)
                    return new ObjectPair(playerObject, enemyObject);
            }
        }

        return null;
    }
}
