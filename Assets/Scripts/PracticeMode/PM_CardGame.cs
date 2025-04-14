using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PM_CardGame : MonoBehaviour
{
    public List<int> playerCards = new List<int> { 1, 2, 3, 4, 5, 7 }; // 각 카드의 점수
    public List<int> aiCards = new List<int> { 1, 2, 3, 4, 5, 7 }; // 각 카드의 점수
    public List<Button> aiCardButtons; // AI 카드 버튼 리스트

    public Text playerScoreText;
    public Text aiScoreText;
    public Text playerTargetScoreText;
    public Text aiTargetScoreText;
    public Text timerText;
    public GameObject eatStealPanel;
    public GameObject resultPanel;
    public Text resultText;
    public List<Button> playerCardButtons;
    public Text playerChosenCardText; // 플레이어가 고른 카드를 표시할 텍스트
    public Text aiChosenCardText; // AI가 고른 카드를 표시할 텍스트

    private int playerScore = 0;
    private int aiScore = 0;
    private int playerTargetScore = 16;
    private int aiTargetScore = 16;
    private int currentTurn = 0; // 0 for player, 1 for AI
    private float timer = 10f;
    private bool isChoosing = false;
    private int playerChosenCard = -1;
    private int aiChosenCard = -1;

    void Start()
    {
        StartNewRound();
    }

    void StartNewRound()
    {
        // 초기화 작업
        playerCards = new List<int> { 1, 2, 3, 4, 5, 7 };
        aiCards = new List<int> { 1, 2, 3, 4, 5, 7 };

        playerScoreText.text = "Player Score: " + playerScore;
        aiScoreText.text = "AI Score: " + aiScore;
        playerTargetScoreText.text = "Target Score: " + playerTargetScore;
        aiTargetScoreText.text = "Target Score: " + aiTargetScore;

        foreach (Button button in aiCardButtons)
        {
            int cardValue = int.Parse(button.GetComponentInChildren<Text>().text);
            button.interactable = aiCards.Contains(cardValue);
        }

        foreach (Button button in playerCardButtons)
        {
            int cardValue = int.Parse(button.GetComponentInChildren<Text>().text);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnPlayerCardSelected(cardValue));
            button.interactable = playerCards.Contains(cardValue);
        }

        eatStealPanel.SetActive(false);
        resultPanel.SetActive(false);

        currentTurn = 0; // 플레이어가 먼저 시작
        StartPlayerTurn();
    }

    void Update()
    {
        if (isChoosing && eatStealPanel.activeSelf)
        {
            timer -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(timer).ToString();

            if (timer <= 0)
            {
                OnEatSelected();
            }
        }
    }

    void StartPlayerTurn()
    {
        currentTurn = 0;
        EnableCardButtons(true);
        playerChosenCardText.text = ""; // 초기화
        aiChosenCardText.text = ""; // 초기화
    }

    void StartAITurn()
    {
        currentTurn = 1;
        EnableCardButtons(false); // AI 턴에서는 플레이어 버튼 비활성화
        StartCoroutine(AIDelayedCardSelection());
    }

    void OnPlayerCardSelected(int card)
    {
        if (currentTurn == 0)
        {
            playerChosenCard = card;
            playerCards.Remove(card);
            DisableCardButton(playerCardButtons, card);
            DisplayChosenCard(card, "Player");
            EnableCardButtons(false); // 플레이어가 카드를 선택한 후 버튼을 비활성화합니다.
            StartAITurn();
        }
    }

    int ChooseAICard()
    {
        int chosenCard;
        do
        {
            chosenCard = aiCards[Random.Range(0, aiCards.Count)];
        } while (chosenCard == playerChosenCard);

        return chosenCard;
    }

    IEnumerator AIDelayedCardSelection()
    {
        yield return new WaitForSeconds(3f); // 3초 지연 후 AI가 카드를 선택합니다.
        int chosenCard = ChooseAICard();
        aiChosenCard = chosenCard;
        aiCards.Remove(chosenCard);
        DisableCardButton(aiCardButtons, chosenCard);
        DisplayChosenCard(chosenCard, "AI");
        ProceedToEatSteal();
    }

    void ProceedToEatSteal()
    {
        eatStealPanel.SetActive(true); // 필요할 때 Eat/Steal 패널 활성화
        isChoosing = true; // 선택 상태 활성화
        timer = 10f; // 타이머 초기화
    }

    public void OnEatSelected()
    {
        if (!isChoosing) return;
        isChoosing = false;
        ResolveTurn(true);
    }

    public void OnStealSelected()
    {
        if (!isChoosing) return;
        isChoosing = false;
        ResolveTurn(false);
    }

    void ResolveTurn(bool playerEat)
    {
        bool aiEat = (Random.value > 0.5f);
        string playerChoice = playerEat ? "Eat" : "Steal";
        string aiChoice = aiEat ? "Eat" : "Steal";

        resultText.text = "Player chose: " + playerChoice + "\nAI chose: " + aiChoice + "\n\n";
        if (playerEat && aiEat)
        {
            resultText.text += "Player gained: " + (playerChosenCard + 1) + "\nAI gained: " + (aiChosenCard + 1) + "\n";
        }
        else if (playerEat && !aiEat)
        {
            resultText.text += "Player gained: 0\nAI gained: " + Mathf.Abs(playerChosenCard - aiChosenCard) + "\n";
        }
        else if (!playerEat && aiEat)
        {
            resultText.text += "Player gained: " + Mathf.Abs(playerChosenCard - aiChosenCard) + "\nAI gained: 0\n";
        }

        // 점수 계산 로직
        if (playerEat && aiEat)
        {
            playerScore += playerChosenCard + 1;
            aiScore += aiChosenCard + 1;
        }
        else if (playerEat && !aiEat)
        {
            aiScore += Mathf.Abs(playerChosenCard - aiChosenCard);
        }
        else if (!playerEat && aiEat)
        {
            playerScore += Mathf.Abs(playerChosenCard - aiChosenCard);
        }

        StartCoroutine(ShowResultAndNextTurn());
    }

    int GetNextTargetScore(int currentTargetScore)
    {
        if (currentTargetScore == 16)
        {
            return 20;
        }
        else if (currentTargetScore == 20)
        {
            return 45;
        }
        return currentTargetScore;
    }

    IEnumerator ShowResultAndNextTurn()
    {
        if (playerCards.Count == 1 && aiCards.Count == 1 && playerCards[0] == aiCards[0])
        {
            EndGame();
            yield break;
        }
        yield return new WaitForSeconds(2); // 결과를 표시할 시간 대기
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling(); // 결과 패널을 화면 가장 위로 표시합니다.
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0)); // 사용자가 클릭할 때까지 대기
        resultPanel.SetActive(false);
        eatStealPanel.SetActive(false); // 결과 패널이 사라질 때 Eat/Steal 패널 비활성화
        UpdateUI();

        if (playerCards.Count == 0 || aiCards.Count == 0)
        {
            EndGame();
        }
        else
        {
            currentTurn = (currentTurn == 0) ? 1 : 0;
            if (currentTurn == 0)
            {
                StartPlayerTurn();
            }
            else
            {
                StartAITurn();
            }
        }
    }

    void EndGame()
    {
        resultPanel.SetActive(true);
        if (playerScore >= playerTargetScore && aiScore >= aiTargetScore)
        {
            resultText.text = "Both reached target score!\nPlayer Score: " + playerScore + "\nAI Score: " + aiScore;
        }
        else if (playerScore >= playerTargetScore)
        {
            resultText.text = "Player reached target score!\nPlayer Score: " + playerScore + "\nAI Score: " + aiScore;
        }
        else if (aiScore >= aiTargetScore)
        {
            resultText.text = "AI reached target score!\nPlayer Score: " + playerScore + "\nAI Score: " + aiScore;
        }
        else
        {
            resultText.text = "Player Score: " + playerScore + "\nAI Score: " + aiScore;
        }

        // 목표 점수를 초과 달성하더라도, 기존 라운드 종료 규칙을 따름
        if (playerCards.Count == 0 || aiCards.Count == 0 || (playerCards.Count == 1 && aiCards.Count == 1 && playerCards[0] == aiCards[0]))
        {
            StartCoroutine(WaitAndStartNewRound());
        }
    }

    IEnumerator WaitAndStartNewRound()
    {
        yield return new WaitForSeconds(5f); // 5초 대기 후 새로운 라운드 시작

        // 목표 점수 갱신은 라운드 종료 후에 수행
        if (playerScore >= playerTargetScore)
        {
            playerScore = 0;
            playerTargetScore = GetNextTargetScore(playerTargetScore);
        }
        if (aiScore >= aiTargetScore)
        {
            aiScore = 0;
            aiTargetScore = GetNextTargetScore(aiTargetScore);
        }

        StartNewRound();
    }

    void UpdateUI()
    {
        foreach (Button button in aiCardButtons)
        {
            int cardValue = int.Parse(button.GetComponentInChildren<Text>().text);
            button.interactable = aiCards.Contains(cardValue);
        }
        playerScoreText.text = "Player Score: " + playerScore;
        aiScoreText.text = "AI Score: " + aiScore;
        playerTargetScoreText.text = "Target Score: " + playerTargetScore;
        aiTargetScoreText.text = "Target Score: " + aiTargetScore;
    }

    void DisplayChosenCard(int card, string who)
    {
        if (who == "Player")
        {
            playerChosenCardText.gameObject.SetActive(true);
            playerChosenCardText.text = "Player chose card: " + card;
        }
        else if (who == "AI")
        {
            aiChosenCardText.gameObject.SetActive(true);
            aiChosenCardText.text = "AI chose card: " + card;
        }
    }

    void EnableCardButtons(bool enable)
    {
        foreach (Button button in playerCardButtons)
        {
            button.interactable = enable && playerCards.Contains(int.Parse(button.GetComponentInChildren<Text>().text));
        }
    }

    void DisableCardButton(List<Button> cardButtons, int card)
    {
        foreach (Button button in cardButtons)
        {
            if (int.Parse(button.GetComponentInChildren<Text>().text) == card)
            {
                button.interactable = false;
            }
        }
    }
}
