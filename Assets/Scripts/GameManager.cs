using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private BoardBehaviour boardPrefab;
    [SerializeField] private QuestSO quest;

    private BoardBehaviour _board;
    private QuestTracker _tracker;
    private QuestUI _questUI;
    private void Awake()
    {
        _board = Instantiate(boardPrefab);
        _board.Setup(settings);
        _tracker = new QuestTracker(quest.quest);
        _questUI = Instantiate(settings.questUIPrefab);
        _questUI.Setup(quest.quest);
    }

    private void OnEnable()
    {
        _tracker.StartTracking();
    }

    private void OnDisable()
    {
        _tracker.StopTracking();
    }
}
